using System;
using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.LogicalTree;

/// <summary>
/// Provides extension methods for working with the logical tree.
/// </summary>
public static class LogicalExtensions
{
	/// <summary>
	/// Enumerates the ancestors of an <see cref="T:Avalonia.LogicalTree.ILogical" /> in the logical tree.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <returns>The logical's ancestors.</returns>
	public static IEnumerable<ILogical> GetLogicalAncestors(this ILogical logical)
	{
		if (logical == null)
		{
			throw new ArgumentNullException("logical");
		}
		for (ILogical l = logical.LogicalParent; l != null; l = l.LogicalParent)
		{
			yield return l;
		}
	}

	/// <summary>
	/// Enumerates an <see cref="T:Avalonia.LogicalTree.ILogical" /> and its ancestors in the logical tree.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <returns>The logical and its ancestors.</returns>
	public static IEnumerable<ILogical> GetSelfAndLogicalAncestors(this ILogical logical)
	{
		yield return logical;
		foreach (ILogical logicalAncestor in logical.GetLogicalAncestors())
		{
			yield return logicalAncestor;
		}
	}

	/// <summary>
	/// Finds first ancestor of given type.
	/// </summary>
	/// <typeparam name="T">Ancestor type.</typeparam>
	/// <param name="logical">The logical.</param>
	/// <param name="includeSelf">If given logical should be included in search.</param>
	/// <returns>First ancestor of given type.</returns>
	public static T? FindLogicalAncestorOfType<T>(this ILogical? logical, bool includeSelf = false) where T : class
	{
		if (logical == null)
		{
			return null;
		}
		for (ILogical logical2 = (includeSelf ? logical : logical.LogicalParent); logical2 != null; logical2 = logical2.LogicalParent)
		{
			if (logical2 is T result)
			{
				return result;
			}
		}
		return null;
	}

	/// <summary>
	/// Enumerates the children of an <see cref="T:Avalonia.LogicalTree.ILogical" /> in the logical tree.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <returns>The logical children.</returns>
	public static IEnumerable<ILogical> GetLogicalChildren(this ILogical logical)
	{
		return logical.LogicalChildren;
	}

	/// <summary>
	/// Enumerates the descendants of an <see cref="T:Avalonia.LogicalTree.ILogical" /> in the logical tree.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <returns>The logical's ancestors.</returns>
	public static IEnumerable<ILogical> GetLogicalDescendants(this ILogical logical)
	{
		foreach (ILogical child in logical.LogicalChildren)
		{
			yield return child;
			foreach (ILogical logicalDescendant in child.GetLogicalDescendants())
			{
				yield return logicalDescendant;
			}
		}
	}

	/// <summary>
	/// Enumerates an <see cref="T:Avalonia.LogicalTree.ILogical" /> and its descendants in the logical tree.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <returns>The logical and its ancestors.</returns>
	public static IEnumerable<ILogical> GetSelfAndLogicalDescendants(this ILogical logical)
	{
		yield return logical;
		foreach (ILogical logicalDescendant in logical.GetLogicalDescendants())
		{
			yield return logicalDescendant;
		}
	}

	/// <summary>
	/// Finds first descendant of given type.
	/// </summary>
	/// <typeparam name="T">Descendant type.</typeparam>
	/// <param name="logical">The logical.</param>
	/// <param name="includeSelf">If given logical should be included in search.</param>
	/// <returns>First descendant of given type.</returns>
	public static T? FindLogicalDescendantOfType<T>(this ILogical? logical, bool includeSelf = false) where T : class
	{
		if (logical == null)
		{
			return null;
		}
		if (includeSelf && logical is T result)
		{
			return result;
		}
		return FindDescendantOfTypeCore<T>(logical);
	}

	/// <summary>
	/// Gets the logical parent of an <see cref="T:Avalonia.LogicalTree.ILogical" />.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <returns>The parent, or null if the logical is unparented.</returns>
	public static ILogical? GetLogicalParent(this ILogical logical)
	{
		return logical.LogicalParent;
	}

	/// <summary>
	/// Gets the logical parent of an <see cref="T:Avalonia.LogicalTree.ILogical" />.
	/// </summary>
	/// <typeparam name="T">The type of the logical parent.</typeparam>
	/// <param name="logical">The logical.</param>
	/// <returns>
	/// The parent, or null if the logical is unparented or its parent is not of type <typeparamref name="T" />.
	/// </returns>
	public static T? GetLogicalParent<T>(this ILogical logical) where T : class
	{
		return logical.LogicalParent as T;
	}

	/// <summary>
	/// Enumerates the siblings of an <see cref="T:Avalonia.LogicalTree.ILogical" /> in the logical tree.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <returns>The logical siblings.</returns>
	public static IEnumerable<ILogical> GetLogicalSiblings(this ILogical logical)
	{
		ILogical logicalParent = logical.LogicalParent;
		if (logicalParent == null)
		{
			yield break;
		}
		foreach (ILogical logicalChild in logicalParent.LogicalChildren)
		{
			yield return logicalChild;
		}
	}

	/// <summary>
	/// Tests whether an <see cref="T:Avalonia.LogicalTree.ILogical" /> is an ancestor of another logical.
	/// </summary>
	/// <param name="logical">The logical.</param>
	/// <param name="target">The potential descendant.</param>
	/// <returns>
	/// True if <paramref name="logical" /> is an ancestor of <paramref name="target" />;
	/// otherwise false.
	/// </returns>
	public static bool IsLogicalAncestorOf(this ILogical? logical, ILogical? target)
	{
		for (ILogical logical2 = target?.LogicalParent; logical2 != null; logical2 = logical2.LogicalParent)
		{
			if (logical2 == logical)
			{
				return true;
			}
		}
		return false;
	}

	private static T? FindDescendantOfTypeCore<T>(ILogical logical) where T : class
	{
		IAvaloniaReadOnlyList<ILogical> logicalChildren = logical.LogicalChildren;
		int count = logicalChildren.Count;
		for (int i = 0; i < count; i++)
		{
			ILogical logical2 = logicalChildren[i];
			if (logical2 is T result)
			{
				return result;
			}
			T val = FindDescendantOfTypeCore<T>(logical2);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}
}
