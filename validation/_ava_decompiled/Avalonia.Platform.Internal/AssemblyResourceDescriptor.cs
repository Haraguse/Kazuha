using System;
using System.IO;
using System.Reflection;

namespace Avalonia.Platform.Internal;

internal class AssemblyResourceDescriptor : IAssetDescriptor
{
	private readonly Assembly _asm;

	private readonly string _name;

	public Assembly Assembly => _asm;

	public AssemblyResourceDescriptor(Assembly asm, string name)
	{
		_asm = asm;
		_name = name;
	}

	public Stream GetStream()
	{
		return _asm.GetManifestResourceStream(_name) ?? throw new InvalidOperationException("Could not find manifest resource stream '" + _name + "',");
	}
}
