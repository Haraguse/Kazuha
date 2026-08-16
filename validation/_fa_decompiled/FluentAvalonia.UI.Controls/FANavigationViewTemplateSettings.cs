using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent 
/// sources when defining templates for a NavigationView. Not intended for general use.
/// </summary>
public sealed class FANavigationViewTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.BackButtonVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<bool> BackButtonVisibilityProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, bool>("BackButtonVisibility", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.LeftPaneVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<bool> LeftPaneVisibilityProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, bool>("LeftPaneVisibility", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.OverflowButtonVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<bool> OverflowButtonVisibilityProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, bool>("OverflowButtonVisibility", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.PaneToggleButtonVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<bool> PaneToggleButtonVisibilityProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, bool>("PaneToggleButtonVisibility", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.PaneToggleButtonWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> PaneToggleButtonWidthProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, double>("PaneToggleButtonWidth", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.SingleSelectionFollowsFocus" /> property
	/// </summary>
	public static readonly StyledProperty<bool> SingleSelectionFollowsFocusProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, bool>("SingleSelectionFollowsFocus", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.SmallerPaneToggleButtonWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> SmallerPaneToggleButtonWidthProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, double>("SmallerPaneToggleButtonWidth", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.TopPadding" /> property
	/// </summary>
	public static readonly StyledProperty<double> TopPaddingProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, double>("TopPadding", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.TopPaneVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<bool> TopPaneVisibilityProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, bool>("TopPaneVisibility", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FANavigationViewTemplateSettings.OpenPaneWidth" /> property
	/// </summary>
	public static readonly StyledProperty<double> OpenPaneWidthProperty = AvaloniaProperty.Register<FANavigationViewTemplateSettings, double>("OpenPaneWidth", 320.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);

	/// <summary>
	/// Gets the visibility of the back button.
	/// </summary>
	public bool BackButtonVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(BackButtonVisibilityProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<bool>(BackButtonVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the visibility of the left pane.
	/// </summary>
	public bool LeftPaneVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(LeftPaneVisibilityProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<bool>(LeftPaneVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the visibility of the overflow button.
	/// </summary>
	public bool OverflowButtonVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(OverflowButtonVisibilityProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<bool>(OverflowButtonVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the visibility of the pane toggle button.
	/// </summary>
	public bool PaneToggleButtonVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(PaneToggleButtonVisibilityProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<bool>(PaneToggleButtonVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the width of the pane toggle button
	/// </summary>
	public double PaneToggleButtonWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(PaneToggleButtonWidthProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<double>(PaneToggleButtonWidthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the SelectionFollowsFocus value.
	/// </summary>
	public bool SingleSelectionFollowsFocus
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(SingleSelectionFollowsFocusProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<bool>(SingleSelectionFollowsFocusProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// TODO: Relatively new property - need docs from MS
	/// </summary>
	public double SmallerPaneToggleButtonWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(SmallerPaneToggleButtonWidthProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<double>(SmallerPaneToggleButtonWidthProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the padding value of the top pane.
	/// </summary>
	public double TopPadding
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(TopPaddingProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<double>(TopPaddingProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the visibility of the top pane.
	/// </summary>
	public bool TopPaneVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(TopPaneVisibilityProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<bool>(TopPaneVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// TODO: Relatively new property - need docs from MS
	/// </summary>
	public double OpenPaneWidth
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(OpenPaneWidthProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<double>(OpenPaneWidthProperty, value, (BindingPriority)0);
		}
	}

	internal FANavigationViewTemplateSettings()
	{
	}
}
