using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.Core;

/// <summary>
/// Helper class for evaluating a binding from an Item and BindingBase instance
/// </summary>
internal sealed class BindingEvaluator<T> : StyledElement, IDisposable
{
	public static readonly StyledProperty<T> ValueProperty = AvaloniaProperty.Register<BindingEvaluator<T>, T>("Value", default(T), false, (BindingMode)1, (Func<T, bool>)null, (Func<AvaloniaObject, T, T>)null, false);

	private BindingExpressionBase _expression;

	private BindingBase _lastBinding;

	/// <summary>
	/// Gets or sets the data item value.
	/// </summary>
	public T Value
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<T>(ValueProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<T>(ValueProperty, value, (BindingPriority)0);
		}
	}

	public T Evaluate(object dataContext)
	{
		if (!object.Equals(dataContext, ((StyledElement)this).DataContext))
		{
			((StyledElement)this).DataContext = dataContext;
		}
		return ((AvaloniaObject)this).GetValue<T>(ValueProperty);
	}

	public void UpdateBinding(BindingBase binding)
	{
		if (binding != _lastBinding)
		{
			BindingExpressionBase expression = _expression;
			if (expression != null)
			{
				expression.Dispose();
			}
			_expression = ((AvaloniaObject)this).Bind((AvaloniaProperty)(object)ValueProperty, binding);
			_lastBinding = binding;
		}
	}

	public void ClearDataContext()
	{
		((StyledElement)this).DataContext = null;
	}

	public void Dispose()
	{
		BindingExpressionBase expression = _expression;
		if (expression != null)
		{
			expression.Dispose();
		}
		_expression = null;
		_lastBinding = null;
		((StyledElement)this).DataContext = null;
	}

	[return: NotNullIfNotNull("binding")]
	public static BindingEvaluator<T> TryCreate(BindingBase binding)
	{
		if (binding == null)
		{
			return null;
		}
		BindingEvaluator<T> bindingEvaluator = new BindingEvaluator<T>();
		bindingEvaluator.UpdateBinding(binding);
		return bindingEvaluator;
	}
}
