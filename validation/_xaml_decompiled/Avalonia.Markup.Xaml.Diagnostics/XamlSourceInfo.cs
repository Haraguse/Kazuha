using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.Diagnostics;

/// <summary>
/// Represents source location information for an element within a XAML or code file.
/// </summary>
public record XamlSourceInfo
{
	/// <summary>
	/// Gets the full path of the source file containing the element, or <c>null</c> if unavailable.
	/// </summary>
	public Uri? SourceUri { get; }

	/// <summary>
	/// Gets the 1-based line number in the source file where the element is defined.
	/// </summary>
	public int LineNumber { get; }

	/// <summary>
	/// Gets the 1-based column number in the source file where the element is defined.
	/// </summary>
	public int LinePosition { get; }

	private static readonly AttachedProperty<XamlSourceInfo?> s_xamlSourceInfo = AvaloniaProperty.RegisterAttached<AvaloniaObject, XamlSourceInfo>("XamlSourceInfo", typeof(XamlSourceInfo));

	private static readonly ConditionalWeakTable<object, XamlSourceInfo?> s_sourceInfo = new ConditionalWeakTable<object, XamlSourceInfo>();

	private static readonly ConditionalWeakTable<IResourceDictionary, Dictionary<object, XamlSourceInfo?>> s_keyedSourceInfo = new ConditionalWeakTable<IResourceDictionary, Dictionary<object, XamlSourceInfo>>();

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo" /> class
	/// with a specified line, column, and file path.
	/// </summary>
	/// <param name="line">The line number of the source element.</param>
	/// <param name="column">The column number of the source element.</param>
	/// <param name="filePath">The full path of the source file.</param>
	public XamlSourceInfo(int line, int column, string? filePath)
	{
		LineNumber = line;
		LinePosition = column;
		SourceUri = ((filePath != null) ? new UriBuilder("file", "")
		{
			Path = filePath
		}.Uri : null);
	}

	/// <summary>
	/// Associates XAML source information with the specified object for debugging or diagnostic purposes.
	/// </summary>
	/// <remarks>This method is typically used to enable enhanced debugging or diagnostics by tracking
	/// the origin of XAML elements at runtime. If the same object is passed multiple times, the most recent source
	/// information will overwrite any previous value.</remarks>
	/// <param name="obj">The object to associate with the XAML source information. Cannot be null.</param>
	/// <param name="info">The XAML source information to associate with the object, or null to remove any existing association.</param>
	public static void SetXamlSourceInfo(object obj, XamlSourceInfo? info)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (obj is AvaloniaObject avaloniaObject)
		{
			avaloniaObject.SetValue(s_xamlSourceInfo, info);
		}
		else
		{
			s_sourceInfo.AddOrUpdate(obj, info);
		}
	}

	/// <summary>
	/// Associates XAML source information with the specified key in the given resource dictionary.
	/// </summary>
	/// <param name="dictionary"> The resource dictionary to associate with the XAML source information.</param>
	/// <param name="key">The key associated with the source info.</param>
	/// <param name="info">The XAML source information to associate with the object, or null to remove any existing association.</param>
	public static void SetXamlSourceInfo(IResourceDictionary dictionary, object key, XamlSourceInfo? info)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		Dictionary<object, XamlSourceInfo> orCreateValue = s_keyedSourceInfo.GetOrCreateValue(dictionary);
		if (info == null)
		{
			orCreateValue.Remove(key);
		}
		else
		{
			orCreateValue[key] = info;
		}
	}

	/// <summary>
	/// Retrieves the XAML source information associated with the specified object, if available.
	/// </summary>
	/// <param name="obj">The object for which to obtain XAML source information. Cannot be null.</param>
	/// <returns>A <see cref="T:Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo" /> instance containing the XAML source information for the specified object, or
	/// <see langword="null" /> if no source information is available.</returns>
	public static XamlSourceInfo? GetXamlSourceInfo(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (obj is AvaloniaObject avaloniaObject)
		{
			return avaloniaObject.GetValue(s_xamlSourceInfo);
		}
		s_sourceInfo.TryGetValue(obj, out XamlSourceInfo value);
		return value;
	}

	/// <summary>
	/// Retrieves the XAML source information associated with the specified key in the given resource dictionary, if available.
	/// </summary>
	/// <param name="dictionary"> The resource dictionary associated with the XAML source information.</param>
	/// <param name="key">The key associated with the source info.</param>
	/// <returns>A <see cref="T:Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo" /> instance containing the XAML source information for the specified key, or
	/// <see langword="null" /> if no source information is available.</returns>
	public static XamlSourceInfo? GetXamlSourceInfo(IResourceDictionary dictionary, object key)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		if (s_keyedSourceInfo.TryGetValue(dictionary, out Dictionary<object, XamlSourceInfo> value) && value.TryGetValue(key, out var value2))
		{
			return value2;
		}
		return null;
	}

	/// <summary>
	/// Returns a string that represents the current <see cref="T:Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo" />.
	/// </summary>
	/// <returns>
	/// A formatted string in the form <c>"FilePath:Line,Column"</c>,
	/// or <c>"(unknown):Line,Column"</c> if the file path is not set.
	/// </returns>
	public override string ToString()
	{
		string value = SourceUri?.LocalPath ?? "(unknown)";
		return $"{value}:{LineNumber},{LinePosition}";
	}
}
