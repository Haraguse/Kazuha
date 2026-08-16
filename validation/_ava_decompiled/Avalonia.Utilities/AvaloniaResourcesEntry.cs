using System;
using System.IO;

namespace Avalonia.Utilities;

public class AvaloniaResourcesEntry
{
	public string? Path { get; init; }

	public Func<Stream>? Open { get; init; }

	public int Size { get; init; }

	public string? SystemPath { get; init; }
}
