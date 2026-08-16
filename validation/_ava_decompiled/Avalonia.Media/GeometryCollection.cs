using System;
using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Media;

public sealed class GeometryCollection : AvaloniaList<Geometry>
{
	public GeometryGroup? Parent { get; set; }

	public GeometryCollection()
	{
		base.ResetBehavior = ResetBehavior.Remove;
		this.ForEachItem(delegate(Geometry x)
		{
			x.Changed += ChildChanged;
			Parent?.Invalidate();
		}, delegate(Geometry x)
		{
			x.Changed -= ChildChanged;
			Parent?.Invalidate();
		}, delegate
		{
			throw new NotSupportedException();
		});
	}

	public GeometryCollection(IEnumerable<Geometry> items)
		: base(items)
	{
		base.ResetBehavior = ResetBehavior.Remove;
		this.ForEachItem(delegate(Geometry x)
		{
			x.Changed += ChildChanged;
			Parent?.Invalidate();
		}, delegate(Geometry x)
		{
			x.Changed -= ChildChanged;
			Parent?.Invalidate();
		}, delegate
		{
			throw new NotSupportedException();
		});
	}

	private void ChildChanged(object? sender, EventArgs e)
	{
		Parent?.Invalidate();
	}
}
