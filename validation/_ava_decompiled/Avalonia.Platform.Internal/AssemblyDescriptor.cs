using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Utilities;

namespace Avalonia.Platform.Internal;

internal class AssemblyDescriptor : IAssemblyDescriptor
{
	public Assembly Assembly { get; }

	public Dictionary<string, IAssetDescriptor>? Resources { get; }

	public Dictionary<string, IAssetDescriptor>? AvaloniaResources { get; }

	public string? Name { get; }

	public AssemblyDescriptor(Assembly assembly)
	{
		Assembly = assembly ?? throw new ArgumentNullException("assembly");
		Resources = ((IEnumerable<string>)assembly.GetManifestResourceNames()).ToDictionary((Func<string, string>)((string n) => n), (Func<string, IAssetDescriptor>)((string n) => new AssemblyResourceDescriptor(assembly, n)));
		Name = assembly.GetName().Name;
		using Stream stream = assembly.GetManifestResourceStream(Constants.AvaloniaResourceName);
		if (stream != null)
		{
			Resources.Remove(Constants.AvaloniaResourceName);
			int num = new BinaryReader(stream).ReadInt32();
			List<AvaloniaResourcesIndexEntry> source = AvaloniaResourcesIndexReaderWriter.ReadIndex(new SlicedStream(stream, 4, num));
			int baseOffset = num + 4;
			AvaloniaResources = ((IEnumerable<AvaloniaResourcesIndexEntry>)source).ToDictionary((Func<AvaloniaResourcesIndexEntry, string>)GetPathRooted, (Func<AvaloniaResourcesIndexEntry, IAssetDescriptor>)((AvaloniaResourcesIndexEntry r) => new AvaloniaResourceDescriptor(assembly, baseOffset + r.Offset, r.Size)));
		}
	}

	private static string GetPathRooted(AvaloniaResourcesIndexEntry r)
	{
		if (r.Path[0] != '/')
		{
			return "/" + r.Path;
		}
		return r.Path;
	}
}
