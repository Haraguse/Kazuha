using Avalonia.Collections;
using Avalonia.Visuals.Platform;

namespace Avalonia.Media;

public sealed class PathFigures : AvaloniaList<PathFigure>
{
	/// <summary>
	/// Parses the specified path data to a <see cref="T:Avalonia.Media.PathFigures" />.
	/// </summary>
	/// <param name="pathData">The s.</param>
	/// <returns></returns>
	public static PathFigures Parse(string pathData)
	{
		PathGeometry pathGeometry = new PathGeometry();
		using (PathGeometryContext geometryContext = new PathGeometryContext(pathGeometry))
		{
			using PathMarkupParser pathMarkupParser = new PathMarkupParser(geometryContext);
			pathMarkupParser.Parse(pathData);
		}
		return pathGeometry.Figures;
	}
}
