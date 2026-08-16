namespace Avalonia.Input;

internal static class KeySymbolHelper
{
	public static bool IsAllowedAsciiKeySymbol(char c)
	{
		if (c < ' ')
		{
			switch (c)
			{
			case '\b':
			case '\t':
			case '\r':
			case '\u001b':
				return true;
			default:
				return false;
			}
		}
		if (c == '\u007f')
		{
			return false;
		}
		return true;
	}
}
