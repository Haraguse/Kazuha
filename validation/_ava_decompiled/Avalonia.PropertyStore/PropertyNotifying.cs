using System;

namespace Avalonia.PropertyStore;

/// <summary>
/// Raises <see cref="P:Avalonia.AvaloniaProperty.Notifying" /> where necessary.
/// </summary>
/// <remarks>
/// Uses the disposable pattern to ensure that the closing Notifying call is made even in the
/// presence of exceptions. 
/// </remarks>
internal struct PropertyNotifying : IDisposable
{
	private readonly AvaloniaObject? _owner;

	private Action<AvaloniaObject, bool>? _notifying;

	private PropertyNotifying(AvaloniaObject owner, Action<AvaloniaObject, bool>? notifying)
	{
		_owner = owner;
		_notifying = notifying;
		notifying?.Invoke(owner, arg2: true);
	}

	public void Dispose()
	{
		if (_notifying != null)
		{
			_notifying(_owner, arg2: false);
			_notifying = null;
		}
	}

	public static PropertyNotifying Start(AvaloniaObject owner, AvaloniaProperty property)
	{
		return new PropertyNotifying(owner, property.Notifying);
	}
}
