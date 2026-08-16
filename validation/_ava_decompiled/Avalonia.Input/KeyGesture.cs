using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia.Input.Platform;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// Defines a keyboard input combination.
/// </summary>
public sealed class KeyGesture : IEquatable<KeyGesture>, IFormattable
{
	private static readonly Dictionary<string, Key> s_keySynonyms = new Dictionary<string, Key>
	{
		{
			"+",
			Key.OemPlus
		},
		{
			"-",
			Key.OemMinus
		},
		{
			".",
			Key.OemPeriod
		},
		{
			",",
			Key.OemComma
		}
	};

	public Key Key { get; }

	public KeyModifiers KeyModifiers { get; }

	public KeyGesture(Key key, KeyModifiers modifiers = KeyModifiers.None)
	{
		Key = key;
		KeyModifiers = modifiers;
	}

	public bool Equals(KeyGesture? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		if (Key == other.Key)
		{
			return KeyModifiers == other.KeyModifiers;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj is KeyGesture other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((int)Key * 397) ^ (int)KeyModifiers;
	}

	public static bool operator ==(KeyGesture? left, KeyGesture? right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(KeyGesture? left, KeyGesture? right)
	{
		return !object.Equals(left, right);
	}

	public static KeyGesture Parse(string gesture)
	{
		Key key = Key.None;
		KeyModifiers keyModifiers = KeyModifiers.None;
		int num = 0;
		for (int i = 0; i <= gesture.Length; i++)
		{
			char c = ((i != gesture.Length) ? gesture[i] : '\0');
			if (i == gesture.Length || (c == '+' && num != i))
			{
				ReadOnlySpan<char> modifier = gesture.AsSpan(num, i - num).Trim();
				if (!TryParseKey(modifier.ToString(), out key))
				{
					keyModifiers |= ParseModifier(modifier);
				}
				num = i + 1;
			}
		}
		return new KeyGesture(key, keyModifiers);
	}

	public override string ToString()
	{
		return ToString(null, null);
	}

	/// <summary>
	/// Returns the current KeyGesture as a string formatted according to the format string and appropriate IFormatProvider
	/// </summary>
	/// <param name="format">The format to use. 
	/// <list type="bullet">
	/// <item><term>null or "" or "g"</term><description>The Invariant format, uses Enum.ToString() to format Keys.</description></item>
	/// <item><term>"p"</term><description>Use platform specific formatting as registerd.</description></item>
	/// </list></param>
	/// <param name="formatProvider">The IFormatProvider to use.  If null, uses the appropriate provider registered in the Avalonia Locator, or Invariant.</param>
	/// <returns>The formatted string.</returns>
	/// <exception cref="T:System.FormatException">Thrown if the format string is not null, "", "g", or "p"</exception>
	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		KeyGestureFormatInfo keyGestureFormatInfo;
		if (format != null && (format == null || format.Length != 0) && !(format == "g"))
		{
			if (!(format == "p"))
			{
				throw new FormatException("Unknown format specifier");
			}
			keyGestureFormatInfo = KeyGestureFormatInfo.GetInstance(formatProvider);
		}
		else
		{
			keyGestureFormatInfo = KeyGestureFormatInfo.Invariant;
		}
		KeyGestureFormatInfo keyGestureFormatInfo2 = keyGestureFormatInfo;
		StringBuilder stringBuilder = StringBuilderCache.Acquire();
		if (KeyModifiers.HasAllFlags(KeyModifiers.Control))
		{
			stringBuilder.Append(keyGestureFormatInfo2.Ctrl);
		}
		if (KeyModifiers.HasAllFlags(KeyModifiers.Shift))
		{
			Plus(stringBuilder);
			stringBuilder.Append(keyGestureFormatInfo2.Shift);
		}
		if (KeyModifiers.HasAllFlags(KeyModifiers.Alt))
		{
			Plus(stringBuilder);
			stringBuilder.Append(keyGestureFormatInfo2.Alt);
		}
		if (KeyModifiers.HasAllFlags(KeyModifiers.Meta))
		{
			Plus(stringBuilder);
			stringBuilder.Append(keyGestureFormatInfo2.Meta);
		}
		if (Key != Key.None || KeyModifiers == KeyModifiers.None)
		{
			Plus(stringBuilder);
			stringBuilder.Append(keyGestureFormatInfo2.FormatKey(Key));
		}
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
		static void Plus(StringBuilder s)
		{
			if (s.Length > 0)
			{
				s.Append("+");
			}
		}
	}

	public bool Matches(KeyEventArgs? keyEvent)
	{
		if (keyEvent != null && keyEvent.KeyModifiers == KeyModifiers)
		{
			return ResolveNumPadOperationKey(keyEvent.Key) == ResolveNumPadOperationKey(Key);
		}
		return false;
	}

	private static bool TryParseKey(string keyStr, out Key key)
	{
		key = Key.None;
		if (s_keySynonyms.TryGetValue(keyStr.ToLower(CultureInfo.InvariantCulture), out key))
		{
			return true;
		}
		if (Enum.TryParse<Key>(keyStr, ignoreCase: true, out key))
		{
			return true;
		}
		return false;
	}

	private static KeyModifiers ParseModifier(ReadOnlySpan<char> modifier)
	{
		if (modifier.Equals("ctrl".AsSpan(), StringComparison.OrdinalIgnoreCase))
		{
			return KeyModifiers.Control;
		}
		if (modifier.Equals("cmd".AsSpan(), StringComparison.OrdinalIgnoreCase) || modifier.Equals("win".AsSpan(), StringComparison.OrdinalIgnoreCase) || modifier.Equals("⌘".AsSpan(), StringComparison.OrdinalIgnoreCase))
		{
			return KeyModifiers.Meta;
		}
		return Enum.Parse<KeyModifiers>(modifier.ToString(), ignoreCase: true);
	}

	private static Key ResolveNumPadOperationKey(Key key)
	{
		return key switch
		{
			Key.Add => Key.OemPlus, 
			Key.Subtract => Key.OemMinus, 
			Key.Decimal => Key.OemPeriod, 
			_ => key, 
		};
	}
}
