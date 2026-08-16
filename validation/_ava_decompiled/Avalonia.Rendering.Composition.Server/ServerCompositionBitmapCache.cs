using System;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionBitmapCache : ServerCompositionCacheMode
{
	private double _renderAtScale;

	internal static readonly CompositionProperty<double> s_IdOfRenderAtScaleProperty = CompositionProperty.Register<ServerCompositionBitmapCache, double>("RenderAtScale", (SimpleServerObject obj) => ((ServerCompositionBitmapCache)obj)._renderAtScale, delegate(SimpleServerObject obj, double v)
	{
		((ServerCompositionBitmapCache)obj)._renderAtScale = v;
	}, (SimpleServerObject obj) => ((ServerCompositionBitmapCache)obj)._renderAtScale);

	private bool _snapsToDevicePixels;

	internal static readonly CompositionProperty<bool> s_IdOfSnapsToDevicePixelsProperty = CompositionProperty.Register<ServerCompositionBitmapCache, bool>("SnapsToDevicePixels", (SimpleServerObject obj) => ((ServerCompositionBitmapCache)obj)._snapsToDevicePixels, delegate(SimpleServerObject obj, bool v)
	{
		((ServerCompositionBitmapCache)obj)._snapsToDevicePixels = v;
	}, (SimpleServerObject obj) => ((ServerCompositionBitmapCache)obj)._snapsToDevicePixels);

	private bool _enableClearType;

	internal static readonly CompositionProperty<bool> s_IdOfEnableClearTypeProperty = CompositionProperty.Register<ServerCompositionBitmapCache, bool>("EnableClearType", (SimpleServerObject obj) => ((ServerCompositionBitmapCache)obj)._enableClearType, delegate(SimpleServerObject obj, bool v)
	{
		((ServerCompositionBitmapCache)obj)._enableClearType = v;
	}, (SimpleServerObject obj) => ((ServerCompositionBitmapCache)obj)._enableClearType);

	public double RenderAtScale
	{
		get
		{
			return _renderAtScale;
		}
		set
		{
			SetAnimatedValue(s_IdOfRenderAtScaleProperty, out _renderAtScale, value);
		}
	}

	public bool SnapsToDevicePixels
	{
		get
		{
			return _snapsToDevicePixels;
		}
		set
		{
			SetAnimatedValue(s_IdOfSnapsToDevicePixelsProperty, out _snapsToDevicePixels, value);
		}
	}

	public bool EnableClearType
	{
		get
		{
			return _enableClearType;
		}
		set
		{
			SetAnimatedValue(s_IdOfEnableClearTypeProperty, out _enableClearType, value);
		}
	}

	internal ServerCompositionBitmapCache(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionBitmapCacheChangedFields compositionBitmapCacheChangedFields = reader.Read<CompositionBitmapCacheChangedFields>();
		if ((compositionBitmapCacheChangedFields & CompositionBitmapCacheChangedFields.RenderAtScaleAnimated) == CompositionBitmapCacheChangedFields.RenderAtScaleAnimated)
		{
			SetAnimatedValue(s_IdOfRenderAtScaleProperty, ref _renderAtScale, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionBitmapCacheChangedFields & CompositionBitmapCacheChangedFields.RenderAtScale) == CompositionBitmapCacheChangedFields.RenderAtScale)
		{
			RenderAtScale = reader.Read<double>();
		}
		if ((compositionBitmapCacheChangedFields & CompositionBitmapCacheChangedFields.SnapsToDevicePixelsAnimated) == CompositionBitmapCacheChangedFields.SnapsToDevicePixelsAnimated)
		{
			SetAnimatedValue(s_IdOfSnapsToDevicePixelsProperty, ref _snapsToDevicePixels, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionBitmapCacheChangedFields & CompositionBitmapCacheChangedFields.SnapsToDevicePixels) == CompositionBitmapCacheChangedFields.SnapsToDevicePixels)
		{
			SnapsToDevicePixels = reader.Read<bool>();
		}
		if ((compositionBitmapCacheChangedFields & CompositionBitmapCacheChangedFields.EnableClearTypeAnimated) == CompositionBitmapCacheChangedFields.EnableClearTypeAnimated)
		{
			SetAnimatedValue(s_IdOfEnableClearTypeProperty, ref _enableClearType, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionBitmapCacheChangedFields & CompositionBitmapCacheChangedFields.EnableClearType) == CompositionBitmapCacheChangedFields.EnableClearType)
		{
			EnableClearType = reader.Read<bool>();
		}
	}

	public override CompositionProperty? GetCompositionProperty(string name)
	{
		return name switch
		{
			"RenderAtScale" => s_IdOfRenderAtScaleProperty, 
			"SnapsToDevicePixels" => s_IdOfSnapsToDevicePixelsProperty, 
			"EnableClearType" => s_IdOfEnableClearTypeProperty, 
			_ => base.GetCompositionProperty(name), 
		};
	}
}
