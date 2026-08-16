using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Stores settings for use in the template of a CommandBarButton
/// </summary>
public sealed class FACommandBarButtonTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarButtonTemplateSettings.Icon" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> IconProperty = FAMenuFlyoutItemTemplateSettings.IconProperty.AddOwner<FACommandBarButtonTemplateSettings>((StyledPropertyMetadata<FAIconElement>)null);

	/// <summary>
	/// Gets the Icon for the CommandBarButton
	/// </summary>
	public FAIconElement Icon
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconElement>(IconProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<FAIconElement>(IconProperty, value, (BindingPriority)0);
		}
	}
}
