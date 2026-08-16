using System.Numerics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// The base visual object in the composition visual hierarchy.
/// </summary>
public abstract class CompositionVisual : CompositionObject
{
	private IBrush? _opacityMask;

	protected int CustomHitTestCountInSubTree;

	private CompositionVisualChangedFields _changedFieldsOfCompositionVisual;

	private CompositionTarget? _root;

	private CompositionVisual? _parent;

	private bool _visible;

	private float _opacity;

	private IGeometryImpl? _clip;

	private bool _clipToBounds;

	private Vector3D _offset;

	private Vector3D _translation;

	private Vector _size;

	private Vector _anchorPoint;

	private Vector3D _centerPoint;

	private float _rotationAngle;

	private Quaternion _orientation;

	private Vector3D _scale;

	private Matrix _transformMatrix;

	private CompositionVisual? _adornedVisual;

	private bool _adornerIsClipped;

	private IBrush? _opacityMaskBrush;

	private IImmutableEffect? _effect;

	private RenderOptions _renderOptions;

	private TextOptions _textOptions;

	private CompositionCacheMode? _cacheMode;

	public bool DisableSubTreeBoundsHitTestOptimization => CustomHitTestCountInSubTree != 0;

	public IBrush? OpacityMask
	{
		get
		{
			return _opacityMask;
		}
		set
		{
			if (_opacityMask != value)
			{
				if (_opacityMask is ICompositionRenderResource<IBrush> compositionRenderResource)
				{
					compositionRenderResource.ReleaseOnCompositor(base.Compositor);
					_opacityMask = null;
					OpacityMaskBrushTransportField = null;
				}
				if (value is ICompositionRenderResource<IBrush> compositionRenderResource2)
				{
					compositionRenderResource2.AddRefOnCompositor(base.Compositor);
					OpacityMaskBrushTransportField = compositionRenderResource2.GetForCompositor(base.Compositor);
					_opacityMask = value;
				}
				else
				{
					OpacityMaskBrushTransportField = (_opacityMask = value)?.ToImmutable();
				}
			}
		}
	}

	internal object? Tag { get; set; }

	internal new ServerCompositionVisual Server { get; }

	internal CompositionTarget? Root
	{
		get
		{
			return _root;
		}
		set
		{
			bool flag = false;
			if (_root != value)
			{
				flag = true;
				_root = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Root;
				RegisterForSerialization();
			}
			_root = value;
			if (flag)
			{
				OnRootChanged();
			}
		}
	}

