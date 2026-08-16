using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Avalonia.Platform.Storage.FileIO;

/// <summary>
/// In order to have unique bookmarks across platforms, we prepend a platform specific suffix before native bookmark.
/// And always encoding them in base64 before returning to the user.
/// </summary>
/// <remarks>
/// Bookmarks are encoded as:
/// 0-6 - avalonia prefix with version number
/// 7-15  - platform key
/// 16+ - native bookmark value
/// Which is then encoded in Base64.
/// </remarks>
internal static class StorageBookmarkHelper
{
	public enum DecodeResult
	{
		Success,
		InvalidFormat,
		InvalidPlatform
	}

	private const int HeaderLength = 16;

	private static ReadOnlySpan<byte> AvaHeaderPrefix => "ava.v1."u8;

	private static ReadOnlySpan<byte> FakeBclBookmarkPlatform => "bcl"u8;

	[return: NotNullIfNotNull("nativeBookmark")]
	public static string? EncodeBookmark(ReadOnlySpan<byte> platform, string? nativeBookmark)
	{
		if (nativeBookmark != null)
		{
			return EncodeBookmark(platform, Encoding.UTF8.GetBytes(nativeBookmark));
		}
		return null;
	}

	public static string? EncodeBookmark(ReadOnlySpan<byte> platform, ReadOnlySpan<byte> nativeBookmarkBytes)
	{
		if (nativeBookmarkBytes.Length == 0)
		{
			return null;
		}
		if (platform.Length > 16)
		{
			throw new ArgumentException($"Platform name should not be longer than {16} bytes", "platform");
		}
		int num = 16 + nativeBookmarkBytes.Length;
		byte[] array = ArrayPool<byte>.Shared.Rent(num);
		try
		{
			Span<byte> span = array.AsSpan(0, num);
			span.Clear();
			AvaHeaderPrefix.CopyTo(span);
			platform.CopyTo(span.Slice(AvaHeaderPrefix.Length));
			nativeBookmarkBytes.CopyTo(span.Slice(16));
			return Convert.ToBase64String(span);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public static DecodeResult TryDecodeBookmark(ReadOnlySpan<byte> platform, string? base64bookmark, out byte[]? nativeBookmark)
	{
		if (platform.Length > 16 || platform.Length == 0 || base64bookmark == null || base64bookmark.Length % 4 != 0)
		{
			nativeBookmark = null;
			return DecodeResult.InvalidFormat;
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(16 + base64bookmark.Length * 6);
		if (Convert.TryFromBase64Chars(base64bookmark.AsSpan(), array, out var bytesWritten))
		{
			Span<byte> span = array.AsSpan().Slice(0, bytesWritten);
			try
			{
				if (span.Length < 16 && !AvaHeaderPrefix.SequenceEqual(span.Slice(0, AvaHeaderPrefix.Length)))
				{
					nativeBookmark = null;
					return DecodeResult.InvalidFormat;
				}
				if (!((ReadOnlySpan<byte>)span.Slice(AvaHeaderPrefix.Length, platform.Length)).SequenceEqual(platform))
				{
					nativeBookmark = null;
					return DecodeResult.InvalidPlatform;
				}
				nativeBookmark = span.Slice(16).ToArray();
				return DecodeResult.Success;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
		nativeBookmark = null;
		return DecodeResult.InvalidFormat;
	}

	public static string EncodeBclBookmark(string localPath)
	{
		return EncodeBookmark(FakeBclBookmarkPlatform, localPath);
	}

	public static bool TryDecodeBclBookmark(string nativeBookmark, [NotNullWhen(true)] out string? localPath)
	{
		byte[] nativeBookmark2;
		switch (TryDecodeBookmark(FakeBclBookmarkPlatform, nativeBookmark, out nativeBookmark2))
		{
		case DecodeResult.Success:
			localPath = Encoding.UTF8.GetString(nativeBookmark2);
			return true;
		case DecodeResult.InvalidFormat:
			if (nativeBookmark.IndexOfAny(Path.GetInvalidPathChars()) < 0 && !string.IsNullOrEmpty(Path.GetDirectoryName(nativeBookmark)))
			{
				localPath = nativeBookmark;
				return true;
			}
			break;
		}
		localPath = null;
		return false;
	}
}
