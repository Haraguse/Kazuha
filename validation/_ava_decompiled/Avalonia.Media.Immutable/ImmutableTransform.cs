namespace Avalonia.Media.Immutable;

/// <summary>
/// Represents a transform on an <see cref="T:Avalonia.Visual" />.
/// </summary>
public class ImmutableTransform : ITransform
{
	public Matrix Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Immutable.ImmutableTransform" /> class.
	/// </summary>
	/// <param name="matrix">The transform matrix.</param>
	public ImmutableTransform(Matrix matrix)
	{
		Value = matrix;
	}
}
