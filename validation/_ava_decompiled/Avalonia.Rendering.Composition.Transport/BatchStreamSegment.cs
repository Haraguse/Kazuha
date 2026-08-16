namespace Avalonia.Rendering.Composition.Transport;

public record struct BatchStreamSegment<TData>
{
	public TData Data { get; set; }

	public int ElementCount { get; set; }
}
