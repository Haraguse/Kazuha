using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Server-side <see cref="T:Avalonia.Rendering.Composition.CompositionVisual" /> counterpart.
/// Is responsible for computing the transformation matrix, for applying various visual
/// properties before calling visual-specific drawing code and for notifying the
/// <see cref="T:Avalonia.Rendering.Composition.Server.ServerCompositionTarget" /> for new dirty rects
/// </summary>
internal abstract class ServerCompositionVisual : ServerObject
{
	private class AttHelper
	{
		public readonly HashSet<Action> AncestorChainTransformSubscribers = new HashSet<Action>();

		public required Action ParentActSubscriptionAction;

		public required Action AdornedVisualActSubscriptionAction;

		public bool EnqueuedForAdornerUpdate;
	}

	[StructLayout(LayoutKind.Auto)]
	private struct RenderContext : IServerTreeVisitor, IDisposable
	{
		private enum Op
		{
			PopClip,
			PopGeometryClip,
			Stop
		}

		private Stack<int>? _adornerPushedClipStack;

		private ServerCompositionVisual? _currentAdornerLayer;

		private readonly IDrawingContextImpl _canvas;

		private readonly IDirtyRectTracker? _dirtyRects;

		private readonly CompositorPools _pools;

		private readonly bool _renderChildren;

		private TreeWalkContext _walkContext;

		private Stack<double> _opacityStack;

		private double _opacity;

		private bool _fullSkip;

		private bool _usedCache;

		public int RenderedVisuals;

		public int VisitedVisuals;

		private ServerVisualRenderContext _publicContext;

		private readonly ServerCompositionVisual _rootVisual;

		private bool _skipNextVisualTransform;

		private bool _renderingToBitmapCache;

		private bool AdornerLayer_WalkAdornerParentClipRecursive(ServerCompositionVisual? visual)
		{
			if (visual != _currentAdornerLayer)
			{
				if (visual == null)
				{
					return false;
				}
				if (!AdornerLayer_WalkAdornerParentClipRecursive(visual.Parent))
				{
					return false;
				}
			}
			if (visual._ownTransform.HasValue)
			{
				_canvas.Transform = visual._ownTransform.Value * _canvas.Transform;
			}
			if (visual.ClipToBounds)
			{
				_canvas.PushClip(new Rect(0.0, 0.0, visual.Size.X, visual.Size.Y));
				_adornerPushedClipStack.Push(0);
			}
			if (visual.Clip != null)
			{
				_canvas.PushGeometryClip(visual.Clip);
				_adornerPushedClipStack.Push(1);
			}
			return true;
		}

		private bool SkipAdornerClip(ServerCompositionVisual visual)
		{
			if (!visual.AdornerIsClipped || visual == _rootVisual || visual._parent == _rootVisual || AdornerLayer_GetExpectedSharedAncestor(visual) == null)
			{
				return true;
			}
			return false;
		}

		private void AdornerHelper_RenderPreGraphPushAdornerClip(ServerCompositionVisual visual)
		{
			if (SkipAdornerClip(visual))
			{
				return;
			}
			if (_adornerPushedClipStack == null)
			{
				_adornerPushedClipStack = _pools.IntStackPool.Rent();
			}
			_adornerPushedClipStack.Push(2);
			Matrix transform = _canvas.Transform;
			Matrix matrix = transform;
			if (visual._ownTransform.HasValue)
			{
				if (!visual._ownTransform.Value.TryInvert(out var inverted))
				{
					return;
				}
				matrix = inverted * matrix;
			}
			_canvas.Transform = matrix;
			_currentAdornerLayer = AdornerLayer_GetExpectedSharedAncestor(visual);
			AdornerLayer_WalkAdornerParentClipRecursive(visual.AdornedVisual);
			_canvas.Transform = transform;
		}

		private void AdornerHelper_RenderPostGraphPushAdornerClip(ServerCompositionVisual visual)
		{
			if (SkipAdornerClip(visual) || _adornerPushedClipStack == null)
			{
				return;
			}
			while (_adornerPushedClipStack.Count > 0)
			{
				switch ((Op)_adornerPushedClipStack.Pop())
				{
				case Op.PopGeometryClip:
					_canvas.PopGeometryClip();
					break;
				case Op.PopClip:
					_canvas.PopClip();
					break;
				case Op.Stop:
					return;
				}
			}
		}

		private void AdornerHelper_Dispose()
		{
			_pools.IntStackPool.Return(ref _adornerPushedClipStack);
		}

		public RenderContext(ServerCompositionVisual rootVisual, IDrawingContextImpl canvas, IDirtyRectTracker? dirtyRects, CompositorPools pools, Matrix matrix, LtrbRect clip, bool renderChildren, bool skipRootVisualTransform, bool renderingToBitmapCache)
		{
			_adornerPushedClipStack = null;
			_currentAdornerLayer = null;
			_fullSkip = false;
			_usedCache = false;
			RenderedVisuals = 0;
			VisitedVisuals = 0;
			_publicContext = new ServerVisualRenderContext(canvas);
			if (dirtyRects != null)
			{
				LtrbRect combinedRect = dirtyRects.CombinedRect;
				if (dirtyRects is SingleDirtyRectTracker)
				{
					dirtyRects = null;
				}
				clip = clip.IntersectOrEmpty(combinedRect);
			}
			_canvas = canvas;
			_dirtyRects = dirtyRects;
			_pools = pools;
			_renderChildren = renderChildren;
			_rootVisual = rootVisual;
			_walkContext = new TreeWalkContext(pools, matrix, clip);
			_opacity = 1.0;
			_opacityStack = pools.DoubleStackPool.Rent();
			_skipNextVisualTransform = skipRootVisualTransform;
			_renderingToBitmapCache = renderingToBitmapCache;
		}

		private bool HandlePreGraphTransformClipOpacity(ServerCompositionVisual visual)
		{
			if (!visual.Visible || !visual._transformedSubTreeBounds.HasValue)
			{
				return false;
			}
			double num = (double)visual.Opacity * _opacity;
			if (num <= 0.003)
			{
				return false;
			}
			ref Matrix reference = ref _walkContext.Transform;
			if (visual._ownTransform.HasValue && !_skipNextVisualTransform)
			{
				Matrix matrix = visual._ownTransform.Value * _walkContext.Transform;
				reference = ref matrix;
			}
			_skipNextVisualTransform = false;
			LtrbRect rect = _walkContext.Clip;
			if (visual._ownClipRect.HasValue)
			{
				rect = rect.IntersectOrEmpty(visual._ownClipRect.Value.TransformToAABB(reference));
			}
			LtrbRect rect2 = visual._transformedSubTreeBounds.Value.TransformToAABB(_walkContext.Transform);
			if (rect.Intersects(rect2))
			{
				IDirtyRectTracker? dirtyRects = _dirtyRects;
				if (dirtyRects == null || dirtyRects.Intersects(rect2))
				{
					RenderedVisuals++;
					if (visual.Opacity != 1f)
					{
						_opacityStack.Push(_opacity);
						_opacity = num;
						_canvas.PushOpacity(visual.Opacity, visual._transformedSubTreeBounds.Value.ToRect());
					}
					if (visual._ownTransform.HasValue)
					{
						_walkContext.PushSetTransform(in reference);
						_canvas.Transform = reference;
					}
					if (visual._ownClipRect.HasValue)
					{
						_walkContext.PushClip(rect);
					}
					if (visual.ClipToBounds)
					{
						visual.PushClipToBounds(_canvas);
					}
					if (visual.Clip != null)
					{
						_canvas.PushGeometryClip(visual.Clip);
					}
					return true;
				}
			}
			return false;
		}

