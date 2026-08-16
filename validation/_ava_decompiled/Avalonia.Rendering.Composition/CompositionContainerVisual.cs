using System;
using Avalonia.Collections.Pooled;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A node in the visual tree that can have children.
/// </summary>
public class CompositionContainerVisual : CompositionVisual
{
	internal const int HitTestAabbTreeThreshold = 32;

	private CompositionHitTestAabbTree? _hitTestChildren;

	public CompositionVisualCollection Children { get; private set; }

	internal new ServerCompositionContainerVisual Server { get; }

	private protected override void OnRootChangedCore()
	{
		foreach (CompositionVisual child in Children)
		{
			child.Root = base.Root;
		}
		base.OnRootChangedCore();
	}

	internal void AddHitTestChild(CompositionVisual child)
	{
		if (_hitTestChildren != null)
		{
			int num = Children.IndexOf(child);
			if (num >= 0)
			{
				_hitTestChildren.Update(child, num);
			}
			UpdateHitTestChildOrder();
		}
	}

	internal void RemoveHitTestChild(CompositionVisual child)
	{
		if (_hitTestChildren != null)
		{
			_hitTestChildren.Remove(child);
			UpdateHitTestChildOrder();
		}
	}

	internal void ClearHitTestChildren()
	{
		_hitTestChildren?.Clear();
	}

	internal bool TryQueryHitTestChildren(Point point, PooledList<CompositionVisual> results)
	{
		if (Children.Count < 32)
		{
			_hitTestChildren = null;
			return false;
		}
		if (_hitTestChildren == null)
		{
			_hitTestChildren = new CompositionHitTestAabbTree(Children);
		}
		_hitTestChildren.Query(point, results, Server.Compositor.Readback.ReadRevision);
		return true;
	}

	internal bool TryQueryFirstHitTestChild(CompositionTarget target, Point point, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter, out CompositionVisual? hit)
	{
		if (Children.Count < 32)
		{
			_hitTestChildren = null;
			hit = null;
			return false;
		}
		if (_hitTestChildren == null)
		{
			_hitTestChildren = new CompositionHitTestAabbTree(Children);
		}
		hit = _hitTestChildren.QueryFirst(target, point, filter, resultFilter, Server.Compositor.Readback.ReadRevision);
		return true;
	}

	private void UpdateHitTestChildOrder()
	{
		if (_hitTestChildren != null)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				_hitTestChildren.UpdateOrder(Children[i], i);
			}
		}
	}

	internal CompositionContainerVisual(Compositor compositor, ServerCompositionContainerVisual server)
		: base(compositor, server)
	{
		Server = server;
		InitializeDefaults();
	}

	private void InitializeDefaults()
	{
		InitializeDefaultsExtra();
	}

	private void InitializeDefaultsExtra()
	{
		Children = new CompositionVisualCollection(this, Server.Children);
	}
}
