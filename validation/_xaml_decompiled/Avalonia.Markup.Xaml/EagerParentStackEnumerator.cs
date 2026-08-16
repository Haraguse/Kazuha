using System.Collections.Generic;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace Avalonia.Markup.Xaml;

internal struct EagerParentStackEnumerator(IAvaloniaXamlIlEagerParentStackProvider? provider)
{
	private IAvaloniaXamlIlEagerParentStackProvider? _provider = provider;

	private IReadOnlyList<object>? _currentParentsStack = null;

	private int _currentIndex = 0;

	public object? TryGetNext()
	{
		while (_provider != null)
		{
			if (_currentParentsStack == null)
			{
				_currentParentsStack = _provider.DirectParentsStack;
				_currentIndex = _currentParentsStack.Count;
			}
			_currentIndex--;
			if (_currentIndex >= 0)
			{
				return _currentParentsStack[_currentIndex];
			}
			_currentParentsStack = null;
			_provider = _provider.ParentProvider;
		}
		return null;
	}

	public T? TryGetNextOfType<T>() where T : class
	{
		while (true)
		{
			object obj = TryGetNext();
			if (obj == null)
			{
				break;
			}
			if (obj is T result)
			{
				return result;
			}
		}
		return null;
	}
}
