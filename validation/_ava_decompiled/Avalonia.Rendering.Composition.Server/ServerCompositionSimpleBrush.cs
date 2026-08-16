using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSimpleBrush : SimpleServerRenderResource, IBrush
{
	private double _opacity;

	internal static readonly CompositionProperty<double> s_IdOfOpacityProperty = CompositionProperty.Register<ServerCompositionSimpleBrush, double>("Opacity", (SimpleServerObject obj) => ((ServerCompositionSimpleBrush)obj)._opacity, delegate(SimpleServerObject obj, double v)
	{
		((ServerCompositionSimpleBrush)obj)._opacity = v;
	}, (SimpleServerObject obj) => ((ServerCompositionSimpleBrush)obj)._opacity);

	private RelativePoint _transformOrigin;

	internal static readonly CompositionProperty<RelativePoint> s_IdOfTransformOriginProperty = CompositionProperty.Register<ServerCompositionSimpleBrush, RelativePoint>("TransformOrigin", (SimpleServerObject obj) => ((ServerCompositionSimpleBrush)obj)._transformOrigin, delegate(SimpleServerObject obj, RelativePoint v)
	{
		((ServerCompositionSimpleBrush)obj)._transformOrigin = v;
	}, null);

	private ITransform? _transform;

	internal static readonly CompositionProperty<ITransform?> s_IdOfTransformProperty = CompositionProperty.Register<ServerCompositionSimpleBrush, ITransform>("Transform", (SimpleServerObject obj) => ((ServerCompositionSimpleBrush)obj)._transform, delegate(SimpleServerObject obj, ITransform? v)
	{
		((ServerCompositionSimpleBrush)obj)._transform = v;
	}, null);

	ITransform? IBrush.Transform => Transform;

	public double Opacity
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
			}
			SetValue(s_IdOfOpacityProperty, ref _opacity, value);
		}
	}

	public RelativePoint TransformOrigin
	{
		get
		{
			return _transformOrigin;
		}
		set
		{
			bool flag = false;
			if (_transformOrigin != value)
			{
				flag = true;
			}
			SetValue(s_IdOfTransformOriginProperty, ref _transformOrigin, value);
		}
	}

	public ITransform? Transform
	{
		get
		{
			return _transform;
		}
		set
		{
			bool flag = false;
			if (_transform != value)
			{
				flag = true;
			}
			SetValue(s_IdOfTransformProperty, ref _transform, value);
		}
	}

	internal ServerCompositionSimpleBrush(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionSimpleBrushChangedFields num = reader.Read<CompositionSimpleBrushChangedFields>();
		if ((num & CompositionSimpleBrushChangedFields.Opacity) == CompositionSimpleBrushChangedFields.Opacity)
		{
			Opacity = reader.Read<double>();
		}
		if ((num & CompositionSimpleBrushChangedFields.TransformOrigin) == CompositionSimpleBrushChangedFields.TransformOrigin)
		{
			TransformOrigin = reader.Read<RelativePoint>();
		}
		if ((num & CompositionSimpleBrushChangedFields.Transform) == CompositionSimpleBrushChangedFields.Transform)
		{
			Transform = reader.ReadObject<ITransform>();
		}
	}

	internal static void SerializeAllChanges(BatchStreamWriter writer, double opacity, RelativePoint transformOrigin, ITransform? transform)
	{
		writer.Write(CompositionSimpleBrushChangedFields.Opacity | CompositionSimpleBrushChangedFields.TransformOrigin | CompositionSimpleBrushChangedFields.Transform);
		writer.Write(opacity);
		writer.Write(transformOrigin);
		writer.WriteObject(transform);
	}
}
