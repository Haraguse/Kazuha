using System;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// Represents a format usable with the clipboard and drag-and-drop.
/// </summary>
public abstract class DataFormat : IEquatable<DataFormat>
{
	/// <summary>
	/// Gets the kind of the data format.
	/// </summary>
	public DataFormatKind Kind { get; }

	/// <summary>
	/// Gets the identifier of the data format.
	/// </summary>
	public string Identifier { get; }

	/// <summary>
	/// Gets a data format representing plain text.
	/// Its data type is <see cref="T:System.String" />.
	/// </summary>
	public static DataFormat<string> Text { get; } = CreateUniversalFormat<string>("Text");

	/// <summary>
	/// Gets a data format representing a bitmap.
	/// Its data type is <see cref="T:Avalonia.Media.Imaging.Bitmap" />.
	/// </summary>
	public static DataFormat<Bitmap> Bitmap { get; } = CreateUniversalFormat<Bitmap>("Bitmap");

	/// <summary>
	/// Gets a data format representing a single file.
	/// Its data type is <see cref="T:Avalonia.Platform.Storage.IStorageItem" />.
	/// </summary>
	public static DataFormat<IStorageItem> File { get; } = CreateUniversalFormat<IStorageItem>("File");

	private protected DataFormat(DataFormatKind kind, string identifier)
	{
		Kind = kind;
		Identifier = identifier;
	}

	/// <summary>
	/// Creates a name for this format, usable by the underlying platform.
	/// </summary>
	/// <param name="applicationPrefix">The system prefix used to recognize the name as an application format.</param>
	/// <returns>A system name for the format.</returns>
	/// <remarks>
	/// This method can only be called if <see cref="P:Avalonia.Input.DataFormat.Kind" /> is
	/// <see cref="F:Avalonia.Input.DataFormatKind.Application" /> or <see cref="F:Avalonia.Input.DataFormatKind.Platform" />.
	/// </remarks>
	public string ToSystemName(string applicationPrefix)
	{
		ThrowHelper.ThrowIfNull(applicationPrefix, "applicationPrefix");
		return Kind switch
		{
			DataFormatKind.Application => applicationPrefix + Identifier, 
			DataFormatKind.Platform => Identifier, 
			_ => throw new InvalidOperationException($"Cannot get system name for {Kind} format {Identifier}"), 
		};
	}

