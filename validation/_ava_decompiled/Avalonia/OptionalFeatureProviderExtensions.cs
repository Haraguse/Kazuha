using System.Diagnostics.CodeAnalysis;

namespace Avalonia;

public static class OptionalFeatureProviderExtensions
{
	/// <inheritdoc cref="M:Avalonia.IOptionalFeatureProvider.TryGetFeature(System.Type)" />
	public static T? TryGetFeature<T>(this IOptionalFeatureProvider provider) where T : class
	{
		return (T)provider.TryGetFeature(typeof(T));
	}

	/// <inheritdoc cref="M:Avalonia.IOptionalFeatureProvider.TryGetFeature(System.Type)" />
	public static bool TryGetFeature<T>(this IOptionalFeatureProvider provider, [MaybeNullWhen(false)] out T rv) where T : class
	{
		rv = provider.TryGetFeature<T>();
		return rv != null;
	}
}