		public void PreSubgraph(ServerCompositionVisual visual, out bool visitChildren)
		{
			VisitedVisuals++;
			if (!_renderingToBitmapCache || visual != _rootVisual)
			{
				if (!HandlePreGraphTransformClipOpacity(visual))
				{
					_fullSkip = true;
					visitChildren = false;
					return;
				}
				if (visual.AdornedVisual != null)
				{
					AdornerHelper_RenderPreGraphPushAdornerClip(visual);
				}
				if (visual.Cache != null)
				{
					var (num, num2) = visual.Cache.Draw(_canvas);
					VisitedVisuals += num;
					RenderedVisuals += num2;
					_usedCache = true;
					visitChildren = false;
					return;
				}
			}
			if (visual.RenderOptions != default(RenderOptions))
			{
				_canvas.PushRenderOptions(visual.RenderOptions);
			}
			if (visual.TextOptions != default(TextOptions))
			{
				_canvas.PushTextOptions(visual.TextOptions);
			}
			if (visual.OpacityMaskBrush != null)
			{
				_canvas.PushOpacityMask(visual.OpacityMaskBrush, visual._subTreeBounds.Value.ToRect());
			}
			if (visual.Effect != null && _canvas is IDrawingContextImplWithEffects drawingContextImplWithEffects)
			{
				drawingContextImplWithEffects.PushEffect(visual._subTreeBounds.Value.ToRect(), visual.Effect);
			}
			visual.RenderCore(_publicContext, _walkContext.Clip);
			visitChildren = _renderChildren;
		}

		public void PostSubgraph(ServerCompositionVisual visual)
		{
			if (_fullSkip)
			{
				_fullSkip = false;
				return;
			}
			bool num = _renderingToBitmapCache && visual == _rootVisual;
			if (!_usedCache)
			{
				if (visual.Effect != null && _canvas is IDrawingContextImplWithEffects drawingContextImplWithEffects)
				{
					drawingContextImplWithEffects.PopEffect();
				}
				if (visual.OpacityMaskBrush != null)
				{
					_canvas.PopOpacityMask();
				}
				if (visual.TextOptions != default(TextOptions))
				{
					_canvas.PopTextOptions();
				}
				if (visual.RenderOptions != default(RenderOptions))
				{
					_canvas.PopRenderOptions();
				}
			}
			_usedCache = false;
			if (!num)
			{
				if (visual.AdornedVisual != null)
				{
					AdornerHelper_RenderPostGraphPushAdornerClip(visual);
				}
				if (visual.Clip != null)
				{
					_canvas.PopGeometryClip();
				}
				if (visual.ClipToBounds)
				{
					_canvas.PopClip();
				}
				if (visual._ownClipRect.HasValue)
				{
					_walkContext.PopClip();
				}
				if (visual._ownTransform.HasValue)
				{
					_walkContext.PopTransform();
					_canvas.Transform = _walkContext.Transform;
				}
				if (visual.Opacity != 1f)
				{
					_canvas.PopOpacity();
					_opacity = _opacityStack.Pop();
				}
			}
		}

		public void Dispose()
		{
			_walkContext.Dispose();
			_pools.DoubleStackPool.Return(ref _opacityStack);
			AdornerHelper_Dispose();
		}
	}

	public class ReadbackData
	{
		public Matrix Matrix;

		public ulong Revision;

		public long TargetId;

		public bool Visible;

		public LtrbRect? TransformedSubtreeBounds;
	}

	private struct UpdateContext : IServerTreeVisitor, IDisposable
	{
		private TreeWalkContext _context;

		private IDirtyRectCollector _dirtyRegion;

		private int _dirtyRegionDisableCount;

		private Stack<int> _dirtyRegionDisableCountStack;

		private Stack<IDirtyRectCollector> _dirtyRegionCollectorStack;

		private bool AreDirtyRegionsDisabled()
		{
			return _dirtyRegionDisableCount != 0;
		}

		public UpdateContext(CompositorPools pools, IDirtyRectCollector dirtyRects, Matrix transform, LtrbRect clip)
		{
			_dirtyRegionDisableCount = 0;
			_dirtyRegion = dirtyRects;
			_context = new TreeWalkContext(pools, transform, clip);
			_dirtyRegionDisableCountStack = pools.IntStackPool.Rent();
			_dirtyRegionCollectorStack = pools.DirtyRectCollectorStackPool.Rent();
		}

		private void PushCacheIfNeeded(ServerCompositionVisual visual)
		{
			if (visual.Cache != null)
			{
				_dirtyRegionCollectorStack.Push(_dirtyRegion);
				_dirtyRegion = visual.Cache.DirtyRectCollector;
				_dirtyRegionDisableCountStack.Push(_dirtyRegionDisableCount);
				_dirtyRegionDisableCount = 0;
				_context.PushSetTransform(Matrix.Identity);
				_context.ResetClip(LtrbRect.Infinite);
			}
		}

		private void PopCacheIfNeeded(ServerCompositionVisual visual)
		{
			if (visual.Cache != null)
			{
				_context.PopClip();
				_context.PopTransform();
				_dirtyRegion = _dirtyRegionCollectorStack.Pop();
				_dirtyRegionDisableCount = _dirtyRegionDisableCountStack.Pop();
				if (visual.Cache.IsDirty)
				{
					AddToDirtyRegion(visual._subTreeBounds);
				}
			}
		}

		private bool NeedToPushBoundsAffectingProperties(ServerCompositionVisual node)
		{
			if (!node._isDirtyForRenderInSubgraph && !node._needsToAddExtraDirtyRectToDirtyRegion)
			{
				return node._contentChanged;
			}
			return true;
		}

		public void PreSubgraph(ServerCompositionVisual node, out bool visitChildren)
		{
			visitChildren = node._isDirtyForRenderInSubgraph || node._needsBoundingBoxUpdate;
			if (node != null && node._needsBoundingBoxUpdate && node.OpacityMaskBrush != null)
			{
				node._isDirtyForRender = true;
			}
			if (node._isDirtyForRender || (node != null && node._isDirtyForRenderInSubgraph && node.HasEffect))
			{
				if (node._needsBoundingBoxUpdate && !AreDirtyRegionsDisabled())
				{
					AddToDirtyRegion(node._transformedSubTreeBounds);
				}
				_dirtyRegionDisableCount++;
			}
			if (NeedToPushBoundsAffectingProperties(node))
			{
				if (!AreDirtyRegionsDisabled())
				{
					PushBoundsAffectingProperties(node);
				}
				PushCacheIfNeeded(node);
			}
			if (node._needsBoundingBoxUpdate)
			{
				node._subTreeBounds = node._ownContentBounds;
			}
		}

		public void PostSubgraph(ServerCompositionVisual node)
		{
			ServerCompositionVisual parent = node.Parent;
			if (node._needsBoundingBoxUpdate)
			{
				FinalizeSubtreeBounds(node);
			}
			if (parent != null && parent._needsBoundingBoxUpdate)
			{
				parent._subTreeBounds = LtrbRect.FullUnion(parent._subTreeBounds, node._transformedSubTreeBounds);
			}
			if (node._needsToAddExtraDirtyRectToDirtyRegion)
			{
				AddToDirtyRegion(node._extraDirtyRect);
			}
			if (NeedToPushBoundsAffectingProperties(node))
			{
				PopCacheIfNeeded(node);
				if (!AreDirtyRegionsDisabled())
				{
					PopBoundsAffectingProperties(node);
				}
			}
			if (node._isDirtyForRender || (node != null && node._isDirtyForRenderInSubgraph && node.Effect != null))
			{
				_dirtyRegionDisableCount--;
				AddToDirtyRegion(node._transformedSubTreeBounds);
			}
			node._isDirtyForRender = false;
			node._isDirtyForRenderInSubgraph = false;
			node._needsBoundingBoxUpdate = false;
			node._needsToAddExtraDirtyRectToDirtyRegion = false;
			node._contentChanged = false;
		}

