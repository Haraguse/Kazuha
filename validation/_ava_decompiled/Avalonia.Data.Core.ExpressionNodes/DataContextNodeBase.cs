using System;

namespace Avalonia.Data.Core.ExpressionNodes;

internal abstract class DataContextNodeBase : SourceNode
{
	public override object? SelectSource(object? source, object target, object? anchor)
	{
		if (source != AvaloniaProperty.UnsetValue)
		{
			throw new NotSupportedException("DataContextNode is invalid in conjunction with a binding source.");
		}
		if (target is IDataContextProvider dataContextProvider && dataContextProvider is AvaloniaObject)
		{
			return target;
		}
		if (anchor is IDataContextProvider dataContextProvider2 && dataContextProvider2 is AvaloniaObject)
		{
			return anchor;
		}
		throw new InvalidOperationException("Cannot find a DataContext to bind to.");
	}
}
