using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a specialized command bar that provides layout for CommandBarButton and related command elements.
/// </summary>
[TemplatePart("PrimaryItemsControl", typeof(ItemsControl))]
[TemplatePart("ContentControl", typeof(ContentControl))]
[TemplatePart("SecondaryItemsControl", typeof(FACommandBarOverflowPresenter))]
[TemplatePart("MoreButton", typeof(Button))]
[PseudoClasses(new string[] { ":dynamicoverflow" })]
[PseudoClasses(new string[] { ":compact", ":minimal", ":hidden" })]
[PseudoClasses(new string[] { ":labelbottom", ":labelright", ":labelcollapsed" })]
[PseudoClasses(new string[] { ":primaryOnly", ":secondaryOnly" })]
[PseudoClasses(new string[] { ":open" })]
public class FACommandBar : ContentControl
{
	private bool _appliedTemplate;

	private AvaloniaList<IFACommandBarElement> _primaryItems;

	private AvaloniaList<IFACommandBarElement> _overflowItems;

	private ItemsControl _primaryItemsHost;

	private FACommandBarOverflowPresenter _overflowItemsHost;

	private ContentControl _contentHost;

	private Button _moreButton;

	private FACommandBarSeparator _overflowSeparator;

	private int _hasOrderedOverflow;

	private Dictionary<IFACommandBarElement, double> _widthCache;

	private int _numInOverflow;