		private void FinalizeSubtreeBounds(ServerCompositionVisual node)
		{
			if (!node.Visible)
			{
				node._subTreeBounds = null;
			}
			if (node._subTreeBounds.HasValue)
			{
				if (node.Effect != null)
				{
					node._subTreeBounds = node._subTreeBounds.Value.Inflate(node.Effect.GetEffectOutputPadding());
				}
				if (node._ownClipRect.HasValue)
				{
					node._subTreeBounds = node._subTreeBounds.Value.IntersectOrNull(node._ownClipRect.Value);
				}
			}
			if (!node._subTreeBounds.HasValue)
			{
				node._transformedSubTreeBounds = null;
			}
			else if (node._ownTransform.HasValue)
			{
				node._transformedSubTreeBounds = node._subTreeBounds?.TransformToAABB(node._ownTransform.Value);
			}
			else
			{
				node._transformedSubTreeBounds = node._subTreeBounds;
			}
			node.EnqueueForReadbackUpdate();
		}

		private void AddToDirtyRegion(LtrbRect? bounds)
		{
			if (_dirtyRegionDisableCount == 0 && bounds.HasValue)
			{
				LtrbRect rect = bounds.Value.TransformToAABB(_context.Transform).IntersectOrEmpty(_context.Clip);
				if (!rect.IsZeroSize)
				{
					_dirtyRegion.AddRect(rect);
				}
			}
		}

		private void PushBoundsAffectingProperties(ServerCompositionVisual node)
		{
			if (node._ownTransform.HasValue)
			{
				_context.PushTransform(node._ownTransform.Value);
			}
			if (node._ownClipRect.HasValue)
			{
				_context.PushClip(node._ownClipRect.Value.TransformToAABB(_context.Transform));
			}
		}

		private void PopBoundsAffectingProperties(ServerCompositionVisual node)
		{
			if (node._ownTransform.HasValue)
			{
				_context.PopTransform();
			}
			if (node._ownClipRect.HasValue)
			{
				_context.PopClip();
			}
		}

		public void Dispose()
		{
			_context.Pools.IntStackPool.Return(ref _dirtyRegionDisableCountStack);
			_context.Pools.DirtyRectCollectorStackPool.Return(ref _dirtyRegionCollectorStack);
			_context.Dispose();
		}
	}

	private interface IServerTreeVisitor
	{
		void PreSubgraph(ServerCompositionVisual visual, out bool visitChildren);

		void PostSubgraph(ServerCompositionVisual visual);
	}

	public record struct TreeWalkerFrame(ServerCompositionVisual Visual, int CurrentIndex);

	private static class ServerTreeWalker<TVisitor> where TVisitor : struct, IServerTreeVisitor
	{
		public static void Walk(ref TVisitor visitor, ServerCompositionVisual root)
		{
			CompositorPools.StackPool<TreeWalkerFrame> treeWalkerFrameStackPool = root.Compositor.Pools.TreeWalkerFrameStackPool;
			Stack<TreeWalkerFrame> stack = treeWalkerFrameStackPool.Rent();
			try
			{
				visitor.PreSubgraph(root, out var visitChildren);
				ServerCompositionVisual serverCompositionVisual = root;
				if (!visitChildren || serverCompositionVisual.Children == null || serverCompositionVisual.Children.List.Count == 0)
				{
					visitor.PostSubgraph(root);
					return;
				}
				int num = 0;
				while (true)
				{
					if (num >= serverCompositionVisual.Children.List.Count)
					{
						visitor.PostSubgraph(serverCompositionVisual);
						if (!stack.TryPop(out var result))
						{
							break;
						}
						TreeWalkerFrame treeWalkerFrame = result;
						(serverCompositionVisual, num) = treeWalkerFrame;
						continue;
					}
					ServerCompositionVisual serverCompositionVisual3 = serverCompositionVisual.Children.List[num];
					visitor.PreSubgraph(serverCompositionVisual3, out visitChildren);
					if (visitChildren && serverCompositionVisual3.Children.List.Count > 0)
					{
						stack.Push(new TreeWalkerFrame(serverCompositionVisual, num + 1));
						serverCompositionVisual = serverCompositionVisual3;
						num = 0;
					}
					else
					{
						visitor.PostSubgraph(serverCompositionVisual3);
						num++;
					}
				}
			}
			finally
			{
				treeWalkerFrameStackPool.Return(stack);
			}
		}
	}

	private struct TreeWalkContext : IDisposable
	{
		private readonly CompositorPools _pools;

		public Matrix Transform;

		public LtrbRect Clip;

		private Stack<Matrix> _transformStack;

		private Stack<LtrbRect> _clipStack;

		public CompositorPools Pools => _pools;

		public TreeWalkContext(CompositorPools pools, Matrix transform, LtrbRect clip)
		{
			_pools = pools;
			Transform = transform;
			Clip = clip;
			_transformStack = pools.MatrixStackPool.Rent();
			_clipStack = pools.LtrbRectStackPool.Rent();
		}

		public void PushTransform(in Matrix m)
		{
			_transformStack.Push(Transform);
			Transform = m * Transform;
		}

		public void PushSetTransform(in Matrix m)
		{
			_transformStack.Push(Transform);
			Transform = m;
		}

		public void PushClip(LtrbRect rect)
		{
			_clipStack.Push(Clip);
			Clip = Clip.IntersectOrEmpty(rect);
		}

		public void ResetClip(LtrbRect rect)
		{
			_clipStack.Push(Clip);
			Clip = rect;
		}

		public void PopTransform()
		{
			Transform = _transformStack.Pop();
		}

		public void PopClip()
		{
			Clip = _clipStack.Pop();
		}

		public void Dispose()
		{
			_pools.MatrixStackPool.Return(ref _transformStack);
			_pools.LtrbRectStackPool.Return(ref _clipStack);
		}
	}

	private AttHelper? _AttHelper;

	private bool _combinedTransformDirty;

	private bool _clipSizeDirty;

	private bool _ownBoundsDirty;

	private bool _compositionFieldsDirty;

	private bool _contentChanged;

	private bool _delayPropagateNeedsBoundsUpdate;

	private bool _delayPropagateIsDirtyForRender;

	private bool _delayPropagateHasExtraDirtyRects;

	private bool _needsBoundingBoxUpdate;

	private bool _isDirtyForRender;

	private bool _isDirtyForRenderInSubgraph;

	private Matrix? _ownTransform;

	private LtrbRect? _ownContentBounds;

	private LtrbRect? _subTreeBounds;

	private LtrbRect? _transformedSubTreeBounds;

	private LtrbRect? _ownClipRect;

	private bool _needsToAddExtraDirtyRectToDirtyRegion;

	private LtrbRect _extraDirtyRect;

	private bool _enqueuedForOwnPropertiesRecompute;

	private const CompositionVisualChangedFields CompositionFieldsMask = CompositionVisualChangedFields.Opacity | CompositionVisualChangedFields.OpacityAnimated | CompositionVisualChangedFields.Clip | CompositionVisualChangedFields.ClipToBounds | CompositionVisualChangedFields.ClipToBoundsAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.OpacityMaskBrush | CompositionVisualChangedFields.Effect | CompositionVisualChangedFields.RenderOptions;

	private const CompositionVisualChangedFields OwnBoundsUpdateFieldsMask = CompositionVisualChangedFields.Clip | CompositionVisualChangedFields.ClipToBounds | CompositionVisualChangedFields.ClipToBoundsAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.Effect;

