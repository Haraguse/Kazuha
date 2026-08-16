using System;
using System.Diagnostics;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Represents a portion of a <see cref="T:Avalonia.Media.TextFormatting.TextLine" /> object.
/// </summary>
[DebuggerTypeProxy(typeof(TextRunDebuggerProxy))]
public abstract class TextRun
{
	private class TextRunDebuggerProxy
	{
		private readonly TextRun _textRun;

		public string Text => _textRun.Text.ToString();

		public TextRunProperties? Properties => _textRun.Properties;

		public TextRunDebuggerProxy(TextRun textRun)
		{
			_textRun = textRun;
		}
	}

	public const int DefaultTextSourceLength = 1;

	/// <summary>
	///  Gets the text source length.
	/// </summary>
	public virtual int Length => 1;

	/// <summary>
	/// Gets the text run's text.
	/// </summary>
	public virtual ReadOnlyMemory<char> Text => default(ReadOnlyMemory<char>);

	/// <summary>
	/// A set of properties shared by every characters in the run
	/// </summary>
	public virtual TextRunProperties? Properties => null;
}
