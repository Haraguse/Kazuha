namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Specifies the format used for the 'loca' table.
/// </summary>
internal enum IndexToLocFormat : short
{
	/// <summary>
	/// Short offsets (Offset16). The actual local offset divided by 2 is stored.
	/// </summary>
	Short,
	/// <summary>
	/// Long offsets (Offset32). The actual local offset is stored.
	/// </summary>
	Long
}
