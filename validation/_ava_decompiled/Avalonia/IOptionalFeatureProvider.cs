using System;

namespace Avalonia;

public interface IOptionalFeatureProvider
{
	/// <summary>
	/// Queries for an optional feature.
	/// </summary>
	/// <param name="featureType">Feature type.</param>
	object? TryGetFeature(Type featureType);
}
