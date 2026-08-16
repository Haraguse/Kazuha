using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

/// <summary>
/// Wraps a <see cref="T:Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlParentStackProvider" /> into a <see cref="T:Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlEagerParentStackProvider" />,
/// for backwards compatibility.
/// </summary>
internal sealed class XamlIlParentStackProviderWrapper : IAvaloniaXamlIlEagerParentStackProvider, IAvaloniaXamlIlParentStackProvider
{
	private readonly IAvaloniaXamlIlParentStackProvider _provider;

	private IReadOnlyList<object>? _directParentsStack;

	public IEnumerable<object> Parents => _provider.Parents;

	public IReadOnlyList<object> DirectParentsStack => _directParentsStack ?? (_directParentsStack = _provider.Parents.Reverse().ToArray());

	public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider => null;

	public XamlIlParentStackProviderWrapper(IAvaloniaXamlIlParentStackProvider provider)
	{
		_provider = provider;
	}
}
