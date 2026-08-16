using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal sealed class ServerCompositionSimpleContentBrush : ServerCompositionSimpleTileBrush, ITileBrush, IBrush, ISceneBrush
{
	private CompositionRenderDataSceneBrushContent.Properties? _content;

	internal ServerCompositionSimpleContentBrush(ServerCompositor compositor)
		: base(compositor)
	{
	}

	public ISceneBrushContent? CreateContent()
	{
		if (!(_content == null) && !_content.RenderData.IsDisposed)
		{
			return new CompositionRenderDataSceneBrushContent(this, _content);
		}
		return null;
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionRenderDataSceneBrushContent.Properties properties = reader.ReadObject<CompositionRenderDataSceneBrushContent.Properties>();
		if (_content?.RenderData != properties?.RenderData)
		{
			_content?.RenderData.RemoveObserver(this);
			properties?.RenderData.AddObserver(this);
		}
		_content = properties;
	}

	public override void Dispose()
	{
		_content?.RenderData.RemoveObserver(this);
		base.Dispose();
	}
}
