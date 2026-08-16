using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Base class for controls that provides rendering and related visual properties.
/// </summary>
/// <remarks>
/// The <see cref="T:Avalonia.Visual" /> class represents elements that have a visual on-screen
/// representation and stores all the information needed for an 
/// <see cref="T:Avalonia.Rendering.IRenderer" /> to render the control. To traverse the visual tree, use the
/// extension methods defined in <see cref="T:Avalonia.VisualExtensions" />.
/// </remarks>
[UsableDuringInitialization]
public class Visual : StyledElement, IAvaloniaListItemValidator<Visual>
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.Bounds" /> property.
	/// </summary>
	public static readonly DirectProperty<Visual, Rect> BoundsProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.ClipToBounds" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> ClipToBoundsProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.Clip" /> property.
	/// </summary>
	public static readonly StyledProperty<Geometry?> ClipProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.IsVisible" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsVisibleProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.Opacity" /> property.
	/// </summary>
	public static readonly StyledProperty<double> OpacityProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.OpacityMask" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> OpacityMaskProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.CacheMode" /> property.
	/// </summary>
	public static readonly StyledProperty<CacheMode?> CacheModeProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.Effect" /> property.
	/// </summary>
	public static readonly StyledProperty<IEffect?> EffectProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.HasMirrorTransform" /> property.
	/// </summary>
	public static readonly DirectProperty<Visual, bool> HasMirrorTransformProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.RenderTransform" /> property.
	/// </summary>
	public static readonly StyledProperty<ITransform?> RenderTransformProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.RenderTransformOrigin" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativePoint> RenderTransformOriginProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.FlowDirection" /> property.
	/// </summary>
	public static readonly AttachedProperty<FlowDirection> FlowDirectionProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.VisualParent" /> property.
	/// </summary>
	public static readonly DirectProperty<Visual, Visual?> VisualParentProperty;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Visual.ZIndex" /> property.
	/// </summary>
	public static readonly StyledProperty<int> ZIndexProperty;

	private static readonly WeakEvent<IAffectsRender, EventArgs> InvalidatedWeakEvent;

	private Rect _bounds;

	private Visual? _visualParent;

	private bool _hasMirrorTransform;

	private TargetWeakEventSubscriber<Visual, EventArgs>? _affectsRenderWeakSubscriber;

	private RenderOptions _renderOptions;

	private TextOptions _textOptions;

	internal CompositionDrawListVisual? CompositionVisual { get; private set; }

	internal CompositionVisual? ChildCompositionVisual { get; set; }

	internal static int RootedVisualChildrenCount { get; private set; }

	internal IPresentationSource? PresentationSource { get; private set; }

	/// <summary>
	/// Gets the bounds of the control relative to its parent.
	/// </summary>
	public Rect Bounds
	{
		get
		{
			return _bounds;
		}
		protected set
		{
			SetAndRaise(BoundsProperty, ref _bounds, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the control should be clipped to its bounds.
	/// </summary>
	public bool ClipToBounds
	{
		get
		{
			return GetValue(ClipToBoundsProperty);
		}
		set
		{
			SetValue(ClipToBoundsProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the geometry clip for this visual.
	/// </summary>
	public Geometry? Clip
	{
		get
		{
			return GetValue(ClipProperty);
		}
		set
		{
			SetValue(ClipProperty, value);
		}
	}

	/// <summary>
	/// Gets a value indicating whether this control and all its parents are visible.
	/// </summary>
	public bool IsEffectivelyVisible { get; private set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether this control is visible.
	/// </summary>
	public bool IsVisible
	{
		get
		{
			return GetValue(IsVisibleProperty);
		}
		set
		{
			SetValue(IsVisibleProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the opacity of the control.
	/// </summary>
	public double Opacity
	{
		get
		{
			return GetValue(OpacityProperty);
		}
		set
		{
			SetValue(OpacityProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the opacity mask of the control.
	/// </summary>
	public IBrush? OpacityMask
	{
		get
		{
			return GetValue(OpacityMaskProperty);
		}
		set
		{
			SetValue(OpacityMaskProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the cache mode of the visual.
	/// </summary>
	public CacheMode? CacheMode
	{
		get
		{
			return GetValue(CacheModeProperty);
		}
		set
		{
			SetValue(CacheModeProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the effect of the control.
	/// </summary>
	public IEffect? Effect
	{
		get
		{
			return GetValue(EffectProperty);
		}
		set
		{
			SetValue(EffectProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether to apply mirror transform on this control.
	/// </summary>
	public bool HasMirrorTransform
	{
		get
		{
			return _hasMirrorTransform;
		}
		protected set
		{
			SetAndRaise(HasMirrorTransformProperty, ref _hasMirrorTransform, value);
		}
	}

	/// <summary>
	/// Gets or sets the render transform of the control.
	/// </summary>
	public ITransform? RenderTransform
	{
		get
		{
			return GetValue(RenderTransformProperty);
		}
		set
		{
			SetValue(RenderTransformProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the transform origin of the control.
	/// </summary>
	public RelativePoint RenderTransformOrigin
	{
		get
		{
			return GetValue(RenderTransformOriginProperty);
		}
		set
		{
			SetValue(RenderTransformOriginProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the text flow direction.
	/// </summary>
	public FlowDirection FlowDirection
	{
		get
		{
			return GetValue(FlowDirectionProperty);
		}
		set
		{
			SetValue(FlowDirectionProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the Z index of the control.
	/// </summary>
	/// <remarks>
	/// Controls with a higher <see cref="P:Avalonia.Visual.ZIndex" /> will appear in front of controls with
	/// a lower ZIndex. If two controls have the same ZIndex then the control that appears
	/// later in the containing element's children collection will appear on top.
	/// </remarks>
	public int ZIndex
	{
		get
		{
			return GetValue(ZIndexProperty);
		}
		set
		{
			SetValue(ZIndexProperty, value);
		}
	}

	/// <summary>
	/// Gets the control's child visuals.
	/// </summary>
	protected internal IAvaloniaList<Visual> VisualChildren { get; }

	/// <summary>
	/// Gets the root of the visual tree, if the control is attached to a visual tree.
	/// </summary>
	protected internal Visual? VisualRoot => PresentationSource?.RootVisual;

	internal RenderOptions RenderOptions
	{
		get
		{
			return _renderOptions;
		}
		set
		{
			_renderOptions = value;
			InvalidateVisual();
		}
	}

	internal TextOptions TextOptions
	{
		get
		{
			return _textOptions;
		}
		set
		{
			_textOptions = value;
			InvalidateVisual();
		}
	}

	internal bool HasNonUniformZIndexChildren { get; private set; }

	/// <summary>
	/// Gets a value indicating whether this control is attached to a visual root.
	/// </summary>
	internal bool IsAttachedToVisualTree => PresentationSource != null;

	/// <summary>
	/// Gets the control's parent visual.
	/// </summary>
	internal Visual? VisualParent => _visualParent;

	/// <summary>
	/// Gets a value indicating whether control bypass FlowDirecton policies.
	/// </summary>
	/// <remarks>
	/// Related to FlowDirection system and returns false as default, so if 
	/// <see cref="P:Avalonia.Visual.FlowDirection" /> is RTL then control will get a mirror presentation. 
	/// For controls that want to avoid this behavior, override this property and return true.
	/// </remarks>
	protected virtual bool BypassFlowDirectionPolicies => false;

	/// <summary>
	/// Raised when the control is attached to a rooted visual tree.
	/// </summary>
	public event EventHandler<VisualTreeAttachmentEventArgs>? AttachedToVisualTree;

	/// <summary>
	/// Raised when the control is detached from a rooted visual tree.
	/// </summary>
	public event EventHandler<VisualTreeAttachmentEventArgs>? DetachedFromVisualTree;

	/// <summary>
	/// Raised when <see cref="P:Avalonia.Visual.IsEffectivelyVisible" /> changes.
	/// </summary>
	internal event EventHandler? IsEffectivelyVisibleChanged;

	private protected virtual CompositionDrawListVisual CreateCompositionVisual(Compositor compositor)
	{
		return new CompositionDrawListVisual(compositor, new ServerCompositionDrawListVisual(compositor.Server, this), this);
	}

	internal CompositionVisual AttachToCompositor(Compositor compositor)
	{
		if (CompositionVisual == null || CompositionVisual.Compositor != compositor)
		{
			CompositionVisual = CreateCompositionVisual(compositor);
		}
		return CompositionVisual;
	}

	internal virtual void DetachFromCompositor()
	{
		if (CompositionVisual != null)
		{
			if (ChildCompositionVisual != null)
			{
				CompositionVisual.Children.Remove(ChildCompositionVisual);
			}
			CompositionVisual.DrawList = null;
			CompositionVisual.OpacityMask = null;
			CompositionVisual = null;
		}
	}

	internal virtual void SynchronizeCompositionChildVisuals()
	{
		if (CompositionVisual == null)
		{
			return;
		}
		CompositionVisualCollection children = CompositionVisual.Children;
		AvaloniaList<Visual> avaloniaList = (AvaloniaList<Visual>)VisualChildren;
		PooledList<(Visual, int)> pooledList = null;
		if (HasNonUniformZIndexChildren && avaloniaList.Count > 1)
		{
			pooledList = new PooledList<(Visual, int)>(avaloniaList.Count);
			for (int i = 0; i < avaloniaList.Count; i++)
			{
				pooledList.Add((avaloniaList[i], i));
			}
			pooledList.Sort(delegate((Visual visual, int index) lhs, (Visual visual, int index) rhs)
			{
				int num4 = lhs.visual.ZIndex.CompareTo(rhs.visual.ZIndex);
				return (num4 != 0) ? num4 : lhs.index.CompareTo(rhs.index);
			});
		}
		CompositionVisual compositionVisual = ChildCompositionVisual;
		if (compositionVisual != null && compositionVisual.Compositor != CompositionVisual.Compositor)
		{
			compositionVisual = null;
		}
		int num = avaloniaList.Count;
		if (compositionVisual != null)
		{
			num++;
		}
		if (children.Count == num)
		{
			bool flag = false;
			if (pooledList != null)
			{
				for (int num2 = 0; num2 < avaloniaList.Count; num2++)
				{
					if (children[num2] != pooledList[num2].Item1.CompositionVisual)
					{
						flag = true;
						break;
					}
				}
			}
			else
			{
				for (int num3 = 0; num3 < avaloniaList.Count; num3++)
				{
					if (children[num3] != avaloniaList[num3].CompositionVisual)
					{
						flag = true;
						break;
					}
				}
			}
			if (compositionVisual != null && children[children.Count - 1] != compositionVisual)
			{
				flag = true;
			}
			if (!flag)
			{
				pooledList?.Dispose();
				return;
			}
		}
		children.Clear();
		if (pooledList != null)
		{
			foreach (var item in pooledList)
			{
				CompositionDrawListVisual compositionVisual2 = item.Item1.CompositionVisual;
				if (compositionVisual2 != null)
				{
					children.Add(compositionVisual2);
				}
			}
			pooledList.Dispose();
		}
		else
		{
			foreach (Visual item2 in avaloniaList)
			{
				CompositionDrawListVisual compositionVisual3 = item2.CompositionVisual;
				if (compositionVisual3 != null)
				{
					children.Add(compositionVisual3);
				}
			}
		}
		if (compositionVisual != null)
		{
			children.Add(compositionVisual);
		}
	}

	internal virtual void SynchronizeCompositionProperties()
	{
		if (CompositionVisual != null)
		{
			CompositionDrawListVisual compositionVisual = CompositionVisual;
			compositionVisual.Offset = new Vector3D(Bounds.Left, Bounds.Top, 0.0);
			compositionVisual.Size = new Vector(Bounds.Width, Bounds.Height);
			compositionVisual.Visible = IsVisible;
			compositionVisual.Opacity = (float)Opacity;
			compositionVisual.ClipToBounds = ClipToBounds;
			compositionVisual.Clip = Clip?.PlatformImpl;
			if (!object.Equals(compositionVisual.OpacityMask, OpacityMask))
			{
				compositionVisual.OpacityMask = OpacityMask;
			}
			CompositionCacheMode compositionCacheMode = CacheMode?.GetForCompositor(compositionVisual.Compositor);
			if (compositionVisual.CacheMode != compositionCacheMode)
			{
				compositionVisual.CacheMode = compositionCacheMode;
			}
			if (!compositionVisual.Effect.EffectEquals(Effect))
			{
				compositionVisual.Effect = Effect?.ToImmutable();
			}
			compositionVisual.RenderOptions = RenderOptions;
			compositionVisual.TextOptions = TextOptions;
			Matrix transformMatrix = Matrix.Identity;
			if (HasMirrorTransform)
			{
				transformMatrix = new Matrix(-1.0, 0.0, 0.0, 1.0, Bounds.Width, 0.0);
			}
			if (RenderTransform != null)
			{
				Matrix matrix = Matrix.CreateTranslation(RenderTransformOrigin.ToPixels(new Size(Bounds.Width, Bounds.Height)));
				transformMatrix *= -matrix * RenderTransform.Value * matrix;
			}
			compositionVisual.TransformMatrix = transformMatrix;
		}
	}

	/// <summary>
	/// Initializes static members of the <see cref="T:Avalonia.Visual" /> class.
	/// </summary>
	static Visual()
	{
		BoundsProperty = AvaloniaProperty.RegisterDirect("Bounds", (Visual o) => o.Bounds);
		ClipToBoundsProperty = AvaloniaProperty.Register<Visual, bool>("ClipToBounds", defaultValue: false);
		ClipProperty = AvaloniaProperty.Register<Visual, Geometry>("Clip");
		IsVisibleProperty = AvaloniaProperty.Register<Visual, bool>("IsVisible", defaultValue: true);
		OpacityProperty = AvaloniaProperty.Register<Visual, double>("Opacity", 1.0);
		OpacityMaskProperty = AvaloniaProperty.Register<Visual, IBrush>("OpacityMask");
		CacheModeProperty = AvaloniaProperty.Register<Visual, CacheMode>("CacheMode");
		EffectProperty = AvaloniaProperty.Register<Visual, IEffect>("Effect");
		HasMirrorTransformProperty = AvaloniaProperty.RegisterDirect("HasMirrorTransform", (Visual o) => o.HasMirrorTransform, null, unsetValue: false);
		RenderTransformProperty = AvaloniaProperty.Register<Visual, ITransform>("RenderTransform");
		RenderTransformOriginProperty = AvaloniaProperty.Register<Visual, RelativePoint>("RenderTransformOrigin", RelativePoint.Center);
		FlowDirectionProperty = AvaloniaProperty.RegisterAttached<Visual, Visual, FlowDirection>("FlowDirection", FlowDirection.LeftToRight, inherits: true);
		VisualParentProperty = AvaloniaProperty.RegisterDirect("VisualParent", (Visual o) => o._visualParent);
		ZIndexProperty = AvaloniaProperty.Register<Visual, int>("ZIndex", 0);
		InvalidatedWeakEvent = WeakEvent.Register(delegate(IAffectsRender s, EventHandler h)
		{
			s.Invalidated += h;
		}, delegate(IAffectsRender s, EventHandler h)
		{
			s.Invalidated -= h;
		});
		AffectsRender<Visual>(new AvaloniaProperty[8] { BoundsProperty, ClipProperty, ClipToBoundsProperty, IsVisibleProperty, OpacityProperty, OpacityMaskProperty, EffectProperty, HasMirrorTransformProperty });
		RenderTransformProperty.Changed.Subscribe(RenderTransformChanged);
		ZIndexProperty.Changed.Subscribe(ZIndexChanged);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Visual" /> class.
	/// </summary>
	public Visual()
	{
		DisableTransitions();
		AvaloniaList<Visual> avaloniaList = new AvaloniaList<Visual>();
		avaloniaList.ResetBehavior = ResetBehavior.Remove;
		avaloniaList.Validator = this;
		avaloniaList.CollectionChanged += VisualChildrenChanged;
		VisualChildren = avaloniaList;
	}

	/// <summary>
	/// Updates the <see cref="P:Avalonia.Visual.IsEffectivelyVisible" /> property based on the parent's
	/// <see cref="P:Avalonia.Visual.IsEffectivelyVisible" />.
	/// </summary>
	/// <param name="parentState">The effective visibility of the parent control.</param>
	private void UpdateIsEffectivelyVisible(bool parentState)
	{
		bool flag = parentState && IsVisible;
		if (IsEffectivelyVisible != flag)
		{
			IsEffectivelyVisible = flag;
			IsEffectivelyVisibleChanged?.Invoke(this, EventArgs.Empty);
			IAvaloniaList<Visual> visualChildren = VisualChildren;
			for (int i = 0; i < visualChildren.Count; i++)
			{
				visualChildren[i].UpdateIsEffectivelyVisible(flag);
			}
		}
	}

	internal IInputRoot? GetInputRoot()
	{
		return PresentationSource?.InputRoot;
	}

	/// <summary>
	/// Gets the value of the attached <see cref="F:Avalonia.Visual.FlowDirectionProperty" /> on a control.
	/// </summary>
	/// <param name="visual">The control.</param>
	/// <returns>The flow direction.</returns>
	public static FlowDirection GetFlowDirection(Visual visual)
	{
		return visual.GetValue(FlowDirectionProperty);
	}

	/// <summary>
	/// Sets the value of the attached <see cref="F:Avalonia.Visual.FlowDirectionProperty" /> on a control.
	/// </summary>
	/// <param name="visual">The control.</param>
	/// <param name="value">The property value to set.</param>
	public static void SetFlowDirection(Visual visual, FlowDirection value)
	{
		visual.SetValue(FlowDirectionProperty, value);
	}

	/// <summary>
	/// Invalidates the visual and queues a repaint.
	/// </summary>
	public void InvalidateVisual()
	{
		PresentationSource?.Renderer.AddDirty(this);
	}

	/// <summary>
	/// Renders the visual to a <see cref="T:Avalonia.Media.DrawingContext" />.
	/// </summary>
	/// <param name="context">The drawing context.</param>
	public virtual void Render(DrawingContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
	}

	/// <summary>
	/// Indicates that a property change should cause <see cref="M:Avalonia.Visual.InvalidateVisual" /> to be
	/// called.
	/// </summary>
	/// <typeparam name="T">The control which the property affects.</typeparam>
	/// <param name="properties">The properties.</param>
	/// <remarks>
	/// This method should be called in a control's static constructor with each property
	/// on the control which when changed should cause a redraw. This is similar to WPF's
	/// FrameworkPropertyMetadata.AffectsRender flag.
	/// </remarks>
	protected static void AffectsRender<T>(params AvaloniaProperty[] properties) where T : Visual
	{
		AnonymousObserver<AvaloniaPropertyChangedEventArgs> observer = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(delegate(AvaloniaPropertyChangedEventArgs e)
		{
			if (e.Sender is T val)
			{
				val.InvalidateVisual();
			}
		});
		AnonymousObserver<AvaloniaPropertyChangedEventArgs> observer2 = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(delegate(AvaloniaPropertyChangedEventArgs e)
		{
			if (e.Sender is T val)
			{
				if (e.OldValue is IAffectsRender target && val._affectsRenderWeakSubscriber != null)
				{
					InvalidatedWeakEvent.Unsubscribe(target, val._affectsRenderWeakSubscriber);
				}
				if (e.NewValue is IAffectsRender target2)
				{
					if (val._affectsRenderWeakSubscriber == null)
					{
						val._affectsRenderWeakSubscriber = new TargetWeakEventSubscriber<Visual, EventArgs>(val, delegate(Visual visual, object? _, WeakEvent _, EventArgs _)
						{
							visual.InvalidateVisual();
						});
					}
					InvalidatedWeakEvent.Subscribe(target2, val._affectsRenderWeakSubscriber);
				}
				val.InvalidateVisual();
			}
		});
		foreach (AvaloniaProperty avaloniaProperty in properties)
		{
			if (avaloniaProperty.CanValueAffectRender())
			{
				avaloniaProperty.Changed.Subscribe(observer2);
			}
			else
			{
				avaloniaProperty.Changed.Subscribe(observer);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IsVisibleProperty)
		{
			UpdateIsEffectivelyVisible(VisualParent?.IsEffectivelyVisible ?? true);
		}
		else
		{
			if (!(change.Property == FlowDirectionProperty))
			{
				return;
			}
			InvalidateMirrorTransform();
			foreach (Visual visualChild in VisualChildren)
			{
				visualChild.InvalidateMirrorTransform();
			}
		}
	}

	protected override void LogicalChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		base.LogicalChildrenCollectionChanged(sender, e);
		PresentationSource?.Renderer.RecalculateChildren(this);
	}

	/// <summary>
	/// Calls the <see cref="M:Avalonia.Visual.OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs)" /> method 
	/// for this control and all of its visual descendants.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected virtual void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
	{
		Logger.TryGet(LogEventLevel.Verbose, "Visual")?.Log(this, "Attached to visual tree");
		PresentationSource = e.PresentationSource;
		RootedVisualChildrenCount++;
		if (RenderTransform is IMutableTransform mutableTransform)
		{
			mutableTransform.Changed += RenderTransformChanged;
		}
		EnableTransitions();
		if (PresentationSource.Renderer is IRendererWithCompositor rendererWithCompositor)
		{
			AttachToCompositor(rendererWithCompositor.Compositor);
		}
		InvalidateMirrorTransform();
		UpdateIsEffectivelyVisible(_visualParent?.IsEffectivelyVisible ?? true);
		OnAttachedToVisualTree(e);
		AttachedToVisualTree?.Invoke(this, e);
		InvalidateVisual();
		if (_visualParent != null)
		{
			PresentationSource.Renderer.RecalculateChildren(_visualParent);
			if (ZIndex != 0)
			{
				_visualParent.HasNonUniformZIndexChildren = true;
			}
		}
		IAvaloniaList<Visual> visualChildren = VisualChildren;
		int count = visualChildren.Count;
		for (int i = 0; i < count; i++)
		{
			Visual visual = visualChildren[i];
			if (visual != null && visual.PresentationSource != e.PresentationSource)
			{
				visual.OnAttachedToVisualTreeCore(e);
			}
		}
	}

	/// <summary>
	/// Calls the <see cref="M:Avalonia.Visual.OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs)" /> method 
	/// for this control and all of its visual descendants.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected virtual void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
	{
		Logger.TryGet(LogEventLevel.Verbose, "Visual")?.Log(this, "Detached from visual tree");
		RootedVisualChildrenCount--;
		if (RenderTransform is IMutableTransform mutableTransform)
		{
			mutableTransform.Changed -= RenderTransformChanged;
		}
		DisableTransitions();
		UpdateIsEffectivelyVisible(parentState: true);
		OnDetachedFromVisualTree(e);
		DetachFromCompositor();
		DetachedFromVisualTree?.Invoke(this, e);
		PresentationSource?.Renderer.AddDirty(this);
		PresentationSource = null;
		IAvaloniaList<Visual> visualChildren = VisualChildren;
		int count = visualChildren.Count;
		for (int i = 0; i < count; i++)
		{
			visualChildren[i]?.OnDetachedFromVisualTreeCore(e);
		}
	}

	/// <summary>
	/// Called when the control is added to a rooted visual tree.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected virtual void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
	}

	/// <summary>
	/// Called when the control is removed from a rooted visual tree.
	/// </summary>
	/// <param name="e">The event args.</param>
	protected virtual void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
	}

	/// <summary>
	/// Called when the control's visual parent changes.
	/// </summary>
	/// <param name="oldParent">The old visual parent.</param>
	/// <param name="newParent">The new visual parent.</param>
	protected virtual void OnVisualParentChanged(Visual? oldParent, Visual? newParent)
	{
		RaisePropertyChanged(VisualParentProperty, oldParent, newParent);
	}

	/// <summary>
	/// Called when a visual's <see cref="P:Avalonia.Visual.RenderTransform" /> changes.
	/// </summary>
	/// <param name="e">The event args.</param>
	private static void RenderTransformChanged(AvaloniaPropertyChangedEventArgs<ITransform?> e)
	{
		Visual visual = e.Sender as Visual;
		if (visual?.VisualRoot != null)
		{
			var (transform, transform2) = e.GetOldAndNewValue<ITransform>();
			if (transform is Transform transform3)
			{
				transform3.Changed -= visual.RenderTransformChanged;
			}
			if (transform2 is Transform transform4)
			{
				transform4.Changed += visual.RenderTransformChanged;
			}
			visual.InvalidateVisual();
		}
	}

	/// <summary>
	/// Ensures a visual child is not null and not already parented.
	/// </summary>
	/// <param name="item">The visual child.</param>
	void IAvaloniaListItemValidator<Visual>.Validate(Visual item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item", "Cannot add null to VisualChildren.");
		}
		Visual visualParent = item.VisualParent;
		if (visualParent != null)
		{
			throw new InvalidOperationException($"The control {item.DebugDisplay} already has a visual parent {visualParent.GetDebugDisplay(includeContent: false)} while trying to add it as a child of {GetDebugDisplay(includeContent: false)}.");
		}
	}

	/// <summary>
	/// Called when the <see cref="P:Avalonia.Visual.ZIndex" /> property changes on any control.
	/// </summary>
	/// <param name="e">The event args.</param>
	private static void ZIndexChanged(AvaloniaPropertyChangedEventArgs e)
	{
		Visual obj = e.Sender as Visual;
		Visual visual = obj?.VisualParent;
		if ((obj == null || obj.ZIndex != 0) && visual != null)
		{
			Visual visual2 = visual;
			visual2.HasNonUniformZIndexChildren = true;
		}
		obj?.InvalidateVisual();
		visual?.PresentationSource?.Renderer.RecalculateChildren(visual);
	}

	/// <summary>
	/// Called when the <see cref="P:Avalonia.Visual.RenderTransform" />'s <see cref="E:Avalonia.Media.Transform.Changed" /> event
	/// is fired.
	/// </summary>
	/// <param name="sender">The sender.</param>
	/// <param name="e">The event args.</param>
	private void RenderTransformChanged(object? sender, EventArgs e)
	{
		InvalidateVisual();
	}

	/// <summary>
	/// Sets the visual parent of the Visual.
	/// </summary>
	/// <param name="value">The visual parent.</param>
	private void SetVisualParent(Visual? value)
	{
		if (_visualParent != value)
		{
			Visual visualParent = _visualParent;
			_visualParent = value;
			if (PresentationSource != null && visualParent != null)
			{
				VisualTreeAttachmentEventArgs e = new VisualTreeAttachmentEventArgs(visualParent, PresentationSource);
				OnDetachedFromVisualTreeCore(e);
			}
			Visual? visualParent2 = _visualParent;
			if (visualParent2 != null && visualParent2.IsAttachedToVisualTree)
			{
				VisualTreeAttachmentEventArgs e2 = new VisualTreeAttachmentEventArgs(_visualParent, _visualParent.PresentationSource);
				OnAttachedToVisualTreeCore(e2);
			}
			OnVisualParentChanged(visualParent, value);
		}
	}

	/// <summary>
	/// Called when the <see cref="P:Avalonia.Visual.VisualChildren" /> collection changes.
	/// </summary>
	/// <param name="sender">The sender.</param>
	/// <param name="e">The event args.</param>
	private void VisualChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		switch (e.Action)
		{
		case NotifyCollectionChangedAction.Add:
			SetVisualParent(e.NewItems, this);
			break;
		case NotifyCollectionChangedAction.Remove:
			SetVisualParent(e.OldItems, null);
			break;
		case NotifyCollectionChangedAction.Replace:
			SetVisualParent(e.OldItems, null);
			SetVisualParent(e.NewItems, this);
			break;
		}
	}

	private static void SetVisualParent(IList children, Visual? parent)
	{
		int count = children.Count;
		for (int i = 0; i < count; i++)
		{
			((Visual)children[i]).SetVisualParent(parent);
		}
	}

	internal override void OnTemplatedParentControlThemeChanged()
	{
		base.OnTemplatedParentControlThemeChanged();
		int count = VisualChildren.Count;
		AvaloniaObject templatedParent = base.TemplatedParent;
		for (int i = 0; i < count; i++)
		{
			StyledElement styledElement = VisualChildren[i];
			if (styledElement != null && styledElement.TemplatedParent == templatedParent)
			{
				styledElement.OnTemplatedParentControlThemeChanged();
			}
		}
	}

	/// <summary>
	/// Computes the <see cref="P:Avalonia.Visual.HasMirrorTransform" /> value according to the 
	/// <see cref="P:Avalonia.Visual.FlowDirection" /> and <see cref="P:Avalonia.Visual.BypassFlowDirectionPolicies" />
	/// </summary>
	protected internal virtual void InvalidateMirrorTransform()
	{
		FlowDirection flowDirection = FlowDirection;
		FlowDirection flowDirection2 = FlowDirection.LeftToRight;
		bool bypassFlowDirectionPolicies = BypassFlowDirectionPolicies;
		bool flag = false;
		Visual visualParent = VisualParent;
		if (visualParent != null)
		{
			flowDirection2 = visualParent.FlowDirection;
			flag = visualParent.BypassFlowDirectionPolicies;
		}
		bool num = flowDirection == FlowDirection.RightToLeft && !bypassFlowDirectionPolicies;
		bool flag2 = flowDirection2 == FlowDirection.RightToLeft && !flag;
		bool hasMirrorTransform = num != flag2;
		HasMirrorTransform = hasMirrorTransform;
	}

	internal void SetPresentationSourceForRootVisual(IPresentationSource? presentationSource)
	{
		if (presentationSource == PresentationSource)
		{
			return;
		}
		if (PresentationSource != null)
		{
			if (presentationSource != null)
			{
				throw new InvalidOperationException("Visual is already attached to a presentation source. Only one presentation source can be attached to a visual tree.");
			}
			OnDetachedFromVisualTreeCore(new VisualTreeAttachmentEventArgs(null, PresentationSource));
		}
		PresentationSource = presentationSource;
		if (PresentationSource != null)
		{
			VisualTreeAttachmentEventArgs e = new VisualTreeAttachmentEventArgs(null, PresentationSource);
			OnAttachedToVisualTreeCore(e);
		}
	}
}
