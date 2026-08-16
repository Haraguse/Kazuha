using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in the binding path of an <see cref="T:Avalonia.Data.Core.BindingExpression" />.
/// </summary>
internal abstract class ExpressionNode
{
	private WeakReference<object?>? _source;

	private object? _value = AvaloniaProperty.UnsetValue;

	/// <summary>
	/// Gets the index of the node in the binding path.
	/// </summary>
	public int Index { get; private set; }

	/// <summary>
	/// Gets the owning <see cref="T:Avalonia.Data.Core.BindingExpression" />.
	/// </summary>
	public BindingExpression? Owner { get; private set; }

	/// <summary>
	/// Gets the source object from which the node will read its value.
	/// </summary>
	public object? Source
	{
		get
		{
			WeakReference<object?>? source = _source;
			if (source != null && source.TryGetTarget(out object target))
			{
				return target;
			}
			return null;
		}
	}

	/// <summary>
	/// Gets the current value of the node.
	/// </summary>
	public object? Value => _value;

	/// <summary>
	/// Appends a string representation of the expression node to a string builder.
	/// </summary>
	/// <param name="builder">The string builder.</param>
	public virtual void BuildString(StringBuilder builder)
	{
	}

	/// <summary>
	/// Builds a string representation of a binding expression.
	/// </summary>
	/// <param name="builder">The string builder.</param>
	/// <param name="nodes">The nodes in the binding expression.</param>
	public virtual void BuildString(StringBuilder builder, IReadOnlyList<ExpressionNode> nodes)
	{
		if (Index > 0)
		{
			nodes[Index - 1].BuildString(builder, nodes);
		}
		BuildString(builder);
	}

	/// <summary>
	/// Sets the owner binding.
	/// </summary>
	/// <param name="owner">The owner binding.</param>
	/// <param name="index">The index of the node in the binding path.</param>
	/// <exception cref="T:System.InvalidOperationException">
	/// The node already has an owner.
	/// </exception>
	public void SetOwner(BindingExpression owner, int index)
	{
		if (Owner != null)
		{
			throw new InvalidOperationException($"{this} already has an owner.");
		}
		Owner = owner;
		Index = index;
	}

	/// <summary>
	/// Sets the <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Source" /> from which the node will read its value and updates
	/// the current <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Value" />, notifying the <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Owner" /> if the value
	/// changes.
	/// </summary>
	/// <param name="source">
	/// The new source from which the node will read its value. May be 
	/// <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> in which case the source will be considered
	/// to be null.
	/// </param>
	/// <param name="dataValidationError">
	/// Any data validation error reported by the previous expression node.
	/// </param>
	public void SetSource(object? source, Exception? dataValidationError)
	{
		WeakReference<object?>? source2 = _source;
		if (source2 == null || !source2.TryGetTarget(out object target))
		{
			target = AvaloniaProperty.UnsetValue;
		}
		if (source == target)
		{
			return;
		}
		if (target != null && target != AvaloniaProperty.UnsetValue)
		{
			Unsubscribe(target);
		}
		if (source == AvaloniaProperty.UnsetValue)
		{
			_source = null;
			_value = AvaloniaProperty.UnsetValue;
			return;
		}
		_source = new WeakReference<object>(source);
		try
		{
			OnSourceChanged(source, dataValidationError);
		}
		catch (Exception error)
		{
			SetError(error);
		}
	}

	/// <summary>
	/// Sets the current value to <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />.
	/// </summary>
	protected void ClearValue()
	{
		SetValue(AvaloniaProperty.UnsetValue);
	}

	/// <summary>
	/// Notifies the <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Owner" /> of a data validation error.
	/// </summary>
	/// <param name="error">The error.</param>
	protected void SetDataValidationError(Exception error)
	{
		if (error is TargetInvocationException ex)
		{
			error = ex.InnerException;
		}
		Owner?.OnDataValidationError(error);
	}

	/// <summary>
	/// Sets the current value to <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> and notifies the
	/// <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Owner" /> of the error.
	/// </summary>
	/// <param name="message">The error message.</param>
	protected void SetError(string message)
	{
		_value = AvaloniaProperty.UnsetValue;
		Owner?.OnNodeError(Index, message);
	}

	/// <summary>
	/// Sets the current value to <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> and notifies the
	/// <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Owner" /> of the error.
	/// </summary>
	/// <param name="e">The error.</param>
	protected void SetError(Exception e)
	{
		if (e is TargetInvocationException ex)
		{
			e = ex.InnerException;
		}
		if (e is AggregateException ex2 && ex2.InnerExceptions.Count == 1)
		{
			e = e.InnerException;
		}
		SetError(e.Message);
	}

	/// <summary>
	/// Sets the current <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Value" />, notifying the <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Owner" /> if the value
	/// has changed.
	/// </summary>
	/// <param name="valueOrNotification">
	/// The new value. May be a <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </param>
	protected void SetValue(object? valueOrNotification)
	{
		if (valueOrNotification is BindingNotification bindingNotification)
		{
			if (bindingNotification.ErrorType == BindingErrorType.Error)
			{
				SetError(bindingNotification.Error);
			}
			else if (bindingNotification.ErrorType == BindingErrorType.DataValidationError)
			{
				if (bindingNotification.HasValue)
				{
					if (bindingNotification.Value is BindingNotification value)
					{
						SetValue(value);
					}
					else
					{
						SetValue(bindingNotification.Value, bindingNotification.Error);
					}
				}
				else
				{
					SetDataValidationError(bindingNotification.Error);
				}
			}
			else
			{
				SetValue(bindingNotification.Value);
			}
		}
		else
		{
			SetValue(valueOrNotification, null);
		}
	}

	/// <summary>
	/// Sets the current <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Value" />, notifying the <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Owner" /> if the value
	/// has changed.
	/// </summary>
	/// <param name="value">
	/// The new value. May not be a <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </param>
	/// <param name="dataValidationError">
	/// The data validation error associated with the new value, if any.
	/// </param>
	protected void SetValue(object? value, Exception? dataValidationError = null)
	{
		_value = value;
		Owner?.OnNodeValueChanged(Index, value, dataValidationError);
	}

	/// <summary>
	/// Called from <see cref="M:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.OnSourceChanged(System.Object,System.Exception)" /> to validate that the source
	/// is non-null and raise a node error if it is not.
	/// </summary>
	/// <param name="source">The expression node source.</param>
	/// <returns>
	/// True if the source is non-null; otherwise, false.
	/// </returns>
	protected bool ValidateNonNullSource([NotNullWhen(true)] object? source)
	{
		if (source == null)
		{
			Owner?.OnNodeError(Index - 1, "Value is null.");
			_value = null;
			return false;
		}
		return true;
	}

	/// <summary>
	/// When implemented in a derived class, subscribes to the new source, and updates the current 
	/// <see cref="P:Avalonia.Data.Core.ExpressionNodes.ExpressionNode.Value" />.
	/// </summary>
	/// <param name="source">The new source.</param>
	/// <param name="dataValidationError">
	/// Any data validation error reported by the previous expression node.
	/// </param>
	protected abstract void OnSourceChanged(object? source, Exception? dataValidationError);

	/// <summary>
	/// When implemented in a derived class, unsubscribes from the previous source.
	/// </summary>
	/// <param name="oldSource">The old source.</param>
	protected virtual void Unsubscribe(object oldSource)
	{
	}
}
