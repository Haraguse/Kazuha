using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

/// <summary>
/// A binding expression which accepts and produces (possibly boxed) object values.
/// </summary>
/// <remarks>
/// A <see cref="T:Avalonia.Data.Core.BindingExpression" /> represents a untyped binding which has been
/// instantiated on an object.
/// </remarks>
internal class BindingExpression : UntypedBindingExpressionBase, IDescription, IDisposable
{
	/// <summary>
	/// Uncommonly used fields are separated out to reduce memory usage.
	/// </summary>
	private class UncommonFields
	{
		public TimeSpan _delay;

		public DispatcherTimer? _delayTimer;

		public IValueConverter? _converter;

		public object? _converterParameter;

		public CultureInfo? _converterCulture;

		public object? _fallbackValue;

		public string? _stringFormat;

		public object? _targetNullValue;

		public UpdateSourceTrigger _updateSourceTrigger;
	}

	private static readonly List<ExpressionNode> s_emptyExpressionNodes = new List<ExpressionNode>();

	private readonly WeakReference<object?>? _source;

	private readonly BindingMode _mode;

	private readonly List<ExpressionNode> _nodes;

	private readonly TargetTypeConverter? _targetTypeConverter;

	private readonly UncommonFields? _uncommon;

	private int _updateTargetDepth;

	private bool _shouldUpdateOneTimeBindingTarget;

