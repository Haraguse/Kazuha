using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace FluentAvalonia.UI.Media;

/// <summary>
/// Represents a color in RGB, HSV, HSL, or CMYK colorspace
/// </summary>
[TypeConverter(typeof(Color2ToColorConverter))]
public struct Color2 : IEquatable<Color2>
{
	private const float EPSILON = 0.001f;

	private ColorType _cType;

	private float _alpha;

	private float _c1;

	private float _c2;

	private float _c3;

	private float _c4;

	public static readonly Color2 Empty;

	/// <summary>
	/// Gets the Alpha channel of the color, [0,255]
	/// </summary>
	public byte A => (byte)Math.Round(_alpha * 255f);

	/// <summary>
	/// Gets the Red channel of the color. If color is not an RGB color, it is converted to one. [0,255]
	/// </summary>
	public byte R
	{
		get
		{
			if (_cType != ColorType.RGB)
			{
				return ToRGB().R;
			}
			return (byte)Math.Round(_c1 * 255f);
		}
	}

	/// <summary>
	/// Gets the Green channel of the color. If color is not an RGB color, it is converted to one. [0,255]
	/// </summary>
	public byte G
	{
		get
		{
			if (_cType != ColorType.RGB)
			{
				return ToRGB().G;
			}
			return (byte)Math.Round(_c2 * 255f);
		}
	}

	/// <summary>
	/// Gets the Blue channel of the color. If color is not an RGB color, it is converted to one. [0,255]
	/// </summary>
	public byte B
	{
		get
		{
			if (_cType != ColorType.RGB)
			{
				return ToRGB().B;
			}
			return (byte)Math.Round(_c3 * 255f);
		}
	}

	/// <summary>
	/// Gets the Alpha channel of the color. [0,1]
	/// </summary>
	public float Af => _alpha;

	/// <summary>
	/// Gets the Red channel of the color. If color is not an RGB color, it is converted to one. [0,1]
	/// </summary>
	public float Rf
	{
		get
		{
			if (_cType != ColorType.RGB)
			{
				return ToRGB().Rf;
			}
			return _c1;
		}
	}

	/// <summary>
	/// Gets the Green channel of the color. If color is not an RGB color, it is converted to one. [0,1]
	/// </summary>
	public float Gf
	{
		get
		{
			if (_cType != ColorType.RGB)
			{
				return ToRGB().Gf;
			}
			return _c2;
		}
	}

	/// <summary>
	/// Gets the Blue channel of the color. If color is not an RGB color, it is converted to one. [0,1]
	/// </summary>
	public float Bf
	{
		get
		{
			if (_cType != ColorType.RGB)
			{
				return ToRGB().Bf;
			}
			return _c3;
		}
	}

	/// <summary>
	/// Gets the HSL or HSV Hue of the color. HSL and HSV hue is the same, so if the color type is neither HSV
	/// or HSL, it is converted to HSV first. [0,360)
	/// </summary>
	public int Hue
	{
		get
		{
			if (_cType != ColorType.HSV && _cType != ColorType.HSL)
			{
				return ToHSV().Hue;
			}
			return (int)Math.Round(_c1);
		}
	}

	/// <summary>
	/// Gets the HSL or HSV Hue of the color. HSL and HSV hue is the same, so if the color type is neither HSV
	/// or HSL, it is converted to HSV first. [0,360)
	/// </summary>
	public float Huef
	{
		get
		{
			if (_cType != ColorType.HSV && _cType != ColorType.HSL)
			{
				return ToHSV().Huef;
			}
			return _c1;
		}
	}

	/// <summary>
	/// Gets the HSV Saturation of the color. If the color is not an HSV color, it is converted first. [0,100]
	/// </summary>
	public int Saturation
	{
		get
		{
			if (_cType != ColorType.HSV)
			{
				return ToHSV().Saturation;
			}
			return (int)Math.Round(_c2 * 100f);
		}
	}

	/// <summary>
	/// Gets the HSV Saturation of the color. If the color is not an HSV color, it is converted first. [0,1]
	/// </summary>
	public float Saturationf
	{
		get
		{
			if (_cType != ColorType.HSV)
			{
				return ToHSV().Saturationf;
			}
			return _c2;
		}
	}

	/// <summary>
	/// Gets the Value of the color. If the color is not an HSV color, it is converted first. [0,100]
	/// </summary>
	public int Value
	{
		get
		{
			if (_cType != ColorType.HSV)
			{
				return ToHSV().Value;
			}
			return (int)Math.Round(_c3 * 100f);
		}
	}

	/// <summary>
	/// Gets the HSV Value of the color. If the color is not an HSV color, it is converted first. [0,1]
	/// </summary>
	public float Valuef
	{
		get
		{
			if (_cType != ColorType.HSV)
			{
				return ToHSV().Valuef;
			}
			return _c3;
		}
	}

