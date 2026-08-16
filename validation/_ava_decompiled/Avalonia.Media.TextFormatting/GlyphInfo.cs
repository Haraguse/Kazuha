using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Represents a single glyph.
/// </summary>
public readonly record struct GlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, Vector GlyphOffset = default(Vector))
{
	internal static Comparer<GlyphInfo> ClusterAscendingComparer { get; } = Comparer<GlyphInfo>.Create((GlyphInfo x, GlyphInfo y) => x.GlyphCluster.CompareTo(y.GlyphCluster));

	internal static Comparer<GlyphInfo> ClusterDescendingComparer { get; } = Comparer<GlyphInfo>.Create((GlyphInfo x, GlyphInfo y) => y.GlyphCluster.CompareTo(x.GlyphCluster));
}
