using Avalonia.Metadata;

namespace Avalonia.Media;

/// <summary>
/// Fills an area with a solid color.
/// </summary>
[NotClientImplementable]
public interface IImmutableSolidColorBrush : ISolidColorBrush, IBrush, IImmutableBrush
{
}