	private double _minRecoverWidth;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.IsSticky" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsStickyProperty = AvaloniaProperty.Register<FACommandBar, bool>("IsSticky", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Define the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.IsOpen" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<FACommandBar, bool>("IsOpen", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.ClosedDisplayMode" /> property
	/// </summary>
	public static readonly StyledProperty<FACommandBarClosedDisplayMode> ClosedDisplayModeProperty = AvaloniaProperty.Register<FACommandBar, FACommandBarClosedDisplayMode>("ClosedDisplayMode", FACommandBarClosedDisplayMode.Compact, false, (BindingMode)1, (Func<FACommandBarClosedDisplayMode, bool>)null, (Func<AvaloniaObject, FACommandBarClosedDisplayMode, FACommandBarClosedDisplayMode>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.PrimaryCommands" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBar, IAvaloniaList<IFACommandBarElement>> PrimaryCommandsProperty = AvaloniaProperty.RegisterDirect<FACommandBar, IAvaloniaList<IFACommandBarElement>>("PrimaryCommands", (Func<FACommandBar, IAvaloniaList<IFACommandBarElement>>)((FACommandBar x) => x.PrimaryCommands), (Action<FACommandBar, IAvaloniaList<IFACommandBarElement>>)null, (IAvaloniaList<IFACommandBarElement>)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.SecondaryCommands" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBar, IAvaloniaList<IFACommandBarElement>> SecondaryCommandsProperty = AvaloniaProperty.RegisterDirect<FACommandBar, IAvaloniaList<IFACommandBarElement>>("SecondaryCommands", (Func<FACommandBar, IAvaloniaList<IFACommandBarElement>>)((FACommandBar x) => x.SecondaryCommands), (Action<FACommandBar, IAvaloniaList<IFACommandBarElement>>)null, (IAvaloniaList<IFACommandBarElement>)null, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.OverflowButtonVisibility" /> property
	/// </summary>
	public static readonly StyledProperty<FACommandBarOverflowButtonVisibility> OverflowButtonVisibilityProperty = AvaloniaProperty.Register<FACommandBar, FACommandBarOverflowButtonVisibility>("OverflowButtonVisibility", FACommandBarOverflowButtonVisibility.Auto, false, (BindingMode)1, (Func<FACommandBarOverflowButtonVisibility, bool>)null, (Func<AvaloniaObject, FACommandBarOverflowButtonVisibility, FACommandBarOverflowButtonVisibility>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.IsDynamicOverflowEnabled" />
	/// </summary>
	public static readonly StyledProperty<bool> IsDynamicOverflowEnabledProperty = AvaloniaProperty.Register<FACommandBar, bool>("IsDynamicOverflowEnabled", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.ItemsAlignment" /> property
	/// </summary>
	public static readonly StyledProperty<FACommandBarItemsAlignment> ItemsAlignmentProperty = AvaloniaProperty.Register<FACommandBar, FACommandBarItemsAlignment>("ItemsAlignment", FACommandBarItemsAlignment.Left, false, (BindingMode)1, (Func<FACommandBarItemsAlignment, bool>)null, (Func<AvaloniaObject, FACommandBarItemsAlignment, FACommandBarItemsAlignment>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.DefaultLabelPosition" /> property
	/// </summary>
	public static readonly StyledProperty<FACommandBarDefaultLabelPosition> DefaultLabelPositionProperty = AvaloniaProperty.Register<FACommandBar, FACommandBarDefaultLabelPosition>("DefaultLabelPosition", FACommandBarDefaultLabelPosition.Bottom, false, (BindingMode)1, (Func<FACommandBarDefaultLabelPosition, bool>)null, (Func<AvaloniaObject, FACommandBarDefaultLabelPosition, FACommandBarDefaultLabelPosition>)null, false);

	private IAvaloniaList<IFACommandBarElement> _primaryCommands;

	private IAvaloniaList<IFACommandBarElement> _secondaryCommands;

	private const string s_tpPrimaryItemsControl = "PrimaryItemsControl";

	private const string s_tpContentControl = "ContentControl";

	private const string s_tpSecondaryItemsControl = "SecondaryItemsControl";

	private const string s_tpMoreButton = "MoreButton";

	private const string s_pcDynamicOverflow = ":dynamicoverflow";

	private const string s_pcLabelBottom = ":labelbottom";

	private const string s_pcLabelRight = ":labelright";

	private const string s_pcLabelCollapsed = ":labelcollapsed";

	private const string s_pcMinimal = ":minimal";

	private const string s_pcHidden = ":hidden";

	private const string s_pcPrimaryOnly = ":primaryOnly";

	private const string s_pcSecondaryOnly = ":secondaryOnly";

	/// <summary>
	/// Gets or sets a value that indicates whether the CommandBar does not close on light dismiss.
	/// </summary>
	public bool IsSticky
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsStickyProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsStickyProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the CommandBar is open.
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
	/// Gets or sets a value that indicates whether icon buttons are displayed when the 
	/// command bar is not completely open.
	/// </summary>
	public FACommandBarClosedDisplayMode ClosedDisplayMode
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FACommandBarClosedDisplayMode>(ClosedDisplayModeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FACommandBarClosedDisplayMode>(ClosedDisplayModeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the collection of primary command elements for the CommandBar.
	/// </summary>
	public IAvaloniaList<IFACommandBarElement> PrimaryCommands
	{
		get
		{
			return _primaryCommands;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IAvaloniaList<IFACommandBarElement>>((DirectPropertyBase<IAvaloniaList<IFACommandBarElement>>)(object)PrimaryCommandsProperty, ref _primaryCommands, value);
		}
	}

	/// <summary>
	/// Gets the collection of secondary command elements for the CommandBar.
	/// </summary>
	public IAvaloniaList<IFACommandBarElement> SecondaryCommands
	{
		get
		{
			return _secondaryCommands;
		}
		private set
		{
			((AvaloniaObject)this).SetAndRaise<IAvaloniaList<IFACommandBarElement>>((DirectPropertyBase<IAvaloniaList<IFACommandBarElement>>)(object)SecondaryCommandsProperty, ref _secondaryCommands, value);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates when a command bar's overflow button is shown.
	/// </summary>
	public FACommandBarOverflowButtonVisibility OverflowButtonVisibility
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FACommandBarOverflowButtonVisibility>(OverflowButtonVisibilityProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FACommandBarOverflowButtonVisibility>(OverflowButtonVisibilityProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether primary commands automatically 
	/// move to the overflow menu when space is limited.
	/// </summary>
	public bool IsDynamicOverflowEnabled
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsDynamicOverflowEnabledProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsDynamicOverflowEnabledProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets how the <see cref="P:FluentAvalonia.UI.Controls.FACommandBar.PrimaryCommands" /> align in the CommandBar
	/// </summary>
	/// <remarks>
	/// This property doesn't exist in WinUI, where PrimaryCommands are always right aligned.
	/// This property gives you more flexibility to control this behavior.
	/// </remarks>
	public FACommandBarItemsAlignment ItemsAlignment
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FACommandBarItemsAlignment>(ItemsAlignmentProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FACommandBarItemsAlignment>(ItemsAlignmentProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates the placement and visibility of the labels 
	/// on the command bar's buttons.
	/// </summary>
	public FACommandBarDefaultLabelPosition DefaultLabelPosition
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FACommandBarDefaultLabelPosition>(DefaultLabelPositionProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FACommandBarDefaultLabelPosition>(DefaultLabelPositionProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Occurs when the CommandBar changes from hidden to visible.
	/// </summary>
	public event TypedEventHandler<FACommandBar, EventArgs> Opened;

	/// <summary>
	/// Occurs when the CommandBar starts to change from hidden to visible.
	/// </summary>
	public event TypedEventHandler<FACommandBar, EventArgs> Opening;

	/// <summary>
	/// Occurs when the CommandBar changes from visible to hidden.
	/// </summary>
	public event TypedEventHandler<FACommandBar, EventArgs> Closed;

	/// <summary>
	/// Occurs when the CommandBar starts to change from visible to hidden.
	/// </summary>
	public event TypedEventHandler<FACommandBar, EventArgs> Closing;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" /> class.
	/// </summary>
	public FACommandBar()
	{
		PrimaryCommands = (IAvaloniaList<IFACommandBarElement>)(object)new AvaloniaList<IFACommandBarElement>();
		SecondaryCommands = (IAvaloniaList<IFACommandBarElement>)(object)new AvaloniaList<IFACommandBarElement>();
		((INotifyCollectionChanged)_primaryCommands).CollectionChanged += OnPrimaryCommandsChanged;
		((INotifyCollectionChanged)_secondaryCommands).CollectionChanged += OnSecondaryCommandsChanged;
		((StyledElement)this).PseudoClasses.Add(":dynamicoverflow");
		((StyledElement)this).PseudoClasses.Add(":compact");
		((StyledElement)this).PseudoClasses.Add(":labelbottom");
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		_appliedTemplate = false;
		if (_moreButton != null)
		{
			_moreButton.Click -= OnMoreButtonClick;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		_primaryItemsHost = NameScopeExtensions.Find<ItemsControl>(e.NameScope, "PrimaryItemsControl");
		_contentHost = NameScopeExtensions.Find<ContentControl>(e.NameScope, "ContentControl");
		_overflowItemsHost = NameScopeExtensions.Find<FACommandBarOverflowPresenter>(e.NameScope, "SecondaryItemsControl");
		_moreButton = NameScopeExtensions.Find<Button>(e.NameScope, "MoreButton");
		if (_moreButton != null)
		{
			_moreButton.Click += OnMoreButtonClick;
		}
		_appliedTemplate = true;
		AttachItems();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((ContentControl)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IsOpenProperty)
		{
			OnIsOpenedChanged(AvaloniaPropertyChangedExtensions.GetNewValue<bool>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)DefaultLabelPositionProperty)
		{
			FACommandBarDefaultLabelPosition newValue = AvaloniaPropertyChangedExtensions.GetNewValue<FACommandBarDefaultLabelPosition>(change);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":labelright", newValue == FACommandBarDefaultLabelPosition.Right);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":labelbottom", newValue == FACommandBarDefaultLabelPosition.Bottom);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":labelcollapsed", newValue == FACommandBarDefaultLabelPosition.Collapsed);
		}
		else if (change.Property == (AvaloniaProperty)(object)ClosedDisplayModeProperty)
		{
			FACommandBarClosedDisplayMode newValue2 = AvaloniaPropertyChangedExtensions.GetNewValue<FACommandBarClosedDisplayMode>(change);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":compact", newValue2 == FACommandBarClosedDisplayMode.Compact);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":minimal", newValue2 == FACommandBarClosedDisplayMode.Minimal);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":hidden", newValue2 == FACommandBarClosedDisplayMode.Hidden);
		}
		else if (change.Property == (AvaloniaProperty)(object)ItemsAlignmentProperty)
		{
			FACommandBarItemsAlignment newValue3 = AvaloniaPropertyChangedExtensions.GetNewValue<FACommandBarItemsAlignment>(change);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":itemsRight", newValue3 == FACommandBarItemsAlignment.Right);
		}
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		bool isDynamicOverflowEnabled = IsDynamicOverflowEnabled;
		if (isDynamicOverflowEnabled)
		{
			if (!((Visual)_moreButton).IsVisible)
			{
				((Visual)_moreButton).IsVisible = true;
			}
			Size result = ((Layoutable)this).MeasureOverride(Size.Infinity);
			if (_primaryCommands.Count == 0)
			{
				((Visual)_moreButton).IsVisible = true;
				((Visual)_overflowSeparator).IsVisible = false;
				return result;
			}
			_ = ((Size)(ref availableSize)).Width;
			double width = ((Size)(ref availableSize)).Width;
			double num;
			Size desiredSize;
			if (_contentHost == null)
			{
				num = 0.0;
			}
			else
			{
				desiredSize = ((Layoutable)_contentHost).DesiredSize;
				num = ((Size)(ref desiredSize)).Width;
			}
			double num2 = width - num;
			desiredSize = ((Layoutable)_moreButton).DesiredSize;
			double num3 = num2 - ((Size)(ref desiredSize)).Width - 5.0;
			if (_minRecoverWidth < num3 && _numInOverflow > 0)
			{
				desiredSize = ((Layoutable)_primaryItemsHost).DesiredSize;
				double num4 = ((Size)(ref desiredSize)).Width;
				while (_numInOverflow > 0)
				{
					IList<IFACommandBarElement> returnToPrimaryItems = GetReturnToPrimaryItems();
					double num5 = 0.0;
					for (int i = 0; i < returnToPrimaryItems.Count; i++)
					{
						num5 += _widthCache[returnToPrimaryItems[i]];
						_overflowItems.Remove(returnToPrimaryItems[i]);
						int num6 = Math.Min(_primaryItems.Count, ((IList<IFACommandBarElement>)_primaryCommands).IndexOf(returnToPrimaryItems[i]));
						_primaryItems.Insert(num6, returnToPrimaryItems[i]);
						_numInOverflow--;
						if (returnToPrimaryItems[i] is FACommandBarSeparator fACommandBarSeparator)
						{
							((Visual)fACommandBarSeparator).IsVisible = true;
						}
					}
					num4 += num5;
					_minRecoverWidth += num5;
					if (num4 >= num3)
					{
						break;
					}
				}
			}
			else
			{
				desiredSize = ((Layoutable)_primaryItemsHost).DesiredSize;
				if (((Size)(ref desiredSize)).Width > num3)
				{
					double num7 = 0.0;
					while (true)
					{
						desiredSize = ((Layoutable)_primaryItemsHost).DesiredSize;
						if (!(((Size)(ref desiredSize)).Width - num7 > num3))
						{
							break;
						}
						IList<IFACommandBarElement> nextItemsToOverflow = GetNextItemsToOverflow();
						if (nextItemsToOverflow == null)
						{
							break;
						}
						for (int j = 0; j < nextItemsToOverflow.Count; j++)
						{
							IFACommandBarElement iFACommandBarElement = nextItemsToOverflow[j];
							Control val = (Control)((iFACommandBarElement is Control) ? iFACommandBarElement : null);
							IFACommandBarElement item = nextItemsToOverflow[j];
							desiredSize = ((Layoutable)val).DesiredSize;
							UpdateWidthCacheForItem(item, ((Size)(ref desiredSize)).Width);
							double num8 = num7;
							desiredSize = ((Layoutable)val).DesiredSize;
							num7 = num8 + ((Size)(ref desiredSize)).Width;
							_primaryItems.Remove(nextItemsToOverflow[j]);
							_overflowItems.Insert(_numInOverflow, nextItemsToOverflow[j]);
							_numInOverflow++;
							if (nextItemsToOverflow[j] is FACommandBarSeparator fACommandBarSeparator2)
							{
								((Visual)fACommandBarSeparator2).IsVisible = false;
							}
						}
					}
					desiredSize = ((Layoutable)_primaryItemsHost).DesiredSize;
					_minRecoverWidth = ((Size)(ref desiredSize)).Width;
				}
			}
			if (_overflowSeparator != null)
			{
				((Visual)_overflowSeparator).IsVisible = _numInOverflow > 0 && SecondaryCommands.Count > 0;
				int numInOverflow = _numInOverflow;
				int num9 = _overflowItems.IndexOf((IFACommandBarElement)_overflowSeparator);
				_overflowItems.Move(num9, numInOverflow);
			}
		}
		FACommandBarOverflowButtonVisibility overflowButtonVisibility = OverflowButtonVisibility;
		if (overflowButtonVisibility == FACommandBarOverflowButtonVisibility.Auto)
		{
			((Visual)_moreButton).IsVisible = _overflowItems != null && (isDynamicOverflowEnabled ? (_overflowItems.Count > 1) : (_overflowItems.Count > 0));
		}
		else
		{
			((Visual)_moreButton).IsVisible = overflowButtonVisibility == FACommandBarOverflowButtonVisibility.Visible;
		}
		return ((Layoutable)this).MeasureOverride(availableSize);
	}

	/// <summary>
	/// Invoked when the <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" /> starts to change from hidden to visible, or starts to be first displayed.
	/// </summary>
	protected virtual void OnOpening()
	{
		Opening?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Invoked when the <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" /> starts to change from visible to hidden.
	/// </summary>
	protected virtual void OnClosing()
	{
		Closing?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Invoked when the <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" /> changes from hidden to visible, or is first displayed.
	/// </summary>
	protected virtual void OnOpened()
	{
		if (_overflowItems != null && _overflowItems.Count > 0)
		{
			IFACommandBarElement iFACommandBarElement = _overflowItems[0];
			((InputElement)((iFACommandBarElement is Control) ? iFACommandBarElement : null)).Focus((NavigationMethod)0, (KeyModifiers)0);
		}
		Opened?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Invoked when the <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" /> changes from visible to hidden.
	/// </summary>
	protected virtual void OnClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);
		Button moreButton = _moreButton;
		if (moreButton != null)
		{
			((InputElement)moreButton).Focus((NavigationMethod)0, (KeyModifiers)0);
		}
	}

	private void OnIsOpenedChanged(bool newValue)
	{
		if (newValue)
		{
			OnOpening();
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":open", true);
			SetElementVisualStateForOpen(open: true);
			OnOpened();
		}
		else
		{
			OnClosing();
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":open", false);
			SetElementVisualStateForOpen(open: false);
			OnClosed();
		}
	}

	private void OnPrimaryCommandsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (!_appliedTemplate)
		{
			return;
		}
		if (_primaryItems == null)
		{
			AttachItems();
		}
		else
		{
			if (IsDynamicOverflowEnabled)
			{
				ReturnOverflowToPrimary();
			}
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Add:
			{
				for (int l = 0; l < e.NewItems.Count; l++)
				{
					if (e.NewItems[l] is IFACommandBarElement { DynamicOverflowOrder: not 0 })
					{
						_hasOrderedOverflow++;
					}
				}
				_primaryItems.InsertRange(e.NewStartingIndex, e.NewItems.Cast<IFACommandBarElement>());
				break;
			}
			case NotifyCollectionChangedAction.Remove:
			{
				for (int k = 0; k < e.OldItems.Count; k++)
				{
					if (e.OldItems[k] is IFACommandBarElement { DynamicOverflowOrder: not 0 })
					{
						_hasOrderedOverflow--;
					}
				}
				_primaryItems.RemoveAll(e.OldItems.Cast<IFACommandBarElement>());
				break;
			}
			case NotifyCollectionChangedAction.Reset:
				_hasOrderedOverflow = 0;
				_primaryItems.Clear();
				break;
			case NotifyCollectionChangedAction.Replace:
			{
				for (int i = 0; i < e.OldItems.Count; i++)
				{
					if (e.OldItems[i] is IFACommandBarElement { DynamicOverflowOrder: not 0 })
					{
						_hasOrderedOverflow--;
					}
				}
				_primaryItems.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
				_primaryItems.InsertRange(e.NewStartingIndex, e.NewItems.Cast<IFACommandBarElement>());
				for (int j = 0; j < e.NewItems.Count; j++)
				{
					if (e.NewItems[j] is IFACommandBarElement { DynamicOverflowOrder: not 0 })
					{
						_hasOrderedOverflow++;
					}
				}
				break;
			}
			case NotifyCollectionChangedAction.Move:
				_primaryItems.Move(e.OldStartingIndex, e.NewStartingIndex);
				break;
			}
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":primaryOnly", _primaryCommands.Count > 0 && _secondaryCommands.Count == 0);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":secondaryOnly", _primaryCommands.Count == 0 && _secondaryCommands.Count > 0);
		((Layoutable)this).InvalidateMeasure();
	}

	private void OnSecondaryCommandsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (!_appliedTemplate)
		{
			return;
		}
		if (_overflowItems == null)
		{
			AttachItems();
		}
		else
		{
			int num = ((_numInOverflow != 0) ? (_numInOverflow + 1) : 0);
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Add:
			{
				for (int l = 0; l < e.NewItems.Count; l++)
				{
					_overflowItems.Insert(e.NewStartingIndex + l + num, e.NewItems[l] as IFACommandBarElement);
				}
				break;
			}
			case NotifyCollectionChangedAction.Remove:
			{
				for (int k = 0; k < e.OldItems.Count; k++)
				{
					_overflowItems.RemoveAt(e.OldStartingIndex + k + num);
				}
				break;
			}
			case NotifyCollectionChangedAction.Reset:
				_overflowItems.RemoveRange(num, _overflowItems.Count - num);
				break;
			case NotifyCollectionChangedAction.Replace:
			case NotifyCollectionChangedAction.Move:
			{
				for (int i = 0; i < e.OldItems.Count; i++)
				{
					_overflowItems.RemoveAt(e.OldStartingIndex + i + num);
				}
				for (int j = 0; j < e.NewItems.Count; j++)
				{
					_overflowItems.Insert(e.NewStartingIndex + j + num, e.NewItems[j] as IFACommandBarElement);
				}
				break;
			}
			}
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":primaryOnly", _primaryCommands.Count > 0 && _secondaryCommands.Count == 0);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":secondaryOnly", _primaryCommands.Count == 0 && _secondaryCommands.Count > 0);
		((Layoutable)this).InvalidateMeasure();
	}

	private void AttachItems()
	{
		if (_primaryCommands.Count > 0)
		{
			_primaryItems = new AvaloniaList<IFACommandBarElement>();
			_primaryItems.CollectionChanged += PrimaryItemsCollectionChanged;
			_primaryItems.AddRange((IEnumerable<IFACommandBarElement>)_primaryCommands);
			if (IsDynamicOverflowEnabled)
			{
				for (int i = 0; i < _primaryItems.Count; i++)
				{
					if (_primaryItems[i].DynamicOverflowOrder != 0)
					{
						_hasOrderedOverflow++;
					}
				}
			}
			_primaryItemsHost.ItemsSource = (IEnumerable)_primaryItems;
		}
		if (_secondaryCommands.Count > 0 || IsDynamicOverflowEnabled)
		{
			FACommandBarSeparator fACommandBarSeparator = new FACommandBarSeparator();
			((Visual)fACommandBarSeparator).IsVisible = false;
			_overflowSeparator = fACommandBarSeparator;
			_overflowItems = new AvaloniaList<IFACommandBarElement>();
			_overflowItems.Add((IFACommandBarElement)_overflowSeparator);
			_overflowItems.AddRange((IEnumerable<IFACommandBarElement>)_secondaryCommands);
			((ItemsControl)_overflowItemsHost).ItemsSource = (IEnumerable)_overflowItems;
		}
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":primaryOnly", _primaryCommands.Count > 0 && _secondaryCommands.Count == 0);
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":secondaryOnly", _primaryCommands.Count == 0 && _secondaryCommands.Count > 0);
	}

	private void PrimaryItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		FACommandBarDefaultLabelPosition defaultLabelPosition = DefaultLabelPosition;
		switch (e.Action)
		{
		case NotifyCollectionChangedAction.Add:
		{
			IList newItems = e.NewItems;
			for (int j = 0; j < newItems.Count; j++)
			{
				object? obj2 = newItems[j];
				Control val2 = (Control)((obj2 is Control) ? obj2 : null);
				if (val2 != null)
				{
					IPseudoClasses classes2 = (IPseudoClasses)(object)((StyledElement)val2).Classes;
					if (classes2 != null)
					{
						PseudoClassesExtensions.Set(classes2, ":labelcollapsed", defaultLabelPosition == FACommandBarDefaultLabelPosition.Collapsed);
						PseudoClassesExtensions.Set(classes2, ":labelright", defaultLabelPosition == FACommandBarDefaultLabelPosition.Right);
						PseudoClassesExtensions.Set(classes2, ":labelbottom", defaultLabelPosition == FACommandBarDefaultLabelPosition.Bottom);
					}
				}
			}
			break;
		}
		case NotifyCollectionChangedAction.Remove:
		case NotifyCollectionChangedAction.Reset:
		{
			IList oldItems = e.OldItems;
			if (oldItems == null)
			{
				break;
			}
			for (int i = 0; i < oldItems.Count; i++)
			{
				object? obj = oldItems[i];
				Control val = (Control)((obj is Control) ? obj : null);
				if (val != null)
				{
					IPseudoClasses classes = (IPseudoClasses)(object)((StyledElement)val).Classes;
					if (classes != null)
					{
						PseudoClassesExtensions.Set(classes, ":labelcollapsed", false);
						PseudoClassesExtensions.Set(classes, ":labelright", false);
						PseudoClassesExtensions.Set(classes, ":labelbottom", false);
					}
				}
			}
			break;
		}
		case NotifyCollectionChangedAction.Replace:
		case NotifyCollectionChangedAction.Move:
			break;
		}
	}

	private void ReturnOverflowToPrimary()
	{
		for (int num = _numInOverflow - 1; num >= 0; num--)
		{
			IFACommandBarElement iFACommandBarElement = _overflowItems[num];
			_overflowItems.RemoveAt(num);
			_primaryItems.Insert(Math.Min(_primaryItems.Count, ((IList<IFACommandBarElement>)_primaryCommands).IndexOf(iFACommandBarElement)), iFACommandBarElement);
		}
		_numInOverflow = 0;
	}

	private void OnMoreButtonClick(object sender, RoutedEventArgs e)
	{
		IsOpen = !IsOpen;
	}

	private IList<IFACommandBarElement> GetNextItemsToOverflow()
	{
		if (_hasOrderedOverflow > 0)
		{
			if (_primaryItems.Count == 0)
			{
				return null;
			}
			List<IFACommandBarElement> list = new List<IFACommandBarElement>(2);
			int num = int.MaxValue;
			bool flag = false;
			for (int num2 = _primaryItems.Count - 1; num2 >= 0; num2--)
			{
				if (_primaryItems[num2].DynamicOverflowOrder != 0)
				{
					flag = true;
					num = Math.Min(num, _primaryItems[num2].DynamicOverflowOrder);
				}
			}
			if (flag)
			{
				for (int i = 0; i < _primaryItems.Count; i++)
				{
					if (_primaryItems[i].DynamicOverflowOrder == num)
					{
						list.Add(_primaryItems[i]);
					}
				}
				return list;
			}
			return new IFACommandBarElement[1] { _primaryItems[_primaryItems.Count - 1] };
		}
		if (_primaryItems.Count == 0)
		{
			return null;
		}
		return new IFACommandBarElement[1] { _primaryItems[_primaryItems.Count - 1] };
	}

	private IList<IFACommandBarElement> GetReturnToPrimaryItems()
	{
		if (_overflowItems[_numInOverflow - 1].DynamicOverflowOrder == 0)
		{
			return new IFACommandBarElement[1] { _overflowItems[_numInOverflow - 1] };
		}
		int dynamicOverflowOrder = _overflowItems[_numInOverflow - 1].DynamicOverflowOrder;
		int num = 1;
		int num2 = _numInOverflow - 2;
		while (num2 >= 0 && _overflowItems[num2].DynamicOverflowOrder == dynamicOverflowOrder)
		{
			num++;
			num2--;
		}
		return _overflowItems.GetRange(_numInOverflow - num, num).ToList();
	}

	private void UpdateWidthCacheForItem(IFACommandBarElement item, double wid)
	{
		if (_widthCache == null)
		{
			_widthCache = new Dictionary<IFACommandBarElement, double>();
		}
		if (_widthCache.ContainsKey(item))
		{
			_widthCache[item] = wid;
		}
		else
		{
			_widthCache.Add(item, wid);
		}
	}

	private void SetElementVisualStateForOpen(bool open)
	{
		if (_primaryItems == null)
		{
			return;
		}
		int i = 0;
		for (int count = _primaryItems.Count; i < count; i++)
		{
			IFACommandBarElement iFACommandBarElement = _primaryItems[i];
			Control val = (Control)((iFACommandBarElement is Control) ? iFACommandBarElement : null);
			if (val != null)
			{
				IPseudoClasses classes = (IPseudoClasses)(object)((StyledElement)val).Classes;
				if (classes != null)
				{
					PseudoClassesExtensions.Set(classes, ":open", open);
				}
			}
		}
	}
}
