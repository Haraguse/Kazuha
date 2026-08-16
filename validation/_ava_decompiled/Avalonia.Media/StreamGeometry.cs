using Avalonia.Platform;

namespace Avalonia.Media;

/// <summary>
/// Represents the geometry of an arbitrarily complex shape.
/// </summary>
public class StreamGeometry : Geometry
{
	private IStreamGeometryImpl? _impl;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.StreamGeometry" /> class.
	/// </summary>
	public StreamGeometry()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.StreamGeometry" /> class.
	/// </summary>
	/// <param name="impl">The platform-specific implementation.</param>
	private StreamGeometry(IStreamGeometryImpl impl)
	{
		_impl = impl;
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Media.StreamGeometry" /> from a string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>A <see cref="T:Avalonia.Media.StreamGeometry" />.</returns>
	public new static StreamGeometry Parse(string s)
	{
		StreamGeometry streamGeometry = new StreamGeometry();
		using StreamGeometryContext geometryContext = streamGeometry.Open();
		using PathMarkupParser pathMarkupParser = new PathMarkupParser(geometryContext);
		pathMarkupParser.Parse(s);
		return streamGeometry;
	}

	/// <inheritdoc />
	public override Geometry Clone()
	{
		return new StreamGeometry(((IStreamGeometryImpl)base.PlatformImpl).Clone());
	}

	/// <summary>
	/// Opens the geometry to start defining it.
	/// </summary>
	/// <returns>
	/// A <see cref="T:Avalonia.Media.StreamGeometryContext" /> which can be used to define the geometry.
	/// </returns>
	public StreamGeometryContext Open()
	{
		return new StreamGeometryContext(((IStreamGeometryImpl)base.PlatformImpl).Open());
	}

	/// <inheritdoc />
	private protected override IGeometryImpl? CreateDefiningGeometry()
	{
		if (_impl == null)
		{
			IPlatformRenderInterface requiredService = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
			_impl = requiredService.CreateStreamGeometry();
		}
		return _impl;
	}
}
