namespace Avalonia.Platform;

public static class DrawingContextImplExtensions
{
	/// <summary>
	/// Attempts to get an optional feature from the drawing context implementation.
	/// </summary>
	public static T? GetFeature<T>(this IDrawingContextImpl context) where T : class
	{
		return (T)context.GetFeature(typeof(T));
	}
}
