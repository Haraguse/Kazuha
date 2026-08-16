using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data;

internal class ObservableStreamPathElement<T> : IStronglyTypedStreamElement, ICompiledBindingPathElement
{
	public static readonly ObservableStreamPathElement<T> Instance = new ObservableStreamPathElement<T>();

	public IStreamPlugin CreatePlugin()
	{
		return new ObservableStreamPlugin<T>();
	}
}
[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
internal class ObservableStreamPathElement : IStronglyTypedStreamElement, ICompiledBindingPathElement
{
	public static readonly ObservableStreamPathElement Instance = new ObservableStreamPathElement();

	public IStreamPlugin CreatePlugin()
	{
		return new ObservableStreamPlugin();
	}
}
