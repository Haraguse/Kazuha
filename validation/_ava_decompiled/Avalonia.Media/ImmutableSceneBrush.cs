using Avalonia.Media.Immutable;

namespace Avalonia.Media;

internal class ImmutableSceneBrush : ImmutableTileBrush
{
	public ImmutableSceneBrush(ITileBrush source)
		: base(source)
	{
	}
}
