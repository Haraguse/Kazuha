using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

internal class CompositionCacheMode : CompositionObject
{
	internal new ServerCompositionCacheMode Server { get; }

	internal CompositionCacheMode(Compositor compositor, ServerCompositionCacheMode server)
		: base(compositor, server)
	{
		Server = server;
		InitializeDefaults();
	}

	private void InitializeDefaults()
	{
	}
}
