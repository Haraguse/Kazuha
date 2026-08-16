using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;

namespace Avalonia.Data;

internal class TemplateBindingExpression : UntypedBindingExpressionBase
{
	private IValueConverter? _converter;

	private CultureInfo? _converterCulture;

	private object? _converterParameter;

	private BindingMode _mode;

	private readonly AvaloniaProperty? _property;

	private bool _hasPublishedValue;

	public override string Description => $"{{TemplateBinding {_property}}}";

	public TemplateBindingExpression(AvaloniaProperty? property, IValueConverter? converter, CultureInfo? converterCulture, object? converterParameter, BindingMode mode)
		: base(BindingPriority.Template)
	{
		_property = property;
		_converter = converter;
		_converterCulture = converterCulture;
		_converterParameter = converterParameter;
		_mode = mode;
	}

	protected override void StartCore()
	{
		_hasPublishedValue = false;
		OnTemplatedParentChanged();
		if (TryGetTarget(out AvaloniaObject target))
		{
			target.PropertyChanged += OnTargetPropertyChanged;
		}
	}

	protected override void StopCore()
	{
		if (!TryGetTarget(out AvaloniaObject target))
		{
			return;
		}
		if (target is StyledElement styledElement)
		{
			AvaloniaObject avaloniaObject = styledElement?.TemplatedParent;
			if (avaloniaObject != null)
			{
				avaloniaObject.PropertyChanged -= OnTemplatedParentPropertyChanged;
			}
		}
		if (target != null)
		{
			target.PropertyChanged -= OnTargetPropertyChanged;
		}
	}

	internal override bool WriteValueToSource(object? value)
	{
		if ((object)_property != null && TryGetTemplatedParent(out AvaloniaObject result))
		{
			if (_converter != null)
			{
				value = ConvertBack(_converter, _converterCulture, _converterParameter, value, base.TargetType);
			}
			if (value != BindingOperations.DoNothing)
			{
				result.SetCurrentValue(_property, value);
			}
			return true;
		}
		return false;
	}

	private object? ConvertToTargetType(object? value)
	{
		if (TargetTypeConverter.GetDefaultConverter().TryConvert(value, base.TargetType, CultureInfo.InvariantCulture, out object result))
		{
			return result;
		}
		if (TryGetTarget(out AvaloniaObject target))
		{
			string value2 = value?.ToString() ?? "(null)";
			string value3 = value?.GetType().FullName ?? "null";
			string error = $"Could not convert '{value2}' ({value3}) to '{base.TargetType}'.";
			Log(target, error);
		}
		return AvaloniaProperty.UnsetValue;
	}

	private void PublishValue()
	{
		if (_mode == BindingMode.OneWayToSource)
		{
			return;
		}
		if (TryGetTemplatedParent(out AvaloniaObject result))
		{
			object value = (((object)_property != null) ? result.GetValue(_property) : result);
			BindingError error = null;
			if (_converter != null)
			{
				value = Convert(_converter, _converterCulture, _converterParameter, value, base.TargetType, ref error);
			}
			value = ConvertToTargetType(value);
			PublishValue(value, error);
			_hasPublishedValue = true;
			if (_mode == BindingMode.OneTime)
			{
				Stop();
			}
		}
		else if (_hasPublishedValue)
		{
			PublishValue(AvaloniaProperty.UnsetValue);
		}
	}

	private void OnTemplatedParentChanged()
	{
		if (TryGetTemplatedParent(out AvaloniaObject result))
		{
			result.PropertyChanged += OnTemplatedParentPropertyChanged;
		}
		PublishValue();
	}

	private void OnTemplatedParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == _property)
		{
			PublishValue();
		}
	}

	private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == StyledElement.TemplatedParentProperty)
		{
			if (e.OldValue is AvaloniaObject avaloniaObject)
			{
				avaloniaObject.PropertyChanged -= OnTemplatedParentPropertyChanged;
			}
			OnTemplatedParentChanged();
			return;
		}
		BindingMode mode = _mode;
		bool flag = ((mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource) ? true : false);
		if (flag && e.Property == base.TargetProperty)
		{
			WriteValueToSource(e.NewValue);
		}
	}

	private bool TryGetTemplatedParent([NotNullWhen(true)] out AvaloniaObject? result)
	{
		if (TryGetTarget(out AvaloniaObject target) && target is StyledElement styledElement)
		{
			AvaloniaObject templatedParent = styledElement.TemplatedParent;
			if (templatedParent != null)
			{
				result = templatedParent;
				return true;
			}
		}
		result = null;
		return false;
	}
}
