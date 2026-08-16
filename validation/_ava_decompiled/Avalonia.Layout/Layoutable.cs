using System;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Layout;

/// <summary>
/// Implements layout-related functionality for a control.
/// </summary>
public class Layoutable : Visual
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.DesiredSize" /> property.
	/// </summary>
	public static readonly DirectProperty<Layoutable, Size> DesiredSizeProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.Width" /> property.
	/// </summary>
	public static readonly StyledProperty<double> WidthProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.Height" /> property.
	/// </summary>
	public static readonly StyledProperty<double> HeightProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.MinWidth" /> property.
	/// </summary>
	public static readonly StyledProperty<double> MinWidthProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.MaxWidth" /> property.
	/// </summary>
	public static readonly StyledProperty<double> MaxWidthProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.MinHeight" /> property.
	/// </summary>
	public static readonly StyledProperty<double> MinHeightProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.MaxHeight" /> property.
	/// </summary>
	public static readonly StyledProperty<double> MaxHeightProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.Margin" /> property.
	/// </summary>
	public static readonly StyledProperty<Thickness> MarginProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.HorizontalAlignment" /> property.
	/// </summary>
	public static readonly StyledProperty<HorizontalAlignment> HorizontalAlignmentProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.VerticalAlignment" /> property.
	/// </summary>
	public static readonly StyledProperty<VerticalAlignment> VerticalAlignmentProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Layout.Layoutable.UseLayoutRounding" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> UseLayoutRoundingProperty;

	private bool _measuring;

	private Size? _previousMeasure;

	private Rect? _previousArrange;

	private EventHandler<EffectiveViewportChangedEventArgs>? _effectiveViewportChanged;

	private EventHandler? _layoutUpdated;

	private bool _isAttachingToVisualTree;

	/// <summary>
	/// Gets or sets the width of the element.
	/// </summary>
	public double Width
	{
		get
		{
			return GetValue(WidthProperty);
		}
		set
		{
			SetValue(WidthProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the height of the element.
	/// </summary>
	public double Height
	{
		get
		{
			return GetValue(HeightProperty);
		}
		set
		{
			SetValue(HeightProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the minimum width of the element.
	/// </summary>
	public double MinWidth
	{
		get
		{
			return GetValue(MinWidthProperty);
		}
		set
		{
			SetValue(MinWidthProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the maximum width of the element.
	/// </summary>
	public double MaxWidth
	{
		get
		{
			return GetValue(MaxWidthProperty);
		}
		set
		{
			SetValue(MaxWidthProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the minimum height of the element.
	/// </summary>
	public double MinHeight
	{
		get
		{
			return GetValue(MinHeightProperty);
		}
		set
		{
			SetValue(MinHeightProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the maximum height of the element.
	/// </summary>
	public double MaxHeight
	{
		get
		{
			return GetValue(MaxHeightProperty);
		}
		set
		{
			SetValue(MaxHeightProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the margin around the element.
	/// </summary>
	public Thickness Margin
	{
		get
		{
			return GetValue(MarginProperty);
		}
		set
		{
			SetValue(MarginProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the element's preferred horizontal alignment in its parent.
	/// </summary>
	public HorizontalAlignment HorizontalAlignment
	{
		get
		{
			return GetValue(HorizontalAlignmentProperty);
		}
		set
		{
			SetValue(HorizontalAlignmentProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the element's preferred vertical alignment in its parent.
	/// </summary>
	public VerticalAlignment VerticalAlignment
	{
		get
		{
			return GetValue(VerticalAlignmentProperty);
		}
		set
		{
			SetValue(VerticalAlignmentProperty, value);
		}
	}

	/// <summary>
	/// Gets the size that this element computed during the measure pass of the layout process.
	/// </summary>
	public Size DesiredSize { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the control's layout measure is valid.
	/// </summary>
	public bool IsMeasureValid { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the control's layouts arrange is valid.
	/// </summary>
	public bool IsArrangeValid { get; private set; }

	/// <summary>
	/// Gets or sets a value that determines whether the element should be snapped to pixel
	/// boundaries at layout time.
	/// </summary>
	public bool UseLayoutRounding
	{
		get
		{
			return GetValue(UseLayoutRoundingProperty);
		}
		set
		{
			SetValue(UseLayoutRoundingProperty, value);
		}
	}

	/// <summary>
	/// Gets the available size passed in the previous layout pass, if any.
	/// </summary>
	internal Size? PreviousMeasure => _previousMeasure;

	/// <summary>
	/// Gets the layout rect passed in the previous layout pass, if any.
	/// </summary>
	internal Rect? PreviousArrange => _previousArrange;

	/// <summary>
	/// Occurs when the element's effective viewport changes.
	/// </summary>
	public event EventHandler<EffectiveViewportChangedEventArgs>? EffectiveViewportChanged
	{
		add
		{
			if (_effectiveViewportChanged == null)
			{
				ILayoutRoot layoutRoot = this.GetLayoutRoot();
				if (layoutRoot != null && !_isAttachingToVisualTree)
				{
					layoutRoot.LayoutManager.RegisterEffectiveViewportListener(this);
				}
			}
			_effectiveViewportChanged = (EventHandler<EffectiveViewportChangedEventArgs>)Delegate.Combine(_effectiveViewportChanged, value);
		}
		remove
		{
			_effectiveViewportChanged = (EventHandler<EffectiveViewportChangedEventArgs>)Delegate.Remove(_effectiveViewportChanged, value);
			if (_effectiveViewportChanged == null)
			{
				this.GetLayoutRoot()?.LayoutManager.UnregisterEffectiveViewportListener(this);
			}
		}
	}

	/// <summary>
	/// Occurs when a layout pass completes for the control.
	/// </summary>
	public event EventHandler? LayoutUpdated
	{
		add
		{
			if (_layoutUpdated == null)
			{
				ILayoutRoot layoutRoot = this.GetLayoutRoot();
				if (layoutRoot != null && !_isAttachingToVisualTree)
				{
					layoutRoot.LayoutManager.LayoutUpdated += LayoutManagedLayoutUpdated;
				}
			}
			_layoutUpdated = (EventHandler)Delegate.Combine(_layoutUpdated, value);
		}
		remove
		{
			_layoutUpdated = (EventHandler)Delegate.Remove(_layoutUpdated, value);
			if (_layoutUpdated == null)
			{
				ILayoutRoot layoutRoot = this.GetLayoutRoot();
				if (layoutRoot != null)
				{
					layoutRoot.LayoutManager.LayoutUpdated -= LayoutManagedLayoutUpdated;
				}
			}
		}
	}

	/// <summary>
	/// Initializes static members of the <see cref="T:Avalonia.Layout.Layoutable" /> class.
	/// </summary>
	static Layoutable()
	{
		DesiredSizeProperty = AvaloniaProperty.RegisterDirect("DesiredSize", (Layoutable o) => o.DesiredSize);
		WidthProperty = AvaloniaProperty.Register<Layoutable, double>("Width", double.NaN, inherits: false, BindingMode.OneWay, ValidateDimension);
		HeightProperty = AvaloniaProperty.Register<Layoutable, double>("Height", double.NaN, inherits: false, BindingMode.OneWay, ValidateDimension);
		MinWidthProperty = AvaloniaProperty.Register<Layoutable, double>("MinWidth", 0.0, inherits: false, BindingMode.OneWay, ValidateMinimumDimension);
		MaxWidthProperty = AvaloniaProperty.Register<Layoutable, double>("MaxWidth", double.PositiveInfinity, inherits: false, BindingMode.OneWay, ValidateMaximumDimension);
		MinHeightProperty = AvaloniaProperty.Register<Layoutable, double>("MinHeight", 0.0, inherits: false, BindingMode.OneWay, ValidateMinimumDimension);
		MaxHeightProperty = AvaloniaProperty.Register<Layoutable, double>("MaxHeight", double.PositiveInfinity, inherits: false, BindingMode.OneWay, ValidateMaximumDimension);
		Func<Thickness, bool> validate = ValidateThickness;
		MarginProperty = AvaloniaProperty.Register<Layoutable, Thickness>("Margin", default(Thickness), inherits: false, BindingMode.OneWay, validate);
		HorizontalAlignmentProperty = AvaloniaProperty.Register<Layoutable, HorizontalAlignment>("HorizontalAlignment", HorizontalAlignment.Stretch);
		VerticalAlignmentProperty = AvaloniaProperty.Register<Layoutable, VerticalAlignment>("VerticalAlignment", VerticalAlignment.Stretch);
		UseLayoutRoundingProperty = AvaloniaProperty.Register<Layoutable, bool>("UseLayoutRounding", defaultValue: true, inherits: true);
		AffectsMeasure<Layoutable>(new AvaloniaProperty[9] { WidthProperty, HeightProperty, MinWidthProperty, MaxWidthProperty, MinHeightProperty, MaxHeightProperty, MarginProperty, HorizontalAlignmentProperty, VerticalAlignmentProperty });
	}

	private static bool ValidateDimension(double value)
	{
		if (!double.IsNaN(value))
		{
			return ValidateMinimumDimension(value);
		}
		return true;
	}

	private static bool ValidateMinimumDimension(double value)
	{
		if (!double.IsPositiveInfinity(value))
		{
			return ValidateMaximumDimension(value);
		}
		return false;
	}

	private static bool ValidateMaximumDimension(double value)
	{
		return value >= 0.0;
	}

	private static bool ValidateThickness(Thickness value)
	{
		if (double.IsFinite(value.Left) && double.IsFinite(value.Top) && double.IsFinite(value.Right))
		{
			return double.IsFinite(value.Bottom);
		}
		return false;
	}

	/// <summary>
	/// Executes a layout pass.
	/// </summary>
	/// <remarks>
	/// You should not usually need to call this method explictly, the layout manager will
	/// schedule layout passes itself.
	/// </remarks>
	public void UpdateLayout()
	{
		this.GetLayoutManager()?.ExecuteLayoutPass();
	}

	/// <summary>
	/// Creates the visual children of the control, if necessary
	/// </summary>
	public virtual void ApplyTemplate()
	{
	}

	/// <summary>
	/// Carries out a measure of the control.
	/// </summary>
	/// <param name="availableSize">The available size for the control.</param>
	public void Measure(Size availableSize)
	{
		if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
		{
			throw new InvalidOperationException("Cannot call Measure using a size with NaN values.");
		}
		if (IsMeasureValid && !(_previousMeasure != availableSize))
		{
			return;
		}
		using (Diagnostic.MeasuringLayoutable()?.AddTag("Control", this))
		{
			Size desiredSize = DesiredSize;
			Size size = default(Size);
			IsMeasureValid = true;
			try
			{
				_measuring = true;
				size = MeasureCore(availableSize);
			}
			finally
			{
				_measuring = false;
			}
			if (IsInvalidSize(size))
			{
				throw new InvalidOperationException("Invalid size returned for Measure.");
			}
			DesiredSize = size;
			_previousMeasure = availableSize;
			Logger.TryGet(LogEventLevel.Verbose, "Layout")?.Log(this, "Measure requested {DesiredSize}", DesiredSize);
			if (DesiredSize != desiredSize)
			{
				this.GetVisualParent<Layoutable>()?.ChildDesiredSizeChanged(this);
			}
		}
	}

	/// <summary>
	/// Arranges the control and its children.
	/// </summary>
	/// <param name="rect">The control's new bounds.</param>
	public void Arrange(Rect rect)
	{
		if (IsInvalidRect(rect))
		{
			throw new InvalidOperationException("Invalid Arrange rectangle.");
		}
		if (!IsMeasureValid)
		{
			Measure(_previousMeasure ?? rect.Size);
		}
		if (!IsArrangeValid || _previousArrange != rect)
		{
			using (Diagnostic.ArrangingLayoutable()?.AddTag("Control", this))
			{
				Logger.TryGet(LogEventLevel.Verbose, "Layout")?.Log(this, "Arrange to {Rect} ", rect);
				IsArrangeValid = true;
				ArrangeCore(rect);
				_previousArrange = rect;
			}
		}
	}

	/// <summary>
	/// Invalidates the measurement of the control and queues a new layout pass.
	/// </summary>
	public void InvalidateMeasure()
	{
		if (IsMeasureValid)
		{
			Logger.TryGet(LogEventLevel.Verbose, "Layout")?.Log(this, "Invalidated measure");
			IsMeasureValid = false;
			IsArrangeValid = false;
			if (base.IsAttachedToVisualTree)
			{
				this.GetLayoutManager()?.InvalidateMeasure(this);
				InvalidateVisual();
			}
			OnMeasureInvalidated();
		}
	}

	/// <summary>
	/// Invalidates the arrangement of the control and queues a new layout pass.
	/// </summary>
	public void InvalidateArrange()
	{
		if (IsArrangeValid)
		{
			Logger.TryGet(LogEventLevel.Verbose, "Layout")?.Log(this, "Invalidated arrange");
			IsArrangeValid = false;
			this.GetLayoutManager()?.InvalidateArrange(this);
			InvalidateVisual();
		}
	}

	/// <summary>
	/// Called when a child control's desired size changes.
	/// </summary>
	/// <param name="control">The child control.</param>
	internal void ChildDesiredSizeChanged(Layoutable control)
	{
		if (!_measuring)
		{
			InvalidateMeasure();
		}
	}

	internal void RaiseEffectiveViewportChanged(EffectiveViewportChangedEventArgs e)
	{
		_effectiveViewportChanged?.Invoke(this, e);
	}

	/// <summary>
	/// Marks a property as affecting the control's measurement.
	/// </summary>
	/// <typeparam name="T">The control which the property affects.</typeparam>
	/// <param name="properties">The properties.</param>
	/// <remarks>
	/// After a call to this method in a control's static constructor, any change to the
	/// property will cause <see cref="M:Avalonia.Layout.Layoutable.InvalidateMeasure" /> to be called on the element.
	/// </remarks>
	protected static void AffectsMeasure<T>(params AvaloniaProperty[] properties) where T : Layoutable
	{
		AnonymousObserver<AvaloniaPropertyChangedEventArgs> observer = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(delegate(AvaloniaPropertyChangedEventArgs e)
		{
			(e.Sender as T)?.InvalidateMeasure();
		});
		for (int num = 0; num < properties.Length; num++)
		{
			properties[num].Changed.Subscribe(observer);
		}
	}

	/// <summary>
	/// Marks a property as affecting the control's arrangement.
	/// </summary>
	/// <typeparam name="T">The control which the property affects.</typeparam>
	/// <param name="properties">The properties.</param>
	/// <remarks>
	/// After a call to this method in a control's static constructor, any change to the
	/// property will cause <see cref="M:Avalonia.Layout.Layoutable.InvalidateArrange" /> to be called on the element.
	/// </remarks>
	protected static void AffectsArrange<T>(params AvaloniaProperty[] properties) where T : Layoutable
	{
		AnonymousObserver<AvaloniaPropertyChangedEventArgs> observer = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(delegate(AvaloniaPropertyChangedEventArgs e)
		{
			(e.Sender as T)?.InvalidateArrange();
		});
		for (int num = 0; num < properties.Length; num++)
		{
			properties[num].Changed.Subscribe(observer);
		}
	}

	/// <summary>
	/// The default implementation of the control's measure pass.
	/// </summary>
	/// <param name="availableSize">The size available to the control.</param>
	/// <returns>The desired size for the control.</returns>
	/// <remarks>
	/// This method calls <see cref="M:Avalonia.Layout.Layoutable.MeasureOverride(Avalonia.Size)" /> which is probably the method you
	/// want to override in order to modify a control's arrangement.
	/// </remarks>
	protected virtual Size MeasureCore(Size availableSize)
	{
		if (base.IsVisible)
		{
			Thickness thickness = Margin;
			bool useLayoutRounding = UseLayoutRounding;
			double dpiScale = 1.0;
			if (useLayoutRounding)
			{
				dpiScale = LayoutHelper.GetLayoutScale(this);
				thickness = LayoutHelper.RoundLayoutThickness(thickness, dpiScale);
			}
			ApplyStyling();
			ApplyTemplate();
			MinMax minMax = new MinMax(this);
			Size availableSize2 = LayoutHelper.ApplyLayoutConstraints(minMax, availableSize.Deflate(thickness));
			bool flag = false;
			ContainerSizing containerSizing = ContainerSizing.Normal;
			VisualQueryProvider queryProvider = Container.GetQueryProvider(this);
			if (queryProvider != null)
			{
				ContainerSizing sizing = Container.GetSizing(this);
				if (sizing != ContainerSizing.Normal)
				{
					flag = true;
					containerSizing = sizing;
					queryProvider.SetSize(availableSize2.Width, availableSize2.Height, containerSizing);
				}
			}
			Size size = MeasureOverride(availableSize2);
			double num = MathUtilities.Clamp(size.Width, minMax.MinWidth, minMax.MaxWidth);
			double num2 = MathUtilities.Clamp(size.Height, minMax.MinHeight, minMax.MaxHeight);
			if (flag)
			{
				switch (containerSizing)
				{
				case ContainerSizing.Width:
					num = (double.IsInfinity(availableSize2.Width) ? num : availableSize2.Width);
					break;
				case ContainerSizing.Height:
					num = size.Width;
					num2 = (double.IsInfinity(availableSize2.Height) ? num2 : availableSize2.Height);
					break;
				case ContainerSizing.WidthAndHeight:
					num = (double.IsInfinity(availableSize2.Width) ? num : availableSize2.Width);
					num2 = (double.IsInfinity(availableSize2.Height) ? num2 : availableSize2.Height);
					break;
				}
			}
			if (useLayoutRounding)
			{
				(num, num2) = LayoutHelper.RoundLayoutSizeUp(new Size(num, num2), dpiScale);
			}
			num += thickness.Left + thickness.Right;
			num2 += thickness.Top + thickness.Bottom;
			if (num > availableSize.Width)
			{
				num = availableSize.Width;
			}
			if (num2 > availableSize.Height)
			{
				num2 = availableSize.Height;
			}
			if (num < 0.0)
			{
				num = 0.0;
			}
			if (num2 < 0.0)
			{
				num2 = 0.0;
			}
			return new Size(num, num2);
		}
		return default(Size);
	}

	/// <summary>
	/// Measures the control and its child elements as part of a layout pass.
	/// </summary>
	/// <param name="availableSize">The size available to the control.</param>
	/// <returns>The desired size for the control.</returns>
	protected virtual Size MeasureOverride(Size availableSize)
	{
		double num = 0.0;
		double num2 = 0.0;
		IAvaloniaList<Visual> visualChildren = base.VisualChildren;
		int count = visualChildren.Count;
		for (int i = 0; i < count; i++)
		{
			if (visualChildren[i] is Layoutable layoutable)
			{
				layoutable.Measure(availableSize);
				Size desiredSize = layoutable.DesiredSize;
				if (desiredSize.Width > num)
				{
					num = desiredSize.Width;
				}
				if (desiredSize.Height > num2)
				{
					num2 = desiredSize.Height;
				}
			}
		}
		return new Size(num, num2);
	}

	/// <summary>
	/// The default implementation of the control's arrange pass.
	/// </summary>
	/// <param name="finalRect">The control's new bounds.</param>
	/// <remarks>
	/// This method calls <see cref="M:Avalonia.Layout.Layoutable.ArrangeOverride(Avalonia.Size)" /> which is probably the method you
	/// want to override in order to modify a control's arrangement.
	/// </remarks>
	protected virtual void ArrangeCore(Rect finalRect)
	{
		if (base.IsVisible)
		{
			bool useLayoutRounding = UseLayoutRounding;
			double layoutScale = LayoutHelper.GetLayoutScale(this);
			Thickness thickness = Margin;
			double num = finalRect.X + thickness.Left;
			double num2 = finalRect.Y + thickness.Top;
			if (useLayoutRounding)
			{
				thickness = LayoutHelper.RoundLayoutThickness(thickness, layoutScale);
			}
			double num3 = finalRect.Width - thickness.Left - thickness.Right;
			if (num3 < 0.0)
			{
				num3 = 0.0;
			}
			double num4 = finalRect.Height - thickness.Top - thickness.Bottom;
			if (num4 < 0.0)
			{
				num4 = 0.0;
			}
			Size size = new Size(num3, num4);
			HorizontalAlignment horizontalAlignment = HorizontalAlignment;
			VerticalAlignment verticalAlignment = VerticalAlignment;
			Size constraints = size;
			if (horizontalAlignment != HorizontalAlignment.Stretch)
			{
				constraints = constraints.WithWidth(Math.Min(constraints.Width, DesiredSize.Width - thickness.Left - thickness.Right));
			}
			if (verticalAlignment != VerticalAlignment.Stretch)
			{
				constraints = constraints.WithHeight(Math.Min(constraints.Height, DesiredSize.Height - thickness.Top - thickness.Bottom));
			}
			constraints = LayoutHelper.ApplyLayoutConstraints(new MinMax(this), constraints);
			if (useLayoutRounding)
			{
				constraints = LayoutHelper.RoundLayoutSizeUp(constraints, layoutScale);
				size = LayoutHelper.RoundLayoutSizeUp(size, layoutScale);
			}
			constraints = ArrangeOverride(constraints).Constrain(constraints);
			switch (horizontalAlignment)
			{
			case HorizontalAlignment.Stretch:
			case HorizontalAlignment.Center:
				num += (size.Width - constraints.Width) / 2.0;
				break;
			case HorizontalAlignment.Right:
				num += size.Width - constraints.Width;
				break;
			}
			switch (verticalAlignment)
			{
			case VerticalAlignment.Stretch:
			case VerticalAlignment.Center:
				num2 += (size.Height - constraints.Height) / 2.0;
				break;
			case VerticalAlignment.Bottom:
				num2 += size.Height - constraints.Height;
				break;
			}
			Point point = new Point(num, num2);
			if (useLayoutRounding)
			{
				point = LayoutHelper.RoundLayoutPoint(point, layoutScale);
			}
			base.Bounds = new Rect(point, constraints);
		}
	}

	/// <summary>
	/// Positions child elements as part of a layout pass.
	/// </summary>
	/// <param name="finalSize">The size available to the control.</param>
	/// <returns>The actual size used.</returns>
	protected virtual Size ArrangeOverride(Size finalSize)
	{
		Rect rect = new Rect(finalSize);
		IAvaloniaList<Visual> visualChildren = base.VisualChildren;
		int count = visualChildren.Count;
		for (int i = 0; i < count; i++)
		{
			if (visualChildren[i] is Layoutable layoutable)
			{
				layoutable.Arrange(rect);
			}
		}
		return finalSize;
	}

	internal sealed override void InvalidateStyles(bool recurse)
	{
		base.InvalidateStyles(recurse);
		InvalidateMeasure();
	}

	/// <inheritdoc />
	protected override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
	{
		_isAttachingToVisualTree = true;
		try
		{
			base.OnAttachedToVisualTreeCore(e);
		}
		finally
		{
			_isAttachingToVisualTree = false;
		}
		ILayoutRoot layoutRoot = this.GetLayoutRoot();
		if (layoutRoot != null)
		{
			if (_layoutUpdated != null)
			{
				layoutRoot.LayoutManager.LayoutUpdated += LayoutManagedLayoutUpdated;
			}
			if (_effectiveViewportChanged != null)
			{
				layoutRoot.LayoutManager.RegisterEffectiveViewportListener(this);
			}
		}
	}

	protected override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
	{
		ILayoutRoot layoutRoot = this.GetLayoutRoot();
		if (layoutRoot != null)
		{
			if (_layoutUpdated != null)
			{
				layoutRoot.LayoutManager.LayoutUpdated -= LayoutManagedLayoutUpdated;
			}
			if (_effectiveViewportChanged != null)
			{
				layoutRoot.LayoutManager.UnregisterEffectiveViewportListener(this);
			}
		}
		base.OnDetachedFromVisualTreeCore(e);
	}

	/// <summary>
	/// Called by InvalidateMeasure
	/// </summary>
	protected virtual void OnMeasureInvalidated()
	{
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (!(change.Property == Visual.IsVisibleProperty))
		{
			return;
		}
		DesiredSize = default(Size);
		this.GetVisualParent<Layoutable>()?.ChildDesiredSizeChanged(this);
		if (!change.GetNewValue<bool>())
		{
			return;
		}
		InvalidateMeasure();
		ILayoutRoot layoutRoot = this.GetLayoutRoot();
		if (layoutRoot != null)
		{
			int count = base.VisualChildren.Count;
			for (int i = 0; i < count; i++)
			{
				(base.VisualChildren[i] as Layoutable)?.AncestorBecameVisible(layoutRoot.LayoutManager);
			}
		}
	}

	/// <inheritdoc />
	protected sealed override void OnVisualParentChanged(Visual? oldParent, Visual? newParent)
	{
		LayoutHelper.InvalidateSelfAndChildrenMeasure(this);
		base.OnVisualParentChanged(oldParent, newParent);
	}

	private protected override void OnControlThemeChanged()
	{
		base.OnControlThemeChanged();
		InvalidateMeasure();
	}

	internal override void OnTemplatedParentControlThemeChanged()
	{
		base.OnTemplatedParentControlThemeChanged();
		InvalidateMeasure();
	}

	private void AncestorBecameVisible(ILayoutManager layoutManager)
	{
		if (base.IsVisible)
		{
			if (!IsMeasureValid)
			{
				layoutManager.InvalidateMeasure(this);
				InvalidateVisual();
			}
			else if (!IsArrangeValid)
			{
				layoutManager.InvalidateArrange(this);
				InvalidateVisual();
			}
			int count = base.VisualChildren.Count;
			for (int i = 0; i < count; i++)
			{
				(base.VisualChildren[i] as Layoutable)?.AncestorBecameVisible(layoutManager);
			}
		}
	}

	/// <summary>
	/// Called when the layout manager raises a LayoutUpdated event.
	/// </summary>
	/// <param name="sender">The sender.</param>
	/// <param name="e">The event args.</param>
	private void LayoutManagedLayoutUpdated(object? sender, EventArgs e)
	{
		_layoutUpdated?.Invoke(this, e);
	}

	/// <summary>
	/// Tests whether any of a <see cref="T:Avalonia.Rect" />'s properties include negative values,
	/// a NaN or Infinity.
	/// </summary>
	/// <param name="rect">The rect.</param>
	/// <returns>True if the rect is invalid; otherwise false.</returns>
	private static bool IsInvalidRect(Rect rect)
	{
		if (!MathUtilities.IsNegativeOrNonFinite(rect.Width) && !MathUtilities.IsNegativeOrNonFinite(rect.Height) && MathUtilities.IsFinite(rect.X))
		{
			return !MathUtilities.IsFinite(rect.Y);
		}
		return true;
	}

	/// <summary>
	/// Tests whether any of a <see cref="T:Avalonia.Size" />'s properties include negative values,
	/// a NaN or Infinity.
	/// </summary>
	/// <param name="size">The size.</param>
	/// <returns>True if the size is invalid; otherwise false.</returns>
	private static bool IsInvalidSize(Size size)
	{
		if (!MathUtilities.IsNegativeOrNonFinite(size.Width))
		{
			return MathUtilities.IsNegativeOrNonFinite(size.Height);
		}
		return true;
	}

	/// <summary>
	/// Ensures neither component of a <see cref="T:Avalonia.Size" /> is negative.
	/// </summary>
	/// <param name="size">The size.</param>
	/// <returns>The non-negative size.</returns>
	private static Size NonNegative(Size size)
	{
		return new Size(Math.Max(size.Width, 0.0), Math.Max(size.Height, 0.0));
	}
}
