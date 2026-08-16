namespace FluentAvalonia.UI.Controls;

internal readonly struct MathToken
{
	public MathTokenType Type { get; }

	public char Char { get; }

	public double Value { get; }

	public MathToken(MathTokenType t, char c)
	{
		Type = t;
		Char = c;
		Value = double.NaN;
	}

	public MathToken(MathTokenType t, double d)
	{
		Type = t;
		Value = d;
		Char = '\0';
	}
}
