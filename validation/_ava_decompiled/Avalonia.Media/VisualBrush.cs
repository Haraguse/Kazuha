using System;
using System.Collections.Generic;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Paints an area with an <see cref="P:Avalonia.Media.VisualBrush.Visual" />.
/// </summary>
public sealed class VisualBrush : TileBrush, ISceneBrush, ITileBrush, IBrush
{
	private class RenderDataItem(CompositionRenderData data, Rect rect) : IDisposable
	{
		public bool IsDirty;

		public CompositionRenderData Data { get; } = data;

		public Rect Rect { get; } = rect;

		public void Dispose()
		{
			Data?.Dispose();
		}
	}

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.VisualBrush.Visual" /> property.
	/// </summary>
	public static readonly StyledProperty<Visual?> VisualProperty = AvaloniaProperty.Register<VisualBrush, Visual>("Visual");

	private InlineDictionary<Compositor, RenderDataItem?> _renderDataDictionary;

	/// <summary>
	/// Gets or sets the visual to draw.
	/// </summary>
	public Visual? Visual
	{
		get
		{
			return GetValue(VisualProperty);
		}
		set
		{
			SetValue(VisualProperty, value);
		}
	}

	internal override Func<Compositor, ServerCompositionSimpleBrush> Factory => (Compositor c) => new ServerCompositionSimpleContentBrush(c.Server);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.VisualBrush" /> class.
	/// </summary>
	public VisualBrush()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.VisualBrush" /> class.
	/// </summary>
	/// <param name="visual">The visual to draw.</param>
	public VisualBrush(Visual visual)
	{
		Visual = visual;
	}

	ISceneBrushContent? ISceneBrush.CreateContent()
	{
		if (Visual == null)
		{
			return null;
		}
		if (Visual is IVisualBrushInitialize visualBrushInitialize)
		{
			visualBrushInitialize.EnsureInitialized();
		}
		using RenderDataDrawingContext renderDataDrawingContext = new RenderDataDrawingContext(null);
		ImmediateRenderer.Render(renderDataDrawingContext, Visual);
		return renderDataDrawingContext.GetImmediateSceneBrushContent(this, new Rect(Visual.Bounds.Size), useScalableRasterization: true);
	}

	protected override void OnUnreferencedFromCompositor(Compositor c)
	{
		if (_renderDataDictionary.TryGetAndRemoveValue(c, out RenderDataItem value))
		{
			value?.Dispose();
		}
		base.OnUnreferencedFromCompositor(c);
	}

	private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		base.SerializeChanges(c, writer);
		CompositionRenderDataSceneBrushContent.Properties item = null;
		if (IsOnCompositor(c))
		{
			_renderDataDictionary.TryGetValue(c, out RenderDataItem value);
			if (value == null || value.IsDirty)
			{
				RenderDataItem renderDataItem = CreateServerContent(c);
				value?.Dispose();
				value = (_renderDataDictionary[c] = renderDataItem);
			}
			if (value != null)
			{
				item = new CompositionRenderDataSceneBrushContent.Properties(value.Data.Server, value.Rect, UseScalableRasterization: true);
			}
		}
		writer.WriteObject(item);
	}

	private void InvalidateContent()
	{
		foreach (KeyValuePair<Compositor, RenderDataItem> item in _renderDataDictionary)
		{
			if (item.Value != null)
			{
				item.Value.IsDirty = true;
			}
		}
		RegisterForSerialization();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		InvalidateContent();
		base.OnPropertyChanged(change);
	}

	private RenderDataItem? CreateServerContent(Compositor c)
	{
		if (Visual == null)
		{
			return null;
		}
		if (Visual is IVisualBrushInitialize visualBrushInitialize)
		{
			visualBrushInitialize.EnsureInitialized();
		}
		using RenderDataDrawingContext renderDataDrawingContext = new RenderDataDrawingContext(c);
		ImmediateRenderer.Render(renderDataDrawingContext, Visual);
		CompositionRenderData renderResults = renderDataDrawingContext.GetRenderResults();
		if (renderResults == null)
		{
			return null;
		}
		return new RenderDataItem(renderResults, new Rect(Visual.Bounds.Size));
	}
}
