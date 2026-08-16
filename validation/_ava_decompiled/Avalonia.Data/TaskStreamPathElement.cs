using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data;

internal class TaskStreamPathElement<T> : IStronglyTypedStreamElement, ICompiledBindingPathElement
{
	public static readonly TaskStreamPathElement<T> Instance = new TaskStreamPathElement<T>();

	public IStreamPlugin CreatePlugin()
	{
		return new TaskStreamPlugin<T>();
	}
}
[RequiresUnreferencedCode("StreamPlugin might require unreferenced code.")]
internal class TaskStreamPathElement : IStronglyTypedStreamElement, ICompiledBindingPathElement
{
	public static readonly TaskStreamPathElement Instance = new TaskStreamPathElement();

	public IStreamPlugin CreatePlugin()
	{
		return new TaskStreamPlugin();
	}
}
