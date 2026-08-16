using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Paints an area with an <see cref="P:Avalonia.Media.DrawingBrush.Drawing" />.
/// </summary>
public sealed class DrawingBrush : TileBrush, ISceneBrush, ITileBrush, IBrush
{
	private sealed class RenderDataItem(CompositionRenderData data) : IDisposable
	{
		public bool IsDirty;

		public CompositionRenderData Data { get; } = data;

		public void Dispose()
		{
			Data.Dispose();
		}
	}

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.DrawingBrush.Drawing" /> property.
	/// </summary>
	public static readonly StyledProperty<Drawing?> DrawingProperty = AvaloniaProperty.Register<DrawingBrush, Drawing>("Drawing");

	private InlineDictionary<Compositor, RenderDataItem?> _renderDataDictionary;

	/// <summary>
	/// Gets or sets the visual to draw.
	/// </summary>
	public Drawing? Drawing
	{
		get
		{
			return GetValue(DrawingProperty);
		}
		set
		{
			SetValue(DrawingProperty, value);
		}
	}

	internal override Func<Compositor, ServerCompositionSimpleBrush> Factory => (Compositor c) => new ServerCompositionSimpleContentBrush(c.Server);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.DrawingBrush" /> class.
	/// </summary>
	public DrawingBrush()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.DrawingBrush" /> class.
	/// </summary>
	/// <param name="visual">The visual to draw.</param>
	public DrawingBrush(Drawing visual)
	{
		Drawing = visual;
	}

	ISceneBrushContent? ISceneBrush.CreateContent()
	{
		if (Drawing == null)
		{
			return null;
		}
		using RenderDataDrawingContext renderDataDrawingContext = new RenderDataDrawingContext(null);
		Drawing?.Draw(renderDataDrawingContext);
		return renderDataDrawingContext.GetImmediateSceneBrushContent(this, null, useScalableRasterization: true);
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
				item = new CompositionRenderDataSceneBrushContent.Properties(value.Data.Server, null, UseScalableRasterization: true);
			}
		}
		writer.WriteObject(item);
	}

	private void InvalidateContent()
	{
		foreach (KeyValuePair<Compositor, RenderDataItem> item in _renderDataDictionary)
		{
			RenderDataItem value = item.Value;
			if (value != null)
			{
				value.IsDirty = true;
			}
		}
		RegisterForSerialization();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == DrawingProperty)
		{
			var (drawing, drawing2) = change.GetOldAndNewValue<Drawing>();
			if (drawing != null)
			{
				drawing.Invalidated -= DrawingInvalidated;
			}
			if (drawing2 != null)
			{
				drawing2.Invalidated += DrawingInvalidated;
			}
			InvalidateContent();
		}
		base.OnPropertyChanged(change);
		void DrawingInvalidated(object? sender, EventArgs e)
		{
			InvalidateContent();
		}
	}

	private RenderDataItem? CreateServerContent(Compositor c)
	{
		Drawing drawing = Drawing;
		if (drawing == null)
		{
			return null;
		}
		using RenderDataDrawingContext renderDataDrawingContext = new RenderDataDrawingContext(c);
		drawing.Draw(renderDataDrawingContext);
		CompositionRenderData renderResults = renderDataDrawingContext.GetRenderResults();
		return (renderResults == null) ? null : new RenderDataItem(renderResults);
	}
}
