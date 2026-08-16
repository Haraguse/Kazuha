using System;
using Avalonia.Media.Immutable;

namespace Avalonia.Media;

/// <summary>
/// Extension methods for transform classes.
/// </summary>
public static class TransformExtensions
{
	/// <summary>
	/// Converts a transform to an immutable transform.
	/// </summary>
	/// <param name="transform">The transform.</param>
	/// <returns>
	/// The result of calling <see cref="M:Avalonia.Media.Transform.ToImmutable" /> if the transform is mutable,
	/// otherwise <paramref name="transform" />.
	/// </returns>
	public static ImmutableTransform ToImmutable(this ITransform transform)
	{
		if (transform == null)
		{
			throw new ArgumentNullException("transform");
		}
		return (transform as Transform)?.ToImmutable() ?? new ImmutableTransform(transform.Value);
	}
}
