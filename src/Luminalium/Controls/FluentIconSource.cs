using FluentAvalonia.UI.Controls;

namespace Luminalium.Controls;

/// <summary>
///     Fluent Icon 图标源
/// </summary>
public class FluentIconSource : FAFontIconSource
{
    public FluentIconSource()
    {
        FontFamily = GlobalConstants.FluentIconsFontFamily;
    }

    public FluentIconSource(string glyph) : this()
    {
        Glyph = glyph;
    }

    public FluentIconSource ProvideValue()
    {
        return this;
    }
}