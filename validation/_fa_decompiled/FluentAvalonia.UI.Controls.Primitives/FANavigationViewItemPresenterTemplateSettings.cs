using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Provides settings used in the template of a <see cref="T:FluentAvalonia.UI.Controls.Primitives.FANavigationViewItemPresenter" />
/// </summary>
public class FANavigationViewItemPresenterTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FANavigationViewItemPresenterTemplateSettings.IconWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> IconWidthProperty = AvaloniaProperty.Register<FANavigationViewItemPresenterTemplateSettings, double>("IconWidth", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FANavigationViewItemPresenterTemplateSettings.SmallerIconWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> SmallerIconWidthProperty = AvaloniaProperty.Register<FANavigationViewItemPresenterTemplateSettings, double>("SmallerIconWidth", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FANavigationViewItemPresenterTemplateSettings.Icon" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> IconProperty = FAMenuFlyoutItemTemplateSettings.IconProperty.AddOwner<FANavigationViewItemPresenterTemplateSettings>((StyledPropertyMetadata<FAIconElement>)null);

	/// <summary>
	/// TODO: Get docs from MS - relatively new setting
	/// </summary>
	public double IconWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(IconWidthProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<double>(IconWidthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// TODO: Get docs from MS - relatively new setting
	/// </summary>
	public double SmallerIconWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(SmallerIconWidthProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<double>(SmallerIconWidthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the <see cref="T:FluentAvalonia.UI.Controls.FAIconElement" /> used in the NavigationViewItem
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

	internal FANavigationViewItemPresenterTemplateSettings()
	{
	}
}
