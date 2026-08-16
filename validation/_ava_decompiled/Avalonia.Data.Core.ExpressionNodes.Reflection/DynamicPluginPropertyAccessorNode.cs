using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

/// <summary>
/// A node in the binding path of an <see cref="T:Avalonia.Data.Core.BindingExpression" /> that reads a property
/// via an <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessorPlugin" /> selected at runtime from the registered
/// <see cref="P:Avalonia.Data.Core.Plugins.BindingPlugins.PropertyAccessors" />.
/// </summary>
[RequiresUnreferencedCode("ExpressionNode might require unreferenced code.")]
internal sealed class DynamicPluginPropertyAccessorNode : ExpressionNode, IPropertyAccessorNode, ISettableNode
{
	private readonly bool _acceptsNull;

	private readonly Action<object?> _onValueChanged;

	private IPropertyAccessor? _accessor;

	private bool _enableDataValidation;

	public IPropertyAccessor? Accessor => _accessor;

	public string PropertyName { get; }

	public Type? ValueType => _accessor?.PropertyType;

	public DynamicPluginPropertyAccessorNode(string propertyName, bool acceptsNull)
	{
		_acceptsNull = acceptsNull;
		_onValueChanged = OnValueChanged;
		PropertyName = propertyName;
	}

	public override void BuildString(StringBuilder builder)
	{
		if (builder.Length > 0 && builder[builder.Length - 1] != '!')
		{
			builder.Append('.');
		}
		builder.Append(PropertyName);
	}

	public void EnableDataValidation()
	{
		_enableDataValidation = true;
	}

	public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
	{
		return _accessor?.SetValue(value, BindingPriority.LocalValue) ?? false;
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (source == null)
		{
			if (_acceptsNull)
			{
				SetValue(null);
			}
			else
			{
				ValidateNonNullSource(source);
			}
			return;
		}
		WeakReference<object> reference = new WeakReference<object>(source);
		IPropertyAccessorPlugin plugin = GetPlugin(source);
		if (plugin != null)
		{
			IPropertyAccessor propertyAccessor = plugin.Start(reference, PropertyName);
			if (propertyAccessor != null)
			{
				if (_enableDataValidation)
				{
					foreach (IDataValidationPlugin s_dataValidator in BindingPlugins.s_dataValidators)
					{
						if (s_dataValidator.Match(reference, PropertyName))
						{
							propertyAccessor = s_dataValidator.Start(reference, PropertyName, propertyAccessor);
						}
					}
				}
				_accessor = propertyAccessor;
				_accessor.Subscribe(_onValueChanged);
				return;
			}
		}
		SetError($"Could not find a matching property accessor for '{PropertyName}' on '{source.GetType()}'.");
	}

	protected override void Unsubscribe(object oldSource)
	{
		_accessor?.Dispose();
		_accessor = null;
	}

	private void OnValueChanged(object? newValue)
	{
		SetValue(newValue);
	}

	private IPropertyAccessorPlugin? GetPlugin(object? source)
	{
		if (source == null)
		{
			return null;
		}
		foreach (IPropertyAccessorPlugin s_propertyAccessor in BindingPlugins.s_propertyAccessors)
		{
			if (s_propertyAccessor.Match(source, PropertyName))
			{
				return s_propertyAccessor;
			}
		}
		return null;
	}
}
