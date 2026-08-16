using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Avalonia.Platform.Internal;

internal class AssemblyDescriptorResolver : IAssemblyDescriptorResolver
{
	private readonly Dictionary<string, IAssemblyDescriptor> _assemblyNameCache = new Dictionary<string, IAssemblyDescriptor>();

	public IAssemblyDescriptor GetAssembly(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (!_assemblyNameCache.TryGetValue(name, out IAssemblyDescriptor value))
		{
			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => name.Equals(a.GetName().Name, StringComparison.InvariantCultureIgnoreCase));
			if (assembly != null)
			{
				value = (_assemblyNameCache[name] = new AssemblyDescriptor(assembly));
			}
			else
			{
				if (!RuntimeFeature.IsDynamicCodeSupported)
				{
					throw new InvalidOperationException("Assembly " + name + " needs to be referenced and explicitly loaded before loading resources");
				}
				name = Uri.UnescapeDataString(name);
				value = (_assemblyNameCache[name] = new AssemblyDescriptor(Assembly.Load(name)));
			}
		}
		return value;
	}

	public void InvalidateAssemblyCache(string name)
	{
		_assemblyNameCache.Remove(name);
	}

	public void InvalidateAssemblyCache()
	{
		_assemblyNameCache.Clear();
	}
}
