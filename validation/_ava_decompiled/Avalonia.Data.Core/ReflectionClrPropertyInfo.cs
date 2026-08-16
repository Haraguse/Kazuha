using System;
using System.Reflection;

namespace Avalonia.Data.Core;

public class ReflectionClrPropertyInfo : ClrPropertyInfo
{
	private static Action<object, object?>? CreateSetter(PropertyInfo info)
	{
		MethodInfo setMethod = info.SetMethod;
		if ((object)setMethod == null)
		{
			return null;
		}
		return delegate(object target, object? value)
		{
			setMethod.Invoke(target, new object[1] { value });
		};
	}

	private static Func<object, object?>? CreateGetter(PropertyInfo info)
	{
		MethodInfo getMethod = info.GetMethod;
		if ((object)getMethod == null)
		{
			return null;
		}
		return (object target) => getMethod.Invoke(target, Array.Empty<object>());
	}

	public ReflectionClrPropertyInfo(PropertyInfo info)
		: base(info.Name, CreateGetter(info), CreateSetter(info), info.PropertyType)
	{
	}
}
