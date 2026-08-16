using System;

namespace Avalonia.Media;

internal readonly record struct FontSourceIdentifier
{
	public string Name { get; init; }

	public Uri? Source { get; init; }

	public FontSourceIdentifier(string name, Uri? source)
	{
		Name = name;
		Source = source;
	}
}
