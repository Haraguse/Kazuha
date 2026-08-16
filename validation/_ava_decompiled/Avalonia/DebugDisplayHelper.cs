using System.Text;

namespace Avalonia;

internal static class DebugDisplayHelper
{
	public static void AppendOptionalValue(StringBuilder builder, string name, object? value, bool includeContent)
	{
		if ((value == null || value is string { Length: 0 }) ? true : false)
		{
			return;
		}
		if (builder.Length > 0 && builder[builder.Length - 1] == ')')
		{
			int length = builder.Length - 1;
			builder.Length = length;
			builder.Append(", ");
		}
		else
		{
			builder.Append(" (");
		}
		builder.Append(name);
		builder.Append(" = ");
		if (value is AvaloniaObject avaloniaObject)
		{
			avaloniaObject.BuildDebugDisplay(builder, includeContent);
		}
		else
		{
			string text2 = value.ToString();
			if (text2 != null && text2.Length > 50)
			{
				builder.Append(text2, 0, 49);
				builder.Append('…');
			}
			else
			{
				builder.Append(text2);
			}
		}
		builder.Append(')');
	}
}
