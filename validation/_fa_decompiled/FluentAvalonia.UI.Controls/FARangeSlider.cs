using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

[TemplatePart("ActiveRectangle", typeof(Rectangle))]
[TemplatePart("MinThumb", typeof(Thumb))]
[TemplatePart("MaxThumb", typeof(Thumb))]
[TemplatePart("ContainerCanvas", typeof(Canvas))]
[TemplatePart("ToolTipText", typeof(TextBlock))]
public class FARangeSlider : TemplatedControl
{
	private Rectangle _activeRectangle;

	private Thumb _minThumb;

	private Thumb _maxThumb;

	private Canvas _containerCanvas;

	private double _oldValue;

	private bool _valuesAssigned;

	private bool _minSet;

	private bool _maxSet;

	private bool _pointerManipulatingMin;

	private bool _pointerManipulatingMax;

	private bool _pointerManipulatingBoth;

	private double _absolutePosition;

	private Control _toolTip;

	private TextBlock _toolTipText;

	private const double Epsilon = 0.01;

	private bool _isDraggingStart;

	private bool _isDraggingEnd;

	private readonly DispatcherTimer _keyTimer;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.Minimum" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinimumProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.Maximum" /> property
	/// </summary>
	public static readonly StyledProperty<double> MaximumProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.RangeStart" /> property
	/// </summary>
	public static readonly StyledProperty<double> RangeStartProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.RangeEnd" /> property
	/// </summary>
	public static readonly StyledProperty<double> RangeEndProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.StepFrequency" /> property
	/// </summary>
	public static readonly StyledProperty<double> StepFrequencyProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.ToolTipStringFormat" /> property
	/// </summary>
	public static readonly StyledProperty<string> ToolTipStringFormatProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.MinimumRange" /> property
	/// </summary>
	public static readonly StyledProperty<double> MinimumRangeProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.ShowValueToolTip" /> property
	/// </summary>
	public static readonly StyledProperty<bool> ShowValueToolTipProperty;

	private const string s_tpActiveRectangle = "ActiveRectangle";

	private const string s_tpMinThumb = "MinThumb";

	private const string s_tpMaxThumb = "MaxThumb";

	private const string s_tpContainerCanvas = "ContainerCanvas";

	private const string s_tpToolTipText = "ToolTipText";

