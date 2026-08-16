using System;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Exception font loading can throw if it finds a required table is missing during font loading.
/// </summary>
/// <seealso cref="T:System.Exception" />
internal class MissingFontTableException : Exception
{
	/// <summary>
	/// Gets the table where the error originated.
	/// </summary>
	public string Table { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Fonts.Tables.MissingFontTableException" /> class.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="table">The table.</param>
	public MissingFontTableException(string message, string table)
		: base(message)
	{
		Table = table;
	}
}
