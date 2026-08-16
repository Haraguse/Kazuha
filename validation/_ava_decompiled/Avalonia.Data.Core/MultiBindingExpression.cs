using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Avalonia.Data.Core;

internal class MultiBindingExpression : UntypedBindingExpressionBase, IBindingExpressionSink
{
	private static readonly object s_uninitialized = new object();

	private readonly BindingBase[] _bindings;

	private readonly IMultiValueConverter? _converter;

	private readonly CultureInfo? _converterCulture;

	private readonly object? _converterParameter;

	private readonly UntypedBindingExpressionBase?[] _expressions;

	private readonly object? _fallbackValue;

	private readonly object? _targetNullValue;

	private readonly object?[] _values;

	private readonly ReadOnlyCollection<object?> _valuesView;

	public override string Description => "MultiBinding";

	internal UntypedBindingExpressionBase?[] Expressions => _expressions;

	internal IMultiValueConverter? Converter => _converter;

	internal CultureInfo? ConverterCulture => _converterCulture;

	internal object? ConverterParameter => _converterParameter;

	internal object? FallbackValue => _fallbackValue;

	internal object? TargetNullValue => _targetNullValue;

	public MultiBindingExpression(BindingPriority priority, IList<BindingBase> bindings, IMultiValueConverter? converter, CultureInfo? converterCulture, object? converterParameter, object? fallbackValue, object? targetNullValue)
		: base(priority)
	{
		_bindings = bindings.ToArray();
		_converter = converter;
		_converterCulture = converterCulture;
		_converterParameter = converterParameter;
		_expressions = new UntypedBindingExpressionBase[_bindings.Length];
		_fallbackValue = fallbackValue;
		_targetNullValue = targetNullValue;
		_values = new object[_bindings.Length];
		_valuesView = new ReadOnlyCollection<object>(_values);
		Array.Fill<object>(_values, s_uninitialized);
	}

	protected override void StartCore()
	{
		if (!TryGetTarget(out AvaloniaObject target))
		{
			throw new AvaloniaInternalException("MultiBindingExpression has no target.");
		}
		for (int i = 0; i < _bindings.Length; i++)
		{
			BindingExpressionBase bindingExpressionBase = _bindings[i].CreateInstance(target, null, null);
			if (!(bindingExpressionBase is UntypedBindingExpressionBase untypedBindingExpressionBase))
			{
				throw new NotSupportedException($"Unsupported BindingExpressionBase implementation '{bindingExpressionBase}'.");
			}
			_expressions[i] = untypedBindingExpressionBase;
			untypedBindingExpressionBase.AttachAndStart(this, target, null, base.Priority);
		}
	}

	protected override void StopCore()
	{
		for (int i = 0; i < _expressions.Length; i++)
		{
			_expressions[i]?.Dispose();
			_expressions[i] = null;
			_values[i] = s_uninitialized;
		}
	}

	void IBindingExpressionSink.OnChanged(UntypedBindingExpressionBase instance, bool hasValueChanged, bool hasErrorChanged, object? value, BindingError? error)
	{
		int num = Array.IndexOf<UntypedBindingExpressionBase>(_expressions, instance);
		_values[num] = BindingNotification.ExtractValue(value);
		PublishValue();
	}

	void IBindingExpressionSink.OnCompleted(UntypedBindingExpressionBase instance)
	{
	}

	private void PublishValue()
	{
		object[] values = _values;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] == s_uninitialized)
			{
				return;
			}
		}
		if (_converter != null)
		{
			CultureInfo culture = _converterCulture ?? CultureInfo.CurrentCulture;
			object o = _converter.Convert(_valuesView, base.TargetType, _converterParameter, culture);
			o = BindingNotification.ExtractValue(o);
			if (o != BindingOperations.DoNothing)
			{
				if (o == null)
				{
					o = _targetNullValue;
				}
				if (o == AvaloniaProperty.UnsetValue)
				{
					o = _fallbackValue;
				}
				PublishValue(o);
			}
		}
		else
		{
			PublishValue(_valuesView);
		}
	}
}