	/// <summary>
	/// Gets or sets the minimum allowed value for the RangeSlider
	/// </summary>
	public double Minimum
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinimumProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinimumProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the maximum allowed value for the RangeSlider
	/// </summary>
	public double Maximum
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MaximumProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MaximumProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the start of the selected range
	/// </summary>
	public double RangeStart
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(RangeStartProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(RangeStartProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the end of the selected range
	/// </summary>
	public double RangeEnd
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(RangeEndProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(RangeEndProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the frequency of ticks when dragging the slider
	/// </summary>
	public double StepFrequency
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(StepFrequencyProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(StepFrequencyProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the string format used in the value ToolTip when dragging
	/// </summary>
	public string ToolTipStringFormat
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(ToolTipStringFormatProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(ToolTipStringFormatProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the smallest acceptable range between <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.RangeStart" /> and <see cref="P:FluentAvalonia.UI.Controls.FARangeSlider.RangeEnd" />
	/// when dragging the thumb
	/// </summary>
	/// <remarks>
	/// Use this property to set a minimum distance (in data units) the slider thumbs can get during a drag operation
	/// to prevent them from overlapping. NOTE: This property does NOT have any effect if the RangeStart or RangeEnd
	/// is set programmatically, i.e., Start = 30, End = 50, MinimumRange=15, you cannot drag the RangeStart thumb to 40,
	/// but you can still programmatically set RangeStart to 40.
	/// </remarks>
	public double MinimumRange
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(MinimumRangeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(MinimumRangeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the Value ToolTip is shown when dragging a thumb
	/// </summary>
	public bool ShowValueToolTip
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(ShowValueToolTipProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(ShowValueToolTipProperty, value, (BindingPriority)0);
		}
	}

	internal double DragWidth
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			Rect bounds = ((Visual)_containerCanvas).Bounds;
			double width = ((Rect)(ref bounds)).Width;
			bounds = ((Visual)_maxThumb).Bounds;
			return width - ((Rect)(ref bounds)).Width;
		}
	}

	/// <summary>
	/// Fired when a thumb drag begins
	/// </summary>
	public event EventHandler<VectorEventArgs> ThumbDragStarted;

	/// <summary>
	/// Fired when a thumb drag completes
	/// </summary>
	public event EventHandler<VectorEventArgs> ThumbDragCompleted;

	/// <summary>
	/// Fired when either RangeStart or RangeEnd is changed
	/// </summary>
	public event EventHandler<FARangeChangedEventArgs> ValueChanged;

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)RangeStartProperty)
		{
			_minSet = true;
			if (!_valuesAssigned)
			{
				return;
			}
			double newValue = AvaloniaPropertyChangedExtensions.GetNewValue<double>(change);
			RangeMinToStepFrequency();
			if (_valuesAssigned)
			{
				if (newValue < Minimum)
				{
					RangeStart = Minimum;
				}
				else if (newValue > Maximum)
				{
					RangeStart = Maximum;
				}
				SyncActiveRectangle();
				if (newValue > RangeEnd)
				{
					RangeEnd = newValue;
				}
			}
			SyncThumbs();
			if (!_isDraggingEnd && !_isDraggingStart)
			{
				OnValueChanged(new FARangeChangedEventArgs(AvaloniaPropertyChangedExtensions.GetOldValue<double>(change), newValue, FARangeSelectorProperty.RangeStartValue));
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)RangeEndProperty)
		{
			_maxSet = true;
			if (!_valuesAssigned)
			{
				return;
			}
			double newValue2 = AvaloniaPropertyChangedExtensions.GetNewValue<double>(change);
			RangeMaxToStepFrequency();
			if (_valuesAssigned)
			{
				if (newValue2 < Minimum)
				{
					RangeEnd = Minimum;
				}
				else if (newValue2 > Maximum)
				{
					RangeEnd = Maximum;
				}
				SyncActiveRectangle();
				if (newValue2 < RangeStart)
				{
					RangeStart = newValue2;
				}
			}
			SyncThumbs();
			if (!_isDraggingEnd && !_isDraggingStart)
			{
				OnValueChanged(new FARangeChangedEventArgs(AvaloniaPropertyChangedExtensions.GetOldValue<double>(change), newValue2, FARangeSelectorProperty.RangeEndValue));
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)MinimumProperty)
		{
			if (_valuesAssigned)
			{
				var (num, num2) = AvaloniaPropertyChangedExtensions.GetOldAndNewValue<double>(change);
				if (Maximum < num2)
				{
					Maximum = num2 + 0.01;
				}
				if (RangeStart < num2)
				{
					RangeStart = num2;
				}
				if (RangeEnd < num2)
				{
					RangeEnd = num2;
				}
				if (num2 != num)
				{
					SyncThumbs();
				}
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)MaximumProperty && _valuesAssigned)
		{
			var (num3, num4) = AvaloniaPropertyChangedExtensions.GetOldAndNewValue<double>(change);
			if (Minimum > num4)
			{
				Maximum = num4 + 0.01;
			}
			if (RangeEnd > num4)
			{
				RangeEnd = num4;
			}
			if (RangeStart > num4)
			{
				RangeStart = num4;
			}
			if (num4 != num3)
			{
				SyncThumbs();
			}
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_minThumb != null)
		{
			_minThumb.DragCompleted -= HandleThumbDragCompleted;
			_minThumb.DragDelta -= MinThumbDragDelta;
			_minThumb.DragStarted -= MinThumbDragStarted;
			((InputElement)_minThumb).KeyDown -= MinThumbKeyDown;
			((InputElement)_minThumb).KeyUp -= ThumbKeyUp;
			_maxThumb.DragCompleted -= HandleThumbDragCompleted;
			_maxThumb.DragDelta -= MaxThumbDragDelta;
			_maxThumb.DragStarted -= MaxThumbDragStarted;
			((InputElement)_maxThumb).KeyDown -= MaxThumbKeyDown;
			((InputElement)_maxThumb).KeyUp -= ThumbKeyUp;
			((Control)_containerCanvas).SizeChanged -= ContainerCanvasSizeChanged;
			((InputElement)_containerCanvas).PointerPressed -= ContainerCanvasPointerPressed;
			((InputElement)_containerCanvas).PointerMoved -= ContainerCanvasPointerMoved;
			((InputElement)_containerCanvas).PointerReleased -= ContainerCanvasPointerReleased;
			((InputElement)_containerCanvas).PointerExited -= ContainerCanvasPointerExited;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		VerifyValues();
		_valuesAssigned = true;
		_activeRectangle = NameScopeExtensions.Get<Rectangle>(e.NameScope, "ActiveRectangle");
		_minThumb = NameScopeExtensions.Get<Thumb>(e.NameScope, "MinThumb");
		_maxThumb = NameScopeExtensions.Get<Thumb>(e.NameScope, "MaxThumb");
		_containerCanvas = NameScopeExtensions.Get<Canvas>(e.NameScope, "ContainerCanvas");
		_toolTip = NameScopeExtensions.Find<Control>(e.NameScope, "ToolTip");
		_toolTipText = NameScopeExtensions.Find<TextBlock>(e.NameScope, "ToolTipText");
		if (_toolTip != null)
		{
			StyledElement parent = ((StyledElement)_toolTip).Parent;
			Panel val = (Panel)(object)((parent is Panel) ? parent : null);
			if (val != null)
			{
				((AvaloniaList<Control>)(object)val.Children).Remove(_toolTip);
			}
		}
		_minThumb.DragCompleted += HandleThumbDragCompleted;
		_minThumb.DragDelta += MinThumbDragDelta;
		_minThumb.DragStarted += MinThumbDragStarted;
		((InputElement)_minThumb).KeyDown += MinThumbKeyDown;
		((InputElement)_minThumb).KeyUp += ThumbKeyUp;
		_maxThumb.DragCompleted += HandleThumbDragCompleted;
		_maxThumb.DragDelta += MaxThumbDragDelta;
		_maxThumb.DragStarted += MaxThumbDragStarted;
		((InputElement)_maxThumb).KeyDown += MaxThumbKeyDown;
		((InputElement)_maxThumb).KeyUp += ThumbKeyUp;
		((Control)_containerCanvas).SizeChanged += ContainerCanvasSizeChanged;
		((InputElement)_containerCanvas).PointerPressed += ContainerCanvasPointerPressed;
		((InputElement)_containerCanvas).PointerMoved += ContainerCanvasPointerMoved;
		((InputElement)_containerCanvas).PointerReleased += ContainerCanvasPointerReleased;
		((InputElement)_containerCanvas).PointerExited += ContainerCanvasPointerExited;
	}

	protected virtual void OnThumbDragStarted(VectorEventArgs e)
	{
		ThumbDragStarted?.Invoke(this, e);
	}

	protected virtual void OnThumbDragCompleted(VectorEventArgs e)
	{
		ThumbDragCompleted?.Invoke(this, e);
	}

	protected virtual void OnValueChanged(FARangeChangedEventArgs e)
	{
		ValueChanged?.Invoke(this, e);
	}

	private void MinThumbDragDelta(object sender, VectorEventArgs e)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		double absolutePosition = _absolutePosition;
		Vector vector = e.Vector;
		_absolutePosition = absolutePosition + ((Vector)(ref vector)).X;
		double rangeStart = RangeStart;
		double num = DragThumb(_minThumb, 0.0, DragWidth, _absolutePosition);
		double num2 = RangeEnd - MinimumRange;
		if (num > num2)
		{
			RangeEnd += num - rangeStart;
			num -= num - num2;
			RangeStart = num;
		}
		else
		{
			RangeStart = num;
		}
		if (_toolTipText != null)
		{
			UpdateToolTipText(RangeStart);
		}
	}

	private void MaxThumbDragDelta(object sender, VectorEventArgs e)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		double absolutePosition = _absolutePosition;
		Vector vector = e.Vector;
		_absolutePosition = absolutePosition + ((Vector)(ref vector)).X;
		double rangeEnd = RangeEnd;
		double num = DragThumb(_maxThumb, 0.0, DragWidth, _absolutePosition);
		double num2 = RangeStart + MinimumRange;
		if (num < num2)
		{
			RangeStart -= rangeEnd - num;
			num -= num - num2;
			RangeEnd = num;
		}
		else
		{
			RangeEnd = num;
		}
		if (_toolTipText != null)
		{
			UpdateToolTipText(RangeEnd);
		}
	}

	private void MinThumbDragStarted(object sender, VectorEventArgs e)
	{
		_isDraggingStart = true;
		OnThumbDragStarted(e);
		HandleThumbDragStarted(_minThumb);
	}

	private void MaxThumbDragStarted(object sender, VectorEventArgs e)
	{
		_isDraggingEnd = true;
		OnThumbDragStarted(e);
		HandleThumbDragStarted(_maxThumb);
	}

	private void HandleThumbDragCompleted(object sender, VectorEventArgs e)
	{
		_isDraggingStart = (_isDraggingEnd = false);
		OnThumbDragCompleted(e);
		OnValueChanged(sender.Equals(_minThumb) ? new FARangeChangedEventArgs(_oldValue, RangeStart, FARangeSelectorProperty.RangeStartValue) : new FARangeChangedEventArgs(_oldValue, RangeEnd, FARangeSelectorProperty.RangeEndValue));
		SyncThumbs();
		if (_toolTip != null)
		{
			SetToolTipAt((Thumb)((sender is Thumb) ? sender : null), open: false);
		}
	}

	private double DragThumb(Thumb thumb, double min, double max, double nextPos)
	{
		nextPos = Math.Max(min, nextPos);
		nextPos = Math.Min(max, nextPos);
		Canvas.SetLeft((AvaloniaObject)(object)thumb, nextPos);
		return Minimum + nextPos / DragWidth * (Maximum - Minimum);
	}

	private void HandleThumbDragStarted(Thumb thumb)
	{
		bool flag = thumb == _minThumb;
		Thumb obj = (flag ? _maxThumb : _minThumb);
		_absolutePosition = Canvas.GetLeft((AvaloniaObject)(object)thumb);
		((Visual)thumb).ZIndex = 10;
		((Visual)obj).ZIndex = 0;
		_oldValue = RangeStart;
		if (_toolTipText != null && _toolTip != null)
		{
			SetToolTipAt(thumb, open: true);
			UpdateToolTipText(flag ? RangeStart : RangeEnd);
		}
	}

	private void MinThumbKeyDown(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		Key key = e.Key;
		if ((int)key != 23)
		{
			if ((int)key == 25)
			{
				RangeStart += StepFrequency;
				SyncThumbs(fromMinKeyDown: true);
				SetToolTipAt(_minThumb, open: true);
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else
		{
			RangeStart -= StepFrequency;
			SyncThumbs(fromMinKeyDown: true);
			SetToolTipAt(_minThumb, open: true);
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void MaxThumbKeyDown(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		Key key = e.Key;
		Rect bounds;
		if ((int)key != 23)
		{
			if ((int)key == 25)
			{
				RangeEnd += StepFrequency;
				SyncThumbs(fromMinKeyDown: false, fromMaxKeyDown: true);
				if (!ToolTip.GetIsOpen((Control)(object)_maxThumb))
				{
					UnParentToolTip(_toolTip);
					ToolTip.SetTip((Control)(object)_maxThumb, (object)_toolTip);
					ToolTip.SetIsOpen((Control)(object)_maxThumb, true);
					ToolTip.SetPlacement((Control)(object)_maxThumb, (PlacementMode)4);
					Thumb maxThumb = _maxThumb;
					bounds = ((Visual)_containerCanvas).Bounds;
					ToolTip.SetVerticalOffset((Control)(object)maxThumb, 0.0 - ((Rect)(ref bounds)).Height);
				}
				((RoutedEventArgs)e).Handled = true;
			}
		}
		else
		{
			RangeEnd -= StepFrequency;
			SyncThumbs(fromMinKeyDown: false, fromMaxKeyDown: true);
			if (!ToolTip.GetIsOpen((Control)(object)_maxThumb))
			{
				UnParentToolTip(_toolTip);
				ToolTip.SetTip((Control)(object)_maxThumb, (object)_toolTip);
				ToolTip.SetIsOpen((Control)(object)_maxThumb, true);
				ToolTip.SetPlacement((Control)(object)_maxThumb, (PlacementMode)4);
				Thumb maxThumb2 = _maxThumb;
				bounds = ((Visual)_containerCanvas).Bounds;
				ToolTip.SetVerticalOffset((Control)(object)maxThumb2, 0.0 - ((Rect)(ref bounds)).Height);
			}
			((RoutedEventArgs)e).Handled = true;
		}
	}

	private void ThumbKeyUp(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		Key key = e.Key;
		if ((int)key != 23 && (int)key != 25)
		{
			return;
		}
		if (_toolTip != null)
		{
			_keyTimer.Debounce(delegate
			{
				SetToolTipAt(_minThumb, open: false);
				SetToolTipAt(_maxThumb, open: false);
			}, TimeSpan.FromSeconds(1L));
		}
		((RoutedEventArgs)e).Handled = true;
	}

	private void ContainerCanvasPointerExited(object sender, PointerEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		PointerPoint currentPoint = e.GetCurrentPoint((Visual)(object)_containerCanvas);
		Point position = ((PointerPoint)(ref currentPoint)).Position;
		double x = ((Point)(ref position)).X;
		Rect bounds = ((Visual)_containerCanvas).Bounds;
		if (x >= ((Rect)(ref bounds)).Left)
		{
			double x2 = ((Point)(ref position)).X;
			bounds = ((Visual)_containerCanvas).Bounds;
			if (x2 <= ((Rect)(ref bounds)).Right)
			{
				double y = ((Point)(ref position)).Y;
				bounds = ((Visual)_containerCanvas).Bounds;
				if (y >= ((Rect)(ref bounds)).Top)
				{
					double y2 = ((Point)(ref position)).Y;
					bounds = ((Visual)_containerCanvas).Bounds;
					if (y2 <= ((Rect)(ref bounds)).Bottom)
					{
						return;
					}
				}
			}
		}
		double newValue = ((Point)(ref position)).X / DragWidth * (Maximum - Minimum) + Minimum;
		if (_pointerManipulatingMin)
		{
			_pointerManipulatingMin = false;
			((InputElement)_containerCanvas).IsHitTestVisible = true;
			OnValueChanged(new FARangeChangedEventArgs(RangeStart, newValue, FARangeSelectorProperty.RangeStartValue));
		}
		else if (_pointerManipulatingMax)
		{
			_pointerManipulatingMax = false;
			((InputElement)_containerCanvas).IsHitTestVisible = true;
			OnValueChanged(new FARangeChangedEventArgs(RangeEnd, newValue, FARangeSelectorProperty.RangeEndValue));
		}
	}

	private void ContainerCanvasPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		_pointerManipulatingBoth = false;
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)_containerCanvas);
		Point position = ((PointerPoint)(ref currentPoint)).Position;
		double newValue = ((Point)(ref position)).X / DragWidth * (Maximum - Minimum) + Minimum;
		if (_toolTip != null)
		{
			Thumb val = (_pointerManipulatingMax ? _maxThumb : (_pointerManipulatingMin ? _minThumb : null));
			if (val == null)
			{
				return;
			}
			ToolTip.SetIsOpen((Control)(object)val, false);
			UnParentToolTip(_toolTip);
			ToolTip.SetTip((Control)(object)val, (object)null);
		}
		if (_pointerManipulatingMin)
		{
			_pointerManipulatingMin = false;
			((InputElement)_containerCanvas).IsHitTestVisible = true;
			OnValueChanged(new FARangeChangedEventArgs(RangeStart, newValue, FARangeSelectorProperty.RangeStartValue));
		}
		else if (_pointerManipulatingMax)
		{
			_pointerManipulatingMax = false;
			((InputElement)_containerCanvas).IsHitTestVisible = true;
			OnValueChanged(new FARangeChangedEventArgs(RangeEnd, newValue, FARangeSelectorProperty.RangeEndValue));
		}
		SyncThumbs();
	}

	private void ContainerCanvasPointerMoved(object sender, PointerEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		PointerPoint currentPoint = e.GetCurrentPoint((Visual)(object)_containerCanvas);
		Point position = ((PointerPoint)(ref currentPoint)).Position;
		double x = ((Point)(ref position)).X;
		if (_pointerManipulatingBoth)
		{
			double maximum = Maximum;
			double minimum = Minimum;
			double num = (x - _absolutePosition) / DragWidth * (maximum - minimum);
			if (Math.Abs(num) < StepFrequency)
			{
				return;
			}
			double rangeStart = RangeStart;
			double rangeEnd = RangeEnd;
			if (num > 0.0)
			{
				if (FAMathHelpers.IsClose(rangeEnd, maximum))
				{
					return;
				}
				if (rangeEnd + num > maximum)
				{
					num = maximum - rangeEnd;
				}
			}
			else if (num < 0.0)
			{
				if (FAMathHelpers.IsClose(rangeStart, minimum))
				{
					return;
				}
				if (rangeStart + num < minimum)
				{
					num = minimum - rangeStart;
				}
			}
			RangeStart += num;
			RangeEnd += num;
			_absolutePosition = x;
		}
		else
		{
			double num2 = x / DragWidth * (Maximum - Minimum) + Minimum;
			if (_pointerManipulatingMin && num2 < RangeEnd)
			{
				RangeStart = DragThumb(_minThumb, 0.0, Canvas.GetLeft((AvaloniaObject)(object)_maxThumb), x);
				UpdateToolTipText(RangeStart);
			}
			else if (_pointerManipulatingMax && num2 > RangeStart)
			{
				RangeEnd = DragThumb(_maxThumb, Canvas.GetLeft((AvaloniaObject)(object)_minThumb), DragWidth, x);
				UpdateToolTipText(RangeEnd);
			}
		}
	}

	private void ContainerCanvasPointerPressed(object sender, PointerPressedEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		PointerPoint currentPoint = ((PointerEventArgs)e).GetCurrentPoint((Visual)(object)_containerCanvas);
		Point position = ((PointerPoint)(ref currentPoint)).Position;
		double x = ((Point)(ref position)).X;
		KeyModifiers val = Application.Current.PlatformSettings.HotkeyConfiguration.CommandModifiers;
		if ((int)val == 0)
		{
			val = (KeyModifiers)2;
		}
		if ((KeyModifiers)(((PointerEventArgs)e).KeyModifiers & val) == val)
		{
			_pointerManipulatingBoth = true;
			_absolutePosition = x;
			return;
		}
		double num = x * Math.Abs(Maximum - Minimum) / DragWidth;
		double num2 = Math.Abs(RangeEnd - num);
		double num3 = Math.Abs(RangeStart - num);
		if (num2 < num3)
		{
			RangeEnd = num;
			_pointerManipulatingMax = true;
			HandleThumbDragStarted(_maxThumb);
		}
		else
		{
			RangeStart = num;
			_pointerManipulatingMin = true;
			HandleThumbDragStarted(_minThumb);
		}
		SyncThumbs();
	}

	private void UpdateToolTipText(double newValue)
	{
		if (_toolTipText != null)
		{
			string text = ToolTipStringFormat ?? "0.##";
			_toolTipText.Text = newValue.ToString(text);
		}
	}

	private void VerifyValues()
	{
		if (Minimum > Maximum)
		{
			Minimum = Maximum;
			Maximum = Maximum;
		}
		if (Minimum == Maximum)
		{
			Maximum += 0.01;
		}
		if (!_maxSet)
		{
			RangeEnd = Maximum;
		}
		if (!_minSet)
		{
			RangeStart = Minimum;
		}
		if (RangeStart < Minimum)
		{
			RangeStart = Minimum;
		}
		if (RangeEnd < Minimum)
		{
			RangeEnd = Minimum;
		}
		if (RangeStart > Maximum)
		{
			RangeStart = Maximum;
		}
		if (RangeEnd > Maximum)
		{
			RangeEnd = Maximum;
		}
		if (RangeEnd < RangeStart)
		{
			RangeStart = RangeEnd;
		}
	}

	private void RangeMinToStepFrequency()
	{
		RangeStart = MoveToStepFrequency(RangeStart);
	}

	private void RangeMaxToStepFrequency()
	{
		RangeEnd = MoveToStepFrequency(RangeEnd);
	}

	private double MoveToStepFrequency(double rangeValue)
	{
		double num = Minimum + (double)(int)Math.Round((rangeValue - Minimum) / StepFrequency) * StepFrequency;
		if (num < Minimum)
		{
			return Minimum;
		}
		if (num > Maximum || Maximum - num < StepFrequency)
		{
			return Maximum;
		}
		return num;
	}

	private void SyncThumbs(bool fromMinKeyDown = false, bool fromMaxKeyDown = false)
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (_containerCanvas == null)
		{
			return;
		}
		double num = (RangeStart - Minimum) / (Maximum - Minimum) * DragWidth;
		double num2 = (RangeEnd - Minimum) / (Maximum - Minimum) * DragWidth;
		Canvas.SetLeft((AvaloniaObject)(object)_minThumb, num);
		Canvas.SetLeft((AvaloniaObject)(object)_maxThumb, num2);
		if (_isDraggingStart)
		{
			_absolutePosition += num - _absolutePosition;
		}
		else if (_isDraggingEnd)
		{
			_absolutePosition += num2 - _absolutePosition;
		}
		Rect bounds = ((Visual)_containerCanvas).Bounds;
		double num3 = ((Rect)(ref bounds)).Height / 2.0;
		bounds = ((Visual)_minThumb).Bounds;
		double num4 = num3 - ((Rect)(ref bounds)).Height / 2.0;
		Canvas.SetTop((AvaloniaObject)(object)_minThumb, num4);
		Canvas.SetTop((AvaloniaObject)(object)_maxThumb, num4);
		if (fromMinKeyDown | fromMaxKeyDown)
		{
			DragThumb(fromMinKeyDown ? _minThumb : _maxThumb, fromMinKeyDown ? 0.0 : Canvas.GetLeft((AvaloniaObject)(object)_minThumb), fromMinKeyDown ? Canvas.GetLeft((AvaloniaObject)(object)_maxThumb) : DragWidth, fromMinKeyDown ? num : num2);
			if (_toolTipText != null)
			{
				UpdateToolTipText(fromMinKeyDown ? RangeStart : RangeEnd);
			}
		}
		SyncActiveRectangle();
	}

	private void SyncActiveRectangle()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (_containerCanvas != null && _minThumb != null && _maxThumb != null)
		{
			double left = Canvas.GetLeft((AvaloniaObject)(object)_minThumb);
			Canvas.SetLeft((AvaloniaObject)(object)_activeRectangle, left);
			Rectangle activeRectangle = _activeRectangle;
			Rect bounds = ((Visual)_containerCanvas).Bounds;
			double height = ((Rect)(ref bounds)).Height;
			bounds = ((Visual)_activeRectangle).Bounds;
			Canvas.SetTop((AvaloniaObject)(object)activeRectangle, (height - ((Rect)(ref bounds)).Height) / 2.0);
			((Layoutable)_activeRectangle).Width = Math.Max(0.0, Canvas.GetLeft((AvaloniaObject)(object)_maxThumb) - Canvas.GetLeft((AvaloniaObject)(object)_minThumb));
		}
	}

	private void ContainerCanvasSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SyncThumbs();
	}

	private void SetToolTipAt(Thumb thumb, bool open)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (ShowValueToolTip)
		{
			if (open && !ToolTip.GetIsOpen((Control)(object)thumb))
			{
				UnParentToolTip(_toolTip);
				ToolTip.SetTip((Control)(object)thumb, (object)_toolTip);
				ToolTip.SetIsOpen((Control)(object)thumb, true);
				ToolTip.SetPlacement((Control)(object)thumb, (PlacementMode)4);
				Rect bounds = ((Visual)_containerCanvas).Bounds;
				ToolTip.SetVerticalOffset((Control)(object)thumb, 0.0 - ((Rect)(ref bounds)).Height);
			}
			else if (!open)
			{
				ToolTip.SetIsOpen((Control)(object)thumb, false);
				UnParentToolTip(_toolTip);
				ToolTip.SetTip((Control)(object)thumb, (object)null);
			}
		}
	}