	/// <summary>
	/// Gets the HSL Saturation of the color. If the color is not an HSL color, it is converted first. [0,100]
	/// </summary>
	public int HSLSaturation
	{
		get
		{
			if (_cType != ColorType.HSL)
			{
				return ToHSL().HSLSaturation;
			}
			return (int)Math.Round(_c2 * 100f);
		}
	}

	/// <summary>
	/// Gets the HSL Saturation of the color. If the color is not an HSL color, it is converted first. [0,1]
	/// </summary>
	public float HSLSaturationf
	{
		get
		{
			if (_cType != ColorType.HSL)
			{
				return ToHSL().HSLSaturationf;
			}
			return _c2;
		}
	}

	/// <summary>
	/// Gets the HSL Lightness of the color. If the color is not an HSL color, it is converted first. [0,100]
	/// </summary>
	public int Lightness
	{
		get
		{
			if (_cType != ColorType.HSL)
			{
				return ToHSL().Lightness;
			}
			return (int)Math.Round(_c3 * 100f);
		}
	}

	/// <summary>
	/// Gets the HSL Lightness of the color. If the color is not an HSL color, it is converted first. [0,1]
	/// </summary>
	public float Lightnessf
	{
		get
		{
			if (_cType != ColorType.HSL)
			{
				return ToHSL().Lightnessf;
			}
			return _c3;
		}
	}