	private const CompositionVisualChangedFields CombinedTransformFieldsMask = CompositionVisualChangedFields.Offset | CompositionVisualChangedFields.OffsetAnimated | CompositionVisualChangedFields.Translation | CompositionVisualChangedFields.TranslationAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.AnchorPoint | CompositionVisualChangedFields.AnchorPointAnimated | CompositionVisualChangedFields.CenterPoint | CompositionVisualChangedFields.CenterPointAnimated | CompositionVisualChangedFields.RotationAngle | CompositionVisualChangedFields.RotationAngleAnimated | CompositionVisualChangedFields.Orientation | CompositionVisualChangedFields.OrientationAnimated | CompositionVisualChangedFields.Scale | CompositionVisualChangedFields.ScaleAnimated | CompositionVisualChangedFields.TransformMatrix | CompositionVisualChangedFields.AdornedVisual;

	private const CompositionVisualChangedFields ClipSizeDirtyMask = CompositionVisualChangedFields.Clip | CompositionVisualChangedFields.ClipToBounds | CompositionVisualChangedFields.ClipToBoundsAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated;

	private const CompositionVisualChangedFields ReadbackDirtyMask = CompositionVisualChangedFields.Root | CompositionVisualChangedFields.Visible | CompositionVisualChangedFields.VisibleAnimated | CompositionVisualChangedFields.Offset | CompositionVisualChangedFields.OffsetAnimated | CompositionVisualChangedFields.Translation | CompositionVisualChangedFields.TranslationAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.AnchorPoint | CompositionVisualChangedFields.AnchorPointAnimated | CompositionVisualChangedFields.CenterPoint | CompositionVisualChangedFields.CenterPointAnimated | CompositionVisualChangedFields.RotationAngle | CompositionVisualChangedFields.RotationAngleAnimated | CompositionVisualChangedFields.Orientation | CompositionVisualChangedFields.OrientationAnimated | CompositionVisualChangedFields.Scale | CompositionVisualChangedFields.ScaleAnimated | CompositionVisualChangedFields.TransformMatrix | CompositionVisualChangedFields.AdornedVisual;

	private ReadbackData _readback0 = new ReadbackData
	{
		Revision = ulong.MaxValue
	};

	private ReadbackData _readback1 = new ReadbackData
	{
		Revision = ulong.MaxValue
	};

	private bool _enqueuedForReadbackUpdate;

	private ServerCompositionTarget? _root;

	internal static readonly CompositionProperty<ServerCompositionTarget?> s_IdOfRootProperty = CompositionProperty.Register<ServerCompositionVisual, ServerCompositionTarget>("Root", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._root, delegate(SimpleServerObject obj, ServerCompositionTarget? v)
	{
		((ServerCompositionVisual)obj)._root = v;
	}, null);

	private ServerCompositionVisual? _parent;

	internal static readonly CompositionProperty<ServerCompositionVisual?> s_IdOfParentProperty = CompositionProperty.Register<ServerCompositionVisual, ServerCompositionVisual>("Parent", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._parent, delegate(SimpleServerObject obj, ServerCompositionVisual? v)
	{
		((ServerCompositionVisual)obj)._parent = v;
	}, null);

	private bool _visible;

	internal static readonly CompositionProperty<bool> s_IdOfVisibleProperty = CompositionProperty.Register<ServerCompositionVisual, bool>("Visible", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._visible, delegate(SimpleServerObject obj, bool v)
	{
		((ServerCompositionVisual)obj)._visible = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._visible);

	private float _opacity;

	internal static readonly CompositionProperty<float> s_IdOfOpacityProperty = CompositionProperty.Register<ServerCompositionVisual, float>("Opacity", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._opacity, delegate(SimpleServerObject obj, float v)
	{
		((ServerCompositionVisual)obj)._opacity = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._opacity);

	private IGeometryImpl? _clip;

	internal static readonly CompositionProperty<IGeometryImpl?> s_IdOfClipProperty = CompositionProperty.Register<ServerCompositionVisual, IGeometryImpl>("Clip", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._clip, delegate(SimpleServerObject obj, IGeometryImpl? v)
	{
		((ServerCompositionVisual)obj)._clip = v;
	}, null);

	private bool _clipToBounds;

	internal static readonly CompositionProperty<bool> s_IdOfClipToBoundsProperty = CompositionProperty.Register<ServerCompositionVisual, bool>("ClipToBounds", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._clipToBounds, delegate(SimpleServerObject obj, bool v)
	{
		((ServerCompositionVisual)obj)._clipToBounds = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._clipToBounds);

	private Vector3D _offset;

	internal static readonly CompositionProperty<Vector3D> s_IdOfOffsetProperty = CompositionProperty.Register<ServerCompositionVisual, Vector3D>("Offset", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._offset, delegate(SimpleServerObject obj, Vector3D v)
	{
		((ServerCompositionVisual)obj)._offset = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._offset);

	private Vector3D _translation;

	internal static readonly CompositionProperty<Vector3D> s_IdOfTranslationProperty = CompositionProperty.Register<ServerCompositionVisual, Vector3D>("Translation", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._translation, delegate(SimpleServerObject obj, Vector3D v)
	{
		((ServerCompositionVisual)obj)._translation = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._translation);

	private Vector _size;

	internal static readonly CompositionProperty<Vector> s_IdOfSizeProperty = CompositionProperty.Register<ServerCompositionVisual, Vector>("Size", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._size, delegate(SimpleServerObject obj, Vector v)
	{
		((ServerCompositionVisual)obj)._size = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._size);

	private Vector _anchorPoint;

	internal static readonly CompositionProperty<Vector> s_IdOfAnchorPointProperty = CompositionProperty.Register<ServerCompositionVisual, Vector>("AnchorPoint", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._anchorPoint, delegate(SimpleServerObject obj, Vector v)
	{
		((ServerCompositionVisual)obj)._anchorPoint = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._anchorPoint);

	private Vector3D _centerPoint;

	internal static readonly CompositionProperty<Vector3D> s_IdOfCenterPointProperty = CompositionProperty.Register<ServerCompositionVisual, Vector3D>("CenterPoint", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._centerPoint, delegate(SimpleServerObject obj, Vector3D v)
	{
		((ServerCompositionVisual)obj)._centerPoint = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._centerPoint);

	private float _rotationAngle;

	internal static readonly CompositionProperty<float> s_IdOfRotationAngleProperty = CompositionProperty.Register<ServerCompositionVisual, float>("RotationAngle", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._rotationAngle, delegate(SimpleServerObject obj, float v)
	{
		((ServerCompositionVisual)obj)._rotationAngle = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._rotationAngle);

	private Quaternion _orientation;

	internal static readonly CompositionProperty<Quaternion> s_IdOfOrientationProperty = CompositionProperty.Register<ServerCompositionVisual, Quaternion>("Orientation", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._orientation, delegate(SimpleServerObject obj, Quaternion v)
	{
		((ServerCompositionVisual)obj)._orientation = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._orientation);

	private Vector3D _scale;

	internal static readonly CompositionProperty<Vector3D> s_IdOfScaleProperty = CompositionProperty.Register<ServerCompositionVisual, Vector3D>("Scale", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._scale, delegate(SimpleServerObject obj, Vector3D v)
	{
		((ServerCompositionVisual)obj)._scale = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._scale);

	private Matrix _transformMatrix;

	internal static readonly CompositionProperty<Matrix> s_IdOfTransformMatrixProperty = CompositionProperty.Register<ServerCompositionVisual, Matrix>("TransformMatrix", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._transformMatrix, delegate(SimpleServerObject obj, Matrix v)
	{
		((ServerCompositionVisual)obj)._transformMatrix = v;
	}, null);

	private ServerCompositionVisual? _adornedVisual;

	internal static readonly CompositionProperty<ServerCompositionVisual?> s_IdOfAdornedVisualProperty = CompositionProperty.Register<ServerCompositionVisual, ServerCompositionVisual>("AdornedVisual", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._adornedVisual, delegate(SimpleServerObject obj, ServerCompositionVisual? v)
	{
		((ServerCompositionVisual)obj)._adornedVisual = v;
	}, null);

	private bool _adornerIsClipped;

	internal static readonly CompositionProperty<bool> s_IdOfAdornerIsClippedProperty = CompositionProperty.Register<ServerCompositionVisual, bool>("AdornerIsClipped", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._adornerIsClipped, delegate(SimpleServerObject obj, bool v)
	{
		((ServerCompositionVisual)obj)._adornerIsClipped = v;
	}, (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._adornerIsClipped);

	private IBrush? _opacityMaskBrush;

	internal static readonly CompositionProperty<IBrush?> s_IdOfOpacityMaskBrushProperty = CompositionProperty.Register<ServerCompositionVisual, IBrush>("OpacityMaskBrush", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._opacityMaskBrush, delegate(SimpleServerObject obj, IBrush? v)
	{
		((ServerCompositionVisual)obj)._opacityMaskBrush = v;
	}, null);

	private IImmutableEffect? _effect;

	internal static readonly CompositionProperty<IImmutableEffect?> s_IdOfEffectProperty = CompositionProperty.Register<ServerCompositionVisual, IImmutableEffect>("Effect", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._effect, delegate(SimpleServerObject obj, IImmutableEffect? v)
	{
		((ServerCompositionVisual)obj)._effect = v;
	}, null);

	private RenderOptions _renderOptions;

	internal static readonly CompositionProperty<RenderOptions> s_IdOfRenderOptionsProperty = CompositionProperty.Register<ServerCompositionVisual, RenderOptions>("RenderOptions", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._renderOptions, delegate(SimpleServerObject obj, RenderOptions v)
	{
		((ServerCompositionVisual)obj)._renderOptions = v;
	}, null);

	private TextOptions _textOptions;

	internal static readonly CompositionProperty<TextOptions> s_IdOfTextOptionsProperty = CompositionProperty.Register<ServerCompositionVisual, TextOptions>("TextOptions", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._textOptions, delegate(SimpleServerObject obj, TextOptions v)
	{
		((ServerCompositionVisual)obj)._textOptions = v;
	}, null);

	private ServerCompositionCacheMode? _cacheMode;

	internal static readonly CompositionProperty<ServerCompositionCacheMode?> s_IdOfCacheModeProperty = CompositionProperty.Register<ServerCompositionVisual, ServerCompositionCacheMode>("CacheMode", (SimpleServerObject obj) => ((ServerCompositionVisual)obj)._cacheMode, delegate(SimpleServerObject obj, ServerCompositionCacheMode? v)
	{
		((ServerCompositionVisual)obj)._cacheMode = v;
	}, null);

	public Matrix? OwnTransform => _ownTransform;

	public LtrbRect? SubTreeBounds => _subTreeBounds;

	public Matrix CombinedTransformMatrix { get; private set; } = Matrix.Identity;

	public ServerCompositionVisualCollection? Children { get; private set; }

	public ServerCompositionVisualCache? Cache { get; private set; }

	protected virtual bool HasEffect => Effect != null;

	public ServerCompositionTarget? Root
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
				OnRootChanging();
				flag = true;
			}
			SetValue(s_IdOfRootProperty, ref _root, value);
			if (flag)
			{
				OnRootChanged();
			}
		}
	}

	public ServerCompositionVisual? Parent
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
			}
			SetValue(s_IdOfParentProperty, ref _parent, value);
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
			SetAnimatedValue(s_IdOfVisibleProperty, out _visible, value);
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
			SetAnimatedValue(s_IdOfOpacityProperty, out _opacity, value);
		}
	}

	public IGeometryImpl? Clip
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
			}
			SetValue(s_IdOfClipProperty, ref _clip, value);
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
			SetAnimatedValue(s_IdOfClipToBoundsProperty, out _clipToBounds, value);
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
			SetAnimatedValue(s_IdOfOffsetProperty, out _offset, value);
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
			SetAnimatedValue(s_IdOfTranslationProperty, out _translation, value);
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
			SetAnimatedValue(s_IdOfSizeProperty, out _size, value);
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
			SetAnimatedValue(s_IdOfAnchorPointProperty, out _anchorPoint, value);
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
			SetAnimatedValue(s_IdOfCenterPointProperty, out _centerPoint, value);
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
			SetAnimatedValue(s_IdOfRotationAngleProperty, out _rotationAngle, value);
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
			SetAnimatedValue(s_IdOfOrientationProperty, out _orientation, value);
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
			SetAnimatedValue(s_IdOfScaleProperty, out _scale, value);
		}
	}

	public Matrix TransformMatrix
	{
		get
		{
			return _transformMatrix;
		}
		set
		{
			SetAnimatedValue(s_IdOfTransformMatrixProperty, out _transformMatrix, value);
		}
	}

	public ServerCompositionVisual? AdornedVisual
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
				OnAdornedVisualChanging();
				flag = true;
			}
			SetValue(s_IdOfAdornedVisualProperty, ref _adornedVisual, value);
			if (flag)
			{
				OnAdornedVisualChanged();
			}
		}
	}

