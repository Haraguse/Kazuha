namespace Avalonia.Utilities;

internal class Span
{
	public readonly object? element;

	public int length;

	public Span(object? element, int length)
	{
		this.element = element;
		this.length = length;
	}
}
