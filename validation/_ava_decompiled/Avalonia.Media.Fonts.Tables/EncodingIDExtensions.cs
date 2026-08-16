using System.Text;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Converts encoding ID to TextEncoding
/// </summary>
internal static class EncodingIDExtensions
{
	/// <summary>
	/// Converts encoding ID to TextEncoding
	/// </summary>
	/// <param name="id">The identifier.</param>
	/// <returns>the encoding for this encoding ID</returns>
	public static Encoding AsEncoding(this EncodingIDs id)
	{
		if (id == EncodingIDs.Unicode11 || id == EncodingIDs.Unicode2)
		{
			return Encoding.BigEndianUnicode;
		}
		return Encoding.UTF8;
	}
}
