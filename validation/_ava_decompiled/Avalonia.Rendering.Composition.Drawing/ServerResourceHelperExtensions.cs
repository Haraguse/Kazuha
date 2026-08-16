using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Drawing;

internal static class ServerResourceHelperExtensions
{
	public static IBrush? GetServer(this IBrush? brush, Compositor? compositor)
	{
		if (compositor == null)
		{
			return brush;
		}
		if (brush == null)
		{
			return null;
		}
		if (brush is IImmutableBrush result)
		{
			return result;
		}
		if (brush is ICompositionRenderResource<IBrush> compositionRenderResource)
		{
			return compositionRenderResource.GetForCompositor(compositor);
		}
		ThrowNotCompatible(brush);
		return null;
	}

	public static IPen? GetServer(this IPen? pen, Compositor? compositor)
	{
		if (compositor == null)
		{
			return pen;
		}
		if (pen == null)
		{
			return null;
		}
		if (pen is ImmutablePen result)
		{
			return result;
		}
		if (pen is ICompositionRenderResource<IPen> compositionRenderResource)
		{
			return compositionRenderResource.GetForCompositor(compositor);
		}
		ThrowNotCompatible(pen);
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	private static void ThrowNotCompatible(object o)
	{
		throw new InvalidOperationException(o.GetType()?.ToString() + " is not compatible with composition");
	}

	public static ITransform? GetServer(this ITransform? transform, Compositor? compositor)
	{
		if (compositor == null)
		{
			return transform;
		}
		if (transform == null)
		{
			return null;
		}
		if (transform is ImmutableTransform result)
		{
			return result;
		}
		if (transform is ICompositionRenderResource<ITransform> compositionRenderResource)
		{
			compositionRenderResource.GetForCompositor(compositor);
		}
		return new ImmutableTransform(transform.Value);
	}

	public static IRenderDataGeometry? GetServer(this Geometry? geometry, Compositor? compositor)
	{
		if (geometry == null)
		{
			return null;
		}
		if (compositor == null)
		{
			return geometry.PlatformImpl;
		}
		if (geometry != null)
		{
			return ((ICompositionRenderResource<ServerCompositionSimpleGeometry>)geometry).GetForCompositor(compositor);
		}
		ThrowNotCompatible(geometry);
		return null;
	}
}
