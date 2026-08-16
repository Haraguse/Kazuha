using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Controls;

/// <summary>
/// Holds a collection of style classes for an <see cref="T:Avalonia.StyledElement" />.
/// </summary>
/// <remarks>
/// Similar to CSS, each control may have any number of styling classes applied.
/// </remarks>
public class Classes : AvaloniaList<string>, IPseudoClasses
{
	private SafeEnumerableHashSet<IClassesChangedListener>? _listeners;

	/// <summary>
	/// Gets the number of listeners subscribed to this collection for unit testing purposes.
	/// </summary>
	internal int ListenerCount => _listeners?.Count ?? 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.Classes" /> class.
	/// </summary>
	public Classes()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.Classes" /> class.
	/// </summary>
	/// <param name="items">The initial items.</param>
	public Classes(IEnumerable<string> items)
		: base(items)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.Classes" /> class.
	/// </summary>
	/// <param name="items">The initial items.</param>
	public Classes(params string[] items)
		: base(items)
	{
	}

	/// <summary>
	/// Parses a classes string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The <see cref="T:Avalonia.Controls.Classes" />.</returns>
	public static Classes Parse(string s)
	{
		return new Classes(s.Split(' '));
	}

	/// <summary>
	/// Adds a style class to the collection.
	/// </summary>
	/// <param name="name">The class name.</param>
	/// <remarks>
	/// Only standard classes may be added via this method. To add pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public override void Add(string name)
	{
		ThrowIfPseudoclass(name, "added");
		if (!Contains(name))
		{
			base.Add(name);
			NotifyChanged();
		}
	}

	/// <summary>
	/// Adds a style classes to the collection.
	/// </summary>
	/// <param name="names">The class names.</param>
	/// <remarks>
	/// Only standard classes may be added via this method. To add pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public override void AddRange(IEnumerable<string> names)
	{
		List<string> list = new List<string>();
		foreach (string name in names)
		{
			ThrowIfPseudoclass(name, "added");
			if (!Contains(name))
			{
				list.Add(name);
			}
		}
		base.AddRange(list);
		NotifyChanged();
	}

	/// <summary>
	/// Removes all non-pseudoclasses from the collection.
	/// </summary>
	public override void Clear()
	{
		for (int num = base.Count - 1; num >= 0; num--)
		{
			if (!base[num].StartsWith(":"))
			{
				RemoveAt(num);
			}
		}
		NotifyChanged();
	}

	/// <summary>
	/// Inserts a style class into the collection.
	/// </summary>
	/// <param name="index">The index to insert the class at.</param>
	/// <param name="name">The class name.</param>
	/// <remarks>
	/// Only standard classes may be added via this method. To add pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public override void Insert(int index, string name)
	{
		ThrowIfPseudoclass(name, "added");
		if (!Contains(name))
		{
			base.Insert(index, name);
			NotifyChanged();
		}
	}

	/// <summary>
	/// Inserts style classes into the collection.
	/// </summary>
	/// <param name="index">The index to insert the class at.</param>
	/// <param name="names">The class names.</param>
	/// <remarks>
	/// Only standard classes may be added via this method. To add pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public override void InsertRange(int index, IEnumerable<string> names)
	{
		List<string> list = null;
		foreach (string name in names)
		{
			ThrowIfPseudoclass(name, "added");
			if (!Contains(name))
			{
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(name);
			}
		}
		if (list != null)
		{
			base.InsertRange(index, list);
			NotifyChanged();
		}
	}

	/// <summary>
	/// Removes a style class from the collection.
	/// </summary>
	/// <param name="name">The class name.</param>
	/// <remarks>
	/// Only standard classes may be removed via this method. To remove pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public override bool Remove(string name)
	{
		ThrowIfPseudoclass(name, "removed");
		if (base.Remove(name))
		{
			NotifyChanged();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Removes style classes from the collection.
	/// </summary>
	/// <param name="names">The class name.</param>
	/// <remarks>
	/// Only standard classes may be removed via this method. To remove pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public override void RemoveAll(IEnumerable<string> names)
	{
		List<string> list = null;
		foreach (string name in names)
		{
			ThrowIfPseudoclass(name, "removed");
			if (list == null)
			{
				list = new List<string>();
			}
			list.Add(name);
		}
		if (list != null)
		{
			base.RemoveAll(list);
			NotifyChanged();
		}
	}

	/// <summary>
	/// Removes a style class from the collection.
	/// </summary>
	/// <param name="index">The index of the class in the collection.</param>
	/// <remarks>
	/// Only standard classes may be removed via this method. To remove pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public override void RemoveAt(int index)
	{
		ThrowIfPseudoclass(base[index], "removed");
		base.RemoveAt(index);
		NotifyChanged();
	}

	/// <summary>
	/// Removes style classes from the collection.
	/// </summary>
	/// <param name="index">The first index to remove.</param>
	/// <param name="count">The number of items to remove.</param>
	public override void RemoveRange(int index, int count)
	{
		base.RemoveRange(index, count);
		NotifyChanged();
	}

	/// <summary>
	/// Removes all non-pseudoclasses in the collection and adds a new set.
	/// </summary>
	/// <param name="source">The new contents of the collection.</param>
	public void Replace(IList<string> source)
	{
		List<string> list = null;
		foreach (string item in source)
		{
			ThrowIfPseudoclass(item, "added");
		}
		using (AvaloniaList<string>.Enumerator enumerator2 = GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				string current = enumerator2.Current;
				if (!current.StartsWith(":"))
				{
					if (list == null)
					{
						list = new List<string>();
					}
					list.Add(current);
				}
			}
		}
		if (list != null)
		{
			base.RemoveAll(list);
		}
		base.AddRange(source);
		NotifyChanged();
	}

	/// <inheritdoc />
	void IPseudoClasses.Add(string name)
	{
		if (!Contains(name))
		{
			base.Add(name);
			NotifyChanged();
		}
	}

	/// <inheritdoc />
	bool IPseudoClasses.Remove(string name)
	{
		if (base.Remove(name))
		{
			NotifyChanged();
			return true;
		}
		return false;
	}

	internal void AddListener(IClassesChangedListener listener)
	{
		(_listeners ?? (_listeners = new SafeEnumerableHashSet<IClassesChangedListener>())).Add(listener);
	}

	internal void RemoveListener(IClassesChangedListener listener)
	{
		_listeners?.Remove(listener);
	}

	private void NotifyChanged()
	{
		if (_listeners == null)
		{
			return;
		}
		foreach (IClassesChangedListener listener in _listeners)
		{
			listener.Changed();
		}
	}

	private static void ThrowIfPseudoclass(string name, string operation)
	{
		if (name.StartsWith(":"))
		{
			throw new ArgumentException($"The pseudoclass '{name}' may only be {operation} by the control itself.");
		}
	}

	/// <summary>
	/// Adds a or removes a  style class to/from the collection.
	/// </summary>
	/// <param name="name">The class names.</param>
	/// <param name="value">If true adds the class, if false, removes it.</param>
	/// <remarks>
	/// Only standard classes may be added or removed via this method. To add pseudoclasses (classes
	/// beginning with a ':' character) use the protected <see cref="P:Avalonia.StyledElement.PseudoClasses" />
	/// property.
	/// </remarks>
	public void Set(string name, bool value)
	{
		if (value)
		{
			if (!Contains(name))
			{
				Add(name);
			}
		}
		else
		{
			Remove(name);
		}
	}
}
