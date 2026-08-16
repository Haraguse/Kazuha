using System;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Exception font loading can throw if it encounters invalid data during font loading.
/// </summary>
/// <seealso cref="T:System.Exception" />
internal class InvalidFontTableException : Exception
{
	/// <summary>
	/// Gets the table where the error originated.
	/// </summary>
	public string Table { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Fonts.Tables.InvalidFontTableException" /> class.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="table">The table.</param>
	public InvalidFontTableException(string message, string table)
		: base(message)
	{
		Table = table;
	}
}
