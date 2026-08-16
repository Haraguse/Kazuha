using Avalonia.Platform;

namespace Avalonia.Media;

internal class ImmutableGeometry : Geometry
{
	public ImmutableGeometry(IGeometryImpl? platformImpl)
		: base(platformImpl)
	{
	}

	public override Geometry Clone()
	{
		return new ImmutableGeometry(base.PlatformImpl);
	}

	private protected override IGeometryImpl? CreateDefiningGeometry()
	{
		return base.PlatformImpl;
	}
}
