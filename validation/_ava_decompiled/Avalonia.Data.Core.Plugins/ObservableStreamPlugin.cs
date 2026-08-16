using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Handles binding to <see cref="T:System.IObservable`1" />s for the '^' stream binding operator.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "This method is not supported by NativeAOT.")]
[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
internal class ObservableStreamPlugin : IStreamPlugin
{
	private static MethodInfo? s_observableGeneric;

	private static MethodInfo? s_observableSelect;

	[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "Avalonia.Data.Core.Plugins.ObservableStreamPlugin", "Avalonia.Base")]
	public ObservableStreamPlugin()
	{
	}

	/// <summary>
	/// Checks whether this plugin handles the specified value.
	/// </summary>
	/// <param name="reference">A weak reference to the value.</param>
	/// <returns>True if the plugin can handle the value; otherwise false.</returns>
	public virtual bool Match(WeakReference<object?> reference)
	{
		reference.TryGetTarget(out object target);
		if (target != null)
		{
			return MatchesType(target.GetType());
		}
		return false;
	}

	public static bool MatchesType(Type type)
	{
		IEnumerable<Type> enumerable = type.GetInterfaces().AsEnumerable();
		if (type.IsInterface)
		{
			enumerable = enumerable.Concat(new _003C_003Ez__ReadOnlySingleElementList<Type>(type));
		}
		return enumerable.Any((Type x) => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IObservable<>));
	}

	/// <summary>
	/// Starts producing output based on the specified value.
	/// </summary>
	/// <param name="reference">A weak reference to the object.</param>
	/// <returns>
	/// An observable that produces the output for the value.
	/// </returns>
	public virtual IObservable<object?> Start(WeakReference<object?> reference)
	{
		if (!reference.TryGetTarget(out object target) || target == null)
		{
			return Observable.Empty<object>();
		}
		if (target is IObservable<object> result)
		{
			return result;
		}
		return (IObservable<object>)GetBoxObservable(target.GetType().GetInterfaces().First((Type x) => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IObservable<>))
			.GetGenericArguments()[0]).Invoke(null, new object[1] { target });
	}

	[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
	private static MethodInfo GetBoxObservable(Type source)
	{
		return (s_observableGeneric ?? (s_observableGeneric = GetBoxObservable())).MakeGenericMethod(source);
	}

	[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
	private static MethodInfo GetBoxObservable()
	{
		object obj = s_observableSelect;
		if (obj == null)
		{
			obj = typeof(ObservableStreamPlugin).GetMethod("BoxObservable", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("BoxObservable method was not found.");
			s_observableSelect = (MethodInfo?)obj;
		}
		return (MethodInfo)obj;
	}

	private static IObservable<object?> BoxObservable<T>(IObservable<T> source)
	{
		return source.Select((Func<T, object>)((T v) => v));
	}
}
internal class ObservableStreamPlugin<T> : IStreamPlugin
{
	public bool Match(WeakReference<object?> reference)
	{
		if (reference.TryGetTarget(out object target))
		{
			return target is IObservable<T>;
		}
		return false;
	}

	public IObservable<object?> Start(WeakReference<object?> reference)
	{
		if (!reference.TryGetTarget(out object target) || !(target is IObservable<T> source))
		{
			return Observable.Empty<object>();
		}
		if (target is IObservable<object> result)
		{
			return result;
		}
		return source.Select((Func<T, object>)((T x) => x));
	}
}
