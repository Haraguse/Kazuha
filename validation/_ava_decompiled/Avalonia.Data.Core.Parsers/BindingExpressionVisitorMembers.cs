using System;
using System.Reflection;

namespace Avalonia.Data.Core.Parsers;

/// <summary>
/// Stores reflection members used by <see cref="T:Avalonia.Data.Core.Parsers.BindingExpressionVisitor`1" /> outside of the
/// generic class to avoid duplication for each generic instantiation.
/// </summary>
internal static class BindingExpressionVisitorMembers
{
	public static readonly PropertyInfo AvaloniaObjectIndexer;

	public static readonly MethodInfo CreateDelegateMethod;

	static BindingExpressionVisitorMembers()
	{
		AvaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty("Item", new Type[1] { typeof(AvaloniaProperty) });
		CreateDelegateMethod = typeof(MethodInfo).GetMethod("CreateDelegate", new Type[2]
		{
			typeof(Type),
			typeof(object)
		});
	}
}