	public bool AdornerIsClipped
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
			}
			SetValue(s_IdOfAdornerIsClippedProperty, ref _adornerIsClipped, value);
		}
	}

	public IBrush? OpacityMaskBrush
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
			}
			SetValue(s_IdOfOpacityMaskBrushProperty, ref _opacityMaskBrush, value);
		}
	}

	public IImmutableEffect? Effect
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
			}
			SetValue(s_IdOfEffectProperty, ref _effect, value);
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
			}
			SetValue(s_IdOfRenderOptionsProperty, ref _renderOptions, value);
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
			}
			SetValue(s_IdOfTextOptionsProperty, ref _textOptions, value);
		}
	}

	public ServerCompositionCacheMode? CacheMode
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
				OnCacheModeChanging();
				flag = true;
			}
			SetValue(s_IdOfCacheModeProperty, ref _cacheMode, value);
			if (flag)
			{
				OnCacheModeChanged();
			}
		}
	}

	private AttHelper GetAttHelper()
	{
		AttHelper attHelper = _AttHelper;
		if (attHelper == null)
		{
			AttHelper obj = new AttHelper
			{
				ParentActSubscriptionAction = AttHelper_CombinedTransformChanged,
				AdornedVisualActSubscriptionAction = AttHelper_OnAdornedVisualWorldTransformChanged
			};
			AttHelper attHelper2 = obj;
			_AttHelper = obj;
			attHelper = attHelper2;
		}
		return attHelper;
	}

	private void AttHelper_CombinedTransformChanged()
	{
		if (_AttHelper == null || _AttHelper.AncestorChainTransformSubscribers.Count == 0)
		{
			return;
		}
		foreach (Action ancestorChainTransformSubscriber in _AttHelper.AncestorChainTransformSubscribers)
		{
			ancestorChainTransformSubscriber();
		}
	}

	private void AttHelper_ParentChanging()
	{
		if (Parent != null)
		{
			AttHelper? attHelper = _AttHelper;
			if (attHelper != null && attHelper.AncestorChainTransformSubscribers.Count > 0)
			{
				Parent.AttHelper_UnsubscribeFromActNotification(_AttHelper.ParentActSubscriptionAction);
			}
		}
	}

	private void AttHelper_ParentChanged()
	{
		if (Parent != null)
		{
			AttHelper? attHelper = _AttHelper;
			if (attHelper != null && attHelper.AncestorChainTransformSubscribers.Count > 0)
			{
				Parent.AttHelper_SubscribeToActNotification(_AttHelper.ParentActSubscriptionAction);
			}
		}
		if (Parent != null && AdornedVisual != null)
		{
			AdornerHelper_EnqueueForAdornerUpdate();
		}
	}

	protected void AttHelper_SubscribeToActNotification(Action cb)
	{
		AttHelper attHelper = GetAttHelper();
		attHelper.AncestorChainTransformSubscribers.Add(cb);
		if (attHelper.AncestorChainTransformSubscribers.Count == 1)
		{
			Parent?.AttHelper_SubscribeToActNotification(attHelper.ParentActSubscriptionAction);
		}
	}

	protected void AttHelper_UnsubscribeFromActNotification(Action cb)
	{
		AttHelper attHelper = GetAttHelper();
		attHelper.AncestorChainTransformSubscribers.Remove(cb);
		if (attHelper.AncestorChainTransformSubscribers.Count == 0)
		{
			Parent?.AttHelper_UnsubscribeFromActNotification(attHelper.ParentActSubscriptionAction);
		}
	}

	protected static bool ComputeTransformFromAncestor(ServerCompositionVisual visual, ServerCompositionVisual ancestor, out Matrix transform)
	{
		transform = visual._ownTransform ?? Matrix.Identity;
		while (visual.Parent != null)
		{
			visual = visual.Parent;
			if (visual == ancestor)
			{
				return true;
			}
			if (visual._ownTransform.HasValue)
			{
				transform *= visual._ownTransform.Value;
			}
		}
		return false;
	}

	private void AttHelper_OnAdornedVisualWorldTransformChanged()
	{
		AdornerHelper_EnqueueForAdornerUpdate();
	}

	private void AdornerHelper_AttachedToRoot()
	{
		if (AdornedVisual != null)
		{
			AdornerHelper_EnqueueForAdornerUpdate();
		}
	}

	public void AdornerHelper_EnqueueForAdornerUpdate()
	{
		AttHelper attHelper = GetAttHelper();
		if (!attHelper.EnqueuedForAdornerUpdate)
		{
			base.Compositor.EnqueueAdornerUpdate(this);
			attHelper.EnqueuedForAdornerUpdate = true;
		}
	}

	private static ServerCompositionVisual? AdornerLayer_GetExpectedSharedAncestor(ServerCompositionVisual adorner)
	{
		return adorner?.Parent?.Parent;
	}

	public void UpdateAdorner()
	{
		GetAttHelper().EnqueuedForAdornerUpdate = false;
		if (AdornedVisual != null && Parent != null)
		{
			Matrix? matrix = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint, Matrix.Identity, Scale, RotationAngle, Orientation, Offset + Translation);
			ServerCompositionVisual serverCompositionVisual = AdornerLayer_GetExpectedSharedAncestor(this);
			if (serverCompositionVisual != null && ComputeTransformFromAncestor(AdornedVisual, serverCompositionVisual, out var transform))
			{
				_ownTransform = (matrix ?? Matrix.Identity) * transform;
			}
			else
			{
				_ownTransform = default(Matrix);
			}
		}
		else
		{
			_ownTransform = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint, TransformMatrix, Scale, RotationAngle, Orientation, Offset + Translation);
		}
		PropagateFlags(needsBoundingBoxUpdate: true, dirtyForRender: true);
	}

	public virtual LtrbRect? ComputeOwnContentBounds()
	{
		return null;
	}

	private void PropagateFlags(bool needsBoundingBoxUpdate, bool dirtyForRender, bool additionalDirtyRegion = false)
	{
		Root?.RequestUpdate();
		ServerCompositionVisual parent = Parent;
		bool flag = additionalDirtyRegion | dirtyForRender;
		while (parent != null && ((needsBoundingBoxUpdate && !parent._needsBoundingBoxUpdate) || (flag && !parent._isDirtyForRenderInSubgraph)))
		{
			parent._needsBoundingBoxUpdate |= needsBoundingBoxUpdate;
			parent._isDirtyForRenderInSubgraph |= flag;
			parent = parent.Parent;
		}
		_needsBoundingBoxUpdate |= needsBoundingBoxUpdate;
		_isDirtyForRender |= dirtyForRender;
		_needsToAddExtraDirtyRectToDirtyRegion = !dirtyForRender && (_needsToAddExtraDirtyRectToDirtyRegion | additionalDirtyRegion);
	}

	public void RecomputeOwnProperties()
	{
		bool needsBoundingBoxUpdate = _contentChanged || _delayPropagateNeedsBoundsUpdate;
		bool flag = _contentChanged || _delayPropagateIsDirtyForRender;
		bool delayPropagateHasExtraDirtyRects = _delayPropagateHasExtraDirtyRects;
		_delayPropagateIsDirtyForRender = (_delayPropagateHasExtraDirtyRects = (_delayPropagateIsDirtyForRender = false));
		_enqueuedForOwnPropertiesRecompute = false;
		if (_ownBoundsDirty)
		{
			_ownContentBounds = ComputeOwnContentBounds()?.NullIfZeroSize();
			flag = (needsBoundingBoxUpdate = true);
		}
		if (_clipSizeDirty)
		{
			LtrbRect? ltrbRect = null;
			if (Clip != null)
			{
				ltrbRect = new LtrbRect(Clip.Bounds);
			}
			if (ClipToBounds)
			{
				LtrbRect ltrbRect2 = new LtrbRect(0.0, 0.0, Size.X, Size.Y);
				ltrbRect = ltrbRect?.IntersectOrEmpty(ltrbRect2) ?? ltrbRect2;
			}
			if (_ownClipRect != ltrbRect)
			{
				_ownClipRect = ltrbRect;
				flag = (needsBoundingBoxUpdate = true);
			}
		}
		if (_combinedTransformDirty)
		{
			_ownTransform = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint, TransformMatrix, Scale, RotationAngle, Orientation, Offset + Translation);
			flag = (needsBoundingBoxUpdate = true);
			AttHelper_CombinedTransformChanged();
		}
		flag |= _compositionFieldsDirty;
		_ownBoundsDirty = (_clipSizeDirty = (_combinedTransformDirty = (_compositionFieldsDirty = false)));
		PropagateFlags(needsBoundingBoxUpdate, flag, delayPropagateHasExtraDirtyRects);
	}

	protected virtual void OnDetachedFromRoot(ServerCompositionTarget target)
	{
	}

	protected virtual void OnAttachedToRoot(ServerCompositionTarget target)
	{
	}

	public void OnCacheModeStateChanged()
	{
		Cache?.InvalidateProperties();
		InvalidateContent();
	}

	protected virtual void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
	{
	}

	public override void NotifyAnimatedValueChanged(CompositionProperty property)
	{
		base.NotifyAnimatedValueChanged(property);
		if (property == s_IdOfClipToBoundsProperty || property == s_IdOfOpacityProperty || property == s_IdOfSizeProperty)
		{
			TriggerCompositionFieldsDirty();
		}
		if (property == s_IdOfSizeProperty || property == s_IdOfAnchorPointProperty || property == s_IdOfCenterPointProperty || property == s_IdOfAdornedVisualProperty || property == s_IdOfTransformMatrixProperty || property == s_IdOfScaleProperty || property == s_IdOfRotationAngleProperty || property == s_IdOfOrientationProperty || property == s_IdOfOffsetProperty || property == s_IdOfTranslationProperty)
		{
			TriggerCombinedTransformDirty();
		}
		if (property == s_IdOfClipToBoundsProperty || property == s_IdOfSizeProperty)
		{
			TriggerClipSizeDirty();
		}
		if (property == s_IdOfSizeProperty)
		{
			SizeChanged();
		}
		if (property == s_IdOfVisibleProperty)
		{
			TriggerVisibleDirty();
		}
	}

	protected virtual void SizeChanged()
	{
	}

	protected void TriggerCompositionFieldsDirty()
	{
		_compositionFieldsDirty = true;
		EnqueueOwnPropertiesRecompute();
	}

	protected void TriggerCombinedTransformDirty()
	{
		_combinedTransformDirty = true;
		EnqueueOwnPropertiesRecompute();
		EnqueueForReadbackUpdate();
	}

	protected void TriggerClipSizeDirty()
	{
		EnqueueOwnPropertiesRecompute();
		_clipSizeDirty = true;
	}

	protected void TriggerVisibleDirty()
	{
		EnqueueForReadbackUpdate();
		EnqueueForOwnBoundsRecompute();
	}

	protected void AddExtraDirtyRect(LtrbRect rect)
	{
		_extraDirtyRect = (_delayPropagateHasExtraDirtyRects ? _extraDirtyRect.Union(rect) : rect);
		_delayPropagateHasExtraDirtyRects = true;
		EnqueueOwnPropertiesRecompute();
	}

	protected void EnqueueForOwnBoundsRecompute()
	{
		_ownBoundsDirty = true;
		EnqueueOwnPropertiesRecompute();
	}

	protected void InvalidateContent()
	{
		_contentChanged = true;
		EnqueueForOwnBoundsRecompute();
	}

	private void EnqueueOwnPropertiesRecompute()
	{
		if (!_enqueuedForOwnPropertiesRecompute)
		{
			_enqueuedForOwnPropertiesRecompute = true;
			base.Compositor.EnqueueVisualForOwnPropertiesUpdatePass(this);
		}
	}

	private void EnqueueForReadbackUpdate()
	{
		if (!_enqueuedForReadbackUpdate)
		{
			_enqueuedForReadbackUpdate = true;
			base.Compositor.EnqueueVisualForReadbackUpdatePass(this);
		}
	}

	public ReadbackData? GetReadback(ulong readerRevision)
	{
		ulong num = Interlocked.Read(in _readback0.Revision);
		ulong num2 = Interlocked.Read(in _readback1.Revision);
		if (num <= readerRevision && num2 <= readerRevision)
		{
			if (num2 <= num)
			{
				return _readback0;
			}
			return _readback1;
		}
		if (num <= readerRevision)
		{
			return _readback0;
		}
		if (num2 <= readerRevision)
		{
			return _readback1;
		}
		return null;
	}

	public void UpdateReadback(ulong writerRevision, ulong readerRevision)
	{
		_enqueuedForReadbackUpdate = false;
		ReadbackData readbackData = ((_readback0.Revision > readerRevision) ? _readback0 : ((_readback1.Revision <= readerRevision) ? ((_readback0.Revision < _readback1.Revision) ? _readback0 : _readback1) : _readback1));
		Interlocked.Exchange(ref readbackData.Revision, writerRevision);
		readbackData.Matrix = _ownTransform ?? Matrix.Identity;
		readbackData.TargetId = Root?.Id ?? (-1);
		readbackData.TransformedSubtreeBounds = _transformedSubTreeBounds;
		readbackData.Visible = Visible;
	}

	protected virtual void PushClipToBounds(IDrawingContextImpl canvas)
	{
		canvas.PushClip(new Rect(0.0, 0.0, Size.X, Size.Y));
	}

	public (int visited, int rendered) Render(IDrawingContextImpl canvas, LtrbRect clip, IDirtyRectTracker? dirtyRects, bool renderChildren = true, bool skipRootVisualTransform = false, bool renderingToBitmapCache = false)
	{
		RenderContext visitor = new RenderContext(this, canvas, dirtyRects, base.Compositor.Pools, canvas.Transform, clip, renderChildren, skipRootVisualTransform, renderingToBitmapCache);
		try
		{
			ServerTreeWalker<RenderContext>.Walk(ref visitor, this);
			return (visited: visitor.VisitedVisuals, rendered: visitor.RenderedVisuals);
		}
		finally
		{
			visitor.Dispose();
		}
	}

	public void UpdateRoot(IDirtyRectCollector tracker, Matrix transform, LtrbRect clip)
	{
		UpdateContext visitor = new UpdateContext(base.Compositor.Pools, tracker, transform, clip);
		ServerTreeWalker<UpdateContext>.Walk(ref visitor, this);
		visitor.Dispose();
	}

	internal ServerCompositionVisual(ServerCompositor compositor)
		: base(compositor)
	{
		Initialize();
	}

	private void Initialize()
	{
		Children = new ServerCompositionVisualCollection(base.Compositor);
	}

	private void OnRootChanged()
	{
		if (Root != null)
		{
			Root.AddVisual(this);
			OnAttachedToRoot(Root);
			AdornerHelper_AttachedToRoot();
		}
		Cache?.FreeResources();
	}

	private void OnRootChanging()
	{
		if (Root != null)
		{
			Root.RemoveVisual(this);
			OnDetachedFromRoot(Root);
		}
	}

	private void OnParentChanged()
	{
		if (Parent != null)
		{
			_delayPropagateNeedsBoundsUpdate = (_delayPropagateIsDirtyForRender = true);
			EnqueueOwnPropertiesRecompute();
		}
		AttHelper_ParentChanged();
	}

	private void OnParentChanging()
	{
		if (Parent != null && _transformedSubTreeBounds.HasValue)
		{
			Parent.AddExtraDirtyRect(_transformedSubTreeBounds.Value);
		}
		AttHelper_ParentChanging();
	}

	private void OnAdornedVisualChanged()
	{
		AdornedVisual?.AttHelper_SubscribeToActNotification(GetAttHelper().AdornedVisualActSubscriptionAction);
		AdornerHelper_EnqueueForAdornerUpdate();
	}

	private void OnAdornedVisualChanging()
	{
		AdornedVisual?.AttHelper_UnsubscribeFromActNotification(GetAttHelper().AdornedVisualActSubscriptionAction);
	}

	private void OnCacheModeChanged()
	{
		Cache = ((CacheMode is ServerCompositionBitmapCache cacheMode) ? new ServerCompositionVisualCache(this, cacheMode) : null);
		CacheMode?.Subscribe(this);
		OnCacheModeStateChanged();
	}

	private void OnCacheModeChanging()
	{
		CacheMode?.Unsubscribe(this);
		Cache?.FreeResources();
		Cache = null;
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionVisualChangedFields compositionVisualChangedFields = reader.Read<CompositionVisualChangedFields>();
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.Root) == CompositionVisualChangedFields.Root)
		{
			Root = reader.ReadObject<ServerCompositionTarget>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.Parent) == CompositionVisualChangedFields.Parent)
		{
			Parent = reader.ReadObject<ServerCompositionVisual>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.VisibleAnimated) == CompositionVisualChangedFields.VisibleAnimated)
		{
			SetAnimatedValue(s_IdOfVisibleProperty, ref _visible, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.Visible) == CompositionVisualChangedFields.Visible)
		{
			Visible = reader.Read<bool>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.OpacityAnimated) == CompositionVisualChangedFields.OpacityAnimated)
		{
			SetAnimatedValue(s_IdOfOpacityProperty, ref _opacity, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.Opacity) == CompositionVisualChangedFields.Opacity)
		{
			Opacity = reader.Read<float>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.Clip) == CompositionVisualChangedFields.Clip)
		{
			Clip = reader.ReadObject<IGeometryImpl>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.ClipToBoundsAnimated) == CompositionVisualChangedFields.ClipToBoundsAnimated)
		{
			SetAnimatedValue(s_IdOfClipToBoundsProperty, ref _clipToBounds, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.ClipToBounds) == CompositionVisualChangedFields.ClipToBounds)
		{
			ClipToBounds = reader.Read<bool>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.OffsetAnimated) == CompositionVisualChangedFields.OffsetAnimated)
		{
			SetAnimatedValue(s_IdOfOffsetProperty, ref _offset, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.Offset) == CompositionVisualChangedFields.Offset)
		{
			Offset = reader.Read<Vector3D>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.TranslationAnimated) == CompositionVisualChangedFields.TranslationAnimated)
		{
			SetAnimatedValue(s_IdOfTranslationProperty, ref _translation, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.Translation) == CompositionVisualChangedFields.Translation)
		{
			Translation = reader.Read<Vector3D>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.SizeAnimated) == CompositionVisualChangedFields.SizeAnimated)
		{
			SetAnimatedValue(s_IdOfSizeProperty, ref _size, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.Size) == CompositionVisualChangedFields.Size)
		{
			Size = reader.Read<Vector>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.AnchorPointAnimated) == CompositionVisualChangedFields.AnchorPointAnimated)
		{
			SetAnimatedValue(s_IdOfAnchorPointProperty, ref _anchorPoint, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.AnchorPoint) == CompositionVisualChangedFields.AnchorPoint)
		{
			AnchorPoint = reader.Read<Vector>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.CenterPointAnimated) == CompositionVisualChangedFields.CenterPointAnimated)
		{
			SetAnimatedValue(s_IdOfCenterPointProperty, ref _centerPoint, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.CenterPoint) == CompositionVisualChangedFields.CenterPoint)
		{
			CenterPoint = reader.Read<Vector3D>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.RotationAngleAnimated) == CompositionVisualChangedFields.RotationAngleAnimated)
		{
			SetAnimatedValue(s_IdOfRotationAngleProperty, ref _rotationAngle, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.RotationAngle) == CompositionVisualChangedFields.RotationAngle)
		{
			RotationAngle = reader.Read<float>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.OrientationAnimated) == CompositionVisualChangedFields.OrientationAnimated)
		{
			SetAnimatedValue(s_IdOfOrientationProperty, ref _orientation, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.Orientation) == CompositionVisualChangedFields.Orientation)
		{
			Orientation = reader.Read<Quaternion>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.ScaleAnimated) == CompositionVisualChangedFields.ScaleAnimated)
		{
			SetAnimatedValue(s_IdOfScaleProperty, ref _scale, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.Scale) == CompositionVisualChangedFields.Scale)
		{
			Scale = reader.Read<Vector3D>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.TransformMatrixAnimated) == CompositionVisualChangedFields.TransformMatrixAnimated)
		{
			SetAnimatedValue(s_IdOfTransformMatrixProperty, ref _transformMatrix, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionVisualChangedFields & CompositionVisualChangedFields.TransformMatrix) == CompositionVisualChangedFields.TransformMatrix)
		{
			TransformMatrix = reader.Read<Matrix>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.AdornedVisual) == CompositionVisualChangedFields.AdornedVisual)
		{
			AdornedVisual = reader.ReadObject<ServerCompositionVisual>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.AdornerIsClipped) == CompositionVisualChangedFields.AdornerIsClipped)
		{
			AdornerIsClipped = reader.Read<bool>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.OpacityMaskBrush) == CompositionVisualChangedFields.OpacityMaskBrush)
		{
			OpacityMaskBrush = reader.ReadObject<IBrush>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.Effect) == CompositionVisualChangedFields.Effect)
		{
			Effect = reader.ReadObject<IImmutableEffect>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.RenderOptions) == CompositionVisualChangedFields.RenderOptions)
		{
			RenderOptions = reader.Read<RenderOptions>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.TextOptions) == CompositionVisualChangedFields.TextOptions)
		{
			TextOptions = reader.Read<TextOptions>();
		}
		if ((compositionVisualChangedFields & CompositionVisualChangedFields.CacheMode) == CompositionVisualChangedFields.CacheMode)
		{
			CacheMode = reader.ReadObject<ServerCompositionCacheMode>();
		}
		OnFieldsDeserialized(compositionVisualChangedFields);
	}

	private void OnFieldsDeserialized(CompositionVisualChangedFields changed)
	{
		if ((changed & (CompositionVisualChangedFields.Opacity | CompositionVisualChangedFields.OpacityAnimated | CompositionVisualChangedFields.Clip | CompositionVisualChangedFields.ClipToBounds | CompositionVisualChangedFields.ClipToBoundsAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.OpacityMaskBrush | CompositionVisualChangedFields.Effect | CompositionVisualChangedFields.RenderOptions)) != (CompositionVisualChangedFields)0uL)
		{
			TriggerCompositionFieldsDirty();
		}
		if ((changed & (CompositionVisualChangedFields.Offset | CompositionVisualChangedFields.OffsetAnimated | CompositionVisualChangedFields.Translation | CompositionVisualChangedFields.TranslationAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.AnchorPoint | CompositionVisualChangedFields.AnchorPointAnimated | CompositionVisualChangedFields.CenterPoint | CompositionVisualChangedFields.CenterPointAnimated | CompositionVisualChangedFields.RotationAngle | CompositionVisualChangedFields.RotationAngleAnimated | CompositionVisualChangedFields.Orientation | CompositionVisualChangedFields.OrientationAnimated | CompositionVisualChangedFields.Scale | CompositionVisualChangedFields.ScaleAnimated | CompositionVisualChangedFields.TransformMatrix | CompositionVisualChangedFields.AdornedVisual)) != (CompositionVisualChangedFields)0uL)
		{
			TriggerCombinedTransformDirty();
		}
		if ((changed & (CompositionVisualChangedFields.Clip | CompositionVisualChangedFields.ClipToBounds | CompositionVisualChangedFields.ClipToBoundsAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated)) != (CompositionVisualChangedFields)0uL)
		{
			TriggerClipSizeDirty();
		}
		if ((changed & (CompositionVisualChangedFields.Clip | CompositionVisualChangedFields.ClipToBounds | CompositionVisualChangedFields.ClipToBoundsAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.Effect)) != (CompositionVisualChangedFields)0uL)
		{
			_ownBoundsDirty = true;
			EnqueueOwnPropertiesRecompute();
		}
		if ((changed & (CompositionVisualChangedFields.Root | CompositionVisualChangedFields.Visible | CompositionVisualChangedFields.VisibleAnimated | CompositionVisualChangedFields.Offset | CompositionVisualChangedFields.OffsetAnimated | CompositionVisualChangedFields.Translation | CompositionVisualChangedFields.TranslationAnimated | CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.AnchorPoint | CompositionVisualChangedFields.AnchorPointAnimated | CompositionVisualChangedFields.CenterPoint | CompositionVisualChangedFields.CenterPointAnimated | CompositionVisualChangedFields.RotationAngle | CompositionVisualChangedFields.RotationAngleAnimated | CompositionVisualChangedFields.Orientation | CompositionVisualChangedFields.OrientationAnimated | CompositionVisualChangedFields.Scale | CompositionVisualChangedFields.ScaleAnimated | CompositionVisualChangedFields.TransformMatrix | CompositionVisualChangedFields.AdornedVisual)) != (CompositionVisualChangedFields)0uL)
		{
			EnqueueForReadbackUpdate();
		}
		if ((changed & (CompositionVisualChangedFields.Visible | CompositionVisualChangedFields.VisibleAnimated)) != (CompositionVisualChangedFields)0uL)
		{
			TriggerVisibleDirty();
		}
		if ((changed & (CompositionVisualChangedFields.Size | CompositionVisualChangedFields.SizeAnimated)) != (CompositionVisualChangedFields)0uL)
		{
			SizeChanged();
		}
	}

	public override CompositionProperty? GetCompositionProperty(string name)
	{
		return name switch
		{
			"Visible" => s_IdOfVisibleProperty, 
			"Opacity" => s_IdOfOpacityProperty, 
			"ClipToBounds" => s_IdOfClipToBoundsProperty, 
			"Offset" => s_IdOfOffsetProperty, 
			"Translation" => s_IdOfTranslationProperty, 
			"Size" => s_IdOfSizeProperty, 
			"AnchorPoint" => s_IdOfAnchorPointProperty, 
			"CenterPoint" => s_IdOfCenterPointProperty, 
			"RotationAngle" => s_IdOfRotationAngleProperty, 
			"Orientation" => s_IdOfOrientationProperty, 
			"Scale" => s_IdOfScaleProperty, 
			"AdornerIsClipped" => s_IdOfAdornerIsClippedProperty, 
			_ => base.GetCompositionProperty(name), 
		};
	}
}
