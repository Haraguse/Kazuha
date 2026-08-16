using System;

namespace Avalonia.Input;

internal class AccessKeyRegistration
{
	private readonly WeakReference<IInputElement> _target;

	public string Key { get; }

	public AccessKeyRegistration(string key, WeakReference<IInputElement> target)
	{
		_target = target;
		Key = key;
	}

	public IInputElement? GetInputElement()
	{
		if (!_target.TryGetTarget(out IInputElement target))
		{
			return null;
		}
		return target;
	}
}
