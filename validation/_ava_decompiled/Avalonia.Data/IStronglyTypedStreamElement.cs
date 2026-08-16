using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data;

internal interface IStronglyTypedStreamElement : ICompiledBindingPathElement
{
	IStreamPlugin CreatePlugin();
}
