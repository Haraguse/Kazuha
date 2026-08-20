using FluentAvalonia.UI.Controls;
using Luminalium.Controls;

namespace Luminalium.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SettingsPageInfo : Attribute
{
    public bool IsSeparator { get; }
    
    public string Name { get; set; } = string.Empty;
    public string Id { get; }
    public string IconGlyph { get; }
    
    public bool UseFullWidth { get; }
    public bool HidePageTitle { get; }

    public SettingsPageInfo(bool isSeparator)
    {
        if (isSeparator)
        {
            IsSeparator = true;
            Id = "separator";
            IconGlyph = "";
            UseFullWidth = false;
            HidePageTitle = false;
        }
        else
        {
            throw new ArgumentException("isSeparator 不可为 false");
        }
    }
    
    public SettingsPageInfo(string id, string iconGlyph = "\uE06F", bool useFullWidth = false, bool hidePageTitle = false)
    {
        IsSeparator = false;
        Id = id;
        IconGlyph = iconGlyph;
        UseFullWidth = useFullWidth;
        HidePageTitle = hidePageTitle;
    }

    public FANavigationViewItemBase ToNavigationViewItemBase()
    {
        if (IsSeparator)
        {
            return new FANavigationViewItemSeparator();
        }

        return new FANavigationViewItem
        {
            IconSource = new FluentIconSource(IconGlyph),
            Content = Name,
            Tag = this
        };
    }
}
