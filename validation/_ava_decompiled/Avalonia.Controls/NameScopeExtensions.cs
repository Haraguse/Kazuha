using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.LogicalTree;

namespace Avalonia.Controls;

/// <summary>
/// Extension methods for <see cref="T:Avalonia.Controls.INameScope" />.
/// </summary>
public static class NameScopeExtensions
{
	/// <summary>
	/// Finds a named element in an <see cref="T:Avalonia.Controls.INameScope" />.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="nameScope">The name scope.</param>
	/// <param name="name">The name.</param>
	/// <returns>The named element or null if not found.</returns>
	public static T? Find<T>(this INameScope nameScope, string name) where T : class
	{
		if (nameScope == null)
		{
			throw new ArgumentNullException("nameScope");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		object obj = nameScope.Find(name);
		if (obj == null)
		{
			return null;
		}
		if (obj is T result)
		{
			return result;
		}
		throw new InvalidOperationException($"Expected control '{name}' to be '{typeof(T)} but it was '{obj.GetType()}'.");
	}

	/// <summary>
	/// Finds a named element in an <see cref="T:Avalonia.Controls.INameScope" />.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="anchor">The control to take the name scope from.</param>
	/// <param name="name">The name.</param>
	/// <returns>The named element or null if not found.</returns>
	public static T? Find<T>(this ILogical anchor, string name) where T : class
	{
		if (anchor == null)
		{
			throw new ArgumentNullException("anchor");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (!(anchor is StyledElement styled))
		{
			return null;
		}
		INameScope obj = (anchor as INameScope) ?? NameScope.GetNameScope(styled);
		if (obj == null)
		{
			return null;
		}
		return obj.Find<T>(name);
	}

	/// <summary>
	/// Gets a named element from an <see cref="T:Avalonia.Controls.INameScope" /> or throws if no element of the
	/// requested name was found.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="nameScope">The name scope.</param>
	/// <param name="name">The name.</param>
	/// <returns>The named element.</returns>
	public static T Get<T>(this INameScope nameScope, string name) where T : class
	{
		if (nameScope == null)
		{
			throw new ArgumentNullException("nameScope");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		object obj = nameScope.Find(name);
		if (obj == null)
		{
			throw new KeyNotFoundException("Could not find control '" + name + "'.");
		}
		if (obj is T result)
		{
			return result;
		}
		throw new InvalidOperationException($"Expected control '{name}' to be '{typeof(T)} but it was '{obj.GetType()}'.");
	}

	/// <summary>
	/// Gets a named element from an <see cref="T:Avalonia.Controls.INameScope" /> or throws if no element of the
	/// requested name was found.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="anchor">The control to take the name scope from.</param>
	/// <param name="name">The name.</param>
	/// <returns>The named element.</returns>
	public static T Get<T>(this ILogical anchor, string name) where T : class
	{
		if (anchor == null)
		{
			throw new ArgumentNullException("anchor");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return (((anchor as INameScope) ?? NameScope.GetNameScope((StyledElement)anchor)) ?? throw new InvalidOperationException("The control doesn't have an associated name scope, probably no registrations has been done yet")).Get<T>(name);
	}

	public static INameScope? FindNameScope(this ILogical control)
	{
		if (control == null)
		{
			throw new ArgumentNullException("control");
		}
		return (from x in control.GetSelfAndLogicalAncestors().OfType<StyledElement>()
			select (x as INameScope) ?? NameScope.GetNameScope(x)).FirstOrDefault((INameScope x) => x != null);
	}
}
