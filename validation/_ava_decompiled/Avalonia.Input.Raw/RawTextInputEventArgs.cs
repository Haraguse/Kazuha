using Avalonia.Metadata;

namespace Avalonia.Input.Raw;

[PrivateApi]
public class RawTextInputEventArgs : RawInputEventArgs
{
	public string Text { get; }

	public RawTextInputEventArgs(IKeyboardDevice device, ulong timestamp, IInputRoot root, string text)
		: base(device, timestamp, root)
	{
		Text = text;
	}
}
