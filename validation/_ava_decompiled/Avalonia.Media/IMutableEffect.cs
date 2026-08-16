namespace Avalonia.Media;

public interface IMutableEffect : IEffect
{
	/// <summary>
	/// Creates an immutable clone of the effect.
	/// </summary>
	/// <returns>The immutable clone.</returns>
	internal IImmutableEffect ToImmutable();
}
