using System;
using System.Text;

namespace Avalonia.Media.Fonts.Tables.Name;

internal readonly struct NameRecord(ReadOnlyMemory<byte> stringStorage, PlatformID platform, ushort languageId, KnownNameIds nameId, ushort offset, ushort length, Encoding encoding)
{
	private readonly ReadOnlyMemory<byte> _stringStorage = stringStorage;

	public PlatformID Platform { get; } = platform;

	public ushort LanguageID { get; } = languageId;

	public KnownNameIds NameID { get; } = nameId;

	public ushort Offset { get; } = offset;

	public ushort Length { get; } = length;

	public Encoding Encoding { get; } = encoding;

	public string GetValue()
	{
		if (Length == 0)
		{
			return string.Empty;
		}
		ReadOnlySpan<byte> bytes = _stringStorage.Span.Slice(Offset, Length);
		return Encoding.GetString(bytes);
	}
}
