using System.Collections;
using System.Collections.Generic;
using Avalonia.Styling;

namespace Avalonia.Controls;

/// <summary>
/// An indexed dictionary of resources.
/// </summary>
public interface IResourceDictionary : IResourceProvider, IResourceNode, IDictionary<object, object?>, ICollection<KeyValuePair<object, object?>>, IEnumerable<KeyValuePair<object, object?>>, IEnumerable
{
	/// <summary>
	/// Gets a collection of child resource dictionaries.
	/// </summary>
	IList<IResourceProvider> MergedDictionaries { get; }

	/// <summary>
	/// Gets a collection of merged resource dictionaries that are specifically keyed and composed to address theme scenarios.
	/// </summary>
	IDictionary<ThemeVariant, IThemeVariantProvider> ThemeDictionaries { get; }
}
