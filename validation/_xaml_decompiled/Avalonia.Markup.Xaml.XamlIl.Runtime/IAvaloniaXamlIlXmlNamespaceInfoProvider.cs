using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

public interface IAvaloniaXamlIlXmlNamespaceInfoProvider
{
	IReadOnlyDictionary<string, IReadOnlyList<AvaloniaXamlIlXmlNamespaceInfo>> XmlNamespaces { get; }
}
