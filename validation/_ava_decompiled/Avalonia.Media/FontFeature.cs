using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Avalonia.Media;

/// <summary>
/// Font feature
/// </summary>
public record FontFeature
{
	/// <summary>Gets or sets the tag.</summary>
	public string Tag { get; init; }

	/// <summary>Gets or sets the value.</summary>
	public int Value { get; init; }

	/// <summary>Gets or sets the start.</summary>
	public int Start { get; init; }

	/// <summary>Gets or sets the end.</summary>
	public int End { get; init; }

	private const int DefaultValue = 1;

	private const int InfinityEnd = -1;

	private static readonly Regex s_featureRegex = new Regex("^\\s*(?<Value>[+-])?\\s*(?<Tag>\\w{4})\\s*(\\[\\s*(?<Start>\\d+)?(\\s*(?<Separator>:)\\s*)?(?<End>\\d+)?\\s*\\])?\\s*(?(Value)()|(=\\s*(?<Value>\\d+|on|off)))?\\s*$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

	/// <summary>
	/// Creates an instance of FontFeature.
	/// </summary>
	public FontFeature()
	{
		Tag = string.Empty;
		Value = 1;
		Start = 0;
		End = -1;
	}

	/// <summary>
	/// Parses a string to return a <see cref="T:Avalonia.Media.FontFeature" />.
	/// Syntax is the following:
	///
	///     Syntax 	        Value 	Start 	End 	 
	///     Setting value: 	  	  	  	 
	///     kern 	        1 	    0 	    ∞ 	    Turn feature on
	///     +kern 	        1 	    0 	    ∞ 	    Turn feature on
	///     -kern 	        0 	    0 	    ∞ 	    Turn feature off
	///     kern=0 	        0 	    0 	    ∞ 	    Turn feature off
	///     kern=1 	        1 	    0 	    ∞ 	    Turn feature on
	///     aalt=2 	        2 	    0 	    ∞ 	    Choose 2nd alternate
	///     Setting index: 	  	  	  	 
	///     kern[] 	        1 	    0 	    ∞ 	    Turn feature on
	///     kern[:] 	    1 	    0 	    ∞ 	    Turn feature on
	///     kern[5:] 	    1 	    5 	    ∞ 	    Turn feature on, partial
	///     kern[:5] 	    1 	    0 	    5 	    Turn feature on, partial
	///     kern[3:5] 	    1 	    3 	    5 	    Turn feature on, range
	///     kern[3] 	    1 	    3 	    3+1 	Turn feature on, single char
	///     Mixing it all: 	  	  	  	 
	///     aalt[3:5]=2 	2 	    3 	    5 	    Turn 2nd alternate on for range
	///
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The <see cref="T:Avalonia.Media.FontFeature" />.</returns>
	public static FontFeature Parse(string s)
	{
		Match match = s_featureRegex.Match(s);
		if (!match.Success)
		{
			return new FontFeature();
		}
		bool flag = match.Groups["Separator"].Value == ":";
		bool flag2 = int.TryParse(match.Groups["Start"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var result);
		bool flag3 = int.TryParse(match.Groups["End"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var result2);
		string text = match.Groups["Value"].Value;
		if (text == "-" || text.ToUpperInvariant() == "OFF")
		{
			text = "0";
		}
		if (text == "+" || text.ToUpperInvariant() == "ON")
		{
			text = "1";
		}
		int result3;
		return new FontFeature
		{
			Tag = match.Groups["Tag"].Value,
			Start = (flag2 ? result : 0),
			End = (flag3 ? result2 : ((flag2 && !flag) ? (result + 1) : (-1))),
			Value = ((!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out result3)) ? 1 : result3)
		};
	}

	/// <summary>
	/// Gets a string representation of the <see cref="T:Avalonia.Media.FontFeature" />.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(128);
		if (Value == 0)
		{
			stringBuilder.Append('-');
		}
		stringBuilder.Append(Tag ?? string.Empty);
		if (Start != 0 || End != -1)
		{
			stringBuilder.Append('[');
			if (Start > 0)
			{
				stringBuilder.Append(Start.ToString(CultureInfo.InvariantCulture));
			}
			if (End != Start + 1)
			{
				stringBuilder.Append(':');
				if (End != -1)
				{
					stringBuilder.Append(End.ToString(CultureInfo.InvariantCulture));
				}
			}
			stringBuilder.Append(']');
		}
		int value = Value;
		if ((uint)value <= 1u)
		{
			return stringBuilder.ToString();
		}
		stringBuilder.Append('=');
		stringBuilder.Append(Value.ToString(CultureInfo.InvariantCulture));
		return stringBuilder.ToString();
	}
}