	public override string Description
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			LeafNode?.BuildString(stringBuilder, _nodes);
			return stringBuilder.ToString();
		}
	}

	public Type? SourceType => (LeafNode as ISettableNode)?.ValueType;

	public TimeSpan Delay => _uncommon?._delay ?? default(TimeSpan);

	public IValueConverter? Converter => _uncommon?._converter;

	public CultureInfo ConverterCulture => _uncommon?._converterCulture ?? CultureInfo.CurrentCulture;

	public object? ConverterParameter => _uncommon?._converterParameter;

	public object? FallbackValue
	{
		get
		{
			if (_uncommon == null)
			{
				return AvaloniaProperty.UnsetValue;
			}
			return _uncommon._fallbackValue;
		}
	}

	public ExpressionNode? LeafNode
	{
		get
		{
			if (_nodes.Count <= 0)
			{
				return null;
			}
			return _nodes[_nodes.Count - 1];
		}
	}

	public string? StringFormat => _uncommon?._stringFormat;

	public object? TargetNullValue => _uncommon?._targetNullValue ?? AvaloniaProperty.UnsetValue;

	public UpdateSourceTrigger UpdateSourceTrigger => _uncommon?._updateSourceTrigger ?? UpdateSourceTrigger.PropertyChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Core.BindingExpression" /> class.
	/// </summary>
	/// <param name="source">The source from which the value will be read.</param>
	/// <param name="nodes">The nodes representing the binding path.</param>
	/// <param name="fallbackValue">
	/// The fallback value. Pass <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> for no fallback.
	/// </param>
	/// <param name="delay">The amount of time to wait before updating the binding source after the value on the target changes.</param>
	/// <param name="converter">The converter to use.</param>
	/// <param name="converterCulture">The converter culture to use.</param>
	/// <param name="converterParameter">The converter parameter.</param>
	/// <param name="enableDataValidation">
	/// Whether data validation should be enabled for the binding.
	/// </param>
	/// <param name="mode">The binding mode.</param>
	/// <param name="priority">The binding priority.</param>
	/// <param name="stringFormat">The format string to use.</param>
	/// <param name="targetProperty">The target property being bound to.</param>
	/// <param name="targetNullValue">The null target value.</param>
	/// <param name="targetTypeConverter">
	/// A final type converter to be run on the produced value.
	/// </param>
	/// <param name="updateSourceTrigger">The trigger for updating the source value.</param>
	public BindingExpression(object? source, List<ExpressionNode>? nodes, object? fallbackValue, TimeSpan delay = default(TimeSpan), IValueConverter? converter = null, CultureInfo? converterCulture = null, object? converterParameter = null, bool enableDataValidation = false, BindingMode mode = BindingMode.OneWay, BindingPriority priority = BindingPriority.LocalValue, string? stringFormat = null, object? targetNullValue = null, AvaloniaProperty? targetProperty = null, TargetTypeConverter? targetTypeConverter = null, UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
		: base(priority, targetProperty, enableDataValidation)
	{
		if (mode == BindingMode.Default)
		{
			throw new ArgumentException("Binding mode cannot be Default.", "mode");
		}
		if (updateSourceTrigger == UpdateSourceTrigger.Default)
		{
			throw new ArgumentException("UpdateSourceTrigger cannot be Default.", "updateSourceTrigger");
		}
		if (source == AvaloniaProperty.UnsetValue)
		{
			source = null;
		}
		_source = new WeakReference<object>(source);
		_mode = mode;
		_nodes = nodes ?? s_emptyExpressionNodes;
		_targetTypeConverter = targetTypeConverter;
		_shouldUpdateOneTimeBindingTarget = _mode == BindingMode.OneTime;
		UncommonFields uncommonFields;
		string stringFormat2;
		if (delay != default(TimeSpan) || converter != null || converterCulture != null || converterParameter != null || fallbackValue != AvaloniaProperty.UnsetValue || !string.IsNullOrWhiteSpace(stringFormat) || (targetNullValue != null && targetNullValue != AvaloniaProperty.UnsetValue) || updateSourceTrigger != UpdateSourceTrigger.PropertyChanged)
		{
			uncommonFields = new UncommonFields
			{
				_delay = delay,
				_converter = converter,
				_converterCulture = converterCulture,
				_converterParameter = converterParameter,
				_fallbackValue = fallbackValue
			};
			if (stringFormat == null)
			{
				goto IL_0123;
			}
			if (string.IsNullOrWhiteSpace(stringFormat))
			{
				stringFormat2 = null;
			}
			else
			{
				if (stringFormat.Contains('{'))
				{
					goto IL_0123;
				}
				stringFormat2 = "{0:" + stringFormat + "}";
			}
			goto IL_0126;
		}
		goto IL_014f;
		IL_0126:
		uncommonFields._stringFormat = stringFormat2;
		uncommonFields._targetNullValue = targetNullValue ?? AvaloniaProperty.UnsetValue;
		uncommonFields._updateSourceTrigger = updateSourceTrigger;
		_uncommon = uncommonFields;
		goto IL_014f;
		IL_014f:
		IPropertyAccessorNode propertyAccessorNode = null;
		if (nodes != null)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				ExpressionNode expressionNode = nodes[i];
				expressionNode.SetOwner(this, i);
				if (expressionNode is IPropertyAccessorNode propertyAccessorNode2)
				{
					propertyAccessorNode = propertyAccessorNode2;
				}
			}
		}
		if (enableDataValidation)
		{
			propertyAccessorNode?.EnableDataValidation();
		}
		return;
		IL_0123:
		stringFormat2 = stringFormat;
		goto IL_0126;
	}

	public override void UpdateSource()
	{
		BindingMode mode = _mode;
		if ((mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource) ? true : false)
		{
			WriteTargetValueToSource();
		}
	}

	public override void UpdateTarget()
	{
		if (_nodes.Count == 0)
		{
			return;
		}
		object source = _nodes[0].Source;
		_updateTargetDepth++;
		try
		{
			for (int i = 0; i < _nodes.Count; i++)
			{
				_nodes[i].SetSource(AvaloniaProperty.UnsetValue, null);
			}
			_nodes[0].SetSource(source, null);
		}
		finally
		{
			_updateTargetDepth--;
		}
	}

	/// <summary>
	/// Called by an <see cref="T:Avalonia.Data.Core.ExpressionNodes.ExpressionNode" /> belonging to this binding when its
	/// <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Value" /> changes.
	/// </summary>
	/// <param name="nodeIndex">The <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Index" />.</param>
	/// <param name="value">The <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Value" />.</param>
	/// <param name="dataValidationError">
	/// The data validation error associated with the current value, if any.
	/// </param>
	internal void OnNodeValueChanged(int nodeIndex, object? value, Exception? dataValidationError)
	{
		if (nodeIndex == _nodes.Count - 1)
		{
			if (_mode == BindingMode.OneTime)
			{
				if (!_shouldUpdateOneTimeBindingTarget && !(_nodes[nodeIndex] is DataContextNodeBase))
				{
					return;
				}
				_shouldUpdateOneTimeBindingTarget = false;
			}
			if (_mode != BindingMode.OneWayToSource)
			{
				BindingError error = ((dataValidationError != null) ? new BindingError(dataValidationError, BindingErrorType.DataValidationError) : null);
				bool forceUpdate = _mode == BindingMode.OneWay || _updateTargetDepth > 0;
				ConvertAndPublishValue(value, error, forceUpdate);
			}
		}
		else if (_mode == BindingMode.OneWayToSource && nodeIndex == _nodes.Count - 2 && value != null)
		{
			_nodes[nodeIndex + 1].SetSource(value, dataValidationError);
			WriteTargetValueToSource();
		}
		else
		{
			if (_mode == BindingMode.OneTime && _nodes[nodeIndex] is DataContextNodeBase)
			{
				_shouldUpdateOneTimeBindingTarget = true;
			}
			_nodes[nodeIndex + 1].SetSource(value, dataValidationError);
		}
	}

	/// <summary>
	/// Called by an <see cref="T:Avalonia.Data.Core.ExpressionNodes.ExpressionNode" /> belonging to this binding when an error occurs
	/// reading its value.
	/// </summary>
	/// <param name="nodeIndex">
	/// The <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Index" /> or -1 if the source is null.
	/// </param>
	/// <param name="error">The error message.</param>
	internal void OnNodeError(int nodeIndex, string error)
	{
		for (int i = nodeIndex + 1; i < _nodes.Count; i++)
		{
			_nodes[i].SetSource(AvaloniaProperty.UnsetValue, null);
		}
		if (_mode != BindingMode.OneWayToSource)
		{
			string text = CalculateErrorPoint(nodeIndex);
			if (ShouldLogError(out AvaloniaObject target))
			{
				Log(target, error, text);
			}
			BindingError error2 = new BindingError(new BindingChainException(error, Description, text.ToString()), BindingErrorType.Error);
			ConvertAndPublishValue(AvaloniaProperty.UnsetValue, error2);
		}
	}

	internal void OnDataValidationError(Exception error)
	{
		BindingError error2 = new BindingError(error, BindingErrorType.DataValidationError);
		PublishValue(UntypedBindingExpressionBase.UnchangedValue, error2);
	}

	internal override bool WriteValueToSource(object? value)
	{
		StopDelayTimer();
		if (_nodes.Count != 0 && LeafNode is ISettableNode settableNode)
		{
			Type valueType = settableNode.ValueType;
			if ((object)valueType != null)
			{
				IValueConverter converter = Converter;
				if (converter != null && value != AvaloniaProperty.UnsetValue && value != BindingOperations.DoNothing)
				{
					value = ConvertBack(converter, ConverterCulture, ConverterParameter, value, valueType);
				}
				if (value == BindingOperations.DoNothing)
				{
					return true;
				}
				if (_targetTypeConverter != null)
				{
					if (_targetTypeConverter.TryConvert(value, valueType, ConverterCulture, out object result))
					{
						value = result;
					}
					else
					{
						if (FallbackValue == AvaloniaProperty.UnsetValue)
						{
							if (base.IsDataValidationEnabled)
							{
								string value2 = value?.ToString() ?? "(null)";
								string value3 = value?.GetType().FullName ?? "null";
								InvalidCastException error = new InvalidCastException($"Could not convert '{value2}' ({value3}) to {valueType}.");
								OnDataValidationError(error);
								return false;
							}
							return false;
						}
						value = FallbackValue;
					}
				}
				if (TypeUtilities.IdentityEquals(LeafNode.Value, value, valueType) && base.ErrorType == BindingErrorType.None)
				{
					return true;
				}
				try
				{
					return settableNode.WriteValueToSource(value, _nodes);
				}
				catch
				{
					return false;
				}
			}
		}
		return false;
	}

	protected override bool ShouldLogError([NotNullWhen(true)] out AvaloniaObject? target)
	{
		if (!TryGetTarget(out target))
		{
			return false;
		}
		if (_nodes.Count > 0 && _nodes[0] is SourceNode sourceNode)
		{
			return sourceNode.ShouldLogErrors(target);
		}
		return true;
	}

	protected override void StartCore()
	{
		WeakReference<object?>? source = _source;
		if (source != null && source.TryGetTarget(out object target))
		{
			if (_nodes.Count > 0)
			{
				_nodes[0].SetSource(target, null);
			}
			else
			{
				ConvertAndPublishValue(target, null);
			}
			BindingMode mode = _mode;
			bool flag = ((mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource) ? true : false);
			if (!flag || !TryGetTarget(out AvaloniaObject target2) || (object)base.TargetProperty == null)
			{
				return;
			}
			switch (UpdateSourceTrigger)
			{
			case UpdateSourceTrigger.PropertyChanged:
				target2.PropertyChanged += OnTargetPropertyChanged;
				break;
			case UpdateSourceTrigger.LostFocus:
				if (target2 is IInputElement inputElement)
				{
					inputElement.LostFocus += OnTargetLostFocus;
				}
				break;
			}
		}
		else
		{
			OnNodeError(-1, "Binding Source is null.");
		}
	}

	protected override void StopCore()
	{
		StopDelayTimer();
		foreach (ExpressionNode node in _nodes)
		{
			node.SetSource(AvaloniaProperty.UnsetValue, null);
		}
		BindingMode mode = _mode;
		bool flag = ((mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource) ? true : false);
		if (!flag || !TryGetTarget(out AvaloniaObject target))
		{
			return;
		}
		switch (UpdateSourceTrigger)
		{
		case UpdateSourceTrigger.PropertyChanged:
			target.PropertyChanged -= OnTargetPropertyChanged;
			break;
		case UpdateSourceTrigger.LostFocus:
			if (target is IInputElement inputElement)
			{
				inputElement.LostFocus -= OnTargetLostFocus;
			}
			break;
		}
	}

	private string CalculateErrorPoint(int nodeIndex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (nodeIndex >= 0)
		{
			_nodes[nodeIndex].BuildString(stringBuilder);
		}
		else
		{
			stringBuilder.Append("(source)");
		}
		return stringBuilder.ToString();
	}

	private void Log(AvaloniaObject target, string error, string errorPoint, LogEventLevel level = LogEventLevel.Warning)
	{
		if (Logger.TryGet(level, "Binding", out var outLogger))
		{
			outLogger.Log(target, "An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}", ((object)base.TargetProperty) ?? ((object)"(unknown)"), Description, errorPoint, error);
		}
	}

	private void ConvertAndPublishValue(object? value, BindingError? error, bool forceUpdate = false)
	{
		bool flag = false;
		IValueConverter converter = Converter;
		if (converter != null && value != AvaloniaProperty.UnsetValue && value != BindingOperations.DoNothing)
		{
			value = Convert(converter, ConverterCulture, ConverterParameter, value, base.TargetType, ref error);
		}
		if (value == BindingOperations.DoNothing)
		{
			return;
		}
		if (value == null && TargetNullValue != AvaloniaProperty.UnsetValue)
		{
			value = ConvertFallback(TargetNullValue, "TargetNullValue");
			flag = true;
		}
		if (value != AvaloniaProperty.UnsetValue)
		{
			string stringFormat = StringFormat;
			if (stringFormat != null && (base.TargetType == typeof(object) || base.TargetType == typeof(string)) && !flag)
			{
				value = string.Format(ConverterCulture, stringFormat, value);
			}
			else if (_targetTypeConverter != null)
			{
				value = ConvertFrom(_targetTypeConverter, value, ref error);
			}
		}
		if (value == AvaloniaProperty.UnsetValue && FallbackValue != AvaloniaProperty.UnsetValue)
		{
			value = ConvertFallback(FallbackValue, "FallbackValue");
		}
		PublishValue(value, error, forceUpdate);
	}

	private void WriteTargetValueToSource()
	{
		StopDelayTimer();
		if (TryGetTarget(out AvaloniaObject target) && (object)base.TargetProperty != null)
		{
			WriteValueToSource(target.GetValue(base.TargetProperty));
		}
	}

	private void OnTargetLostFocus(object? sender, RoutedEventArgs e)
	{
		WriteTargetValueToSource();
	}

	private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property != base.TargetProperty)
		{
			return;
		}
		TimeSpan? timeSpan = _uncommon?._delay;
		if (timeSpan.HasValue)
		{
			TimeSpan valueOrDefault = timeSpan.GetValueOrDefault();
			if (valueOrDefault.Ticks > 0)
			{
				DispatcherTimer dispatcherTimer = _uncommon._delayTimer;
				if (dispatcherTimer != null)
				{
					dispatcherTimer.Stop();
				}
				else
				{
					dispatcherTimer = (_uncommon._delayTimer = new DispatcherTimer(valueOrDefault, DispatcherPriority.Normal, OnDelayTimerTick)
					{
						Tag = this
					});
				}
				dispatcherTimer.Start();
				return;
			}
		}
		WriteTargetValueToSource();
	}

	private static void OnDelayTimerTick(object? sender, EventArgs e)
	{
		((BindingExpression)((DispatcherTimer)sender).Tag).WriteTargetValueToSource();
	}

	private void StopDelayTimer()
	{
		_uncommon?._delayTimer?.Stop();
	}

	private object? ConvertFallback(object? fallback, string fallbackName)
	{
		if (_targetTypeConverter == null || base.TargetType == typeof(object) || fallback == AvaloniaProperty.UnsetValue)
		{
			return fallback;
		}
		if (_targetTypeConverter.TryConvert(fallback, base.TargetType, ConverterCulture, out object result))
		{
			return result;
		}
		if (TryGetTarget(out AvaloniaObject target))
		{
			Log(target, $"Could not convert {fallbackName} '{fallback}' to '{base.TargetType}'.", LogEventLevel.Error);
		}
		return AvaloniaProperty.UnsetValue;
	}

	private object? ConvertFrom(TargetTypeConverter? converter, object? value, ref BindingError? error)
	{
		if (converter == null)
		{
			return value;
		}
		if (converter.TryConvert(value, base.TargetType, ConverterCulture, out object result))
		{
			return result;
		}
		string value2 = value?.ToString() ?? "(null)";
		string value3 = value?.GetType().FullName ?? "null";
		string text = $"Could not convert '{value2}' ({value3}) to '{base.TargetType}'.";
		if (ShouldLogError(out AvaloniaObject target))
		{
			Log(target, text);
		}
		error = new BindingError(new InvalidCastException(text), BindingErrorType.Error);
		return AvaloniaProperty.UnsetValue;
	}
}