	/// <inheritdoc />
	public bool Equals(DataFormat? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		if (Kind == other.Kind)
		{
			return Identifier == other.Identifier;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		return Equals(obj as DataFormat);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return ((int)Kind * 397) ^ Identifier.GetHashCode();
	}

	/// <summary>
	/// Compares two instances of <see cref="T:Avalonia.Input.DataFormat" /> for equality.
	/// </summary>
	/// <param name="left">The first instance.</param>
	/// <param name="right">The second instance.</param>
	/// <returns>true if the two instances are equal; otherwise false.</returns>
	public static bool operator ==(DataFormat? left, DataFormat? right)
	{
		return object.Equals(left, right);
	}

	/// <summary>
	/// Compares two instances of <see cref="T:Avalonia.Input.DataFormat" /> for inequality.
	/// </summary>
	/// <param name="left">The first instance.</param>
	/// <param name="right">The second instance.</param>
	/// <returns>true if the two instances are not equal; otherwise false.</returns>
	public static bool operator !=(DataFormat? left, DataFormat? right)
	{
		return !object.Equals(left, right);
	}

	private static DataFormat<T> CreateUniversalFormat<T>(string identifier) where T : class
	{
		return new DataFormat<T>(DataFormatKind.Universal, identifier);
	}

	/// <summary>
	/// Creates a new format specific to the application that returns an array of <see cref="T:System.Byte" />.
	/// </summary>
	/// <param name="identifier">
	/// <para>
	/// The format identifier. To avoid conflicts with system identifiers, this value isn't passed to the underlying
	/// platform directly. However, two different applications using the same identifier
	/// with <see cref="M:Avalonia.Input.DataFormat.CreateBytesApplicationFormat(System.String)" /> or <see cref="M:Avalonia.Input.DataFormat.CreateStringApplicationFormat(System.String)" />
	/// are able to share data using this format.
	/// </para>
	/// <para>Only ASCII letters (A-Z, a-z), digits (0-9), the dot (.) and the hyphen (-) are accepted.</para>
	/// </param>
	/// <returns>A new <see cref="T:Avalonia.Input.DataFormat" />.</returns>
	public static DataFormat<byte[]> CreateBytesApplicationFormat(string identifier)
	{
		return CreateApplicationFormat<byte[]>(identifier);
	}

	/// <summary>
	/// Creates a new format specific to the application that returns a <see cref="T:System.String" />.
	/// </summary>
	/// <param name="identifier">
	/// <para>
	/// The format identifier. To avoid conflicts with system identifiers, this value isn't passed to the underlying
	/// platform directly. However, two different applications using the same identifier
	/// with <see cref="M:Avalonia.Input.DataFormat.CreateBytesApplicationFormat(System.String)" /> or <see cref="M:Avalonia.Input.DataFormat.CreateStringApplicationFormat(System.String)" />
	/// are able to share data using this format.
	/// </para>
	/// <para>Only ASCII letters (A-Z, a-z), digits (0-9), the dot (.) and the hyphen (-) are accepted.</para>
	/// </param>
	/// <returns>A new <see cref="T:Avalonia.Input.DataFormat" />.</returns>
	public static DataFormat<string> CreateStringApplicationFormat(string identifier)
	{
		return CreateApplicationFormat<string>(identifier);
	}

	/// <summary>
	/// Creates a new format that stays within the current process and is never serialized to a platform clipboard or drag-and-drop operation.
	/// </summary>
	/// <typeparam name="T">The data type. Can be any reference type.</typeparam>
	/// <param name="identifier">
	/// The format identifier. This value is only used for equality comparisons within the process
	/// and is never passed to the underlying platform.
	/// </param>
	/// <returns>A new <see cref="T:Avalonia.Input.DataFormat`1" />.</returns>
	public static DataFormat<T> CreateInProcessFormat<T>(string identifier) where T : class
	{
		ThrowHelper.ThrowIfNullOrEmpty(identifier, "identifier");
		return new DataFormat<T>(DataFormatKind.InProcess, identifier);
	}

	private static DataFormat<T> CreateApplicationFormat<T>(string identifier) where T : class
	{
		if (!IsValidApplicationFormatIdentifier(identifier))
		{
			throw new ArgumentException("Invalid application identifier", "identifier");
		}
		return new DataFormat<T>(DataFormatKind.Application, identifier);
	}

	/// <summary>
	/// Creates a new format for the current platform that returns an array of <see cref="T:System.Byte" />.
	/// </summary>
	/// <param name="identifier">
	/// The format identifier. This value is not validated and is passed AS IS to the underlying platform.
	/// Most systems use mime types, but macOS requires Uniform Type Identifiers (UTI).
	/// </param>
	/// <returns>A new <see cref="T:Avalonia.Input.DataFormat" />.</returns>
	public static DataFormat<byte[]> CreateBytesPlatformFormat(string identifier)
	{
		return CreatePlatformFormat<byte[]>(identifier);
	}

	/// <summary>
	/// Creates a new format for the current platform that returns a <see cref="T:System.String" />.
	/// </summary>
	/// <param name="identifier">
	/// The format identifier. This value is not validated and is passed AS IS to the underlying platform.
	/// Most systems use mime types, but macOS requires Uniform Type Identifiers (UTI).
	/// </param>
	/// <returns>A new <see cref="T:Avalonia.Input.DataFormat" />.</returns>
	public static DataFormat<string> CreateStringPlatformFormat(string identifier)
	{
		return CreatePlatformFormat<string>(identifier);
	}

	private static DataFormat<T> CreatePlatformFormat<T>(string identifier) where T : class
	{
		ThrowHelper.ThrowIfNullOrEmpty(identifier, "identifier");
		return new DataFormat<T>(DataFormatKind.Platform, identifier);
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Input.DataFormat" /> from a name coming from the underlying platform.
	/// </summary>
	/// <param name="systemName">The name.</param>
	/// <param name="applicationPrefix">The system prefix used to recognize the name as an application format.</param>
	/// <returns>A <see cref="T:Avalonia.Input.DataFormat" /> corresponding to <paramref name="systemName" />.</returns>
	[PrivateApi]
	public static DataFormat<T> FromSystemName<T>(string systemName, string applicationPrefix) where T : class
	{
		ThrowHelper.ThrowIfNull(systemName, "systemName");
		ThrowHelper.ThrowIfNull(applicationPrefix, "applicationPrefix");
		if (systemName.StartsWith(applicationPrefix, StringComparison.OrdinalIgnoreCase))
		{
			string identifier = systemName.Substring(applicationPrefix.Length);
			if (IsValidApplicationFormatIdentifier(identifier))
			{
				return new DataFormat<T>(DataFormatKind.Application, identifier);
			}
		}
		return new DataFormat<T>(DataFormatKind.Platform, systemName);
	}

	private static bool IsValidApplicationFormatIdentifier(string identifier)
	{
		if (string.IsNullOrEmpty(identifier))
		{
			return false;
		}
		for (int i = 0; i < identifier.Length; i++)
		{
			if (!IsValidChar(identifier[i]))
			{
				return false;
			}
		}
		return true;
		static bool IsValidChar(char c)
		{
			if (!char.IsAsciiLetterOrDigit(c) && c != '.')
			{
				return c == '-';
			}
			return true;
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Kind}: {Identifier}";
	}
}
/// <summary>
/// Represents a format usable with the clipboard and drag-and-drop, with a data type.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <remarks>
/// This class cannot be instantiated directly.
/// Use universal formats such as <see cref="P:Avalonia.Input.DataFormat.Text" /> and <see cref="P:Avalonia.Input.DataFormat.File" />,
/// or create custom formats using <see cref="M:Avalonia.Input.DataFormat.CreateBytesApplicationFormat(System.String)" />,
/// <see cref="M:Avalonia.Input.DataFormat.CreateStringApplicationFormat(System.String)" />, <see cref="M:Avalonia.Input.DataFormat.CreateBytesPlatformFormat(System.String)" />,
/// <see cref="M:Avalonia.Input.DataFormat.CreateStringPlatformFormat(System.String)" />,
/// or <see cref="M:Avalonia.Input.DataFormat.CreateInProcessFormat``1(System.String)" />.
/// </remarks>
public sealed class DataFormat<T> : DataFormat where T : class
{
	internal DataFormat(DataFormatKind kind, string identifier)
		: base(kind, identifier)
	{
	}
}
