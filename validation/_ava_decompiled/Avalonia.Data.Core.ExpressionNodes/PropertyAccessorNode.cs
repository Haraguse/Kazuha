using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in the binding path of an <see cref="T:Avalonia.Data.Core.BindingExpression" /> that reads a property
/// via a predefined <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessorPlugin" />.
/// </summary>
internal sealed class PropertyAccessorNode : ExpressionNode, IPropertyAccessorNode, ISettableNode
{
	private readonly Action<object?> _onValueChanged;

	private readonly IPropertyAccessorPlugin _plugin;

	private readonly bool _acceptsNull;

	private IPropertyAccessor? _accessor;

	private bool _enableDataValidation;

	public IPropertyAccessor? Accessor => _accessor;

	public string PropertyName { get; }

	public Type? ValueType => _accessor?.PropertyType;

	public PropertyAccessorNode(string propertyName, IPropertyAccessorPlugin plugin, bool acceptsNull)
	{
		_plugin = plugin;
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
		if ((object)_accessor?.PropertyType != null)
		{
			return _accessor.SetValue(value, BindingPriority.LocalValue);
		}
		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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
		IPropertyAccessor propertyAccessor = _plugin.Start(reference, PropertyName);
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
		}
		else
		{
			ClearValue();
		}
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
}