	private static void UnParentToolTip(Control c)
	{
		StyledElement parent = ((StyledElement)c).Parent;
		Panel val = (Panel)(object)((parent is Panel) ? parent : null);
		if (val != null)
		{
			((AvaloniaList<Control>)(object)val.Children).Remove(c);
			return;
		}
		StyledElement parent2 = ((StyledElement)c).Parent;
		ContentControl val2 = (ContentControl)(object)((parent2 is ContentControl) ? parent2 : null);
		if (val2 != null)
		{
			val2.Content = null;
			return;
		}
		StyledElement parent3 = ((StyledElement)c).Parent;
		Decorator val3 = (Decorator)(object)((parent3 is Decorator) ? parent3 : null);
		if (val3 != null)
		{
			val3.Child = null;
		}
	}

	public FARangeSlider()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		_keyTimer = new DispatcherTimer();
		((TemplatedControl)this)._002Ector();
	}

	static FARangeSlider()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		MinimumProperty = RangeBase.MinimumProperty.AddOwner<FARangeSlider>(new StyledPropertyMetadata<double>(Optional<double>.op_Implicit(0.0), (BindingMode)0, (Func<AvaloniaObject, double, double>)null, false));
		MaximumProperty = RangeBase.MaximumProperty.AddOwner<FARangeSlider>(new StyledPropertyMetadata<double>(Optional<double>.op_Implicit(100.0), (BindingMode)0, (Func<AvaloniaObject, double, double>)null, false));
		RangeStartProperty = AvaloniaProperty.Register<FARangeSlider, double>("RangeStart", 0.0, false, (BindingMode)2, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		RangeEndProperty = AvaloniaProperty.Register<FARangeSlider, double>("RangeEnd", 100.0, false, (BindingMode)2, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		StepFrequencyProperty = AvaloniaProperty.Register<FARangeSlider, double>("StepFrequency", 1.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		ToolTipStringFormatProperty = AvaloniaProperty.Register<FARangeSlider, string>("ToolTipStringFormat", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);
		MinimumRangeProperty = AvaloniaProperty.Register<FARangeSlider, double>("MinimumRange", 0.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		ShowValueToolTipProperty = AvaloniaProperty.Register<FARangeSlider, bool>("ShowValueToolTip", true, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
	}
}