	internal CompositionVisual? Parent
	{
		get
		{
			return _parent;
		}
		set
		{
			bool flag = false;
			if (_parent != value)
			{
				OnParentChanging();
				flag = true;
				_parent = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Parent;
				RegisterForSerialization();
			}
			_parent = value;
			if (flag)
			{
				OnParentChanged();
			}
		}
	}

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			bool flag = false;
			if (_visible != value)
			{
				flag = true;
				_visible = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Visible;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfVisibleProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.VisibleAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Visible", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.VisibleAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfVisibleProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Visible", value);
				}
			}
			_visible = value;
		}
	}

	public float Opacity
	{
		get
		{
			return _opacity;
		}
		set
		{
			bool flag = false;
			if (_opacity != value)
			{
				flag = true;
				_opacity = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Opacity;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfOpacityProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.OpacityAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Opacity", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.OpacityAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfOpacityProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Opacity", value);
				}
			}
			_opacity = value;
		}
	}

	internal IGeometryImpl? Clip
	{
		get
		{
			return _clip;
		}
		set
		{
			bool flag = false;
			if (_clip != value)
			{
				flag = true;
				_clip = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Clip;
				RegisterForSerialization();
			}
			_clip = value;
		}
	}

	public bool ClipToBounds
	{
		get
		{
			return _clipToBounds;
		}
		set
		{
			bool flag = false;
			if (_clipToBounds != value)
			{
				flag = true;
				_clipToBounds = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.ClipToBounds;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfClipToBoundsProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.ClipToBoundsAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("ClipToBounds", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.ClipToBoundsAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfClipToBoundsProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "ClipToBounds", value);
				}
			}
			_clipToBounds = value;
		}
	}

	public Vector3D Offset
	{
		get
		{
			return _offset;
		}
		set
		{
			bool flag = false;
			if (_offset != value)
			{
				flag = true;
				_offset = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Offset;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfOffsetProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.OffsetAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Offset", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.OffsetAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfOffsetProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Offset", value);
				}
			}
			_offset = value;
		}
	}

	public Vector3D Translation
	{
		get
		{
			return _translation;
		}
		set
		{
			bool flag = false;
			if (_translation != value)
			{
				flag = true;
				_translation = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Translation;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfTranslationProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.TranslationAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Translation", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.TranslationAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfTranslationProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Translation", value);
				}
			}
			_translation = value;
		}
	}

	public Vector Size
	{
		get
		{
			return _size;
		}
		set
		{
			bool flag = false;
			if (_size != value)
			{
				flag = true;
				_size = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Size;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfSizeProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.SizeAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Size", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.SizeAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfSizeProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Size", value);
				}
			}
			_size = value;
		}
	}

	public Vector AnchorPoint
	{
		get
		{
			return _anchorPoint;
		}
		set
		{
			bool flag = false;
			if (_anchorPoint != value)
			{
				flag = true;
				_anchorPoint = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.AnchorPoint;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfAnchorPointProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.AnchorPointAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("AnchorPoint", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.AnchorPointAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfAnchorPointProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "AnchorPoint", value);
				}
			}
			_anchorPoint = value;
		}
	}

	public Vector3D CenterPoint
	{
		get
		{
			return _centerPoint;
		}
		set
		{
			bool flag = false;
			if (_centerPoint != value)
			{
				flag = true;
				_centerPoint = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.CenterPoint;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfCenterPointProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.CenterPointAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("CenterPoint", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.CenterPointAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfCenterPointProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "CenterPoint", value);
				}
			}
			_centerPoint = value;
		}
	}

	public float RotationAngle
	{
		get
		{
			return _rotationAngle;
		}
		set
		{
			bool flag = false;
			if (_rotationAngle != value)
			{
				flag = true;
				_rotationAngle = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.RotationAngle;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfRotationAngleProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.RotationAngleAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("RotationAngle", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.RotationAngleAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfRotationAngleProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "RotationAngle", value);
				}
			}
			_rotationAngle = value;
		}
	}

	public Quaternion Orientation
	{
		get
		{
			return _orientation;
		}
		set
		{
			bool flag = false;
			if (_orientation != value)
			{
				flag = true;
				_orientation = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Orientation;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfOrientationProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.OrientationAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Orientation", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.OrientationAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfOrientationProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Orientation", value);
				}
			}
			_orientation = value;
		}
	}

	public Vector3D Scale
	{
		get
		{
			return _scale;
		}
		set
		{
			bool flag = false;
			if (_scale != value)
			{
				flag = true;
				_scale = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Scale;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfScaleProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.ScaleAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("Scale", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.ScaleAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfScaleProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "Scale", value);
				}
			}
			_scale = value;
		}
	}

	internal Matrix TransformMatrix
	{
		get
		{
			return _transformMatrix;
		}
		set
		{
			bool flag = false;
			if (_transformMatrix != value)
			{
				flag = true;
				_transformMatrix = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.TransformMatrix;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionVisual.s_IdOfTransformMatrixProperty);
				_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.TransformMatrixAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("TransformMatrix", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.TransformMatrixAnimated;
						PendingAnimations[ServerCompositionVisual.s_IdOfTransformMatrixProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "TransformMatrix", value);
				}
			}
			_transformMatrix = value;
		}
	}

	internal CompositionVisual? AdornedVisual
	{
		get
		{
			return _adornedVisual;
		}
		set
		{
			bool flag = false;
			if (_adornedVisual != value)
			{
				flag = true;
				_adornedVisual = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.AdornedVisual;
				RegisterForSerialization();
			}
			_adornedVisual = value;
		}
	}

	internal bool AdornerIsClipped
	{
		get
		{
			return _adornerIsClipped;
		}
		set
		{
			bool flag = false;
			if (_adornerIsClipped != value)
			{
				flag = true;
				_adornerIsClipped = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.AdornerIsClipped;
				RegisterForSerialization();
			}
			_adornerIsClipped = value;
		}
	}

	private IBrush? OpacityMaskBrushTransportField
	{
		get
		{
			return _opacityMaskBrush;
		}
		set
		{
			bool flag = false;
			if (_opacityMaskBrush != value)
			{
				flag = true;
				_opacityMaskBrush = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.OpacityMaskBrush;
				RegisterForSerialization();
			}
			_opacityMaskBrush = value;
		}
	}

	internal IImmutableEffect? Effect
	{
		get
		{
			return _effect;
		}
		set
		{
			bool flag = false;
			if (_effect != value)
			{
				flag = true;
				_effect = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Effect;
				RegisterForSerialization();
			}
			_effect = value;
		}
	}

	public RenderOptions RenderOptions
	{
		get
		{
			return _renderOptions;
		}
		set
		{
			bool flag = false;
			if (_renderOptions != value)
			{
				flag = true;
				_renderOptions = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.RenderOptions;
				RegisterForSerialization();
			}
			_renderOptions = value;
		}
	}

	public TextOptions TextOptions
	{
		get
		{
			return _textOptions;
		}
		set
		{
			bool flag = false;
			if (_textOptions != value)
			{
				flag = true;
				_textOptions = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.TextOptions;
				RegisterForSerialization();
			}
			_textOptions = value;
		}
	}

	internal CompositionCacheMode? CacheMode
	{
		get
		{
			return _cacheMode;
		}
		set
		{
			bool flag = false;
			if (_cacheMode != value)
			{
				flag = true;
				_cacheMode = value;
				_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.CacheMode;
				RegisterForSerialization();
			}
			_cacheMode = value;
		}
	}

	private protected virtual void OnRootChangedCore()
	{
	}

	internal ServerCompositionVisual.ReadbackData? TryGetValidReadback()
	{
		if (Root == null)
		{
			return null;
		}
		ReadbackIndices readback = Server.Compositor.Readback;
		ServerCompositionVisual.ReadbackData readback2 = Server.GetReadback(readback.ReadRevision);
		if (readback2 == null)
		{
			return null;
		}
		if (!readback2.Visible || readback2.TargetId != Root.Server.Id)
		{
			return null;
		}
		if (readback2.TargetId != Root.Server.Id)
		{
			return null;
		}
		return readback2;
	}

	internal virtual bool HitTest(Point point)
	{
		return true;
	}

	internal CompositionVisual(Compositor compositor, ServerCompositionVisual server)
		: base(compositor, server)
	{
		Server = server;
		InitializeDefaults();
	}

	private void OnRootChanged()
	{
		OnRootChangedCore();
	}

	private void OnParentChanged()
	{
		Root = Parent?.Root;
		if (CustomHitTestCountInSubTree != 0)
		{
			for (CompositionVisual parent = Parent; parent != null; parent = parent.Parent)
			{
				parent.CustomHitTestCountInSubTree += CustomHitTestCountInSubTree;
			}
		}
	}

	private void OnParentChanging()
	{
		if (CustomHitTestCountInSubTree != 0)
		{
			for (CompositionVisual parent = Parent; parent != null; parent = parent.Parent)
			{
				parent.CustomHitTestCountInSubTree -= CustomHitTestCountInSubTree;
			}
		}
	}

	private void InitializeDefaults()
	{
		Visible = true;
		Opacity = 1f;
		ClipToBounds = true;
		Orientation = Quaternion.Identity;
		Scale = new Vector3D(1.0, 1.0, 1.0);
		TransformMatrix = Matrix.Identity;
	}

	private protected override void SerializeChangesCore(BatchStreamWriter writer)
	{
		base.SerializeChangesCore(writer);
		writer.Write(_changedFieldsOfCompositionVisual);
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Root) == CompositionVisualChangedFields.Root)
		{
			writer.WriteObject(_root?.Server);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Parent) == CompositionVisualChangedFields.Parent)
		{
			writer.WriteObject(_parent?.Server);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.VisibleAnimated) == CompositionVisualChangedFields.VisibleAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfVisibleProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Visible) == CompositionVisualChangedFields.Visible)
		{
			writer.Write(_visible);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.OpacityAnimated) == CompositionVisualChangedFields.OpacityAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfOpacityProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Opacity) == CompositionVisualChangedFields.Opacity)
		{
			writer.Write(_opacity);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Clip) == CompositionVisualChangedFields.Clip)
		{
			writer.WriteObject(_clip);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.ClipToBoundsAnimated) == CompositionVisualChangedFields.ClipToBoundsAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfClipToBoundsProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.ClipToBounds) == CompositionVisualChangedFields.ClipToBounds)
		{
			writer.Write(_clipToBounds);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.OffsetAnimated) == CompositionVisualChangedFields.OffsetAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfOffsetProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Offset) == CompositionVisualChangedFields.Offset)
		{
			writer.Write(_offset);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.TranslationAnimated) == CompositionVisualChangedFields.TranslationAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfTranslationProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Translation) == CompositionVisualChangedFields.Translation)
		{
			writer.Write(_translation);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.SizeAnimated) == CompositionVisualChangedFields.SizeAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfSizeProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Size) == CompositionVisualChangedFields.Size)
		{
			writer.Write(_size);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.AnchorPointAnimated) == CompositionVisualChangedFields.AnchorPointAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfAnchorPointProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.AnchorPoint) == CompositionVisualChangedFields.AnchorPoint)
		{
			writer.Write(_anchorPoint);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.CenterPointAnimated) == CompositionVisualChangedFields.CenterPointAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfCenterPointProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.CenterPoint) == CompositionVisualChangedFields.CenterPoint)
		{
			writer.Write(_centerPoint);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.RotationAngleAnimated) == CompositionVisualChangedFields.RotationAngleAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfRotationAngleProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.RotationAngle) == CompositionVisualChangedFields.RotationAngle)
		{
			writer.Write(_rotationAngle);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.OrientationAnimated) == CompositionVisualChangedFields.OrientationAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfOrientationProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Orientation) == CompositionVisualChangedFields.Orientation)
		{
			writer.Write(_orientation);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.ScaleAnimated) == CompositionVisualChangedFields.ScaleAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfScaleProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Scale) == CompositionVisualChangedFields.Scale)
		{
			writer.Write(_scale);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.TransformMatrixAnimated) == CompositionVisualChangedFields.TransformMatrixAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionVisual.s_IdOfTransformMatrixProperty));
		}
		else if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.TransformMatrix) == CompositionVisualChangedFields.TransformMatrix)
		{
			writer.Write(_transformMatrix);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.AdornedVisual) == CompositionVisualChangedFields.AdornedVisual)
		{
			writer.WriteObject(_adornedVisual?.Server);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.AdornerIsClipped) == CompositionVisualChangedFields.AdornerIsClipped)
		{
			writer.Write(_adornerIsClipped);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.OpacityMaskBrush) == CompositionVisualChangedFields.OpacityMaskBrush)
		{
			writer.WriteObject(_opacityMaskBrush);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Effect) == CompositionVisualChangedFields.Effect)
		{
			writer.WriteObject(_effect);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.RenderOptions) == CompositionVisualChangedFields.RenderOptions)
		{
			writer.Write(_renderOptions);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.TextOptions) == CompositionVisualChangedFields.TextOptions)
		{
			writer.Write(_textOptions);
		}
		if ((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.CacheMode) == CompositionVisualChangedFields.CacheMode)
		{
			writer.WriteObject(_cacheMode?.Server);
		}
		_changedFieldsOfCompositionVisual = (CompositionVisualChangedFields)0uL;
	}

	internal override void StartAnimation(string propertyName, CompositionAnimation animation, ExpressionVariant? finalValue)
	{
		switch (propertyName)
		{
		case "Visible":
		{
			_ = _visible;
			IAnimationInstance value12 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfVisibleProperty] = value12;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.VisibleAnimated;
			RegisterForSerialization();
			break;
		}
		case "Opacity":
		{
			_ = _opacity;
			IAnimationInstance value11 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfOpacityProperty] = value11;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.OpacityAnimated;
			RegisterForSerialization();
			break;
		}
		case "ClipToBounds":
		{
			_ = _clipToBounds;
			IAnimationInstance value10 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfClipToBoundsProperty] = value10;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.ClipToBoundsAnimated;
			RegisterForSerialization();
			break;
		}
		case "Offset":
		{
			_ = _offset;
			IAnimationInstance value9 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfOffsetProperty] = value9;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.OffsetAnimated;
			RegisterForSerialization();
			break;
		}
		case "Translation":
		{
			_ = _translation;
			IAnimationInstance value8 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfTranslationProperty] = value8;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.TranslationAnimated;
			RegisterForSerialization();
			break;
		}
		case "Size":
		{
			_ = _size;
			IAnimationInstance value7 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfSizeProperty] = value7;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.SizeAnimated;
			RegisterForSerialization();
			break;
		}
		case "AnchorPoint":
		{
			_ = _anchorPoint;
			IAnimationInstance value6 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfAnchorPointProperty] = value6;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.AnchorPointAnimated;
			RegisterForSerialization();
			break;
		}
		case "CenterPoint":
		{
			_ = _centerPoint;
			IAnimationInstance value5 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfCenterPointProperty] = value5;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.CenterPointAnimated;
			RegisterForSerialization();
			break;
		}
		case "RotationAngle":
		{
			_ = _rotationAngle;
			IAnimationInstance value4 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfRotationAngleProperty] = value4;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.RotationAngleAnimated;
			RegisterForSerialization();
			break;
		}
		case "Orientation":
		{
			_ = _orientation;
			IAnimationInstance value3 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfOrientationProperty] = value3;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.OrientationAnimated;
			RegisterForSerialization();
			break;
		}
		case "Scale":
		{
			_ = _scale;
			IAnimationInstance value2 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfScaleProperty] = value2;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.ScaleAnimated;
			RegisterForSerialization();
			break;
		}
		case "TransformMatrix":
		{
			_ = _transformMatrix;
			IAnimationInstance value = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionVisual.s_IdOfTransformMatrixProperty] = value;
			_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.TransformMatrixAnimated;
			RegisterForSerialization();
			break;
		}
		default:
			base.StartAnimation(propertyName, animation, finalValue);
			break;
		}
	}
}
