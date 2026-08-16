using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class VirtualLayoutContextAdapter : FANonVirtualizingLayoutContext
{
	private class ChildrenCollection : IReadOnlyList<Control>, IEnumerable<Control>, IEnumerable, IReadOnlyCollection<Control>
	{
		private FAVirtualizingLayoutContext _context;

		public int Count => _context.ItemCount;

		public Control this[int index] => _context.GetOrCreateElementAt(index, FAElementRealizationOptions.None);

		public ChildrenCollection(FAVirtualizingLayoutContext context)
		{
			_context = context;
		}

		public IEnumerator<Control> GetEnumerator()
		{
			int ct = Count;
			for (int i = 0; i < ct; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private WeakReference<FAVirtualizingLayoutContext> _virtualizingContext;

	private ChildrenCollection _children;

	protected internal override object LayoutStateCore
	{
		get
		{
			return GetContext()?.LayoutStateCore;
		}
		set
		{
			FAVirtualizingLayoutContext context = GetContext();
			if (context != null)
			{
				context.LayoutStateCore = value;
			}
		}
	}

	public VirtualLayoutContextAdapter(FAVirtualizingLayoutContext context)
	{
		_virtualizingContext = new WeakReference<FAVirtualizingLayoutContext>(context);
	}

	protected override IReadOnlyList<Control> ChildrenCore()
	{
		if (_children == null)
		{
			_children = new ChildrenCollection(GetContext());
		}
		return _children;
	}

	private FAVirtualizingLayoutContext GetContext()
	{
		if (!_virtualizingContext.TryGetTarget(out var target))
		{
			return null;
		}
		return target;
	}
}
