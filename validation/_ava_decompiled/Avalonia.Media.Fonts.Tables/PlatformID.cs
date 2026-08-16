namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// platforms ids
/// </summary>
internal enum PlatformID : ushort
{
	/// <summary>
	/// Unicode platform
	/// </summary>
	Unicode,
	/// <summary>
	/// Script manager code
	/// </summary>
	Macintosh,
	/// <summary>
	/// [deprecated] ISO encoding
	/// </summary>
	ISO,
	/// <summary>
	/// Window encoding
	/// </summary>
	Windows,
	/// <summary>
	/// Custom platform
	/// </summary>
	Custom
}
