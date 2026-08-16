using System;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal interface IUISettings : IInspectable, IUnknown, IDisposable
{
	HandPreference HandPreference { get; }

	WinRTSize CursorSize { get; }

	WinRTSize ScrollBarSize { get; }

	WinRTSize ScrollBarArrowSize { get; }

	WinRTSize ScrollBarThumbBoxSize { get; }

	uint MessageDuration { get; }

	int AnimationsEnabled { get; }

	int CaretBrowsingEnabled { get; }

	uint CaretBlinkRate { get; }

	uint CaretWidth { get; }

	uint DoubleClickTime { get; }

	uint MouseHoverTime { get; }

	WinRTColor UIElementColor(UIElementType desiredElement);
}