	/// <summary>
	/// Gets the CMYK Cyan of the color. If the color is not an CMYK color, it is converted first. [0,100]
	/// </summary>
	public int CMYKCyan
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKCyan;
			}
			return (int)Math.Round(_c1 * 100f);
		}
	}

	/// <summary>
	/// Gets the CMYK Cyan of the color. If the color is not an CMYK color, it is converted first. [0,1]
	/// </summary>
	public float CMYKCyanf
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKCyanf;
			}
			return _c1;
		}
	}

	/// <summary>
	/// Gets the CMYK Magenta of the color. If the color is not an CMYK color, it is converted first. [0,100]
	/// </summary>
	public int CMYKMagenta
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKMagenta;
			}
			return (int)Math.Round(_c2 * 100f);
		}
	}

	/// <summary>
	/// Gets the CMYK Magenta of the color. If the color is not an CMYK color, it is converted first. [0,1]
	/// </summary>
	public float CMYKMagentaf
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKMagentaf;
			}
			return _c2;
		}
	}

	/// <summary>
	/// Gets the CMYK Yellow of the color. If the color is not an CMYK color, it is converted first. [0,100]
	/// </summary>
	public int CMYKYellow
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKYellow;
			}
			return (int)Math.Round(_c3 * 100f);
		}
	}

	/// <summary>
	/// Gets the CMYK Yellow of the color. If the color is not an CMYK color, it is converted first. [0,1]
	/// </summary>
	public float CMYKYellowf
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKYellowf;
			}
			return _c3;
		}
	}

	/// <summary>
	/// Gets the CMYK Black of the color. If the color is not an CMYK color, it is converted first. [0,100]
	/// </summary>
	public int CMYKBlack
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKBlack;
			}
			return (int)Math.Round(_c4 * 100f);
		}
	}

	/// <summary>
	/// Gets the CMYK Black of the color. If the color is not an CMYK color, it is converted first. [0,1]
	/// </summary>
	public float CMYKBlackf
	{
		get
		{
			if (_cType != ColorType.CMYK)
			{
				return ToCMYK().CMYKBlackf;
			}
			return _c4;
		}
	}

	/// <summary>
	/// Creates a RGB color 2 from the given RGBA values
	/// </summary>
	/// <param name="r">Red, [0,255]</param>
	/// <param name="g">Green, [0,255]</param>
	/// <param name="b">Blue, [0,255]</param>
	/// <param name="a">Alpha, [0,255]</param>
	public Color2(byte r, byte g, byte b, byte a = byte.MaxValue)
	{
		_cType = ColorType.RGB;
		_c1 = (float)(int)r / 255f;
		_c2 = (float)(int)g / 255f;
		_c3 = (float)(int)b / 255f;
		_c4 = 0f;
		_alpha = (float)(int)a / 255f;
	}

	/// <summary>
	/// Creates a RGB Color2 from an <see cref="T:Avalonia.Media.Color" />
	/// </summary>
	/// <param name="avColor"></param>
	public Color2(Color avColor)
	{
		_cType = ColorType.RGB;
		_c1 = (float)(int)((Color)(ref avColor)).R / 255f;
		_c2 = (float)(int)((Color)(ref avColor)).G / 255f;
		_c3 = (float)(int)((Color)(ref avColor)).B / 255f;
		_c4 = 0f;
		_alpha = (float)(int)((Color)(ref avColor)).A / 255f;
	}

	/// <summary>
	/// Gets all RGBA components of the color. If not RGB, color is converted first
	/// </summary>
	/// <param name="r">Red, [0,255]</param>
	/// <param name="g">Green, [0,255]</param>
	/// <param name="b">Blue, [0,255]</param>
	/// <param name="a">Alpha, [0,255]</param>
	public void GetRGB(out byte r, out byte g, out byte b, out byte a)
	{
		if (_cType != ColorType.RGB)
		{
			ToRGB().GetRGB(out r, out g, out b, out a);
			return;
		}
		r = (byte)Math.Round(_c1 * 255f);
		g = (byte)Math.Round(_c2 * 255f);
		b = (byte)Math.Round(_c3 * 255f);
		a = (byte)Math.Round(_alpha * 255f);
	}

	/// <summary>
	/// Gets all RGBA components of the color as floating point numbers. If not RGB, color is converted first
	/// </summary>
	/// <param name="r">Red, [0,1]</param>
	/// <param name="g">Green, [0,1]</param>
	/// <param name="b">Blue, [0,1]</param>
	/// <param name="a">Alpha, [0,1]</param>
	public void GetRGBf(out float r, out float g, out float b, out float a)
	{
		if (_cType != ColorType.RGB)
		{
			ToRGB().GetRGBf(out r, out g, out b, out a);
			return;
		}
		r = _c1;
		g = _c2;
		b = _c3;
		a = _alpha;
	}

	/// <summary>
	/// Gets all HSV components of the color as floating point numbers. If not HSV, color is converted first
	/// </summary>
	/// <param name="h">Hue, [0,360)</param>
	/// <param name="s">Saturation, [0,1]</param>
	/// <param name="v">Value, [0,1]</param>
	/// <param name="a">Alpha, [0,1]</param>
	public void GetHSVf(out float h, out float s, out float v, out float a)
	{
		if (_cType != ColorType.HSV)
		{
			ToHSV().GetHSVf(out h, out s, out v, out a);
			return;
		}
		h = _c1;
		s = _c2;
		v = _c3;
		a = _alpha;
	}

	/// <summary>
	/// Gets all HSV components of the color. If not HSV, color is converted first
	/// </summary>
	/// <param name="h">Hue, [0,360)</param>
	/// <param name="s">Saturation, [0,100]</param>
	/// <param name="v">Value, [0,100]</param>
	/// <param name="a">Alpha, [0,255]</param>
	public void GetHSV(out int h, out int s, out int v, out int a)
	{
		if (_cType != ColorType.HSV)
		{
			ToHSV().GetHSV(out h, out s, out v, out a);
			return;
		}
		h = (int)Math.Round(_c1);
		s = (int)Math.Round(_c2 * 100f);
		v = (int)Math.Round(_c3 * 100f);
		a = (int)Math.Round(_alpha * 255f);
	}

	/// <summary>
	/// Gets all HSL components of the color as floating point numbers. If not HSL, color is converted first
	/// </summary>
	/// <param name="h">Hue, [0,360)</param>
	/// <param name="s">Saturation, [0,1]</param>
	/// <param name="l">Lightness, [0,1]</param>
	/// <param name="a">Alpha, [0,1]</param>
	public void GetHSLf(out float h, out float s, out float l, out float a)
	{
		if (_cType != ColorType.HSL)
		{
			ToHSL().GetHSLf(out h, out s, out l, out a);
			return;
		}
		h = _c1;
		s = _c2;
		l = _c3;
		a = _alpha;
	}

	/// <summary>
	/// Gets all HSL components of the color. If not HSL, color is converted first
	/// </summary>
	/// <param name="h">Hue, [0,360)</param>
	/// <param name="s">Saturation, [0,100]</param>
	/// <param name="l">Lightness, [0,100]</param>
	/// <param name="a">Alpha, [0,255]</param>
	public void GetHSL(out int h, out int s, out int l, out int a)
	{
		if (_cType != ColorType.HSL)
		{
			ToHSL().GetHSL(out h, out s, out l, out a);
			return;
		}
		h = (int)Math.Round(_c1);
		s = (int)Math.Round(_c2 * 100f);
		l = (int)Math.Round(_c3 * 100f);
		a = (int)Math.Round(_alpha * 255f);
	}

	/// <summary>
	/// Gets all CMYK components of the color as floating point numbers. If not CMYK, color is converted first
	/// </summary>
	/// <param name="c">Cyan, [0,1]</param>
	/// <param name="m">Magenta, [0,1]</param>
	/// <param name="y">Yellow, [0,1]</param>
	/// <param name="k">Black, [0,1]</param>
	/// <param name="a">Alpha, [0,1]</param>
	public void GetCMYKf(out float c, out float m, out float y, out float k, out float a)
	{
		if (_cType != ColorType.CMYK)
		{
			ToCMYK().GetCMYKf(out c, out m, out y, out k, out a);
			return;
		}
		c = _c1;
		m = _c2;
		y = _c3;
		k = _c4;
		a = _alpha;
	}

	/// <summary>
	/// Gets all CMYK components of the color. If not CMYK, color is converted first
	/// </summary>
	/// <param name="c">Cyan, [0,100]</param>
	/// <param name="m">Magenta, [0,100]</param>
	/// <param name="y">Yellow, [0,100]</param>
	/// <param name="k">Black, [0,100]</param>
	/// <param name="a">Alpha, [0,255]</param>
	public void GetCMYK(out int c, out int m, out int y, out int k, out int a)
	{
		if (_cType != ColorType.CMYK)
		{
			ToCMYK().GetCMYK(out c, out m, out y, out k, out a);
			return;
		}
		c = (int)Math.Round(_c1 * 100f);
		m = (int)Math.Round(_c2 * 100f);
		y = (int)Math.Round(_c3 * 100f);
		k = (int)Math.Round(_c4 * 100f);
		a = (int)Math.Round(_alpha * 255f);
	}

	/// <summary>
	/// Converts the current color to RGB color space
	/// </summary>
	/// <returns>RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public Color2 ToRGB()
	{
		if (_cType == ColorType.RGB)
		{
			return this;
		}
		float r = 0f;
		float g = 0f;
		float b = 0f;
		switch (_cType)
		{
		case ColorType.HSV:
			HSVToRGB(_c1, _c2, _c3, out r, out g, out b);
			break;
		case ColorType.HSL:
			HSLToRGB(_c1, _c2, _c3, out r, out g, out b);
			break;
		case ColorType.CMYK:
			CMYKToRGB(_c1, _c2, _c3, _c4, out r, out g, out b);
			break;
		}
		return new Color2
		{
			_cType = ColorType.RGB,
			_c1 = r,
			_c2 = g,
			_c3 = b,
			_alpha = _alpha
		};
	}

	/// <summary>
	/// Converts the current color to HSV color space
	/// </summary>
	/// <returns>HSV <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public Color2 ToHSV()
	{
		if (_cType == ColorType.HSV)
		{
			return this;
		}
		float h = 0f;
		float s = 0f;
		float v = 0f;
		switch (_cType)
		{
		case ColorType.RGB:
			RGBToHSV(_c1, _c2, _c3, out h, out s, out v);
			break;
		case ColorType.HSL:
			h = _c1;
			HSLToHSV(_c2, _c3, out s, out v);
			break;
		case ColorType.CMYK:
		{
			ToRGB().GetHSVf(out h, out s, out v, out var _);
			break;
		}
		}
		return new Color2
		{
			_cType = ColorType.HSV,
			_c1 = h,
			_c2 = s,
			_c3 = v,
			_alpha = _alpha
		};
	}

	/// <summary>
	/// Converts the current color to HSL color space
	/// </summary>
	/// <returns>HSL <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public Color2 ToHSL()
	{
		if (_cType == ColorType.HSL)
		{
			return this;
		}
		float h = 0f;
		float s = 0f;
		float l = 0f;
		switch (_cType)
		{
		case ColorType.RGB:
			RGBToHSL(_c1, _c2, _c3, out h, out s, out l);
			break;
		case ColorType.HSV:
			h = _c1;
			HSVToHSL(_c2, _c3, out s, out l);
			break;
		case ColorType.CMYK:
		{
			ToRGB().GetHSLf(out h, out s, out l, out var _);
			break;
		}
		}
		return new Color2
		{
			_cType = ColorType.HSL,
			_c1 = h,
			_c2 = s,
			_c3 = l,
			_alpha = _alpha
		};
	}

	/// <summary>
	/// Converts the current color to CMYK color space
	/// </summary>
	/// <returns>CMYK <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public Color2 ToCMYK()
	{
		if (_cType == ColorType.CMYK)
		{
			return this;
		}
		float c = 0f;
		float m = 0f;
		float y = 0f;
		float k = 0f;
		switch (_cType)
		{
		case ColorType.RGB:
			RGBToCMYK(_c1, _c2, _c3, out c, out m, out y, out k);
			break;
		case ColorType.HSV:
		case ColorType.HSL:
		{
			ToRGB().GetCMYKf(out c, out m, out y, out k, out var _);
			break;
		}
		}
		return new Color2
		{
			_cType = ColorType.CMYK,
			_c1 = c,
			_c2 = m,
			_c3 = y,
			_c4 = k,
			_alpha = _alpha
		};
	}

	public bool Equals(Color2 other)
	{
		if (other._cType != _cType)
		{
			return false;
		}
		if (Math.Abs(other._c1 - _c1) > 0.001f)
		{
			return false;
		}
		if (Math.Abs(other._c2 - _c2) > 0.001f)
		{
			return false;
		}
		if (Math.Abs(other._c3 - _c3) > 0.001f)
		{
			return false;
		}
		if (_cType == ColorType.CMYK && Math.Abs(other._c4 - _c4) > 0.001f)
		{
			return false;
		}
		if (Math.Abs(other._alpha - _alpha) > 0.001f)
		{
			return false;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj is Color2 color)
		{
			return color.Equals(this);
		}
		return false;
	}

	public override string ToString()
	{
		return ToHexString() + ", " + ToHTML();
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_cType, _alpha, _c1, _c2, _c3);
	}

	public static bool operator ==(Color2 ec1, Color2 ec2)
	{
		return ec1.Equals(ec2);
	}

	public static bool operator !=(Color2 ec1, Color2 ec2)
	{
		return !ec1.Equals(ec2);
	}

	/// <summary>
	/// Return the equivalent hex string representing the color.
	/// </summary>
	/// <param name="includeAlpha">Whether to include the alpha channel or not</param>
	/// <returns>Hex string of the color</returns>
	public string ToHexString(bool includeAlpha = true)
	{
		GetRGB(out var r, out var g, out var b, out var a);
		if (!includeAlpha)
		{
			return $"#{r:x2}{g:x2}{b:x2}";
		}
		return $"#{a:x2}{r:x2}{g:x2}{b:x2}";
	}

	/// <summary>
	/// Returns the rgb, and a, if specified, of the color in html rgb notation
	/// </summary>
	/// <param name="includeAlpha">Whether to include the alpha channel or not</param>
	/// <returns>HTML formatted rgb(r,g,b) or rgba(r,g,b,a)</returns>
	public string ToHTML(bool includeAlpha = true)
	{
		GetRGB(out var r, out var g, out var b, out var a);
		if (!includeAlpha)
		{
			return $"rgb( {r}, {g}, {b} )";
		}
		return $"rgba( {r}, {g}, {b}, {a} )";
	}

	public string GetDisplayName()
	{
		return KnownColorTable.GetColorName(this);
	}

	public static Color2 FromDisplayName(string name)
	{
		return KnownColorTable.FromColorName(name);
	}

	/// <summary>
	/// Parses the string representing a Hex value or HTML notation to a color. If parsing fails
	/// <see cref="F:FluentAvalonia.UI.Media.Color2.Empty" /> is returned
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static Color2 Parse(string value)
	{
		if (TryParse(value.AsSpan(), out var ec))
		{
			return ec;
		}
		return Empty;
	}

	/// <summary>
	/// Attempts to parse a string as a <see cref="T:System.ReadOnlySpan`1" /> of <see cref="T:System.Char" />  into a color
	/// </summary>
	/// <param name="value">Value to parse</param>
	/// <param name="ec">The color, if successful</param>
	/// <returns>True if successful, otherwise false</returns>
	public static bool TryParse(ReadOnlySpan<char> value, out Color2 ec)
	{
		if (value.Contains("#".AsSpan(), StringComparison.Ordinal))
		{
			ReadOnlySpan<char> s = value.Slice(1);
			uint result2;
			if (s.Length == 3 || s.Length == 4)
			{
				Span<char> span = stackalloc char[s.Length * 2];
				for (int i = 0; i < s.Length; i++)
				{
					span[i * 2] = (span[i * 2 + 1] = s[i]);
				}
				if (uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
				{
					ec = FromUInt(result | (uint)((s.Length == 3) ? (-16777216) : 0));
					return true;
				}
			}
			else if ((s.Length == 6 || s.Length == 8) && uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result2))
			{
				ec = FromUInt(result2 | (uint)((s.Length == 6) ? (-16777216) : 0));
				return true;
			}
		}
		else if (value.StartsWith("rgb".AsSpan()))
		{
			int num = value.IndexOf("(".AsSpan()) + 1;
			string[] array = value.Slice(num, value.Length - num - 1).Trim().ToString()
				.Split(',');
			if (array.Length != 3 && array.Length != 4)
			{
				ec = Empty;
				return false;
			}
			if (byte.TryParse(array[0].Trim(), out var result3) && byte.TryParse(array[1].Trim(), out var result4) && byte.TryParse(array[2].Trim(), out var result5))
			{
				if (array.Length == 4 && byte.TryParse(array[3], out var result6))
				{
					ec = FromARGB(result6, result3, result4, result5);
					return true;
				}
				ec = FromRGB(result3, result4, result5);
				return true;
			}
		}
		ec = Empty;
		return false;
	}

	public static implicit operator Color(Color2 ec)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		ec.GetRGB(out var r, out var g, out var b, out var a);
		return Color.FromArgb(a, r, g, b);
	}

	public static implicit operator Color2(Color c)
	{
		return FromUInt(((Color)(ref c)).ToUInt32());
	}

	public static bool AreColorsClose(Color2 col1, Color2 col2, float tolerance = 0.03f)
	{
		return false;
	}

	/// <summary>
	/// Creates an RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the specified R,G,B values. The alpha is set to 255
	/// </summary>
	/// <param name="r">Red, [0,255]</param>
	/// <param name="g">Green, [0,255]</param>
	/// <param name="b">Blue, [0,255]</param>
	/// <returns>RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromRGB(byte r, byte g, byte b)
	{
		return new Color2(r, g, b);
	}

	/// <summary>
	/// Creates an RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the specified A,R,G,B values.
	/// </summary>
	/// <param name="a">Alpha, [0,255]</param>
	/// <param name="r">Red, [0,255]</param>
	/// <param name="g">Green, [0,255]</param>
	/// <param name="b">Blue, [0,255]</param>
	/// <returns>RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromARGB(byte a, byte r, byte g, byte b)
	{
		return new Color2(r, g, b, a);
	}

	/// <summary>
	/// Creates an RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the specified A,R,G,B float values.
	/// </summary>
	/// <param name="r">Red, [0,1]</param>
	/// <param name="g">Green, [0,1]</param>
	/// <param name="b">Blue, [0,1]</param>
	/// /// <param name="a">Alpha, [0,1]</param>
	/// <returns>RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromRGBf(float r, float g, float b, float a = 1f)
	{
		return new Color2
		{
			_cType = ColorType.RGB,
			_c1 = float.Clamp(r, 0f, 1f),
			_c2 = float.Clamp(g, 0f, 1f),
			_c3 = float.Clamp(b, 0f, 1f),
			_alpha = float.Clamp(a, 0f, 1f)
		};
	}

	/// <summary>
	/// Creates an RGB <see cref="T:FluentAvalonia.UI.Media.Color2" /> from an unsigned integer
	/// </summary>
	/// <param name="num"></param>
	/// <returns></returns>
	public static Color2 FromUInt(uint num)
	{
		byte a = (byte)((num >> 24) & 0xFF);
		byte r = (byte)((num >> 16) & 0xFF);
		byte g = (byte)((num >> 8) & 0xFF);
		byte b = (byte)(num & 0xFF);
		return new Color2(r, g, b, a);
	}

	/// <summary>
	/// Creates an HSV <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the given values
	/// </summary>
	/// <param name="hue">Hue, [0,360)</param>
	/// <param name="sat">Saturation, [0,100]</param>
	/// <param name="val">Value, [0,100]</param>
	/// <param name="alpha">Alpha, [0,255]</param>
	/// <returns>HSV <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromHSV(int hue, int sat, int val, int alpha = 255)
	{
		return FromHSVf(hue, (float)sat / 100f, (float)val / 100f, (float)alpha / 255f);
	}

	/// <summary>
	/// Creates an HSV <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the given float values
	/// </summary>
	/// <param name="hue">Hue, [0,360)</param>
	/// <param name="sat">Saturation, [0,1]</param>
	/// <param name="val">Value, [0,1]</param>
	/// <param name="alpha">Alpha, [0,1]</param>
	/// <returns>HSV <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromHSVf(float hue, float sat, float val, float alpha = 1f)
	{
		return new Color2
		{
			_cType = ColorType.HSV,
			_c1 = ((hue == -1f) ? 0f : float.Clamp(hue % 360f, 0f, 360f)),
			_c2 = float.Clamp(sat, 0f, 1f),
			_c3 = float.Clamp(val, 0f, 1f),
			_alpha = float.Clamp(alpha, 0f, 1f)
		};
	}

	/// <summary>
	/// Creates an HSL <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the given values
	/// </summary>
	/// <param name="hue">Hue, [0,360)</param>
	/// <param name="sat">Saturation, [0,100]</param>
	/// <param name="light">Value, [0,100]</param>
	/// <param name="alpha">Alpha, [0,255]</param>
	/// <returns>HSL <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromHSL(int hue, int sat, int light, int alpha = 255)
	{
		return FromHSLf(hue, (float)sat / 100f, (float)light / 100f, (float)alpha / 255f);
	}

	/// <summary>
	/// Creates an HSL <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the given float values
	/// </summary>
	/// <param name="hue">Hue, [0,360)</param>
	/// <param name="sat">Saturation, [0,1]</param>
	/// <param name="light">Value, [0,1]</param>
	/// <param name="alpha">Alpha, [0,1]</param>
	/// <returns>HSL <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromHSLf(float hue, float sat, float light, float alpha = 1f)
	{
		return new Color2
		{
			_cType = ColorType.HSL,
			_c1 = ((hue == -1f) ? 0f : float.Clamp(hue % 360f, 0f, 360f)),
			_c2 = float.Clamp(sat, 0f, 1f),
			_c3 = float.Clamp(light, 0f, 1f),
			_alpha = float.Clamp(alpha, 0f, 1f)
		};
	}

	/// <summary>
	/// Creates a CMYK <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the given values
	/// </summary>
	/// <param name="c">Cyan, [0,100]</param>
	/// <param name="m">Magenta, [0,100]</param>
	/// <param name="y">Yellow, [0,100]</param>
	/// <param name="k">Black, [0,100]</param>
	/// <param name="alpha">Cyan, [0,255]</param>
	/// <returns>CMYK <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromCMYK(int c, int m, int y, int k, int alpha = 255)
	{
		return FromCMYKf((float)c / 100f, (float)m / 100f, (float)y / 100f, (float)k / 100f, (float)alpha / 255f);
	}

	/// <summary>
	/// Creates a CMYK <see cref="T:FluentAvalonia.UI.Media.Color2" /> from the given float values
	/// </summary>
	/// <param name="c">Cyan, [0,1]</param>
	/// <param name="m">Magenta, [0,1]</param>
	/// <param name="y">Yellow, [0,1]</param>
	/// <param name="k">Black, [0,1]</param>
	/// <param name="alpha">Cyan, [0,1]</param>
	/// <returns>CMYK <see cref="T:FluentAvalonia.UI.Media.Color2" /></returns>
	public static Color2 FromCMYKf(float c, float m, float y, float k, float alpha = 1f)
	{
		return new Color2
		{
			_cType = ColorType.CMYK,
			_c1 = float.Clamp(c, 0f, 1f),
			_c2 = float.Clamp(m, 0f, 1f),
			_c3 = float.Clamp(y, 0f, 1f),
			_c4 = float.Clamp(k, 0f, 1f),
			_alpha = float.Clamp(alpha, 0f, 1f)
		};
	}

	public Color2 WithHue(int h)
	{
		GetHSV(out var _, out var s, out var v, out var a);
		return FromHSV(h, s, v, a);
	}

	public Color2 WithHuef(float h)
	{
		GetHSVf(out var _, out var s, out var v, out var a);
		return FromHSVf(h, s, v, a);
	}

	public Color2 WithSat(int s)
	{
		GetHSV(out var h, out var _, out var v, out var a);
		return FromHSV(h, s, v, a);
	}

	public Color2 WithSatf(float s)
	{
		GetHSVf(out var h, out var _, out var v, out var a);
		return FromHSVf(h, s, v, a);
	}

	public Color2 WithVal(int v)
	{
		GetHSV(out var h, out var s, out var _, out var a);
		return FromHSV(h, s, v, a);
	}

	public Color2 WithValf(float v)
	{
		GetHSVf(out var h, out var s, out var _, out var a);
		return FromHSVf(h, s, v, a);
	}

	public Color2 WithRed(int r)
	{
		GetRGB(out var _, out var g, out var b, out var a);
		return new Color2((byte)r, g, b, a);
	}

	public Color2 WithRedf(float r)
	{
		GetRGBf(out var _, out var g, out var b, out var a);
		return FromRGBf(r, g, b, a);
	}

	public Color2 WithGreen(int g)
	{
		GetRGB(out var r, out var _, out var b, out var a);
		return new Color2(r, (byte)g, b, a);
	}

	public Color2 WithGreenf(float g)
	{
		GetRGBf(out var r, out var _, out var b, out var a);
		return FromRGBf(r, g, b, a);
	}

	public Color2 WithBlue(int b)
	{
		GetRGB(out var r, out var g, out var _, out var a);
		return new Color2(r, g, (byte)b, a);
	}

	public Color2 WithBluef(float b)
	{
		GetRGBf(out var r, out var g, out var _, out var a);
		return FromRGBf(r, g, b, a);
	}

	public Color2 WithAlpha(int a)
	{
		return WithAlphaf((float)a / 255f);
	}

	public Color2 WithAlphaf(float a)
	{
		return _cType switch
		{
			ColorType.RGB => FromRGBf(Rf, Gf, Bf, a), 
			ColorType.HSV => FromHSVf(Huef, Saturationf, Valuef, a), 
			ColorType.HSL => FromHSLf(Huef, HSLSaturationf, Lightnessf, a), 
			ColorType.CMYK => FromCMYKf(CMYKCyanf, CMYKMagentaf, CMYKYellowf, CMYKBlackf, a), 
			_ => Empty, 
		};
	}

	/// <summary>
	/// Lightens or darkens a color by a specified lightness, converting to an HSL color if necessary
	/// </summary>
	/// <param name="amount">Amount to lighten/darken</param>
	/// <returns>HSL Color2 with the new lightness (old + amount)</returns>
	public Color2 Lighten(float amount)
	{
		if (_cType != ColorType.HSL)
		{
			return ToHSL().Lighten(amount);
		}
		float num = _c3 + amount;
		float.Clamp(num, 0f, 1f);
		return FromHSLf(_c1, _c2, num, _alpha);
	}

	/// <summary>
	/// Lightens or darkens a color by a percentage of the current lightness
	/// </summary>
	/// <param name="percent"></param>
	/// <returns>HSL Color2 with the new lightness (old + (old * percent))</returns>
	public Color2 LightenPercent(float percent)
	{
		if (_cType != ColorType.HSL)
		{
			return ToHSL().LightenPercent(percent);
		}
		float num = ((_c3 < 0.001f) ? percent : (_c3 + _c3 * percent));
		float.Clamp(num, 0f, 1f);
		return FromHSLf(_c1, _c2, num, _alpha);
	}

	public static void HSVToRGB(float hue, float sat, float val, out float r, out float g, out float b)
	{
		r = (g = (b = val));
		if (hue >= 0f && sat >= 0.001f)
		{
			hue = hue / 360f * 6f;
			int num = (int)hue;
			float num2 = val * (1f - sat);
			float num3 = val * (1f - sat * (hue - (float)num));
			float num4 = val * (1f - sat * (1f - (hue - (float)num)));
			switch (num)
			{
			case 0:
				r = val;
				g = num4;
				b = num2;
				break;
			case 1:
				r = num3;
				g = val;
				b = num2;
				break;
			case 2:
				r = num2;
				g = val;
				b = num4;
				break;
			case 3:
				r = num2;
				g = num3;
				b = val;
				break;
			case 4:
				r = num4;
				g = num2;
				b = val;
				break;
			case 5:
				r = val;
				g = num2;
				b = num3;
				break;
			}
		}
	}

	public static void RGBToHSV(float r, float g, float b, out float h, out float s, out float v)
	{
		float num = MathF.Min(r, MathF.Min(g, b));
		float num2 = MathF.Max(r, MathF.Max(g, b));
		float num3 = num2 - num;
		h = 0f;
		s = 0f;
		v = num2;
		if (num3 > 0.001f)
		{
			s = num3 / num2;
			if (MathF.Abs(r - num2) < 0.001f)
			{
				h = (g - b) / num3;
			}
			else if (MathF.Abs(g - num2) < 0.001f)
			{
				h = 2f + (b - r) / num3;
			}
			else
			{
				h = 4f + (r - g) / num3;
			}
			h *= 60f;
		}
		if (h < 0f)
		{
			h += 360f;
		}
		else if (h >= 360f)
		{
			h -= 360f;
		}
	}

	public static void HSLToRGB(float h, float s, float l, out float r, out float g, out float b)
	{
		h /= 360f;
		r = l;
		g = l;
		b = l;
		if (s > 0.001f)
		{
			float num = ((!(l < 0.5f)) ? (l + s - s * l) : (l * (1f + s)));
			float v = 2f * l - num;
			r = HueToRGB(v, num, h + 1f / 3f);
			g = HueToRGB(v, num, h);
			b = HueToRGB(v, num, h - 1f / 3f);
		}
		static float HueToRGB(float num2, float num3, float vH)
		{
			if (vH < 0f)
			{
				vH++;
			}
			if (vH > 1f)
			{
				vH--;
			}
			if (6f * vH < 1f)
			{
				return num2 + (num3 - num2) * 6f * vH;
			}
			if (2f * vH < 1f)
			{
				return num3;
			}
			if (3f * vH < 2f)
			{
				return num2 + (num3 - num2) * (2f / 3f - vH) * 6f;
			}
			return num2;
		}
	}

	public static void RGBToHSL(float r, float g, float b, out float h, out float s, out float l)
	{
		float num = MathF.Min(r, MathF.Min(g, b));
		float num2 = MathF.Max(r, MathF.Max(g, b));
		float num3 = num2 - num;
		h = 0f;
		s = 0f;
		l = (num2 + num) * 0.5f;
		if (num3 > 0.001f)
		{
			if ((double)l < 0.5)
			{
				s = num3 / (num2 + num);
			}
			else
			{
				s = num3 / (2f - num2 - num);
			}
			if (MathF.Abs(r - num2) < 0.001f)
			{
				h = (g - b) / num3;
			}
			else if (MathF.Abs(g - num2) < 0.001f)
			{
				h = 2f + (b - r) / num3;
			}
			else if (MathF.Abs(b - num2) < 0.001f)
			{
				h = 4f + (r - g) / num3;
			}
			h *= 60f;
			if (h < 0f)
			{
				h += 360f;
			}
			else if (h >= 360f)
			{
				h -= 360f;
			}
		}
	}

	public static void HSVToHSL(float hsvSat, float val, out float hslSat, out float l)
	{
		l = val * (1f - hsvSat / 2f);
		hslSat = 0f;
		if (l > 0.001f && MathF.Abs(1f - l) > 0.001f)
		{
			hslSat = 2f * (1f - l / val);
		}
	}

	public static void HSLToHSV(float hslSat, float l, out float hsvSat, out float v)
	{
		v = l + hslSat * MathF.Min(l, 1f - l);
		hsvSat = ((v < 0.001f) ? 0f : (2f * (1f - l / v)));
	}

	public static void RGBToCMYK(float r, float g, float b, out float c, out float m, out float y, out float k)
	{
		c = 1f - r;
		m = 1f - g;
		y = 1f - b;
		k = MathF.Min(c, MathF.Min(m, y));
		c = (c - k) / (1f - k);
		m = (m - k) / (1f - k);
		y = (y - k) / (1f - k);
	}

	public static void CMYKToRGB(float c, float m, float y, float k, out float r, out float g, out float b)
	{
		r = (1f - c) * (1f - k);
		g = (1f - m) * (1f - k);
		b = (1f - y) * (1f - k);
	}

	public static void HSVToUInt(float hue, float sat, float val, out uint num)
	{
		HSVToRGB(hue, sat, val, out var r, out var g, out var b);
		num = 0xFF000000u | ((uint)(r * 255f) << 16) | ((uint)(g * 255f) << 8) | (uint)(b * 255f);
	}
}
