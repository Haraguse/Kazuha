using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins;

[RequiresUnreferencedCode("PropertyAccessors might require unreferenced code.")]
[RequiresDynamicCode("ExpressionNode requires dynamic code.")]
internal class ReflectionMethodAccessorPlugin : IPropertyAccessorPlugin
{
	private readonly struct MethodLookupResult(MethodInfo? method, string? error)
	{
		public MethodInfo? Method { get; } = method;

		public string? Error { get; } = error;

		public bool IsMatch
		{
			get
			{
				if ((object)Method == null)
				{
					return Error != null;
				}
				return true;
			}
		}
	}

	[RequiresDynamicCode("ExpressionNode requires dynamic code.")]
	private sealed class Accessor : PropertyAccessorBase
	{
		public override Type? PropertyType { get; }

		public override object? Value { get; }

		public Accessor(WeakReference<object?> reference, MethodInfo method)
		{
			if (reference == null)
			{
				throw new ArgumentNullException("reference");
			}
			if ((object)method == null)
			{
				throw new ArgumentNullException("method");
			}
			Type returnType = method.ReturnType;
			ParameterInfo[] parameters = method.GetParameters();
			Type[] array = new Type[parameters.Length + 1];
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo parameterInfo = parameters[i];
				array[i] = parameterInfo.ParameterType;
			}
			array[^1] = returnType;
			PropertyType = Expression.GetDelegateType(array);
			object target;
			if (method.IsStatic)
			{
				Value = method.CreateDelegate(PropertyType);
			}
			else if (reference.TryGetTarget(out target))
			{
				Value = method.CreateDelegate(PropertyType, target);
			}
		}

		public override bool SetValue(object? value, BindingPriority priority)
		{
			return false;
		}

		protected override void SubscribeCore()
		{
			try
			{
				PublishValue(Value);
			}
			catch
			{
			}
		}

		protected override void UnsubscribeCore()
		{
		}
	}

	private readonly Dictionary<(Type, string), MethodLookupResult> _methodLookup = new Dictionary<(Type, string), MethodLookupResult>();

	public bool Match(object obj, string methodName)
	{
		return GetMethod(obj.GetType(), methodName).IsMatch;
	}

	public IPropertyAccessor? Start(WeakReference<object?> reference, string methodName)
	{
		if (reference == null)
		{
			throw new ArgumentNullException("reference");
		}
		if (methodName == null)
		{
			throw new ArgumentNullException("methodName");
		}
		if (!reference.TryGetTarget(out object target) || target == null)
		{
			return null;
		}
		MethodLookupResult method = GetMethod(target.GetType(), methodName);
		MethodInfo method2 = method.Method;
		if ((object)method2 != null)
		{
			return new Accessor(reference, method2);
		}
		string error = method.Error;
		return new PropertyError(new BindingNotification((error != null) ? ((SystemException)new AmbiguousMatchException(error)) : ((SystemException)new MissingMemberException($"Could not find CLR method '{methodName}' on '{target}'")), BindingErrorType.Error));
	}

	private MethodLookupResult GetMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type, string methodName)
	{
		(Type, string) key = (type, methodName);
		if (!_methodLookup.TryGetValue(key, out var value))
		{
			value = FindBestCommandMethod(type, methodName);
			_methodLookup.Add(key, value);
		}
		return value;
	}

	/// <summary>
	/// Finds the method named <paramref name="methodName" /> which can be bound to a command.
	/// </summary>
	/// <remarks>
	/// Priority:
	///  1. One parameter method
	///    1a. Object parameter (amongst several overloads)
	///    1b. Single method with one parameter
	///  2. Zero parameters method
	/// </remarks>
	private static MethodLookupResult FindBestCommandMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type, string methodName)
	{
		List<MethodInfo> list = null;
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.Name == methodName)
			{
				(list ?? (list = new List<MethodInfo>())).Add(methodInfo);
			}
		}
		if (list == null)
		{
			return default(MethodLookupResult);
		}
		MethodInfo methodInfo2 = null;
		Dictionary<Type, MethodInfo> dictionary = null;
		foreach (MethodInfo item in list)
		{
			ParameterInfo[] parameters = item.GetParameters();
			switch (parameters.Length)
			{
			case 0:
				methodInfo2 = GetMostDerived(methodInfo2, item);
				break;
			case 1:
			{
				Type parameterType = parameters[0].ParameterType;
				if (dictionary == null)
				{
					dictionary = new Dictionary<Type, MethodInfo>();
				}
				dictionary[parameterType] = GetMostDerived(dictionary.GetValueOrDefault(parameterType), item);
				break;
			}
			}
		}
		if (dictionary != null)
		{
			if (dictionary.TryGetValue(typeof(object), out var value))
			{
				return new MethodLookupResult(value, null);
			}
			if (dictionary.Count == 1)
			{
				return new MethodLookupResult(dictionary.Values.First(), null);
			}
			string[] array = dictionary.Keys.Select((Type t) => "'" + t.FullName + "'").OrderBy<string, string>((string s) => s, StringComparer.Ordinal).ToArray();
			return new MethodLookupResult(null, $"Unable to resolve method of name '{methodName}' on type '{type.FullName}'. Found {array.Length} overloads accepting one parameter: {string.Join(", ", array)}. " + "Expected either a single overload with one parameter, or an overload accepting System.Object.");
		}
		if ((object)methodInfo2 != null)
		{
			MethodInfo method = methodInfo2;
			return new MethodLookupResult(method, null);
		}
		return new MethodLookupResult(null, $"Unable to resolve method of name '{methodName}' on type '{type.FullName}'. Found {list.Count} overloads accepting more than one parameter. " + "Expected a method with zero or one parameter.");
	}

	private static MethodInfo GetMostDerived(MethodInfo? existing, MethodInfo candidate)
	{
		if ((object)existing == null)
		{
			return candidate;
		}
		Type declaringType = existing.DeclaringType;
		if ((object)declaringType != null)
		{
			Type declaringType2 = candidate.DeclaringType;
			if ((object)declaringType2 != null && declaringType != declaringType2 && declaringType.IsAssignableFrom(declaringType2))
			{
				return candidate;
			}
		}
		return existing;
	}
}
