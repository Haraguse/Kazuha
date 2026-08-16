using System;
using System.Collections.Generic;
using Avalonia.Media.Fonts;

namespace Avalonia.Media;

internal class CompositeFontFamilyKey : FontFamilyKey
{
	public IReadOnlyList<FontFamilyKey> Keys { get; }

	public CompositeFontFamilyKey(Uri source, FontFamilyKey[] keys)
		: base(source)
	{
		Keys = keys;
	}
}
