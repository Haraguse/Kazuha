using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionExperimentalAcrylicVisual : ServerCompositionDrawListVisual
{
	private ImmutableExperimentalAcrylicMaterial _material;

	internal static readonly CompositionProperty<ImmutableExperimentalAcrylicMaterial> s_IdOfMaterialProperty = CompositionProperty.Register<ServerCompositionExperimentalAcrylicVisual, ImmutableExperimentalAcrylicMaterial>("Material", (SimpleServerObject obj) => ((ServerCompositionExperimentalAcrylicVisual)obj)._material, delegate(SimpleServerObject obj, ImmutableExperimentalAcrylicMaterial v)
	{
		((ServerCompositionExperimentalAcrylicVisual)obj)._material = v;
	}, null);

	private CornerRadius _cornerRadius;

	internal static readonly CompositionProperty<CornerRadius> s_IdOfCornerRadiusProperty = CompositionProperty.Register<ServerCompositionExperimentalAcrylicVisual, CornerRadius>("CornerRadius", (SimpleServerObject obj) => ((ServerCompositionExperimentalAcrylicVisual)obj)._cornerRadius, delegate(SimpleServerObject obj, CornerRadius v)
	{
		((ServerCompositionExperimentalAcrylicVisual)obj)._cornerRadius = v;
	}, null);

	public ImmutableExperimentalAcrylicMaterial Material
	{
		get
		{
			return _material;
		}
		set
		{
			bool flag = false;
			if (_material != value)
			{
				flag = true;
			}
			SetValue(s_IdOfMaterialProperty, ref _material, value);
		}
	}

	public CornerRadius CornerRadius
	{
		get
		{
			return _cornerRadius;
		}
		set
		{
			bool flag = false;
			if (_cornerRadius != value)
			{
				flag = true;
			}
			SetValue(s_IdOfCornerRadiusProperty, ref _cornerRadius, value);
		}
	}

	protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
	{
		CornerRadius cornerRadius = CornerRadius;
		if (context.Canvas is IDrawingContextWithAcrylicLikeSupport drawingContextWithAcrylicLikeSupport)
		{
			drawingContextWithAcrylicLikeSupport.DrawRectangle(Material, new RoundedRect(new Rect(0.0, 0.0, base.Size.X, base.Size.Y), cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight, cornerRadius.BottomLeft));
		}
		base.RenderCore(context, currentTransformedClip);
	}

	public override LtrbRect? ComputeOwnContentBounds()
	{
		return LtrbRect.FullUnion(base.ComputeOwnContentBounds(), new LtrbRect(0.0, 0.0, base.Size.X, base.Size.Y));
	}

	protected override void SizeChanged()
	{
		EnqueueForOwnBoundsRecompute();
		base.SizeChanged();
	}

	public ServerCompositionExperimentalAcrylicVisual(ServerCompositor compositor, Visual v)
		: base(compositor, v)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionExperimentalAcrylicVisualChangedFields num = reader.Read<CompositionExperimentalAcrylicVisualChangedFields>();
		if ((num & CompositionExperimentalAcrylicVisualChangedFields.Material) == CompositionExperimentalAcrylicVisualChangedFields.Material)
		{
			Material = reader.Read<ImmutableExperimentalAcrylicMaterial>();
		}
		if ((num & CompositionExperimentalAcrylicVisualChangedFields.CornerRadius) == CompositionExperimentalAcrylicVisualChangedFields.CornerRadius)
		{
			CornerRadius = reader.Read<CornerRadius>();
		}
	}
}
