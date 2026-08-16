using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode("BindingExpression and ReflectionBinding heavily use reflection. Consider using CompiledBindings instead.")]
internal sealed class ReflectionIndexerNode : CollectionNodeBase, ISettableNode
{
	private const BindingFlags InstanceFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;

	private MethodInfo? _getter;

	private MethodInfo? _setter;

	private object?[]? _indexes;

	public IList Arguments { get; }

	public Type? ValueType => _getter?.ReturnType;

	public ReflectionIndexerNode(IList arguments)
	{
		Arguments = arguments;
	}

	public override void BuildString(StringBuilder builder)
	{
		builder.Append('[');
		for (int i = 0; i < Arguments.Count; i++)
		{
			builder.Append(Arguments[i]);
			if (i != Arguments.Count - 1)
			{
				builder.Append(',');
			}
		}
		builder.Append(']');
	}

	public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
	{
		if (base.Source == null || (object)_setter == null)
		{
			return false;
		}
		object[] array = new object[_indexes.Length + 1];
		_indexes.CopyTo(array, 0);
		array[_indexes.Length] = value;
		_setter.Invoke(base.Source, array);
		return true;
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (!ValidateNonNullSource(source))
		{
			return;
		}
		_indexes = null;
		if (GetIndexer(source.GetType(), out _getter, out _setter))
		{
			ParameterInfo[] parameters = _getter.GetParameters();
			if (parameters.Length != Arguments.Count)
			{
				SetError($"Wrong number of arguments for indexer: expected {parameters.Length}, got {Arguments.Count}.");
			}
			else
			{
				_indexes = ConvertIndexes(parameters, Arguments);
				base.OnSourceChanged(source, dataValidationError);
			}
		}
		else
		{
			SetError($"Type '{source.GetType()}' does not have an indexer.");
		}
	}

	protected override bool ShouldUpdate(object? sender, PropertyChangedEventArgs e)
	{
		if (sender == null || e.PropertyName == null)
		{
			return false;
		}
		return sender.GetType().GetTypeInfo().GetDeclaredProperty(e.PropertyName)?.GetIndexParameters().Any() ?? false;
	}

	protected override int? TryGetFirstArgumentAsInt()
	{
		if (TypeUtilities.TryConvert(typeof(int), Arguments[0], CultureInfo.InvariantCulture, out object result))
		{
			return (int?)result;
		}
		return null;
	}

	protected override void UpdateValue(object? source)
	{
		if ((object)_getter != null && _indexes != null)
		{
			SetValue(_getter.Invoke(source, _indexes));
		}
		else
		{
			ClearValue();
		}
	}

	private static object?[] ConvertIndexes(ParameterInfo[] indexParameters, IList arguments)
	{
		List<object> list = new List<object>();
		for (int i = 0; i < indexParameters.Length; i++)
		{
			Type parameterType = indexParameters[i].ParameterType;
			object value = arguments[i];
			if (TypeUtilities.TryConvert(parameterType, value, CultureInfo.InvariantCulture, out object result))
			{
				list.Add(result);
				continue;
			}
			throw new InvalidCastException($"Could not convert list index '{i}' of type '{value}' to '{parameterType}'.");
		}
		return list.ToArray();
	}

	private static bool GetIndexer(Type? type, [NotNullWhen(true)] out MethodInfo? getter, out MethodInfo? setter)
	{
		getter = (setter = null);
		if ((object)type == null)
		{
			return false;
		}
		if (type.IsArray)
		{
			getter = type.GetMethod("Get");
			setter = type.GetMethod("Set");
			return (object)getter != null;
		}
		while (type != null)
		{
			PropertyInfo property = type.GetProperty("Item", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			if ((object)property != null)
			{
				getter = property.GetMethod;
				setter = property.SetMethod;
				return (object)getter != null;
			}
			PropertyInfo[] properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (propertyInfo.GetIndexParameters().Length != 0)
				{
					getter = propertyInfo.GetMethod;
					setter = propertyInfo.SetMethod;
					return (object)getter != null;
				}
			}
			type = type.BaseType;
		}
		return false;
	}
}
