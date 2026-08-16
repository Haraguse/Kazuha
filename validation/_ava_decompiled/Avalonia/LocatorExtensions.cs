using System;
using Avalonia.Metadata;

namespace Avalonia;

[PrivateApi]
public static class LocatorExtensions
{
	public static T? GetService<T>(this IAvaloniaDependencyResolver resolver)
	{
		return (T)resolver.GetService(typeof(T));
	}

	public static object GetRequiredService(this IAvaloniaDependencyResolver resolver, Type t)
	{
		return resolver.GetService(t) ?? throw new InvalidOperationException($"Unable to locate '{t}'.");
	}

	public static T GetRequiredService<T>(this IAvaloniaDependencyResolver resolver)
	{
		T val = (T)resolver.GetService(typeof(T));
		if (val == null)
		{
			throw new InvalidOperationException($"Unable to locate '{typeof(T)}'.");
		}
		return val;
	}
}
