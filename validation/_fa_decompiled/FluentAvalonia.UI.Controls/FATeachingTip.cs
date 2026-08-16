using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.Core;
using FluentAvalonia.Core.Attributes;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// A teaching tip is a notification flyout used to provide contextually relevant information. 
/// It supports rich content (including titles, subtitles, icons, images, and text) and can be 
/// configured for either explicit or light-dismiss.
/// </summary>
[PseudoClasses(new string[] { ":lightDismiss" })]
[PseudoClasses(new string[] { ":actionButton", ":closeButton" })]
[PseudoClasses(new string[] { ":content", ":icon" })]
[PseudoClasses(new string[] { ":footerClose" })]
[PseudoClasses(new string[] { ":heroContentTop", ":heroContentBottom" })]
[PseudoClasses(new string[] { ":top", ":bottom", ":left", ":right", ":center" })]
[PseudoClasses(new string[] { ":topRight", ":topLeft", ":bottomLeft", ":bottomRight" })]
[PseudoClasses(new string[] { ":leftTop", ":leftBottom", ":rightTop", ":rightBottom" })]
[PseudoClasses(new string[] { ":showTitle", ":showSubTitle" })]
[TemplatePart("Container", typeof(Border))]
[TemplatePart("TailOcclusionGrid", typeof(Grid))]
[TemplatePart("ContentRootGrid", typeof(Grid))]
[TemplatePart("NonHeroContentRootGrid", typeof(Grid))]
[TemplatePart("HeroContentBorder", typeof(Border))]
[TemplatePart("ActionButton", typeof(Button))]
[TemplatePart("AlternateCloseButton", typeof(Button))]
[TemplatePart("CloseButton", typeof(Button))]
[TemplatePart("TailPolygon", typeof(Path))]
public class FATeachingTip : ContentControl
{
	private class ScopedBatchHelper
	{
		private DispatcherTimer _timer;

		public Action Completed { get; set; }

		public void Start(TimeSpan duration)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			if (_timer == null)
			{
				_timer = new DispatcherTimer(duration, DispatcherPriority.Background, (EventHandler)Tick);
			}
			_timer.Start();
		}

