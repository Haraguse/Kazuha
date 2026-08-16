using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Represents a name mapped to the associated file types (extensions).
/// </summary>
public sealed class FilePickerFileType(string? name)
{
	/// <summary>
	/// File type name.
	/// </summary>
	public string Name { get; } = name ?? string.Empty;

	/// <summary>
	/// List of extensions in GLOB format. I.e. "*.png" or "*.*".
	/// </summary>
	/// <remarks>
	/// Used on Windows, Linux and Browser platforms.
	/// </remarks>
	public IReadOnlyList<string>? Patterns { get; set; }

	/// <summary>
	/// List of extensions in MIME format.
	/// </summary>
	/// <remarks>
	/// Used on Android, Linux and Browser platforms.
	/// </remarks>
	public IReadOnlyList<string>? MimeTypes { get; set; }

	/// <summary>
	/// List of extensions in Apple uniform format.
	/// </summary>
	/// <remarks>
	/// Used only on Apple devices.
	/// See https://developer.apple.com/documentation/uniformtypeidentifiers/system_declared_uniform_type_identifiers.
	/// </remarks>
	public IReadOnlyList<string>? AppleUniformTypeIdentifiers { get; set; }

	internal IReadOnlyList<string>? TryGetExtensions()
	{
		return Patterns?.Select(Path.GetExtension).Where((string e) => !string.IsNullOrEmpty(e) && !e.Contains('*') && e.StartsWith(".")).Select((string e) => e.TrimStart('.'))
			.ToArray();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Name;
	}
}
