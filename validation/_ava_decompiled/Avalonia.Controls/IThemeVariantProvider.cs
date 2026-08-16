using Avalonia.Styling;

namespace Avalonia.Controls;

/// <summary>
/// Resource provider with theme variant awareness.
/// Can be used with <see cref="P:Avalonia.Controls.IResourceDictionary.ThemeDictionaries" />.
/// </summary>
/// <remarks>
/// This is a helper interface for the XAML compiler to make Key property accessibly by the markup extensions.
/// Which means, it can only be used with ResourceDictionaries and markup extensions in the XAML code.
/// </remarks>
public interface IThemeVariantProvider : IResourceProvider, IResourceNode
{
	/// <summary>
	/// Key property set by the compiler.
	/// </summary>
	ThemeVariant? Key { get; set; }
}
