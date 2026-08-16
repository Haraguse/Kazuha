using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

internal class CompositionBitmapCache : CompositionCacheMode
{
	private CompositionBitmapCacheChangedFields _changedFieldsOfCompositionBitmapCache;

	private double _renderAtScale;

	private bool _snapsToDevicePixels;

	private bool _enableClearType;

	internal new ServerCompositionBitmapCache Server { get; }

	public double RenderAtScale
	{
		get
		{
			return _renderAtScale;
		}
		set
		{
			bool flag = false;
			if (_renderAtScale != value)
			{
				flag = true;
				_renderAtScale = value;
				_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.RenderAtScale;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionBitmapCache.s_IdOfRenderAtScaleProperty);
				_changedFieldsOfCompositionBitmapCache &= ~CompositionBitmapCacheChangedFields.RenderAtScaleAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("RenderAtScale", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.RenderAtScaleAnimated;
						PendingAnimations[ServerCompositionBitmapCache.s_IdOfRenderAtScaleProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "RenderAtScale", value);
				}
			}
			_renderAtScale = value;
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
			bool flag = false;
			if (_snapsToDevicePixels != value)
			{
				flag = true;
				_snapsToDevicePixels = value;
				_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.SnapsToDevicePixels;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionBitmapCache.s_IdOfSnapsToDevicePixelsProperty);
				_changedFieldsOfCompositionBitmapCache &= ~CompositionBitmapCacheChangedFields.SnapsToDevicePixelsAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("SnapsToDevicePixels", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.SnapsToDevicePixelsAnimated;
						PendingAnimations[ServerCompositionBitmapCache.s_IdOfSnapsToDevicePixelsProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "SnapsToDevicePixels", value);
				}
			}
			_snapsToDevicePixels = value;
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
			bool flag = false;
			if (_enableClearType != value)
			{
				flag = true;
				_enableClearType = value;
				_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.EnableClearType;
				RegisterForSerialization();
				PendingAnimations.Remove(ServerCompositionBitmapCache.s_IdOfEnableClearTypeProperty);
				_changedFieldsOfCompositionBitmapCache &= ~CompositionBitmapCacheChangedFields.EnableClearTypeAnimated;
				if (base.ImplicitAnimations != null && base.ImplicitAnimations.TryGetValue("EnableClearType", out ICompositionAnimationBase value2))
				{
					if (value2 is CompositionAnimation compositionAnimation)
					{
						_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.EnableClearTypeAnimated;
						PendingAnimations[ServerCompositionBitmapCache.s_IdOfEnableClearTypeProperty] = compositionAnimation.CreateInstance(Server, value);
					}
					StartAnimationGroup(value2, "EnableClearType", value);
				}
			}
			_enableClearType = value;
		}
	}

	internal CompositionBitmapCache(Compositor compositor, ServerCompositionBitmapCache server)
		: base(compositor, server)
	{
		Server = server;
		InitializeDefaults();
	}

	private void InitializeDefaults()
	{
		RenderAtScale = 1.0;
		SnapsToDevicePixels = false;
		EnableClearType = false;
	}

	private protected override void SerializeChangesCore(BatchStreamWriter writer)
	{
		base.SerializeChangesCore(writer);
		writer.Write(_changedFieldsOfCompositionBitmapCache);
		if ((_changedFieldsOfCompositionBitmapCache & CompositionBitmapCacheChangedFields.RenderAtScaleAnimated) == CompositionBitmapCacheChangedFields.RenderAtScaleAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionBitmapCache.s_IdOfRenderAtScaleProperty));
		}
		else if ((_changedFieldsOfCompositionBitmapCache & CompositionBitmapCacheChangedFields.RenderAtScale) == CompositionBitmapCacheChangedFields.RenderAtScale)
		{
			writer.Write(_renderAtScale);
		}
		if ((_changedFieldsOfCompositionBitmapCache & CompositionBitmapCacheChangedFields.SnapsToDevicePixelsAnimated) == CompositionBitmapCacheChangedFields.SnapsToDevicePixelsAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionBitmapCache.s_IdOfSnapsToDevicePixelsProperty));
		}
		else if ((_changedFieldsOfCompositionBitmapCache & CompositionBitmapCacheChangedFields.SnapsToDevicePixels) == CompositionBitmapCacheChangedFields.SnapsToDevicePixels)
		{
			writer.Write(_snapsToDevicePixels);
		}
		if ((_changedFieldsOfCompositionBitmapCache & CompositionBitmapCacheChangedFields.EnableClearTypeAnimated) == CompositionBitmapCacheChangedFields.EnableClearTypeAnimated)
		{
			writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionBitmapCache.s_IdOfEnableClearTypeProperty));
		}
		else if ((_changedFieldsOfCompositionBitmapCache & CompositionBitmapCacheChangedFields.EnableClearType) == CompositionBitmapCacheChangedFields.EnableClearType)
		{
			writer.Write(_enableClearType);
		}
		_changedFieldsOfCompositionBitmapCache = (CompositionBitmapCacheChangedFields)0;
	}

	internal override void StartAnimation(string propertyName, CompositionAnimation animation, ExpressionVariant? finalValue)
	{
		switch (propertyName)
		{
		case "RenderAtScale":
		{
			_ = _renderAtScale;
			IAnimationInstance value3 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionBitmapCache.s_IdOfRenderAtScaleProperty] = value3;
			_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.RenderAtScaleAnimated;
			RegisterForSerialization();
			break;
		}
		case "SnapsToDevicePixels":
		{
			_ = _snapsToDevicePixels;
			IAnimationInstance value2 = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionBitmapCache.s_IdOfSnapsToDevicePixelsProperty] = value2;
			_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.SnapsToDevicePixelsAnimated;
			RegisterForSerialization();
			break;
		}
		case "EnableClearType":
		{
			_ = _enableClearType;
			IAnimationInstance value = animation.CreateInstance(Server, finalValue);
			PendingAnimations[ServerCompositionBitmapCache.s_IdOfEnableClearTypeProperty] = value;
			_changedFieldsOfCompositionBitmapCache |= CompositionBitmapCacheChangedFields.EnableClearTypeAnimated;
			RegisterForSerialization();
			break;
		}
		default:
			base.StartAnimation(propertyName, animation, finalValue);
			break;
		}
	}
}
