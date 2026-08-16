using System;
using System.Linq;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Styling.Activators;

internal abstract class ContainerQueryActivatorBase : StyleActivatorBase, IStyleActivatorSink
{
	private readonly Visual _visual;

	private readonly string? _containerName;

	private Layoutable? _currentScreenSizeProvider;

	protected Layoutable? CurrentContainer => _currentScreenSizeProvider;

	public ContainerQueryActivatorBase(Visual visual, string? containerName = null)
	{
		_visual = visual;
		_containerName = containerName;
	}

	private void Visual_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
	{
		DeInitializeScreenSizeProvider();
	}

	private void Visual_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
	{
		InitializeScreenSizeProvider();
	}

	void IStyleActivatorSink.OnNext(bool value)
	{
		ReevaluateIsActive();
	}

	protected override void Initialize()
	{
		InitializeScreenSizeProvider();
		_visual.AttachedToVisualTree += Visual_AttachedToVisualTree;
		_visual.DetachedFromVisualTree += Visual_DetachedFromVisualTree;
	}

	protected override void Deinitialize()
	{
		_visual.AttachedToVisualTree -= Visual_AttachedToVisualTree;
		_visual.DetachedFromVisualTree -= Visual_DetachedFromVisualTree;
		DeInitializeScreenSizeProvider();
	}

	private void DeInitializeScreenSizeProvider()
	{
		if (_currentScreenSizeProvider != null)
		{
			VisualQueryProvider queryProvider = Container.GetQueryProvider(_currentScreenSizeProvider);
			if (queryProvider != null)
			{
				queryProvider.WidthChanged -= WidthChanged;
				queryProvider.HeightChanged -= HeightChanged;
				_currentScreenSizeProvider = null;
			}
		}
	}

	private void InitializeScreenSizeProvider()
	{
		if (_currentScreenSizeProvider == null)
		{
			Layoutable container = GetContainer(_visual, _containerName);
			if (container != null)
			{
				VisualQueryProvider queryProvider = Container.GetQueryProvider(container);
				if (queryProvider != null)
				{
					_currentScreenSizeProvider = container;
					queryProvider.WidthChanged += WidthChanged;
					queryProvider.HeightChanged += HeightChanged;
				}
			}
		}
		ReevaluateIsActive();
	}

	internal static Layoutable? GetContainer(Visual visual, string? containerName)
	{
		return (from x in visual.GetVisualAncestors()
			where x is Layoutable layoutable && ((containerName == null && Container.GetSizing(layoutable) != ContainerSizing.Normal) || (containerName != null && Container.GetName(layoutable) == containerName))
			select x).FirstOrDefault() as Layoutable;
	}

	private void HeightChanged(object? sender, EventArgs e)
	{
		ReevaluateIsActive();
	}

	private void WidthChanged(object? sender, EventArgs e)
	{
		ReevaluateIsActive();
	}

	private void OrientationChanged(object? sender, EventArgs e)
	{
		ReevaluateIsActive();
	}
}
