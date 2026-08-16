using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Represents the visual elements of a NavigationViewItem.
/// </summary>
[PseudoClasses(new string[] { ":expanded" })]
[PseudoClasses(new string[] { ":closedcompacttop", ":notclosedcompacttop" })]
[PseudoClasses(new string[] { ":leftnav", ":topnav", ":topoverflow" })]
[PseudoClasses(new string[] { ":chevronopen", ":chevronclosed", ":chevronhidden" })]
[PseudoClasses(new string[] { ":iconleft", ":icononly", ":contentonly" })]
[PseudoClasses(new string[] { ":pressed" })]
public class FANavigationViewItemPresenter : ContentControl
{
	private Panel _contentGrid;

	private Panel _expandCollapseChevron;

	private Control _selectionIndicator;

	private ContentPresenter _infoBadgePresenter;

	private double _compactPaneLengthValue = 40.0;

	private double _leftIndentation;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FANavigationViewItemPresenter.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FANavigationViewItemPresenter>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FANavigationViewItemPresenter.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FANavigationViewItemPresenterTemplateSettings> TemplateSettingsProperty = AvaloniaProperty.Register<FANavigationViewItemPresenter, FANavigationViewItemPresenterTemplateSettings>("TemplateSettings", (FANavigationViewItemPresenterTemplateSettings)null, false, (BindingMode)1, (Func<FANavigationViewItemPresenterTemplateSettings, bool>)null, (Func<AvaloniaObject, FANavigationViewItemPresenterTemplateSettings, FANavigationViewItemPresenterTemplateSettings>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FANavigationViewItemPresenter.InfoBadge" /> property
	/// </summary>
	public static readonly StyledProperty<FAInfoBadge> InfoBadgeProperty = FANavigationViewItem.InfoBadgeProperty.AddOwner<FANavigationViewItemPresenter>((StyledPropertyMetadata<FAInfoBadge>)null);

	private const string s_tpSelectionIndicator = "SelectionIndicator";

	private const string s_tpPresenterContentRootGrid = "PresenterContentRootGrid";

	private const string s_tpInfoBadgePresenter = "InfoBadgePresenter";

	private const string s_tpExpandCollapseChevron = "ExpandCollapseChevron";

	private const string s_pcClosedCompactTop = ":closedcompacttop";

	private const string s_pcNotClosedCompactTop = ":notclosedcompacttop";

	private const string s_pcExpanded = ":expanded";

	/// <summary>
	/// Gets or sets the icon in a NavigationView item.
	/// </summary>
	public FAIconSource IconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(IconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(IconSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the template settings used in the NavigationViewItemPresenter
	/// </summary>
	public FANavigationViewItemPresenterTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FANavigationViewItemPresenterTemplateSettings>(TemplateSettingsProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<FANavigationViewItemPresenterTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the InfoBadge used in the NavigationViewItemPresenter
	/// </summary>
	public FAInfoBadge InfoBadge
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAInfoBadge>(InfoBadgeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAInfoBadge>(InfoBadgeProperty, value, (BindingPriority)0);
		}
	}

	internal FANavigationViewItem GetNVI => VisualExtensions.FindAncestorOfType<FANavigationViewItem>((Visual)(object)this, false);

	internal Control SelectionIndicator => _selectionIndicator;

	public FANavigationViewItemPresenter()
	{
		TemplateSettings = new FANavigationViewItemPresenterTemplateSettings();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		FANavigationViewItemPresenterTemplateSettings templateSettings = TemplateSettings;
		FAIconSource iconSource = IconSource;
		if (iconSource != null)
		{
			templateSettings.Icon = null;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		_selectionIndicator = (Control)(object)NameScopeExtensions.Find<Border>(e.NameScope, "SelectionIndicator");
		_contentGrid = NameScopeExtensions.Find<Panel>(e.NameScope, "PresenterContentRootGrid");
		_infoBadgePresenter = NameScopeExtensions.Find<ContentPresenter>(e.NameScope, "InfoBadgePresenter");
		FANavigationViewItem getNVI = GetNVI;
		if (getNVI != null)
		{
			_expandCollapseChevron = NameScopeExtensions.Find<Panel>(e.NameScope, "ExpandCollapseChevron");
			if (_expandCollapseChevron != null)
			{
				((InputElement)_expandCollapseChevron).Tapped += getNVI.OnExpandCollapseChevronTapped;
			}
			getNVI.UpdateVisualState();
			FANavigationView getNavigationView = getNVI.GetNavigationView;
			if (getNavigationView != null && getNavigationView.PaneDisplayMode != FANavigationViewPaneDisplayMode.Top)
			{
				UpdateCompactPaneLength(_compactPaneLengthValue, update: true);
			}
		}
		UpdateMargin();
		if (iconSource != null)
		{
			templateSettings.Icon = FAIconHelpers.CreateFromUnknown(iconSource);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			TemplateSettings.Icon = FAIconHelpers.CreateFromUnknown(AvaloniaPropertyChangedExtensions.GetNewValue<FAIconSource>(change));
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((InputElement)this).OnPointerPressed(e);
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
		PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
		if (((PointerPointProperties)(ref properties)).IsLeftButtonPressed)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", true);
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		((Control)this).OnPointerReleased(e);
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)this);
		PointerPointProperties properties = ((PointerPoint)(ref currentPoint)).Properties;
		if ((int)((PointerPointProperties)(ref properties)).PointerUpdateKind == 5 && (int)e.InitialPressMouseButton == 1)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", false);
		}
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		((InputElement)this).OnPointerCaptureLost(e);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":pressed", false);
	}

	internal void RotateExpandCollapseChevron(bool isExpanded)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":expanded", isExpanded);
	}

	internal void UpdateContentLeftIndentation(double leftIndent)
	{
		_leftIndentation = leftIndent;
		UpdateMargin();
	}

	private void UpdateMargin()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (_contentGrid != null)
		{
			Thickness margin = ((Layoutable)_contentGrid).Margin;
			((Layoutable)_contentGrid).Margin = new Thickness(_leftIndentation, ((Thickness)(ref margin)).Top, ((Thickness)(ref margin)).Right, ((Thickness)(ref margin)).Bottom);
		}
	}

	internal void UpdateCompactPaneLength(double len, bool update)
	{
		_compactPaneLengthValue = len;
		if (update)
		{
			TemplateSettings.IconWidth = len;
			TemplateSettings.SmallerIconWidth = len - 8.0;
		}
	}

	internal void UpdateClosedCompactVisualState(bool topLevel, bool isClosedCompact)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":closedcompacttop", isClosedCompact & topLevel);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":notclosedcompacttop", !isClosedCompact & topLevel);
	}
}
