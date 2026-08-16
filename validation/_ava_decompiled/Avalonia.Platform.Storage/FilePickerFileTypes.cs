namespace Avalonia.Platform.Storage;

/// <summary>
/// Dictionary of well known file types.
/// </summary>
public static class FilePickerFileTypes
{
	public static FilePickerFileType All { get; } = new FilePickerFileType("All")
	{
		Patterns = new string[1] { "*.*" },
		AppleUniformTypeIdentifiers = new string[1] { "public.item" },
		MimeTypes = new string[1] { "*/*" }
	};

	public static FilePickerFileType TextPlain { get; } = new FilePickerFileType("Plain Text")
	{
		Patterns = new string[1] { "*.txt" },
		AppleUniformTypeIdentifiers = new string[1] { "public.plain-text" },
		MimeTypes = new string[1] { "text/plain" }
	};

	public static FilePickerFileType ImageAll { get; } = new FilePickerFileType("All Images")
	{
		Patterns = new string[6] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" },
		AppleUniformTypeIdentifiers = new string[1] { "public.image" },
		MimeTypes = new string[1] { "image/*" }
	};

	public static FilePickerFileType ImageJpg { get; } = new FilePickerFileType("JPEG image")
	{
		Patterns = new string[2] { "*.jpg", "*.jpeg" },
		AppleUniformTypeIdentifiers = new string[1] { "public.jpeg" },
		MimeTypes = new string[1] { "image/jpeg" }
	};

	public static FilePickerFileType ImagePng { get; } = new FilePickerFileType("PNG image")
	{
		Patterns = new string[1] { "*.png" },
		AppleUniformTypeIdentifiers = new string[1] { "public.png" },
		MimeTypes = new string[1] { "image/png" }
	};

	public static FilePickerFileType ImageWebp { get; } = new FilePickerFileType("WebP image")
	{
		Patterns = new string[1] { "*.webp" },
		AppleUniformTypeIdentifiers = new string[1] { "org.webmproject.webp" },
		MimeTypes = new string[1] { "image/webp" }
	};

	public static FilePickerFileType Pdf { get; } = new FilePickerFileType("PDF document")
	{
		Patterns = new string[1] { "*.pdf" },
		AppleUniformTypeIdentifiers = new string[1] { "com.adobe.pdf" },
		MimeTypes = new string[1] { "application/pdf" }
	};

	public static FilePickerFileType Json { get; } = new FilePickerFileType("JSON document")
	{
		Patterns = new string[1] { "*.json" },
		AppleUniformTypeIdentifiers = new string[1] { "public.json" },
		MimeTypes = new string[1] { "application/json" }
	};

	public static FilePickerFileType Xml { get; } = new FilePickerFileType("XML document")
	{
		Patterns = new string[1] { "*.xml" },
		AppleUniformTypeIdentifiers = new string[1] { "public.xml" },
		MimeTypes = new string[2] { "application/xml", "text/xml" }
	};
}
