namespace Avalonia.Media.TextFormatting;

public readonly struct SplitResult<T>(T? first, T? second)
{
	/// <summary>
	/// Gets the first part.
	/// </summary>
	/// <value>
	/// The first part.
	/// </value>
	public T? First { get; } = first;

	/// <summary>
	/// Gets the second part.
	/// </summary>
	/// <value>
	/// The second part.
	/// </value>
	public T? Second { get; } = second;

	/// <summary>
	/// Deconstructs the split results into its components.
	/// </summary>
	/// <param name="first">On return, contains the first part.</param>
	/// <param name="second">On return, contains the second part.</param>
	public void Deconstruct(out T? first, out T? second)
	{
		first = First;
		second = Second;
	}
}
