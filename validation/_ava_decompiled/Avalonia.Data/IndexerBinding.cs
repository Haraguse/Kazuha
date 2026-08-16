using Avalonia.Data.Core;

namespace Avalonia.Data;

internal class IndexerBinding : BindingBase
{
	public AvaloniaProperty Property { get; }

	private AvaloniaObject Source { get; }

	private BindingMode Mode { get; }

	public IndexerBinding(AvaloniaObject source, AvaloniaProperty property, BindingMode mode)
	{
		Source = source;
		Property = property;
		Mode = mode;
	}

	internal override BindingExpressionBase CreateInstance(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor)
	{
		return new IndexerBindingExpression(Source, Property, target, targetProperty, Mode);
	}
}
