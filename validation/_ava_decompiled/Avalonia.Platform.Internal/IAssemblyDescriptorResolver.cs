namespace Avalonia.Platform.Internal;

internal interface IAssemblyDescriptorResolver
{
	IAssemblyDescriptor GetAssembly(string name);

	void InvalidateAssemblyCache(string name);

	void InvalidateAssemblyCache();
}
