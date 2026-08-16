using System;
using System.IO;
using System.Reflection;

namespace Avalonia.Platform.Internal;

internal class AvaloniaResourceDescriptor : IAssetDescriptor
{
	private readonly int _offset;

	private readonly int _length;

	public Assembly Assembly { get; }

	public AvaloniaResourceDescriptor(Assembly asm, int offset, int length)
	{
		_offset = offset;
		_length = length;
		Assembly = asm;
	}

	public Stream GetStream()
	{
		return new SlicedStream(Assembly.GetManifestResourceStream(Constants.AvaloniaResourceName) ?? throw new InvalidOperationException("Could not find manifest resource stream '" + Constants.AvaloniaResourceName + "',"), _offset, _length);
	}
}