		private void Tick(object sender, EventArgs args)
		{
			_timer.Stop();
			Completed?.Invoke();
			Completed = null;
		}
	}

	private IDisposable _acceleratorKeyActivatedRevoker;

	private IDisposable _xamlRootChangedRevoker;

	private Border _container;

	private Popup _popup;

	private Popup _lightDismissIndicatorPopup;

	private Control _rootElement;

	private Grid _tailOcclusionGrid;

	private Grid _contentRootGrid;

	private Grid _nonHeroContentRootGrid;

	private Border _heroContentBorder;

	private Button _actionButton;

	private Button _alternateCloseButton;

	private Button _closeButton;

	private Path _tailPolygon;

	private IInputElement _previouslyFocusedElement;

	private KeyFrameAnimation _expandAnimation;

	private KeyFrameAnimation _contractAnimation;

	private IEasing _expandEasingFunction;

	private IEasing _contractEasingFunction;

	private readonly ScopedBatchHelper _scopedBatch = new ScopedBatchHelper();

	private FATeachingTipPlacementMode _currentEffectiveTipPlacementMode;

	private FATeachingTipPlacementMode _currentEffectiveTailPlacementMode;

	private FATeachingTipHeroContentPlacementMode _currentHeroContentEffectivePlacementMode;

	private Rect _currentBoundsInCoreWindowSpace;

	private Rect _currentTargetBoundsInCoreWindowSpace;

	private Size _currentXamlRootSize;

	private bool _ignoreNextIsOpenChanged;

	private bool _isTemplateApplied;

	private bool _createNewPopupOnOpen;

	private bool _repositionOnNextOpen;

	private bool _isExpandAnimationPlaying;

	private bool _isContractAnimationPlaying;

	private bool _returnTopForOutOfWindowPlacement = true;

	private TimeSpan _expandAnimationDuration = TimeSpan.FromMilliseconds(300L);

	private TimeSpan _contractAnimationDuration = TimeSpan.FromMilliseconds(200L);

	private FATeachingTipCloseReason _lastCloseReason = FATeachingTipCloseReason.Programmatic;

	private bool _isIdle = true;

	private Control _target;

	private static readonly string s_ScaleTargetName;

	private static readonly float s_untargetedTipWindowEdgeMargin;

	private static readonly float s_defaultTipHeightAndWidth;

	private static readonly float s_tailOcclusionAmount;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.Title" /> property
	/// </summary>
	public static readonly StyledProperty<string> TitleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.Subtitle" /> property
	/// </summary>
	public static readonly StyledProperty<string> SubtitleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.IsOpen" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsOpenProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.Target" /> property
	/// </summary>
	public static readonly StyledProperty<Control> TargetProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.TailVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<FATeachingTipTailVisibility> TailVisibilityProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.ActionButtonContent" /> property
	/// </summary>
	public static readonly StyledProperty<object> ActionButtonContentProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.ActionButtonStyle" /> property
	/// </summary>
	public static readonly StyledProperty<ControlTheme> ActionButtonStyleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.ActionButtonCommand" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> ActionButtonCommandProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.ActionButtonCommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> ActionButtonCommandParameterProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.CloseButtonContent" /> property
	/// </summary>
	public static readonly StyledProperty<object> CloseButtonContentProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.CloseButtonStyle" /> property
	/// </summary>
	public static readonly StyledProperty<ControlTheme> CloseButtonStyleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.CloseButtonCommand" /> property
	/// </summary>
	public static readonly StyledProperty<ICommand> CloseButtonCommandProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.CloseButtonCommandParameter" /> property
	/// </summary>
	public static readonly StyledProperty<object> CloseButtonCommandParameterProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.PlacementMargin" /> property
	/// </summary>
	public static readonly StyledProperty<Thickness> PlacementMarginProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.ShouldConstrainToRootBounds" /> property
	/// </summary>
	[FANotImplemented]
	public static readonly StyledProperty<bool> ShouldConstrainToRootBoundsProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.IsLightDismissEnabled" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsLightDismissEnabledProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.PreferredPlacement" /> property
	/// </summary>
	public static readonly StyledProperty<FATeachingTipPlacementMode> PreferredPlacementProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.HeroContentPlacement" /> property
	/// </summary>
	public static readonly StyledProperty<FATeachingTipHeroContentPlacementMode> HeroContentPlacementProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.HeroContent" /> property
	/// </summary>
	public static readonly StyledProperty<Control> HeroContentProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FATeachingTipTemplateSettings> TemplateSettingsProperty;

	private const string s_tpContainer = "Container";

	private const string s_tpTailOcclusionGrid = "TailOcclusionGrid";

	private const string s_tpContentRootGrid = "ContentRootGrid";

	private const string s_tpNonHeroContentRootGrid = "NonHeroContentRootGrid";

	private const string s_tpHeroContentBorder = "HeroContentBorder";

	private const string s_tpActionButton = "ActionButton";

	private const string s_tpAlternateCloseButton = "AlternateCloseButton";

	private const string s_tpCloseButton = "CloseButton";

	private const string s_tpTailPolygon = "TailPolygon";

	private const string s_pcContent = ":content";

	private const string s_pcLightDismiss = ":lightDismiss";

	private const string s_pcActionButton = ":actionButton";

	private const string s_pcCloseButton = ":closeButton";

	private const string s_pcFooterClose = ":footerClose";

	private const string s_pcHeroContentTop = ":heroContentTop";

	private const string s_pcHeroContentBottom = ":heroContentBottom";

	private const string s_pcShowTitle = ":showTitle";

	private const string s_pcShowSubTitle = ":showSubTitle";

	private const string s_pcTop = ":top";

	private const string s_pcLeft = ":left";

	private const string s_pcRight = ":right";

	private const string s_pcBottom = ":bottom";

	private const string s_pcTopLeft = ":topLeft";

	private const string s_pcTopRight = ":topRight";

	private const string s_pcBottomLeft = ":bottomLeft";

	private const string s_pcBottomRight = ":bottomRight";

	private const string s_pcLeftTop = ":leftTop";

	private const string s_pcRightTop = ":rightTop";

	private const string s_pcLeftBottom = ":leftBottom";

	private const string s_pcRightBottom = ":rightBottom";

	private const string s_pcCenter = ":center";

	private static readonly string SR_TeachingTipAlternateCloseButtonName;

	private static readonly string SR_TeachingTipAlternateCloseButtonTooltip;

	private const string SR_TeachingTipNotification = "TeachingTipNotification";

	private const string SR_TeachingTipNotificationWithoutAppName = "TeachingTipNotificationWithoutAppName";

	/// <summary>
	/// Gets or sets the title of the teaching tip.
	/// </summary>
	public string Title
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(TitleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(TitleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the subtitle of the teaching tip.
	/// </summary>
	public string Subtitle
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(SubtitleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(SubtitleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the teaching tip is open.
	/// </summary>
	public bool IsOpen
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsOpenProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsOpenProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the target for a teaching tip to position itself relative to and point at with its tail.
	/// </summary>
	public Control Target
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Control>(TargetProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Control>(TargetProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Toggles collapse of a teaching tip's tail. Can be used to override auto behavior 
	/// to make a tail visible on a non-targeted teaching tip and hidden on a targeted teaching tip.
	/// </summary>
	public FATeachingTipTailVisibility TailVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATeachingTipTailVisibility>(TailVisibilityProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FATeachingTipTailVisibility>(TailVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the text of the teaching tip's action button.
	/// </summary>
	public object ActionButtonContent
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(ActionButtonContentProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(ActionButtonContentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the Style (ControlTheme) to apply to the action button.
	/// </summary>
	public ControlTheme ActionButtonStyle
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ControlTheme>(ActionButtonStyleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ControlTheme>(ActionButtonStyleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command to invoke when the action button is clicked.
	/// </summary>
	public ICommand ActionButtonCommand
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(ActionButtonCommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(ActionButtonCommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the action button.
	/// </summary>
	public object ActionButtonCommandParameter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(ActionButtonCommandParameterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(ActionButtonCommandParameterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the content of the teaching tip's close button.
	/// </summary>
	public object CloseButtonContent
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(CloseButtonContentProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(CloseButtonContentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the Style (ControlTheme) to apply to the teaching tip's close button.
	/// </summary>
	public ControlTheme CloseButtonStyle
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ControlTheme>(CloseButtonStyleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ControlTheme>(CloseButtonStyleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the command to invoke when the close button is clicked.
	/// </summary>
	public ICommand CloseButtonCommand
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<ICommand>(CloseButtonCommandProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<ICommand>(CloseButtonCommandProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the close button.
	/// </summary>
	public object CloseButtonCommandParameter
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<object>(CloseButtonCommandParameterProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<object>(CloseButtonCommandParameterProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Adds a margin between a targeted teaching tip and its target or between a non-targeted teaching tip and the xaml root.
	/// </summary>
	public Thickness PlacementMargin
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Thickness>(PlacementMarginProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Thickness>(PlacementMarginProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the teaching tip will constrain to the bounds of its xaml root.
	/// </summary>
	[FANotImplemented]
	public bool ShouldConstrainToRootBounds
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(ShouldConstrainToRootBoundsProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(ShouldConstrainToRootBoundsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Enables light-dismiss functionality so that a teaching tip will dismiss when a user scrolls or 
	/// interacts with other elements of the application.
	/// </summary>
	public bool IsLightDismissEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsLightDismissEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsLightDismissEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Preferred placement to be used for the teaching tip. If there is not enough space 
	/// to show at the preferred placement, a new placement will be automatically chosen. 
	/// Placement is relative to its target if Target is non-null or to the parent window 
	/// of the teaching tip if Target is null.
	/// </summary>
	public FATeachingTipPlacementMode PreferredPlacement
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATeachingTipPlacementMode>(PreferredPlacementProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FATeachingTipPlacementMode>(PreferredPlacementProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Placement of the hero content within the teaching tip.
	/// </summary>
	public FATeachingTipHeroContentPlacementMode HeroContentPlacement
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATeachingTipHeroContentPlacementMode>(HeroContentPlacementProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FATeachingTipHeroContentPlacementMode>(HeroContentPlacementProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Border-to-border graphic content displayed in the header or footer
	/// of the teaching tip. Will appear opposite of the tail in targeted teaching tips unless otherwise set.
	/// </summary>
	public Control HeroContent
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Control>(HeroContentProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Control>(HeroContentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the graphic content to appear alongside the title and subtitle.
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
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining 
	/// templates for a TeachingTip. Not intended for general use.
	/// </summary>
	public FATeachingTipTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FATeachingTipTemplateSettings>(TemplateSettingsProperty);
		}
		private set
		{
			((AvaloniaObject)this).SetValue<FATeachingTipTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs after the action button is clicked.
	/// </summary>
	public event TypedEventHandler<FATeachingTip, EventArgs> ActionButtonClick;

	/// <summary>
	/// Occurs after the close button is clicked.
	/// </summary>
	public event TypedEventHandler<FATeachingTip, EventArgs> CloseButtonClick;

	/// <summary>
	/// Occurs after the tip is closed.
	/// </summary>
	public event TypedEventHandler<FATeachingTip, FATeachingTipClosingEventArgs> Closing;

	/// <summary>
	/// Occurs just before the tip begins to close.
	/// </summary>
	public event TypedEventHandler<FATeachingTip, FATeachingTipClosedEventArgs> Closed;

	/// <summary>
	/// Occurs after the tip is opened
	/// </summary>
	public event TypedEventHandler<FATeachingTip, FATeachingTipOpenedEventArgs> Opened;

	public FATeachingTip()
	{
		((Control)this).Unloaded += ClosePopupOnUnloadEvent;
		TemplateSettings = new FATeachingTipTemplateSettings();
		AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)this, (AvaloniaProperty)(object)AutomationProperties.NameProperty).Subscribe(OnAutomationNameChanged);
		AvaloniaObjectExtensions.GetPropertyChangedObservable((AvaloniaObject)(object)this, (AvaloniaProperty)(object)AutomationProperties.AutomationIdProperty).Subscribe(OnAutomationIdChanged);
		TemplateSettings = new FATeachingTipTemplateSettings();
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new FATeachingTipAutomationPeer(this);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((TemplatedControl)this).OnApplyTemplate(e);
		_acceleratorKeyActivatedRevoker?.Dispose();
		Grid tailOcclusionGrid = _tailOcclusionGrid;
		if (tailOcclusionGrid != null)
		{
			((Control)tailOcclusionGrid).SizeChanged -= OnContentSizeChanged;
		}
		Button closeButton = _closeButton;
		if (closeButton != null)
		{
			closeButton.Click -= OnCloseButtonClicked;
		}
		Button alternateCloseButton = _alternateCloseButton;
		if (alternateCloseButton != null)
		{
			alternateCloseButton.Click -= OnCloseButtonClicked;
		}
		Button actionButton = _actionButton;
		if (actionButton != null)
		{
			actionButton.Click -= OnActionButtonClicked;
		}
		_container = NameScopeExtensions.Get<Border>(e.NameScope, "Container");
		_rootElement = ((Decorator)_container).Child;
		_tailOcclusionGrid = NameScopeExtensions.Get<Grid>(e.NameScope, "TailOcclusionGrid");
		_contentRootGrid = NameScopeExtensions.Get<Grid>(e.NameScope, "ContentRootGrid");
		_nonHeroContentRootGrid = NameScopeExtensions.Get<Grid>(e.NameScope, "NonHeroContentRootGrid");
		_heroContentBorder = NameScopeExtensions.Get<Border>(e.NameScope, "HeroContentBorder");
		_actionButton = NameScopeExtensions.Get<Button>(e.NameScope, "ActionButton");
		_alternateCloseButton = NameScopeExtensions.Get<Button>(e.NameScope, "AlternateCloseButton");
		_closeButton = NameScopeExtensions.Get<Button>(e.NameScope, "CloseButton");
		_tailPolygon = NameScopeExtensions.Get<Path>(e.NameScope, "TailPolygon");
		ToggleVisibilityForEmptyContent(":showTitle", Title);
		ToggleVisibilityForEmptyContent(":showSubTitle", Subtitle);
		((Decorator)_container).Child = null;
		((Control)_tailOcclusionGrid).SizeChanged += OnContentSizeChanged;
		_closeButton.Click += OnCloseButtonClicked;
		_alternateCloseButton.Click += OnCloseButtonClicked;
		AutomationProperties.SetName((StyledElement)(object)_alternateCloseButton, FALocalizationHelper.Instance.GetLocalizedStringResource(SR_TeachingTipAlternateCloseButtonName));
		ToolTip.SetTip((Control)(object)_alternateCloseButton, (object)FALocalizationHelper.Instance.GetLocalizedStringResource(SR_TeachingTipAlternateCloseButtonTooltip));
		_actionButton.Click += OnActionButtonClicked;
		UpdateButtonsState();
		OnIsLightDismissEnabledChanged();
		OnIconSourceChanged();
		OnHeroContentPlacementChanged();
		UpdateButtonAutomationProperties(_actionButton, ActionButtonContent);
		UpdateButtonAutomationProperties(_closeButton, CloseButtonContent);
		_isTemplateApplied = true;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IsOpenProperty)
		{
			OnIsOpenChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)TargetProperty)
		{
			var (val, val2) = AvaloniaPropertyChangedExtensions.GetOldAndNewValue<Control>(change);
			if (val != null)
			{
				val.Unloaded -= ClosePopupOnUnloadEvent;
			}
			if (val2 != null)
			{
				val2.Unloaded += ClosePopupOnUnloadEvent;
			}
			OnTargetChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)PlacementMarginProperty)
		{
			OnPlacementMarginChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)IsLightDismissEnabledProperty)
		{
			OnIsLightDismissEnabledChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)ShouldConstrainToRootBoundsProperty)
		{
			OnShouldConstrainToRootBoundsChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)TailVisibilityProperty)
		{
			OnTailVisibilityChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)PreferredPlacementProperty)
		{
			if (IsOpen)
			{
				PositionPopup();
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)HeroContentPlacementProperty)
		{
			OnHeroContentPlacementChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			OnIconSourceChanged();
		}
		else if (change.Property == (AvaloniaProperty)(object)TitleProperty)
		{
			SetPopupAutomationProperties();
			ToggleVisibilityForEmptyContent(":showTitle", AvaloniaPropertyChangedExtensions.GetNewValue<string>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)SubtitleProperty)
		{
			ToggleVisibilityForEmptyContent(":showSubTitle", AvaloniaPropertyChangedExtensions.GetNewValue<string>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)ActionButtonContentProperty)
		{
			UpdateButtonsState();
			UpdateButtonAutomationProperties(_actionButton, change.NewValue);
		}
		else if (change.Property == (AvaloniaProperty)(object)CloseButtonContentProperty)
		{
			UpdateButtonsState();
			UpdateButtonAutomationProperties(_closeButton, change.NewValue);
		}
		else if (change.Property == (AvaloniaProperty)(object)ContentControl.ContentProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":content", change.NewValue != null);
		}
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "MainContentPresenter")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}

	private void UpdateButtonAutomationProperties(Button button, object obj)
	{
		if (button != null)
		{
			string text = ((obj is string text2) ? text2 : string.Empty);
			AutomationProperties.SetName((StyledElement)(object)button, text);
		}
	}

	private bool ToggleVisibilityForEmptyContent(string visibleStatename, string content)
	{
		bool flag = !string.IsNullOrEmpty(content);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, visibleStatename, flag);
		return flag;
	}

	private void SetPopupAutomationProperties()
	{
		if (_popup != null)
		{
			string text = AutomationProperties.GetName((StyledElement)(object)this);
			if (string.IsNullOrEmpty(text))
			{
				text = Title;
			}
			AutomationProperties.SetName((StyledElement)(object)_popup, text);
			AutomationProperties.SetAutomationId((StyledElement)(object)_popup, AutomationProperties.GetAutomationId((StyledElement)(object)this));
		}
	}

	private void CreateLightDismissIndiatorPopup()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		Popup lightDismissIndicatorPopup = new Popup
		{
			Child = (Control)new Panel
			{
				Width = 0.0,
				Height = 0.0
			},
			WindowManagerAddShadowHint = false,
			IsLightDismissEnabled = true,
			PlacementTarget = (Control)(object)TopLevel.GetTopLevel((Visual)(object)this)
		};
		_lightDismissIndicatorPopup = lightDismissIndicatorPopup;
	}

	private bool UpdateTail()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		(FATeachingTipPlacementMode, bool) tuple = DetermineEffectivePlacement();
		FATeachingTipPlacementMode item = tuple.Item1;
		bool item2 = tuple.Item2;
		_currentEffectiveTailPlacementMode = item;
		FATeachingTipTailVisibility tailVisibility = TailVisibility;
		if (tailVisibility == FATeachingTipTailVisibility.Collapsed || (_target == null && tailVisibility != FATeachingTipTailVisibility.Visible))
		{
			_currentEffectiveTailPlacementMode = FATeachingTipPlacementMode.Auto;
		}
		if (item != _currentEffectiveTipPlacementMode)
		{
			_currentEffectiveTipPlacementMode = item;
		}
		Grid tailOcclusionGrid = _tailOcclusionGrid;
		double num;
		Rect bounds;
		if (tailOcclusionGrid == null)
		{
			num = 0.0;
		}
		else
		{
			bounds = ((Visual)tailOcclusionGrid).Bounds;
			num = ((Rect)(ref bounds)).Height;
		}
		double num2 = num;
		Grid tailOcclusionGrid2 = _tailOcclusionGrid;
		double num3;
		if (tailOcclusionGrid2 == null)
		{
			num3 = 0.0;
		}
		else
		{
			bounds = ((Visual)tailOcclusionGrid2).Bounds;
			num3 = ((Rect)(ref bounds)).Width;
		}
		double num4 = num3;
		getColumnWidths(_tailOcclusionGrid, out var firstColumnWidth, out var secondColumnWidth, out var nextToLastColumnWidth, out var lastColumnWidth);
		getRowWidths(_tailOcclusionGrid, out var firstRowHeight, out var secondRowHeight, out var nextToLastRowHeight, out var lastRowHeight);
		UpdateSizeBasedTemplateSettings();
		switch (_currentEffectiveTailPlacementMode)
		{
		case FATeachingTipPlacementMode.Auto:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 / 2.0, num2 / 2.0);
			UpdateDynamicHeroContentPlacementToTop();
			GoToState((FATeachingTipPlacementMode)(-1));
			break;
		case FATeachingTipPlacementMode.Top:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 / 2.0, num2 - lastRowHeight);
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.Top);
			break;
		case FATeachingTipPlacementMode.Bottom:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 / 2.0, firstRowHeight);
			UpdateDynamicHeroContentPlacementToBottom();
			GoToState(FATeachingTipPlacementMode.Bottom);
			break;
		case FATeachingTipPlacementMode.Left:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 - lastColumnWidth, num2 / 2.0);
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.Left);
			break;
		case FATeachingTipPlacementMode.Right:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, firstColumnWidth, num2 / 2.0);
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.Right);
			break;
		case FATeachingTipPlacementMode.TopRight:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, firstColumnWidth + secondColumnWidth + 1.0, num2 - lastRowHeight);
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.TopRight);
			break;
		case FATeachingTipPlacementMode.TopLeft:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 - (nextToLastColumnWidth + lastColumnWidth + 1.0), num2 - lastRowHeight);
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.TopLeft);
			break;
		case FATeachingTipPlacementMode.BottomRight:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, firstColumnWidth + secondColumnWidth + 1.0, firstRowHeight);
			UpdateDynamicHeroContentPlacementToBottom();
			GoToState(FATeachingTipPlacementMode.BottomRight);
			break;
		case FATeachingTipPlacementMode.BottomLeft:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 - (nextToLastColumnWidth + lastColumnWidth + 1.0), firstRowHeight);
			UpdateDynamicHeroContentPlacementToBottom();
			GoToState(FATeachingTipPlacementMode.BottomLeft);
			break;
		case FATeachingTipPlacementMode.LeftTop:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 - lastColumnWidth, num2 - (nextToLastRowHeight + lastRowHeight + 1.0));
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.LeftTop);
			break;
		case FATeachingTipPlacementMode.LeftBottom:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 - lastColumnWidth, firstRowHeight + secondRowHeight + 1.0);
			UpdateDynamicHeroContentPlacementToBottom();
			GoToState(FATeachingTipPlacementMode.LeftBottom);
			break;
		case FATeachingTipPlacementMode.RightTop:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, firstColumnWidth, num2 - (nextToLastRowHeight + lastRowHeight + 1.0));
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.RightTop);
			break;
		case FATeachingTipPlacementMode.RightBottom:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, firstColumnWidth, firstRowHeight + secondRowHeight + 1.0);
			UpdateDynamicHeroContentPlacementToBottom();
			GoToState(FATeachingTipPlacementMode.RightBottom);
			break;
		case FATeachingTipPlacementMode.Center:
			TrySetCenterPoint((Control)(object)_tailOcclusionGrid, num4 / 2.0, num2 - lastRowHeight);
			UpdateDynamicHeroContentPlacementToTop();
			GoToState(FATeachingTipPlacementMode.Center);
			break;
		}
		return item2;
		void GoToState(FATeachingTipPlacementMode mode)
		{
			switch (mode)
			{
			case (FATeachingTipPlacementMode)(-1):
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.Top:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.Bottom:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.Left:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.Right:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.TopRight:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.TopLeft:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.BottomRight:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.BottomLeft:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.LeftTop:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.LeftBottom:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.RightTop:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", true);
				break;
			case FATeachingTipPlacementMode.RightBottom:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			case FATeachingTipPlacementMode.Center:
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":top", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":left", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":right", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":center", true);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":topLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomLeft", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":bottomRight", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftTop", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":leftBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightBottom", false);
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":rightTop", false);
				break;
			}
		}
		static void getColumnWidths(Grid g, out double reference, out double reference2, out double reference3, out double reference4)
		{
			reference = (reference2 = (reference3 = (reference4 = 0.0)));
			if (g != null && g.ColumnDefinitions != null)
			{
				int count = ((AvaloniaList<ColumnDefinition>)(object)g.ColumnDefinitions).Count;
				reference = ((count > 0) ? ((AvaloniaList<ColumnDefinition>)(object)g.ColumnDefinitions)[0].ActualWidth : 0.0);
				reference2 = ((count > 1) ? ((AvaloniaList<ColumnDefinition>)(object)g.ColumnDefinitions)[1].ActualWidth : 0.0);
				reference3 = ((count > 1) ? ((AvaloniaList<ColumnDefinition>)(object)g.ColumnDefinitions)[count - 2].ActualWidth : 0.0);
				reference4 = ((count > 0) ? ((AvaloniaList<ColumnDefinition>)(object)g.ColumnDefinitions)[count - 1].ActualWidth : 0.0);
			}
		}
		static void getRowWidths(Grid g, out double reference, out double reference2, out double reference3, out double reference4)
		{
			reference = (reference2 = (reference3 = (reference4 = 0.0)));
			if (g != null && g.RowDefinitions != null)
			{
				int count = ((AvaloniaList<RowDefinition>)(object)g.RowDefinitions).Count;
				reference = ((count > 0) ? ((AvaloniaList<RowDefinition>)(object)g.RowDefinitions)[0].ActualHeight : 0.0);
				reference2 = ((count > 1) ? ((AvaloniaList<RowDefinition>)(object)g.RowDefinitions)[1].ActualHeight : 0.0);
				reference3 = ((count > 1) ? ((AvaloniaList<RowDefinition>)(object)g.RowDefinitions)[count - 2].ActualHeight : 0.0);
				reference4 = ((count > 0) ? ((AvaloniaList<RowDefinition>)(object)g.RowDefinitions)[count - 1].ActualHeight : 0.0);
			}
		}
	}

	private void PositionPopup()
	{
		bool flag = false;
		if ((_target == null) ? PositionUntargetedPopup() : PositionTargetedPopup())
		{
			IsOpen = false;
		}
	}

	private bool PositionTargetedPopup()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		bool result = UpdateTail();
		Thickness placementMargin = PlacementMargin;
		double num;
		double num2;
		if (_tailOcclusionGrid == null)
		{
			num = 0.0;
			num2 = 0.0;
		}
		else
		{
			Rect bounds = ((Visual)_tailOcclusionGrid).Bounds;
			double height = ((Rect)(ref bounds)).Height;
			bounds = ((Visual)_tailOcclusionGrid).Bounds;
			double width = ((Rect)(ref bounds)).Width;
			num2 = width;
			num = height;
		}
		if (_popup != null)
		{
			switch (_currentEffectiveTipPlacementMode)
			{
			case FATeachingTipPlacementMode.Top:
				_popup.VerticalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y - num - ((Thickness)(ref placementMargin)).Top;
				_popup.HorizontalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width - num2) / 2.0;
				break;
			case FATeachingTipPlacementMode.Bottom:
				_popup.VerticalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height + ((Thickness)(ref placementMargin)).Bottom;
				_popup.HorizontalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width - num2) / 2.0;
				break;
			case FATeachingTipPlacementMode.Left:
				_popup.VerticalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height - num) / 2.0;
				_popup.HorizontalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X - num2 - ((Thickness)(ref placementMargin)).Left;
				break;
			case FATeachingTipPlacementMode.Right:
				_popup.VerticalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height - num) / 2.0;
				_popup.HorizontalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width + ((Thickness)(ref placementMargin)).Right;
				break;
			case FATeachingTipPlacementMode.TopRight:
				_popup.VerticalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y - num - ((Thickness)(ref placementMargin)).Top;
				_popup.HorizontalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width) / 2.0 - MinimumTipEdgeToTailCenter();
				break;
			case FATeachingTipPlacementMode.TopLeft:
				_popup.VerticalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y - num - ((Thickness)(ref placementMargin)).Top;
				_popup.HorizontalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width) / 2.0 - num2 + MinimumTipEdgeToTailCenter();
				break;
			case FATeachingTipPlacementMode.BottomRight:
				_popup.VerticalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height + ((Thickness)(ref placementMargin)).Bottom;
				_popup.HorizontalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width) / 2.0 - MinimumTipEdgeToTailCenter();
				break;
			case FATeachingTipPlacementMode.BottomLeft:
				_popup.VerticalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height + ((Thickness)(ref placementMargin)).Bottom;
				_popup.HorizontalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width) / 2.0 - num2 + MinimumTipEdgeToTailCenter();
				break;
			case FATeachingTipPlacementMode.LeftTop:
				_popup.VerticalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height) / 2.0 - num + MinimumTipEdgeToTailCenter();
				_popup.HorizontalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X - num2 - ((Thickness)(ref placementMargin)).Left;
				break;
			case FATeachingTipPlacementMode.LeftBottom:
				_popup.VerticalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height) / 2.0 - MinimumTipEdgeToTailCenter();
				_popup.HorizontalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X - num2 - ((Thickness)(ref placementMargin)).Left;
				break;
			case FATeachingTipPlacementMode.RightTop:
				_popup.VerticalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height) / 2.0 - num + MinimumTipEdgeToTailCenter();
				_popup.HorizontalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width + ((Thickness)(ref placementMargin)).Right;
				break;
			case FATeachingTipPlacementMode.RightBottom:
				_popup.VerticalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height) / 2.0 - MinimumTipEdgeToTailCenter();
				_popup.HorizontalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width + ((Thickness)(ref placementMargin)).Right;
				break;
			case FATeachingTipPlacementMode.Center:
				_popup.VerticalOffset = ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height / 2.0 - num - ((Thickness)(ref placementMargin)).Top;
				_popup.HorizontalOffset = (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X * 2.0 + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width - num2) / 2.0;
				break;
			default:
				throw new Exception("Invalid TeachingTipPlacementMode");
			}
		}
		return result;
	}

	private bool PositionUntargetedPopup()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		Rect effectiveWindowBoundsInCoreWindowSpace = GetEffectiveWindowBoundsInCoreWindowSpace(GetWindowBounds());
		double tipSize;
		double tipSize2;
		if (_tailOcclusionGrid == null)
		{
			tipSize = 0.0;
			tipSize2 = 0.0;
		}
		else
		{
			Rect bounds = ((Visual)_tailOcclusionGrid).Bounds;
			double height = ((Rect)(ref bounds)).Height;
			bounds = ((Visual)_tailOcclusionGrid).Bounds;
			double width = ((Rect)(ref bounds)).Width;
			tipSize2 = width;
			tipSize = height;
		}
		bool result = UpdateTail();
		Thickness placementMargin = PlacementMargin;
		if (_popup != null)
		{
			switch (GetFlowDirectionAdjustedPlacement(PreferredPlacement))
			{
			case FATeachingTipPlacementMode.Auto:
			case FATeachingTipPlacementMode.Bottom:
				_popup.VerticalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipCenterPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Left, ((Thickness)(ref placementMargin)).Right);
				break;
			case FATeachingTipPlacementMode.Top:
				_popup.VerticalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Thickness)(ref placementMargin)).Top);
				_popup.HorizontalOffset = UntargetedTipCenterPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Left, ((Thickness)(ref placementMargin)).Right);
				break;
			case FATeachingTipPlacementMode.Left:
				_popup.VerticalOffset = UntargetedTipCenterPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Top, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Thickness)(ref placementMargin)).Left);
				break;
			case FATeachingTipPlacementMode.Right:
				_popup.VerticalOffset = UntargetedTipCenterPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Top, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Right);
				break;
			case FATeachingTipPlacementMode.TopRight:
				_popup.VerticalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Thickness)(ref placementMargin)).Top);
				_popup.HorizontalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Right);
				break;
			case FATeachingTipPlacementMode.TopLeft:
				_popup.VerticalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Thickness)(ref placementMargin)).Top);
				_popup.HorizontalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Thickness)(ref placementMargin)).Left);
				break;
			case FATeachingTipPlacementMode.BottomRight:
				_popup.VerticalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Right);
				break;
			case FATeachingTipPlacementMode.BottomLeft:
				_popup.VerticalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Thickness)(ref placementMargin)).Left);
				break;
			case FATeachingTipPlacementMode.LeftTop:
				_popup.VerticalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Thickness)(ref placementMargin)).Top);
				_popup.HorizontalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Thickness)(ref placementMargin)).Left);
				break;
			case FATeachingTipPlacementMode.LeftBottom:
				_popup.VerticalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Thickness)(ref placementMargin)).Left);
				break;
			case FATeachingTipPlacementMode.RightTop:
				_popup.VerticalOffset = UntargetedTipNearPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Thickness)(ref placementMargin)).Top);
				_popup.HorizontalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Right);
				break;
			case FATeachingTipPlacementMode.RightBottom:
				_popup.VerticalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipFarPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Right);
				break;
			case FATeachingTipPlacementMode.Center:
				_popup.VerticalOffset = UntargetedTipCenterPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Y, ((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height, tipSize, ((Thickness)(ref placementMargin)).Top, ((Thickness)(ref placementMargin)).Bottom);
				_popup.HorizontalOffset = UntargetedTipCenterPlacementOffset(((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).X, ((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width, tipSize2, ((Thickness)(ref placementMargin)).Left, ((Thickness)(ref placementMargin)).Right);
				break;
			default:
				throw new Exception("Invalid TeachingTipPlacementMode");
			}
		}
		return result;
	}

	private void UpdateSizeBasedTemplateSettings()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		FATeachingTipTemplateSettings templateSettings = TemplateSettings;
		double width;
		double height;
		if (_contentRootGrid == null)
		{
			width = 0.0;
			height = 0.0;
		}
		else
		{
			Rect bounds = ((Visual)_contentRootGrid).Bounds;
			double width2 = ((Rect)(ref bounds)).Width;
			bounds = ((Visual)_contentRootGrid).Bounds;
			double height2 = ((Rect)(ref bounds)).Height;
			height = height2;
			width = width2;
		}
		switch (_currentEffectiveTailPlacementMode)
		{
		case FATeachingTipPlacementMode.Top:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.Bottom:
			templateSettings.TopRightHighlightMargin = BottomPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = BottomPlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.Left:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = LeftEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.Right:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = RightEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.TopLeft:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.TopRight:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.BottomLeft:
			templateSettings.TopRightHighlightMargin = BottomLeftPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = BottomLeftPlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.BottomRight:
			templateSettings.TopRightHighlightMargin = BottomRightPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = BottomRightPlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.LeftTop:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = LeftEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.LeftBottom:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = LeftEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.RightTop:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = RightEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.RightBottom:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = RightEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.Auto:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		case FATeachingTipPlacementMode.Center:
			templateSettings.TopRightHighlightMargin = OtherPlacementTopRightHighlightMargin(width, height);
			templateSettings.TopLeftHighlightMargin = TopEdgePlacementTopLeftHighlightMargin(width, height);
			break;
		}
	}

	private void UpdateButtonsState()
	{
		object actionButtonContent = ActionButtonContent;
		object closeButtonContent = CloseButtonContent;
		bool isLightDismissEnabled = IsLightDismissEnabled;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":actionButton", actionButtonContent != null);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":closeButton", closeButtonContent != null);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":footerClose", isLightDismissEnabled || closeButtonContent != null);
	}

	private void UpdateDynamicHeroContentPlacementToTop()
	{
		if (HeroContentPlacement == FATeachingTipHeroContentPlacementMode.Auto)
		{
			UpdateDynamicHeroContentPlacementToTopImpl();
		}
	}

	private void UpdateDynamicHeroContentPlacementToTopImpl()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":heroContentTop", true);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":heroContentBottom", false);
		if (_currentHeroContentEffectivePlacementMode != FATeachingTipHeroContentPlacementMode.Top)
		{
			_currentHeroContentEffectivePlacementMode = FATeachingTipHeroContentPlacementMode.Top;
		}
	}

	private void UpdateDynamicHeroContentPlacementToBottom()
	{
		if (HeroContentPlacement == FATeachingTipHeroContentPlacementMode.Auto)
		{
			UpdateDynamicHeroContentPlacementToBottomImpl();
		}
	}

	private void UpdateDynamicHeroContentPlacementToBottomImpl()
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":heroContentTop", false);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":heroContentBottom", true);
		if (_currentHeroContentEffectivePlacementMode != FATeachingTipHeroContentPlacementMode.Bottom)
		{
			_currentHeroContentEffectivePlacementMode = FATeachingTipHeroContentPlacementMode.Bottom;
		}
	}

	private void OnIsOpenChanged()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (_ignoreNextIsOpenChanged)
		{
			_ignoreNextIsOpenChanged = false;
			return;
		}
		Dispatcher.UIThread.Post((Action)delegate
		{
			if (_isIdle)
			{
				if (IsOpen)
				{
					IsOpenChangedToOpen();
				}
				else
				{
					IsOpenChangedToClose();
				}
			}
			else
			{
				_ignoreNextIsOpenChanged = true;
				IsOpen = !IsOpen;
			}
		}, DispatcherPriority.Render);
	}

	private void IsOpenChangedToOpen()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		_lastCloseReason = FATeachingTipCloseReason.Programmatic;
		Rect val = ((Visual)this).Bounds;
		val = new Rect(((Rect)(ref val)).Size);
		_currentBoundsInCoreWindowSpace = ((Rect)(ref val)).TransformToAABB((Matrix)(((_003F?)VisualExtensions.TransformToVisual((Visual)(object)this, (Visual)(object)TopLevel.GetTopLevel((Visual)(object)this))) ?? Matrix.Identity));
		if (_target != null)
		{
			SetViewportChangedEvent(_target);
			val = ((Visual)_target).Bounds;
			val = new Rect(((Rect)(ref val)).Size);
			_currentTargetBoundsInCoreWindowSpace = ((Rect)(ref val)).TransformToAABB((Matrix)(((_003F?)VisualExtensions.TransformToVisual((Visual)(object)_target, (Visual)(object)TopLevel.GetTopLevel((Visual)(object)_target))) ?? Matrix.Identity));
		}
		else
		{
			_currentTargetBoundsInCoreWindowSpace = default(Rect);
		}
		if (_lightDismissIndicatorPopup == null)
		{
			CreateLightDismissIndiatorPopup();
		}
		OnIsLightDismissEnabledChanged();
		if (_contractAnimation == null)
		{
			CreateContractAnimation();
		}
		if (_expandAnimation == null)
		{
			CreateExpandAnimation();
		}
		if (!_isTemplateApplied)
		{
			((Layoutable)this).ApplyTemplate();
		}
		if (_popup == null || _createNewPopupOnOpen)
		{
			CreateNewPopup();
		}
		if (DetermineEffectivePlacement().Item2)
		{
			RaiseClosingEvent(attachDeferralCompletedHandler: false);
			FATeachingTipClosedEventArgs args = new FATeachingTipClosedEventArgs(_lastCloseReason);
			Closed?.Invoke(this, args);
			IsOpen = false;
		}
		else if (_popup != null)
		{
			((ISetLogicalParent)_popup).SetParent((ILogical)(((object)_target) ?? ((object)TopLevel.GetTopLevel((Visual)(object)this))));
			if (_repositionOnNextOpen)
			{
				_repositionOnNextOpen = false;
				PositionPopup();
			}
			if (!_popup.IsOpen)
			{
				SetIsIdle(idle: false);
				_popup.Child = _rootElement;
				Popup lightDismissIndicatorPopup = _lightDismissIndicatorPopup;
				if (lightDismissIndicatorPopup != null)
				{
					lightDismissIndicatorPopup.IsOpen = true;
				}
				_popup.IsOpen = true;
				if (FAUISettings.AreAnimationsEnabled())
				{
					StartExpandToOpen();
				}
				else
				{
					SetIsIdle(idle: true);
					Opened?.Invoke(this, new FATeachingTipOpenedEventArgs());
				}
			}
			else if (!_isExpandAnimationPlaying && !_isContractAnimationPlaying)
			{
				SetIsIdle(idle: true);
			}
		}
		if (((Visual)this).VisualRoot != null)
		{
			_acceleratorKeyActivatedRevoker = InteractiveExtensions.AddDisposableHandler<KeyEventArgs>((Interactive)(object)TopLevel.GetTopLevel((Visual)(object)this), InputElement.KeyDownEvent, (EventHandler<KeyEventArgs>)OnF6PreviewKeyDownClicked, (RoutingStrategies)2, false);
		}
		OnIsLightDismissEnabledChanged();
	}

	private void IsOpenChangedToClose()
	{
		if (_popup != null)
		{
			if (_popup.IsOpen)
			{
				SetIsIdle(idle: false);
				RaiseClosingEvent(attachDeferralCompletedHandler: true);
			}
			else if (!_isExpandAnimationPlaying && !_isContractAnimationPlaying)
			{
				SetIsIdle(idle: true);
			}
			((ISetLogicalParent)_popup).SetParent((ILogical)null);
		}
		_acceleratorKeyActivatedRevoker?.Dispose();
		_currentEffectiveTipPlacementMode = FATeachingTipPlacementMode.Auto;
	}

	private void CreateNewPopup()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		if (_popup != null)
		{
			_popup.Opened -= OnPopupOpened;
			_popup.Closed -= OnPopupClosed;
		}
		Popup val = new Popup
		{
			WindowManagerAddShadowHint = false,
			IsLightDismissEnabled = false,
			PlacementTarget = (Control)(object)TopLevel.GetTopLevel((Visual)(object)this),
			Placement = (PlacementMode)6,
			PlacementAnchor = (PopupAnchor)5,
			PlacementGravity = (PopupGravity)10
		};
		val.Opened += OnPopupOpened;
		val.Closed += OnPopupClosed;
		_popup = val;
		SetPopupAutomationProperties();
		_createNewPopupOnOpen = false;
	}

	private void OnTailVisibilityChanged()
	{
		UpdateTail();
	}

	private void OnIconSourceChanged()
	{
		FATeachingTipTemplateSettings templateSettings = TemplateSettings;
		FAIconSource iconSource = IconSource;
		if (iconSource != null)
		{
			templateSettings.IconElement = FAIconHelpers.CreateFromUnknown(iconSource);
		}
		else
		{
			templateSettings.IconElement = null;
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", iconSource != null);
	}

	private void OnPlacementMarginChanged()
	{
		if (IsOpen)
		{
			PositionPopup();
		}
	}

	private void OnIsLightDismissEnabledChanged()
	{
		bool isLightDismissEnabled = IsLightDismissEnabled;
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":lightDismiss", isLightDismissEnabled);
		if (isLightDismissEnabled)
		{
			if (_lightDismissIndicatorPopup != null)
			{
				_lightDismissIndicatorPopup.IsLightDismissEnabled = true;
				_lightDismissIndicatorPopup.Closed += OnLightDismissIndicatorPopupClosed;
			}
		}
		else if (_lightDismissIndicatorPopup != null)
		{
			_lightDismissIndicatorPopup.IsLightDismissEnabled = false;
			_lightDismissIndicatorPopup.Closed -= OnLightDismissIndicatorPopupClosed;
		}
		UpdateButtonsState();
	}

	private void OnShouldConstrainToRootBoundsChanged()
	{
		if (_popup != null)
		{
			_createNewPopupOnOpen = true;
		}
	}

	private void OnHeroContentPlacementChanged()
	{
		switch (HeroContentPlacement)
		{
		case FATeachingTipHeroContentPlacementMode.Top:
			UpdateDynamicHeroContentPlacementToTopImpl();
			break;
		case FATeachingTipHeroContentPlacementMode.Bottom:
			UpdateDynamicHeroContentPlacementToBottomImpl();
			break;
		}
		_currentEffectiveTipPlacementMode = FATeachingTipPlacementMode.Auto;
		if (IsOpen)
		{
			PositionPopup();
		}
	}

	private void OnContentSizeChanged(object sender, SizeChangedEventArgs args)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		UpdateSizeBasedTemplateSettings();
		_currentEffectiveTipPlacementMode = FATeachingTipPlacementMode.Auto;
		if (IsOpen)
		{
			PositionPopup();
		}
		Size newSize = args.NewSize;
		float num = (float)((Size)(ref newSize)).Width;
		newSize = args.NewSize;
		float num2 = (float)((Size)(ref newSize)).Height;
		if (_expandAnimation != null)
		{
			((CompositionAnimation)_expandAnimation).SetScalarParameter("Width", num);
			((CompositionAnimation)_expandAnimation).SetScalarParameter("Height", num2);
		}
		if (_contractAnimation != null)
		{
			((CompositionAnimation)_contractAnimation).SetScalarParameter("Width", num);
			((CompositionAnimation)_contractAnimation).SetScalarParameter("Height", num2);
		}
	}

	private void OnF6PreviewKeyDownClicked(object sender, KeyEventArgs args)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		if (!((RoutedEventArgs)args).Handled && IsOpen && (int)args.Key == 95)
		{
			((RoutedEventArgs)args).Handled = HandleF6Clicked();
		}
	}

	private bool HandleF6Clicked(bool fromPopup = false)
	{
		if (hasFocusInSubtree() & fromPopup)
		{
			if (_previouslyFocusedElement != null)
			{
				_previouslyFocusedElement.Focus((NavigationMethod)0, (KeyModifiers)0);
				_previouslyFocusedElement = null;
				return true;
			}
		}
		else if (!hasFocusInSubtree() && !fromPopup)
		{
			Button closeButton = _closeButton;
			Button val = _alternateCloseButton;
			Button val2 = closeButton;
			Button val3 = null;
			if (CloseButtonContent == null)
			{
				Button alternateCloseButton = _alternateCloseButton;
				val = _closeButton;
				val2 = alternateCloseButton;
			}
			if (val2 != null && ((Visual)val2).IsVisible)
			{
				val3 = val2;
			}
			else if (val != null && ((Visual)val).IsVisible)
			{
				val3 = val;
			}
			if (val3 != null)
			{
				_previouslyFocusedElement = TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement();
				((InputElement)val3).Focus((NavigationMethod)2, (KeyModifiers)0);
				return true;
			}
		}
		return false;
		bool hasFocusInSubtree()
		{
			if (_rootElement != null)
			{
				IInputElement focusedElement = TopLevel.GetTopLevel((Visual)(object)this).FocusManager.GetFocusedElement();
				for (Visual val4 = (Visual)(object)((focusedElement is Visual) ? focusedElement : null); val4 != null; val4 = VisualExtensions.GetVisualParent(val4))
				{
					if ((object)val4 == _rootElement)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	private void OnAutomationNameChanged(AvaloniaPropertyChangedEventArgs args)
	{
		SetPopupAutomationProperties();
	}

	private void OnAutomationIdChanged(AvaloniaPropertyChangedEventArgs args)
	{
		SetPopupAutomationProperties();
	}

	private void OnCloseButtonClicked(object sender, RoutedEventArgs e)
	{
		CloseButtonClick?.Invoke(this, EventArgs.Empty);
		_lastCloseReason = FATeachingTipCloseReason.CloseButton;
		IsOpen = false;
	}

	private void OnActionButtonClicked(object sender, RoutedEventArgs e)
	{
		ActionButtonClick?.Invoke(this, EventArgs.Empty);
	}

	private void OnPopupOpened(object sender, EventArgs args)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		if (topLevel != null)
		{
			_currentXamlRootSize = topLevel.ClientSize;
			_xamlRootChangedRevoker = AvaloniaObjectExtensions.GetObservable<Rect>((AvaloniaObject)(object)topLevel, (AvaloniaProperty<Rect>)(object)Visual.BoundsProperty).Subscribe(XamlRootChanged);
			if (ControlAutomationPeer.FromElement((Control)(object)this) is FATeachingTipAutomationPeer fATeachingTipAutomationPeer)
			{
				fATeachingTipAutomationPeer.RaiseWindowOpenedEvent();
			}
		}
		if (IsLightDismissEnabled)
		{
			IInputElement obj = FocusManager.FindFirstFocusableElement((IInputElement)(object)_rootElement);
			if (obj != null)
			{
				obj.Focus((NavigationMethod)0, (KeyModifiers)0);
			}
		}
	}

	private void OnPopupClosed(object sender, EventArgs args)
	{
		_xamlRootChangedRevoker?.Dispose();
		Popup lightDismissIndicatorPopup = _lightDismissIndicatorPopup;
		if (lightDismissIndicatorPopup != null)
		{
			lightDismissIndicatorPopup.IsOpen = false;
		}
		Popup popup = _popup;
		if (popup != null)
		{
			popup.Child = null;
		}
		FATeachingTipClosedEventArgs args2 = new FATeachingTipClosedEventArgs(_lastCloseReason);
		Closed?.Invoke(this, args2);
		if (_lastCloseReason == FATeachingTipCloseReason.CloseButton)
		{
			IInputElement previouslyFocusedElement = _previouslyFocusedElement;
			if (previouslyFocusedElement != null)
			{
				previouslyFocusedElement.Focus((NavigationMethod)0, (KeyModifiers)0);
			}
		}
		_previouslyFocusedElement = null;
		if (ControlAutomationPeer.FromElement((Control)(object)this) is FATeachingTipAutomationPeer fATeachingTipAutomationPeer)
		{
			fATeachingTipAutomationPeer.RaiseWindowClosedEvent();
		}
	}

	private void ClosePopupOnUnloadEvent(object sender, RoutedEventArgs e)
	{
		IsOpen = false;
		ClosePopup();
	}

	private void OnLightDismissIndicatorPopupClosed(object sender, EventArgs e)
	{
		if (IsOpen)
		{
			_lastCloseReason = FATeachingTipCloseReason.LightDismiss;
		}
		IsOpen = false;
	}

	private void RaiseClosingEvent(bool attachDeferralCompletedHandler)
	{
		FATeachingTipClosingEventArgs args = new FATeachingTipClosingEventArgs(_lastCloseReason);
		if (attachDeferralCompletedHandler)
		{
			FADeferral deferral = new FADeferral(delegate
			{
				Dispatcher.UIThread.VerifyAccess();
				if (!args.Cancel)
				{
					ClosePopupWithAnimationIfAvailable();
				}
				else
				{
					IsOpen = true;
				}
			});
			args.SetDeferral(deferral);
			args.IncrementDeferralCount();
			Closing?.Invoke(this, args);
			args.DecrementDeferralCount();
		}
		else
		{
			Closing?.Invoke(this, args);
		}
	}

	private void ClosePopupWithAnimationIfAvailable()
	{
		if (_popup != null && _popup.IsOpen)
		{
			if (FAUISettings.AreAnimationsEnabled())
			{
				StartContractToClose();
			}
			else
			{
				ClosePopup();
			}
			if (!_isContractAnimationPlaying && !_isExpandAnimationPlaying)
			{
				SetIsIdle(idle: true);
			}
		}
	}

	private void ClosePopup()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Popup popup = _popup;
		if (popup != null)
		{
			popup.IsOpen = false;
		}
		Popup lightDismissIndicatorPopup = _lightDismissIndicatorPopup;
		if (lightDismissIndicatorPopup != null)
		{
			lightDismissIndicatorPopup.IsOpen = false;
		}
		if (_tailOcclusionGrid != null)
		{
			CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)_tailOcclusionGrid);
			if (elementVisual != null)
			{
				elementVisual.Scale = Vector3D.op_Implicit(Vector3.One);
			}
		}
	}

	private FATeachingTipPlacementMode GetFlowDirectionAdjustedPlacement(FATeachingTipPlacementMode pm)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((Visual)this).FlowDirection == 0)
		{
			return pm;
		}
		return pm switch
		{
			FATeachingTipPlacementMode.Left => FATeachingTipPlacementMode.Right, 
			FATeachingTipPlacementMode.Right => FATeachingTipPlacementMode.Left, 
			FATeachingTipPlacementMode.LeftBottom => FATeachingTipPlacementMode.RightBottom, 
			FATeachingTipPlacementMode.LeftTop => FATeachingTipPlacementMode.RightTop, 
			FATeachingTipPlacementMode.TopLeft => FATeachingTipPlacementMode.TopRight, 
			FATeachingTipPlacementMode.TopRight => FATeachingTipPlacementMode.TopLeft, 
			FATeachingTipPlacementMode.RightTop => FATeachingTipPlacementMode.LeftTop, 
			FATeachingTipPlacementMode.RightBottom => FATeachingTipPlacementMode.LeftBottom, 
			FATeachingTipPlacementMode.BottomRight => FATeachingTipPlacementMode.BottomLeft, 
			FATeachingTipPlacementMode.BottomLeft => FATeachingTipPlacementMode.BottomRight, 
			_ => pm, 
		};
	}

	private void OnTargetChanged()
	{
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		if (_target != null)
		{
			_target.Loaded -= OnTargetLoaded;
			((Layoutable)_target).EffectiveViewportChanged -= OnTargetLayoutUpdated;
		}
		_target = Target;
		bool flag = false;
		if (_target != null)
		{
			flag = _target.IsLoaded;
			Control target = _target;
			if (target != null)
			{
				target.Loaded += OnTargetLoaded;
			}
		}
		if (IsOpen)
		{
			if ((_target != null) & flag)
			{
				Rect val = ((Visual)_target).Bounds;
				val = new Rect(((Rect)(ref val)).Size);
				_currentTargetBoundsInCoreWindowSpace = ((Rect)(ref val)).TransformToAABB(VisualExtensions.TransformToVisual((Visual)(object)_target, (Visual)(object)TopLevel.GetTopLevel((Visual)(object)this)).Value);
				SetViewportChangedEvent(_target);
			}
			PositionPopup();
			if (_target == null || ((_target != null) & flag))
			{
				PositionPopup();
			}
		}
		else
		{
			_repositionOnNextOpen = true;
			_currentTargetBoundsInCoreWindowSpace = default(Rect);
		}
	}

	private void SetViewportChangedEvent(Control target)
	{
	}

	private void RevokeViewportChangedEvent()
	{
		Control target = _target;
		if (target != null)
		{
			((Layoutable)target).EffectiveViewportChanged -= OnTargetLayoutUpdated;
		}
	}

	private void XamlRootChanged(Rect rc)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Dispatcher.UIThread.Post((Action)delegate
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			_currentXamlRootSize = TopLevel.GetTopLevel((Visual)(object)this).ClientSize;
			RepositionPopup();
		}, DispatcherPriority.Render);
	}

	private void RepositionPopup()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (IsOpen)
		{
			Rect val;
			Rect val2;
			if (_target == null)
			{
				val = default(Rect);
				val2 = val;
			}
			else
			{
				val = ((Visual)_target).Bounds;
				val = new Rect(((Rect)(ref val)).Size);
				val2 = ((Rect)(ref val)).TransformToAABB(VisualExtensions.TransformToVisual((Visual)(object)_target, (Visual)(object)TopLevel.GetTopLevel((Visual)(object)this)).Value);
			}
			Rect val3 = val2;
			val = ((Visual)this).Bounds;
			val = new Rect(((Rect)(ref val)).Size);
			Rect val4 = ((Rect)(ref val)).TransformToAABB(VisualExtensions.TransformToVisual((Visual)(object)this, (Visual)(object)TopLevel.GetTopLevel((Visual)(object)this)).Value);
			if (val3 != _currentTargetBoundsInCoreWindowSpace || val4 != _currentBoundsInCoreWindowSpace)
			{
				_currentBoundsInCoreWindowSpace = val4;
				_currentTargetBoundsInCoreWindowSpace = val3;
				PositionPopup();
			}
		}
	}

	private void OnTargetLoaded(object sender, RoutedEventArgs args)
	{
		RepositionPopup();
	}

	private void OnTargetLayoutUpdated(object sender, EffectiveViewportChangedEventArgs e)
	{
		RepositionPopup();
	}

	private void CreateExpandAnimation()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)this);
		Compositor val = ((elementVisual != null) ? ((CompositionObject)elementVisual).Compositor : null);
		if (val != null)
		{
			_expandAnimation = (KeyFrameAnimation)(object)val.CreateVector3KeyFrameAnimation();
			if (_tailOcclusionGrid != null)
			{
				KeyFrameAnimation expandAnimation = _expandAnimation;
				Rect bounds = ((Visual)_tailOcclusionGrid).Bounds;
				((CompositionAnimation)expandAnimation).SetScalarParameter("Width", (float)((Rect)(ref bounds)).Width);
				KeyFrameAnimation expandAnimation2 = _expandAnimation;
				bounds = ((Visual)_tailOcclusionGrid).Bounds;
				((CompositionAnimation)expandAnimation2).SetScalarParameter("Height", (float)((Rect)(ref bounds)).Height);
			}
			else
			{
				((CompositionAnimation)_expandAnimation).SetScalarParameter("Width", s_defaultTipHeightAndWidth);
				((CompositionAnimation)_expandAnimation).SetScalarParameter("Height", s_defaultTipHeightAndWidth);
			}
			_expandEasingFunction = (IEasing)new SplineEasing(0.1, 0.9, 0.2, 1.0);
			_expandAnimation.InsertExpressionKeyFrame(0f, "Vector3(Min(0.01, 20.0 / Width), Min(0.01, 20.0 / Height), 1.0)", (Easing)null);
			KeyFrameAnimation expandAnimation3 = _expandAnimation;
			((Vector3KeyFrameAnimation)((expandAnimation3 is Vector3KeyFrameAnimation) ? expandAnimation3 : null)).InsertKeyFrame(1f, Vector3.One, _expandEasingFunction);
			_expandAnimation.Duration = _expandAnimationDuration;
			((CompositionAnimation)_expandAnimation).Target = s_ScaleTargetName;
		}
	}

	private void CreateContractAnimation()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)this);
		Compositor val = ((elementVisual != null) ? ((CompositionObject)elementVisual).Compositor : null);
		if (val != null)
		{
			_contractEasingFunction = (IEasing)new SplineEasing(0.1, 0.9, 0.2, 1.0);
			_contractAnimation = (KeyFrameAnimation)(object)val.CreateVector3KeyFrameAnimation();
			if (_tailOcclusionGrid != null)
			{
				KeyFrameAnimation contractAnimation = _contractAnimation;
				Rect bounds = ((Visual)_tailOcclusionGrid).Bounds;
				((CompositionAnimation)contractAnimation).SetScalarParameter("Width", (float)((Rect)(ref bounds)).Width);
				KeyFrameAnimation contractAnimation2 = _contractAnimation;
				bounds = ((Visual)_tailOcclusionGrid).Bounds;
				((CompositionAnimation)contractAnimation2).SetScalarParameter("Height", (float)((Rect)(ref bounds)).Height);
			}
			else
			{
				((CompositionAnimation)_contractAnimation).SetScalarParameter("Width", s_defaultTipHeightAndWidth);
				((CompositionAnimation)_contractAnimation).SetScalarParameter("Height", s_defaultTipHeightAndWidth);
			}
			KeyFrameAnimation contractAnimation3 = _contractAnimation;
			((Vector3KeyFrameAnimation)((contractAnimation3 is Vector3KeyFrameAnimation) ? contractAnimation3 : null)).InsertKeyFrame(0f, Vector3.One);
			_contractAnimation.InsertExpressionKeyFrame(1f, "Vector3(20.0 / Width, 20.0 / Height, 1.0)", (Easing)_contractEasingFunction);
			_contractAnimation.Duration = _contractAnimationDuration;
			((CompositionAnimation)_contractAnimation).Target = s_ScaleTargetName;
		}
	}

	private void StartExpandToOpen()
	{
		if (_expandAnimation == null)
		{
			CreateExpandAnimation();
		}
		UpdateTail();
		if (_expandAnimation != null && _tailOcclusionGrid != null)
		{
			CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)_tailOcclusionGrid);
			if (elementVisual != null)
			{
				((CompositionObject)elementVisual).StartAnimationGroup((ICompositionAnimationBase)(object)_expandAnimation);
			}
			_isExpandAnimationPlaying = true;
		}
		ScopedBatchHelper scopedBatch = _scopedBatch;
		scopedBatch.Completed = (Action)Delegate.Combine(scopedBatch.Completed, (Action)delegate
		{
			_isExpandAnimationPlaying = false;
			if (!_isContractAnimationPlaying)
			{
				SetIsIdle(idle: true);
			}
			_expandAnimation = null;
		});
		if (_isExpandAnimationPlaying)
		{
			_scopedBatch.Start(_expandAnimationDuration);
		}
		if (!_isExpandAnimationPlaying && !_isContractAnimationPlaying)
		{
			SetIsIdle(idle: true);
		}
	}

	private void StartContractToClose()
	{
		if (_contractAnimation == null)
		{
			CreateContractAnimation();
		}
		if (_contractAnimation != null && _tailOcclusionGrid != null)
		{
			CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)_tailOcclusionGrid);
			if (elementVisual != null)
			{
				((CompositionObject)elementVisual).StartAnimationGroup((ICompositionAnimationBase)(object)_contractAnimation);
			}
			_isContractAnimationPlaying = true;
		}
		ScopedBatchHelper scopedBatch = _scopedBatch;
		scopedBatch.Completed = (Action)Delegate.Combine(scopedBatch.Completed, (Action)delegate
		{
			_isContractAnimationPlaying = false;
			ClosePopup();
			if (!_isExpandAnimationPlaying)
			{
				SetIsIdle(idle: true);
			}
			_contractAnimation = null;
		});
		if (_isContractAnimationPlaying)
		{
			_scopedBatch.Start(_contractAnimationDuration);
		}
		if (!_isExpandAnimationPlaying && !_isContractAnimationPlaying)
		{
			SetIsIdle(idle: true);
		}
	}

	private (FATeachingTipPlacementMode, bool) DetermineEffectivePlacement()
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if (!ShouldConstrainToRootBounds && _returnTopForOutOfWindowPlacement)
		{
			FATeachingTipPlacementMode flowDirectionAdjustedPlacement = GetFlowDirectionAdjustedPlacement(PreferredPlacement);
			if (flowDirectionAdjustedPlacement == FATeachingTipPlacementMode.Auto)
			{
				return (FATeachingTipPlacementMode.Top, false);
			}
			return (flowDirectionAdjustedPlacement, false);
		}
		if (IsOpen && _currentEffectiveTipPlacementMode != FATeachingTipPlacementMode.Auto)
		{
			return (_currentEffectiveTipPlacementMode, false);
		}
		double contentHeight;
		double contentWidth;
		if (_tailOcclusionGrid == null)
		{
			contentHeight = 0.0;
			contentWidth = 0.0;
		}
		else
		{
			Rect bounds = ((Visual)_tailOcclusionGrid).Bounds;
			double height = ((Rect)(ref bounds)).Height;
			bounds = ((Visual)_tailOcclusionGrid).Bounds;
			double width = ((Rect)(ref bounds)).Width;
			contentWidth = width;
			contentHeight = height;
		}
		if (_target != null)
		{
			return DetermineEffectivePlacementTargeted(contentHeight, contentWidth);
		}
		return DetermineEffectivePlacementUntargeted(contentHeight, contentWidth);
	}

	private (FATeachingTipPlacementMode, bool) DetermineEffectivePlacementTargeted(double contentHeight, double contentWidth)
	{
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		Span<bool> span = stackalloc bool[14]
		{
			false, true, true, true, true, true, true, true, true, true,
			true, true, true, true
		};
		double num = contentHeight + TailShortSideLength();
		double num2 = contentWidth + TailShortSideLength();
		if (HeroContent != null)
		{
			if (_heroContentBorder != null && _nonHeroContentRootGrid != null)
			{
				Rect bounds = ((Visual)_heroContentBorder).Bounds;
				double height = ((Rect)(ref bounds)).Height;
				bounds = ((Visual)_nonHeroContentRootGrid).Bounds;
				if (height > ((Rect)(ref bounds)).Height - TailLongSideActualLength())
				{
					span[3] = false;
					span[4] = false;
				}
			}
			switch (HeroContentPlacement)
			{
			case FATeachingTipHeroContentPlacementMode.Bottom:
				span[1] = false;
				span[5] = false;
				span[6] = false;
				span[11] = false;
				span[9] = false;
				span[13] = false;
				break;
			case FATeachingTipHeroContentPlacementMode.Top:
				span[2] = false;
				span[8] = false;
				span[7] = false;
				span[12] = false;
				span[10] = false;
				break;
			}
		}
		var (val, val2) = DetermineSpaceAroundTarget();
		if (((Thickness)(ref val)).Left < 0.0)
		{
			span[10] = false;
			span[3] = false;
			span[9] = false;
		}
		if (((Thickness)(ref val)).Right < 0.0)
		{
			span[12] = false;
			span[4] = false;
			span[11] = false;
		}
		if (((Thickness)(ref val)).Top < 0.0)
		{
			span[6] = false;
			span[1] = false;
			span[5] = false;
		}
		if (((Thickness)(ref val)).Bottom < 0.0)
		{
			span[8] = false;
			span[2] = false;
			span[7] = false;
		}
		if (((Thickness)(ref val)).Left < (0.0 - ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width) / 2.0 || ((Thickness)(ref val)).Right < (0.0 - ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width) / 2.0)
		{
			span[6] = false;
			span[1] = false;
			span[5] = false;
			span[8] = false;
			span[2] = false;
			span[7] = false;
			span[13] = false;
		}
		if (((Thickness)(ref val)).Top < (0.0 - ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height) / 2.0 || ((Thickness)(ref val)).Bottom < (0.0 - ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height) / 2.0)
		{
			span[10] = false;
			span[3] = false;
			span[9] = false;
			span[12] = false;
			span[4] = false;
			span[11] = false;
			span[13] = false;
		}
		if (num > ((Thickness)(ref val2)).Top)
		{
			span[1] = false;
			span[5] = false;
			span[6] = false;
		}
		if (num > ((Thickness)(ref val2)).Top + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height / 2.0)
		{
			span[13] = false;
		}
		if (contentHeight - MinimumTipEdgeToTailCenter() > ((Thickness)(ref val2)).Top + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height / 2.0)
		{
			span[11] = false;
			span[9] = false;
		}
		if (contentHeight / 2.0 > ((Thickness)(ref val2)).Top + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height / 2.0 || contentHeight / 2.0 > ((Thickness)(ref val2)).Bottom + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height / 2.0)
		{
			span[4] = false;
			span[3] = false;
		}
		if (contentHeight - MinimumTipEdgeToTailCenter() > ((Thickness)(ref val2)).Bottom + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height / 2.0)
		{
			span[12] = false;
			span[10] = false;
		}
		if (num > ((Thickness)(ref val2)).Bottom)
		{
			span[2] = false;
			span[8] = false;
			span[7] = false;
		}
		if (num2 > ((Thickness)(ref val2)).Left)
		{
			span[3] = false;
			span[9] = false;
			span[10] = false;
		}
		if (contentWidth - MinimumTipEdgeToTailCenter() > ((Thickness)(ref val2)).Left + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width / 2.0)
		{
			span[6] = false;
			span[8] = false;
		}
		if (contentWidth / 2.0 > ((Thickness)(ref val2)).Left + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width / 2.0 || contentWidth / 2.0 > ((Thickness)(ref val2)).Right + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width / 2.0)
		{
			span[1] = false;
			span[2] = false;
			span[13] = false;
		}
		if (contentWidth - MinimumTipEdgeToTailCenter() > ((Thickness)(ref val2)).Right + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width / 2.0)
		{
			span[5] = false;
			span[7] = false;
		}
		if (num2 > ((Thickness)(ref val2)).Right)
		{
			span[4] = false;
			span[11] = false;
			span[12] = false;
		}
		FATeachingTipPlacementMode flowDirectionAdjustedPlacement = GetFlowDirectionAdjustedPlacement(PreferredPlacement);
		Span<byte> priorityList = stackalloc byte[13];
		GetPlacementFallbackOrder(flowDirectionAdjustedPlacement, ref priorityList);
		Span<byte> span2 = priorityList;
		for (int i = 0; i < span2.Length; i++)
		{
			byte b = span2[i];
			if (span[b])
			{
				return ((FATeachingTipPlacementMode)b, false);
			}
		}
		return (FATeachingTipPlacementMode.Top, true);
	}

	private (FATeachingTipPlacementMode, bool) DetermineEffectivePlacementUntargeted(double contentHeight, double contentWidth)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Rect windowBounds = GetWindowBounds();
		if (!ShouldConstrainToRootBounds)
		{
			Rect effectiveScreenBoundsInCoreWindowSpace = GetEffectiveScreenBoundsInCoreWindowSpace(windowBounds);
			if (((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).Height > contentHeight && ((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).Width > contentWidth)
			{
				return (FATeachingTipPlacementMode.Bottom, false);
			}
		}
		else
		{
			Rect effectiveWindowBoundsInCoreWindowSpace = GetEffectiveWindowBoundsInCoreWindowSpace(windowBounds);
			if (((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Height > contentHeight && ((Rect)(ref effectiveWindowBoundsInCoreWindowSpace)).Width > contentWidth)
			{
				return (FATeachingTipPlacementMode.Bottom, false);
			}
		}
		return (FATeachingTipPlacementMode.Top, true);
	}

	private (Thickness, Thickness) DetermineSpaceAroundTarget()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		bool shouldConstrainToRootBounds = ShouldConstrainToRootBounds;
		Rect windowBounds = GetWindowBounds();
		Rect effectiveWindowBoundsInCoreWindowSpace = GetEffectiveWindowBoundsInCoreWindowSpace(windowBounds);
		Rect effectiveScreenBoundsInCoreWindowSpace = GetEffectiveScreenBoundsInCoreWindowSpace(windowBounds);
		Rect val = effectiveWindowBoundsInCoreWindowSpace;
		Thickness val2 = default(Thickness);
		((Thickness)(ref val2))._002Ector(((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X - ((Rect)(ref val)).X, ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y - ((Rect)(ref val)).Y, ((Rect)(ref val)).X + ((Rect)(ref val)).Width - (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width), ((Rect)(ref val)).Y + ((Rect)(ref val)).Height - (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height));
		Thickness item = default(Thickness);
		if (!shouldConstrainToRootBounds)
		{
			((Thickness)(ref item))._002Ector(((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X - ((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).X, ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y - ((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).Y, ((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).X + ((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).Width - (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).X + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Width), ((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).Y + ((Rect)(ref effectiveScreenBoundsInCoreWindowSpace)).Height - (((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Y + ((Rect)(ref _currentTargetBoundsInCoreWindowSpace)).Height));
		}
		else
		{
			item = val2;
		}
		return (val2, item);
	}

	private Rect GetEffectiveWindowBoundsInCoreWindowSpace(Rect windowBounds)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(((Rect)(ref windowBounds)).Size);
	}

	private Rect GetEffectiveScreenBoundsInCoreWindowSpace(Rect windowBounds)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (!ShouldConstrainToRootBounds)
		{
			TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
			Window val = (Window)(object)((topLevel is Window) ? topLevel : null);
			if (val != null)
			{
				Screen val2 = ((WindowBase)val).Screens.ScreenFromWindow((WindowBase)(object)val);
				double scaling = val2.Scaling;
				PixelPoint position = val.Position;
				double num = -((PixelPoint)(ref position)).X;
				position = val.Position;
				double num2 = -((PixelPoint)(ref position)).Y;
				PixelRect bounds = val2.Bounds;
				double num3 = (double)((PixelRect)(ref bounds)).Height / scaling;
				bounds = val2.Bounds;
				return new Rect(num, num2, num3, (double)((PixelRect)(ref bounds)).Width / scaling);
			}
		}
		return new Rect(((Rect)(ref windowBounds)).Size);
	}

	private Rect GetWindowBounds()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		_003F val;
		if (topLevel == null)
		{
			val = default(Size);
		}
		else
		{
			Rect bounds = ((Visual)topLevel).Bounds;
			val = ((Rect)(ref bounds)).Size;
		}
		return new Rect((Size)val);
	}

	private void GetPlacementFallbackOrder(FATeachingTipPlacementMode preferredPlacement, ref Span<byte> priorityList)
	{
		priorityList[0] = 1;
		priorityList[1] = 2;
		priorityList[2] = 3;
		priorityList[3] = 4;
		priorityList[4] = 6;
		priorityList[5] = 5;
		priorityList[6] = 8;
		priorityList[7] = 7;
		priorityList[8] = 9;
		priorityList[9] = 10;
		priorityList[10] = 11;
		priorityList[11] = 12;
		priorityList[12] = 13;
		if (IsPlacementBottom(preferredPlacement))
		{
			ref byte reference = ref priorityList[0];
			ref byte reference2 = ref priorityList[1];
			byte b = priorityList[1];
			byte b2 = priorityList[0];
			reference = b;
			reference2 = b2;
			reference = ref priorityList[4];
			ref byte reference3 = ref priorityList[6];
			b2 = priorityList[6];
			b = priorityList[4];
			reference = b2;
			reference3 = b;
			reference = ref priorityList[5];
			ref byte reference4 = ref priorityList[7];
			b = priorityList[7];
			b2 = priorityList[5];
			reference = b;
			reference4 = b2;
		}
		else if (IsPlacementLeft(preferredPlacement))
		{
			ref byte reference = ref priorityList[0];
			ref byte reference5 = ref priorityList[2];
			byte b2 = priorityList[2];
			byte b = priorityList[0];
			reference = b2;
			reference5 = b;
			reference = ref priorityList[1];
			ref byte reference6 = ref priorityList[3];
			b = priorityList[3];
			b2 = priorityList[1];
			reference = b;
			reference6 = b2;
			reference = ref priorityList[4];
			ref byte reference7 = ref priorityList[8];
			b2 = priorityList[8];
			b = priorityList[4];
			reference = b2;
			reference7 = b;
			reference = ref priorityList[5];
			ref byte reference8 = ref priorityList[9];
			b = priorityList[9];
			b2 = priorityList[5];
			reference = b;
			reference8 = b2;
			reference = ref priorityList[6];
			ref byte reference9 = ref priorityList[10];
			b2 = priorityList[10];
			b = priorityList[6];
			reference = b2;
			reference9 = b;
			reference = ref priorityList[7];
			ref byte reference10 = ref priorityList[11];
			b = priorityList[11];
			b2 = priorityList[7];
			reference = b;
			reference10 = b2;
		}
		else if (IsPlacementRight(preferredPlacement))
		{
			ref byte reference = ref priorityList[0];
			ref byte reference11 = ref priorityList[2];
			byte b2 = priorityList[2];
			byte b = priorityList[0];
			reference = b2;
			reference11 = b;
			reference = ref priorityList[1];
			ref byte reference12 = ref priorityList[3];
			b = priorityList[3];
			b2 = priorityList[1];
			reference = b;
			reference12 = b2;
			reference = ref priorityList[4];
			ref byte reference13 = ref priorityList[8];
			b2 = priorityList[8];
			b = priorityList[4];
			reference = b2;
			reference13 = b;
			reference = ref priorityList[5];
			ref byte reference14 = ref priorityList[9];
			b = priorityList[9];
			b2 = priorityList[5];
			reference = b;
			reference14 = b2;
			reference = ref priorityList[6];
			ref byte reference15 = ref priorityList[10];
			b2 = priorityList[10];
			b = priorityList[6];
			reference = b2;
			reference15 = b;
			reference = ref priorityList[7];
			ref byte reference16 = ref priorityList[11];
			b = priorityList[11];
			b2 = priorityList[7];
			reference = b;
			reference16 = b2;
			reference = ref priorityList[0];
			ref byte reference17 = ref priorityList[1];
			b2 = priorityList[1];
			b = priorityList[0];
			reference = b2;
			reference17 = b;
			reference = ref priorityList[4];
			ref byte reference18 = ref priorityList[6];
			b = priorityList[6];
			b2 = priorityList[4];
			reference = b;
			reference18 = b2;
			reference = ref priorityList[5];
			ref byte reference19 = ref priorityList[7];
			b2 = priorityList[7];
			b = priorityList[5];
			reference = b2;
			reference19 = b;
		}
		int num = -1;
		for (int i = 0; i < priorityList.Length; i++)
		{
			if (priorityList[i] == (byte)preferredPlacement)
			{
				num = i;
				break;
			}
		}
		for (int num2 = num; num2 > 0; num2--)
		{
			priorityList[num2] = priorityList[num2 - 1];
		}
		priorityList[0] = (byte)preferredPlacement;
	}

	private void TrySetCenterPoint(Control element, double x, double y)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (element != null)
		{
			CompositionVisual elementVisual = ElementComposition.GetElementVisual((Visual)(object)element);
			if (elementVisual != null)
			{
				elementVisual.CenterPoint = Vector3D.op_Implicit(new Vector3((float)x, (float)y, 1f));
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double TailLongSideActualLength()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (_tailPolygon == null)
		{
			return 0.0;
		}
		Rect bounds = ((Visual)_tailPolygon).Bounds;
		double height = ((Rect)(ref bounds)).Height;
		bounds = ((Visual)_tailPolygon).Bounds;
		return Math.Max(height, ((Rect)(ref bounds)).Width);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double TailLongSideLength()
	{
		return TailLongSideActualLength() - (double)(2f * s_tailOcclusionAmount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double TailShortSideLength()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (_tailPolygon == null)
		{
			return 0.0;
		}
		Rect bounds = ((Visual)_tailPolygon).Bounds;
		double height = ((Rect)(ref bounds)).Height;
		bounds = ((Visual)_tailPolygon).Bounds;
		return Math.Min(height, ((Rect)(ref bounds)).Width);
	}

	private double MinimumTipEdgeToTailEdgeMargin()
	{
		if (_tailOcclusionGrid != null)
		{
			if (((AvaloniaList<ColumnDefinition>)(object)_tailOcclusionGrid.ColumnDefinitions).Count <= 1)
			{
				return 0.0;
			}
			return ((AvaloniaList<ColumnDefinition>)(object)_tailOcclusionGrid.ColumnDefinitions)[1].ActualWidth + (double)s_tailOcclusionAmount;
		}
		return 0.0;
	}

	private double MinimumTipEdgeToTailCenter()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if (_tailOcclusionGrid != null && _tailPolygon != null && ((AvaloniaList<ColumnDefinition>)(object)_tailOcclusionGrid.ColumnDefinitions).Count > 1)
		{
			double num = ((AvaloniaList<ColumnDefinition>)(object)_tailOcclusionGrid.ColumnDefinitions)[0].ActualWidth + ((AvaloniaList<ColumnDefinition>)(object)_tailOcclusionGrid.ColumnDefinitions)[1].ActualWidth;
			Rect bounds = ((Visual)_tailPolygon).Bounds;
			double height = ((Rect)(ref bounds)).Height;
			bounds = ((Visual)_tailPolygon).Bounds;
			return num + Math.Max(height, ((Rect)(ref bounds)).Width) / 2.0;
		}
		return 0.0;
	}

	private CornerRadius GetTeachingTipCornerRadius()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((TemplatedControl)this).CornerRadius;
	}

	private void SetIsIdle(bool idle)
	{
		_isIdle = idle;
	}

	private double TopLeftCornerRadius()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		CornerRadius teachingTipCornerRadius = GetTeachingTipCornerRadius();
		return ((CornerRadius)(ref teachingTipCornerRadius)).TopLeft;
	}

	private double TopRightCornerRadius()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		CornerRadius teachingTipCornerRadius = GetTeachingTipCornerRadius();
		return ((CornerRadius)(ref teachingTipCornerRadius)).TopRight;
	}

	private static bool IsPlacementTop(FATeachingTipPlacementMode p)
	{
		if (p != FATeachingTipPlacementMode.Top && p != FATeachingTipPlacementMode.TopLeft)
		{
			return p == FATeachingTipPlacementMode.TopRight;
		}
		return true;
	}

	private static bool IsPlacementBottom(FATeachingTipPlacementMode p)
	{
		if (p != FATeachingTipPlacementMode.Bottom && p != FATeachingTipPlacementMode.BottomLeft)
		{
			return p == FATeachingTipPlacementMode.BottomRight;
		}
		return true;
	}

	private static bool IsPlacementLeft(FATeachingTipPlacementMode p)
	{
		if (p != FATeachingTipPlacementMode.Left && p != FATeachingTipPlacementMode.TopLeft)
		{
			return p == FATeachingTipPlacementMode.TopRight;
		}
		return true;
	}

	private static bool IsPlacementRight(FATeachingTipPlacementMode p)
	{
		if (p != FATeachingTipPlacementMode.Right && p != FATeachingTipPlacementMode.RightTop)
		{
			return p == FATeachingTipPlacementMode.RightBottom;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness BottomPlacementTopRightHighlightMargin(double width, double height)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(width / 2.0 + (TailShortSideLength() - 1.0), 0.0, TopRightCornerRadius() - 1.0, 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness BottomRightPlacementTopRightHighlightMargin(double width, double height)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(MinimumTipEdgeToTailEdgeMargin() + (TailLongSideLength() - 1.0), 0.0, TopRightCornerRadius() - 1.0, 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness BottomLeftPlacementTopRightHighlightMargin(double width, double height)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(width - (MinimumTipEdgeToTailEdgeMargin() + 1.0), 0.0, TopRightCornerRadius() - 1.0, 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness OtherPlacementTopRightHighlightMargin(double width, double height)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return default(Thickness);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness BottomPlacementTopLeftHighlightMargin(double width, double height)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(TopLeftCornerRadius() - 1.0, 0.0, width / 2.0 + (TailShortSideLength() - 1.0), 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness BottomRightPlacementTopLeftHighlightMargin(double width, double height)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(TopLeftCornerRadius() - 1.0, 0.0, width - (MinimumTipEdgeToTailEdgeMargin() + 1.0), 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness BottomLeftPlacementTopLeftHighlightMargin(double width, double height)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(TopLeftCornerRadius() - 1.0, 0.0, MinimumTipEdgeToTailEdgeMargin() + TailLongSideLength() - 1.0, 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness TopEdgePlacementTopLeftHighlightMargin(double width, double height)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(TopLeftCornerRadius() - 1.0, 1.0, TopRightCornerRadius() - 1.0, 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness LeftEdgePlacementTopLeftHighlightMargin(double width, double height)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(TopLeftCornerRadius() - 1.0, 1.0, TopRightCornerRadius() - 2.0, 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Thickness RightEdgePlacementTopLeftHighlightMargin(double width, double height)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Thickness(TopLeftCornerRadius() - 2.0, 1.0, TopRightCornerRadius() - 1.0, 0.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double UntargetedTipFarPlacementOffset(double farWindowCoordinateInCoreWindowSpace, double tipSize, double offset)
	{
		return farWindowCoordinateInCoreWindowSpace - (tipSize + (double)s_untargetedTipWindowEdgeMargin + offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double UntargetedTipCenterPlacementOffset(double nearWindowCoordinateInCoreWindowSpace, double farWindowCoordinateInCoreWindowSpace, double tipSize, double nearOffset, double farOffset)
	{
		return (nearWindowCoordinateInCoreWindowSpace + farWindowCoordinateInCoreWindowSpace) / 2.0 - tipSize / 2.0 + nearOffset - farOffset;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double UntargetedTipNearPlacementOffset(double nearWindowCoordinateInCoreWindowSpace, double offset)
	{
		return (double)s_untargetedTipWindowEdgeMargin + nearWindowCoordinateInCoreWindowSpace + offset;
	}

	static FATeachingTip()
	{
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		s_ScaleTargetName = "Scale";
		s_untargetedTipWindowEdgeMargin = 24f;
		s_defaultTipHeightAndWidth = 320f;
		s_tailOcclusionAmount = 2f;
		TitleProperty = AvaloniaProperty.Register<FATeachingTip, string>("Title", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);
		SubtitleProperty = AvaloniaProperty.Register<FATeachingTip, string>("Subtitle", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);
		IsOpenProperty = FAInfoBar.IsOpenProperty.AddOwner<FATeachingTip>((StyledPropertyMetadata<bool>)null);
		TargetProperty = AvaloniaProperty.Register<FATeachingTip, Control>("Target", (Control)null, false, (BindingMode)1, (Func<Control, bool>)null, (Func<AvaloniaObject, Control, Control>)null, false);
		TailVisibilityProperty = AvaloniaProperty.Register<FATeachingTip, FATeachingTipTailVisibility>("TailVisibility", FATeachingTipTailVisibility.Auto, false, (BindingMode)1, (Func<FATeachingTipTailVisibility, bool>)null, (Func<AvaloniaObject, FATeachingTipTailVisibility, FATeachingTipTailVisibility>)null, false);
		ActionButtonContentProperty = AvaloniaProperty.Register<FATeachingTip, object>("ActionButtonContent", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);
		ActionButtonStyleProperty = AvaloniaProperty.Register<FATeachingTip, ControlTheme>("ActionButtonStyle", (ControlTheme)null, false, (BindingMode)1, (Func<ControlTheme, bool>)null, (Func<AvaloniaObject, ControlTheme, ControlTheme>)null, false);
		ActionButtonCommandProperty = AvaloniaProperty.Register<FATeachingTip, ICommand>("ActionButtonCommand", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);
		ActionButtonCommandParameterProperty = AvaloniaProperty.Register<FATeachingTip, object>("ActionButtonCommandParameter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);
		CloseButtonContentProperty = AvaloniaProperty.Register<FATeachingTip, object>("CloseButtonContent", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);
		CloseButtonStyleProperty = AvaloniaProperty.Register<FATeachingTip, ControlTheme>("CloseButtonStyle", (ControlTheme)null, false, (BindingMode)1, (Func<ControlTheme, bool>)null, (Func<AvaloniaObject, ControlTheme, ControlTheme>)null, false);
		CloseButtonCommandProperty = AvaloniaProperty.Register<FATeachingTip, ICommand>("CloseButtonCommand", (ICommand)null, false, (BindingMode)1, (Func<ICommand, bool>)null, (Func<AvaloniaObject, ICommand, ICommand>)null, false);
		CloseButtonCommandParameterProperty = AvaloniaProperty.Register<FATeachingTip, object>("CloseButtonCommandParameter", (object)null, false, (BindingMode)1, (Func<object, bool>)null, (Func<AvaloniaObject, object, object>)null, false);
		PlacementMarginProperty = AvaloniaProperty.Register<FATeachingTip, Thickness>("PlacementMargin", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null, false);
		ShouldConstrainToRootBoundsProperty = AvaloniaProperty.Register<FATeachingTip, bool>("ShouldConstrainToRootBounds", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		IsLightDismissEnabledProperty = AvaloniaProperty.Register<FATeachingTip, bool>("IsLightDismissEnabled", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		PreferredPlacementProperty = AvaloniaProperty.Register<FATeachingTip, FATeachingTipPlacementMode>("PreferredPlacement", FATeachingTipPlacementMode.Auto, false, (BindingMode)1, (Func<FATeachingTipPlacementMode, bool>)null, (Func<AvaloniaObject, FATeachingTipPlacementMode, FATeachingTipPlacementMode>)null, false);
		HeroContentPlacementProperty = AvaloniaProperty.Register<FATeachingTip, FATeachingTipHeroContentPlacementMode>("HeroContentPlacement", FATeachingTipHeroContentPlacementMode.Auto, false, (BindingMode)1, (Func<FATeachingTipHeroContentPlacementMode, bool>)null, (Func<AvaloniaObject, FATeachingTipHeroContentPlacementMode, FATeachingTipHeroContentPlacementMode>)null, false);
		HeroContentProperty = AvaloniaProperty.Register<FATeachingTip, Control>("HeroContent", (Control)null, false, (BindingMode)1, (Func<Control, bool>)null, (Func<AvaloniaObject, Control, Control>)null, false);
		IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FATeachingTip>((StyledPropertyMetadata<FAIconSource>)null);
		TemplateSettingsProperty = AvaloniaProperty.Register<FATeachingTip, FATeachingTipTemplateSettings>("TemplateSettings", (FATeachingTipTemplateSettings)null, false, (BindingMode)1, (Func<FATeachingTipTemplateSettings, bool>)null, (Func<AvaloniaObject, FATeachingTipTemplateSettings, FATeachingTipTemplateSettings>)null, false);
		SR_TeachingTipAlternateCloseButtonName = "TeachingTipAlternateCloseButtonName";
		SR_TeachingTipAlternateCloseButtonTooltip = "TeachingTipAlternateCloseButtonTooltip";
	}
}
