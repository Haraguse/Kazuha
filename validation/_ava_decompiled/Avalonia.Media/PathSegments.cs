using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Media;

/// <summary>
/// Represents a collection of <see cref="T:Avalonia.Media.PathSegment" /> objects that can be individually accessed by index.
/// </summary>
public sealed class PathSegments : AvaloniaList<PathSegment>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PathSegments" /> class.
	/// </summary>
	public PathSegments()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.PathSegments" /> class with the specified collection of <see cref="T:Avalonia.Media.PathSegment" /> objects.
	/// </summary>
	/// <param name="collection">The collection of <see cref="T:Avalonia.Media.PathSegment" /> objects that make up the <see cref="T:Avalonia.Media.PathSegments" />.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="collection" /> is <c>null</c>.</exception>
	public PathSegments(IEnumerable<PathSegment> collection)
		: base(collection)
	{
	}

	/// <summary>
	/// Initializes a new instance of the PathSegments class with the specified capacity,
	/// or the number of PathSegment objects the collection is initially capable of storing.
	/// </summary>
	/// <param name="capacity">The number of <see cref="T:Avalonia.Media.PathSegment" /> objects that the collection is initially capable of storing.</param>
	public PathSegments(int capacity)
		: base(capacity)
	{
	}
}
