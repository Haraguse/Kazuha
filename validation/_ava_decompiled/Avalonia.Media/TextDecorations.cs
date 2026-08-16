namespace Avalonia.Media;

/// <summary>
/// Defines a set of commonly used text decorations.
/// </summary>
public static class TextDecorations
{
	/// <summary>
	/// Gets a <see cref="T:Avalonia.Media.TextDecorationCollection" /> containing an underline.
	/// </summary>
	public static TextDecorationCollection Underline { get; }

	/// <summary>
	/// Gets a <see cref="T:Avalonia.Media.TextDecorationCollection" /> containing a strikethrough.
	/// </summary>
	public static TextDecorationCollection Strikethrough { get; }

	/// <summary>
	/// Gets a <see cref="T:Avalonia.Media.TextDecorationCollection" /> containing an overline.
	/// </summary>
	public static TextDecorationCollection Overline { get; }

	/// <summary>
	/// Gets a <see cref="T:Avalonia.Media.TextDecorationCollection" /> containing a baseline.
	/// </summary>
	public static TextDecorationCollection Baseline { get; }

	static TextDecorations()
	{
		Underline = new TextDecorationCollection
		{
			new TextDecoration
			{
				Location = TextDecorationLocation.Underline
			}
		};
		Strikethrough = new TextDecorationCollection
		{
			new TextDecoration
			{
				Location = TextDecorationLocation.Strikethrough
			}
		};
		Overline = new TextDecorationCollection
		{
			new TextDecoration
			{
				Location = TextDecorationLocation.Overline
			}
		};
		Baseline = new TextDecorationCollection
		{
			new TextDecoration
			{
				Location = TextDecorationLocation.Baseline
			}
		};
	}
}
