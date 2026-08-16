namespace Avalonia.Input;

/// <summary>
/// Represents a keyboard physical key.<br />
/// </summary>
/// <remarks>
/// The names follow the W3C codes: https://www.w3.org/TR/uievents-code/
/// </remarks>
public enum PhysicalKey
{
	/// <summary>
	/// Represents no key.
	/// </summary>
	None,
	/// <summary>
	/// <c>`~</c> on a US keyboard.
	/// This is the <c>半角/全角/漢字</c> (hankaku/zenkaku/kanji) key on Japanese keyboards.
	/// </summary>
	Backquote,
	/// <summary>
	/// Used for both the US <c>\|</c> (on the 101-key layout) and also for the key located between the <c>"</c> and
	/// <c>Enter</c> keys on row C of the 102-, 104- and 106-key layouts.
	/// <c>#~</c> on a UK (102) keyboard.
	/// </summary>
	Backslash,
	/// <summary>
	/// <c>[{</c> on a US keyboard.
	/// </summary>
	BracketLeft,
	/// <summary>
	/// <c>]}</c> on a US keyboard.
	/// </summary>
	BracketRight,
	/// <summary>
	/// <c>,&lt;</c> on a US keyboard.
	/// </summary>
	Comma,
	/// <summary>
	/// <c>0)</c> on a US keyboard.
	/// </summary>
	Digit0,
	/// <summary>
	/// <c>1!</c> on a US keyboard.
	/// </summary>
	Digit1,
	/// <summary>
	/// <c>2@</c> on a US keyboard.
	/// </summary>
	Digit2,
	/// <summary>
	/// <c>3#</c> on a US keyboard.
	/// </summary>
	Digit3,
	/// <summary>
	/// <c>4$</c> on a US keyboard.
	/// </summary>
	Digit4,
	/// <summary>
	/// <c>5%</c> on a US keyboard.
	/// </summary>
	Digit5,
	/// <summary>
	/// <c>6^</c> on a US keyboard.
	/// </summary>
	Digit6,
	/// <summary>
	/// <c>7&amp;</c> on a US keyboard.
	/// </summary>
	Digit7,
	/// <summary>
	/// <c>8*</c> on a US keyboard.
	/// </summary>
	Digit8,
	/// <summary>
	/// <c>9(</c> on a US keyboard.
	/// </summary>
	Digit9,
	/// <summary>
	/// <c>=+</c> on a US keyboard.
	/// </summary>
	Equal,
	/// <summary>
	/// Located between the left <c>Shift</c> and <c>Z</c> keys.
	/// <c>\|</c> on a UK keyboard.
	/// </summary>
	IntlBackslash,
	/// <summary>
	/// Located between the <c>/</c> and right <c>Shift</c> keys.
	/// <c>\ろ</c> (ro) on a Japanese keyboard.
	/// </summary>
	IntlRo,
	/// <summary>
	/// Located between the <c>=</c> and <c>Backspace</c> keys.
	/// <c>¥</c> (yen) on a Japanese keyboard.
	/// <c>\/</c> on a Russian keyboard.
	/// </summary>
	IntlYen,
	/// <summary>
	/// <c>a</c> on a US keyboard.
	/// <c>q</c> on an AZERTY (e.g., French) keyboard.
	/// </summary>
	A,
	/// <summary>
	/// <c>b</c> on a US keyboard.
	/// </summary>
	B,
	/// <summary>
	/// <c>c</c> on a US keyboard.
	/// </summary>
	C,
	/// <summary>
	/// <c>d</c> on a US keyboard.
	/// </summary>
	D,
	/// <summary>
	/// <c>e</c> on a US keyboard.
	/// </summary>
	E,
	/// <summary>
	/// <c>f</c> on a US keyboard.
	/// </summary>
	F,
	/// <summary>
	/// <c>g</c> on a US keyboard.
	/// </summary>
	G,
	/// <summary>
	/// <c>h</c> on a US keyboard.
	/// </summary>
	H,
	/// <summary>
	/// <c>i</c> on a US keyboard.
	/// </summary>
	I,
	/// <summary>
	/// <c>j</c> on a US keyboard.
	/// </summary>
	J,
	/// <summary>
	/// <c>k</c> on a US keyboard.
	/// </summary>
	K,
	/// <summary>
	/// <c>l</c> on a US keyboard.
	/// </summary>
	L,
	/// <summary>
	/// <c>m</c> on a US keyboard.
	/// </summary>
	M,
	/// <summary>
	/// <c>n</c> on a US keyboard.
	/// </summary>
	N,
	/// <summary>
	/// <c>o</c> on a US keyboard.
	/// </summary>
	O,
	/// <summary>
	/// <c>p</c> on a US keyboard.
	/// </summary>
	P,
	/// <summary>
	/// <c>q</c> on a US keyboard.
	/// <c>a</c> on an AZERTY (e.g., French) keyboard.
	/// </summary>
	Q,
	/// <summary>
	/// <c>r</c> on a US keyboard.
	/// </summary>
	R,
	/// <summary>
	/// <c>s</c> on a US keyboard.
	/// </summary>
	S,
	/// <summary>
	/// <c>t</c> on a US keyboard.
	/// </summary>
	T,
	/// <summary>
	/// <c>u</c> on a US keyboard.
	/// </summary>
	U,
	/// <summary>
	/// <c>v</c> on a US keyboard.
	/// </summary>
	V,
	/// <summary>
	/// <c>w</c> on a US keyboard.
	/// <c>z</c> on an AZERTY (e.g., French) keyboard.
	/// </summary>
	W,
	/// <summary>
	/// <c>x</c> on a US keyboard.
	/// </summary>
	X,
	/// <summary>
	/// <c>y</c> on a US keyboard.
	/// <c>z</c> on a QWERTZ (e.g., German) keyboard.
	/// </summary>
	Y,
	/// <summary>
	/// <c>z</c> on a US keyboard.
	/// <c>w</c> on an AZERTY (e.g., French) keyboard.
	/// <c>y</c> on a QWERTZ (e.g., German) keyboard.
	/// </summary>
	Z,
	/// <summary>
	/// <c>-_</c> on a US keyboard.
	/// </summary>
	Minus,
	/// <summary>
	/// <c>.&gt;</c> on a US keyboard.
	/// </summary>
	Period,
	/// <summary>
	/// <c>'"</c> on a US keyboard.
	/// </summary>
	Quote,
	/// <summary>
	/// <c>;:</c> on a US keyboard.
	/// </summary>
	Semicolon,
	/// <summary>
	/// <c>/?</c> on a US keyboard.
	/// </summary>
	Slash,
	/// <summary>
	/// <c>Alt</c>, <c>Option</c> or <c>⌥</c>.
	/// </summary>
	AltLeft,
	/// <summary>
	/// <c>Alt</c>, <c>Option</c> or <c>⌥</c>.
	/// This is labelled <c>AltGr</c> key on many keyboard layouts.
	/// </summary>
	AltRight,
	/// <summary>
	/// <c>Backspace</c> or <c>⌫</c>.
	/// Labelled <c>Delete</c> on Apple keyboards.
	/// </summary>
	Backspace,
	/// <summary>
	/// <c>CapsLock</c> or <c>⇪</c>.
	/// </summary>
	CapsLock,
	/// <summary>
	/// The application context menu key, which is typically found between the right <c>Meta</c> key
	/// and the right <c>Control</c> key.
	/// </summary>
	ContextMenu,
	/// <summary>
	/// <c>Control</c> or <c>⌃</c>.
	/// </summary>
	ControlLeft,
	/// <summary>
	/// <c>Control</c> or <c>⌃</c>.
	/// </summary>
	ControlRight,
	/// <summary>
	/// <c>Enter</c> or <c>↵</c>.
	/// Labelled <c>Return</c> on Apple keyboards.
	/// </summary>
	Enter,
	/// <summary>
	/// The <c>⊞</c> (Windows), <c>⌘</c>, <c>Command</c> or other OS symbol key.
	/// </summary>
	MetaLeft,
	/// <summary>
	/// The <c>⊞</c> (Windows), <c>⌘</c>, <c>Command</c> or other OS symbol key.
	/// </summary>
	MetaRight,
	/// <summary>
	/// <c>Shift</c> or <c>⇧</c>.
	/// </summary>
	ShiftLeft,
	/// <summary>
	/// <c>Shift</c> or <c>⇧</c>.
	/// </summary>
	ShiftRight,
	/// <summary>
	/// <c> </c> (space).
	/// </summary>
	Space,
	/// <summary>
	/// <c>Tab</c> or <c>⇥</c>.
	/// </summary>
	Tab,
	/// <summary>
	/// Japanese: <c>変換</c> (henkan).
	/// </summary>
	Convert,
	/// <summary>
	/// Japanese: <c>カタカナ/ひらがな/ローマ字</c> (katakana/hiragana/romaji).
	/// </summary>
	KanaMode,
	/// <summary>
	/// Korean: HangulMode <c>한/영</c> (han/yeong).
	/// Japanese (Mac keyboard): <c>かな</c> (kana).
	/// </summary>
	Lang1,
	/// <summary>
	/// Korean: Hanja <c>한자</c> (hanja).
	/// Japanese (Mac keyboard): <c>英数</c> (eisu).
	/// </summary>
	Lang2,
	/// <summary>
	/// Japanese (word-processing keyboard): Katakana.
	/// </summary>
	Lang3,
	/// <summary>
	/// Japanese (word-processing keyboard): Hiragana.
	/// </summary>
	Lang4,
	/// <summary>
	/// Japanese (word-processing keyboard): Zenkaku/Hankaku.
	/// </summary>
	Lang5,
	/// <summary>
	/// Japanese: <c>無変換</c> (muhenkan).
	/// </summary>
	NonConvert,
	/// <summary>
	/// <c>⌦</c>. The forward delete key.
	/// Note that on Apple keyboards, the key labelled <c>Delete</c> on the main part of the keyboard is
	/// <see cref="F:Avalonia.Input.PhysicalKey.Backspace" />.
	/// </summary>
	Delete,
	/// <summary>
	/// <c>End</c> or <c>↘</c>.
	/// </summary>
	End,
	/// <summary>
	/// <c>Help</c>.
	/// Not present on standard PC keyboards.
	/// </summary>
	Help,
	/// <summary>
	/// <c>Home</c> or <c>↖</c>.
	/// </summary>
	Home,
	/// <summary>
	/// <c>Insert</c> or <c>Ins</c>.
	/// Not present on Apple keyboards.
	/// </summary>
	Insert,
	/// <summary>
	/// <c>Page Down</c>, <c>PgDn</c> or <c>⇟</c>.
	/// </summary>
	PageDown,
	/// <summary>
	/// <c>Page Up</c>, <c>PgUp</c> or <c>⇞</c>.
	/// </summary>
	PageUp,
	/// <summary>
	/// <c>↓</c>.
	/// </summary>
	ArrowDown,
	/// <summary>
	/// <c>←</c>.
	/// </summary>
	ArrowLeft,
	/// <summary>
	/// <c>→</c>.
	/// </summary>
	ArrowRight,
	/// <summary>
	/// <c>↑</c>.
	/// </summary>
	ArrowUp,
	/// <summary>
	/// Numeric keypad <c>Num Lock</c>.
	/// On the Mac, this is used for the numpad <c>Clear</c> key.
	/// </summary>
	NumLock,
	/// <summary>
	/// Numeric keypad <c>0 Ins</c> on a keyboard.
	/// <c>0</c> on a phone or remote control.
	/// </summary>
	NumPad0,
	/// <summary>
	/// Numeric keypad <c>1 End</c> on a keyboard.
	/// <c>1</c> or <c>1 QZ</c> on a phone or remote control.
	/// </summary>
	NumPad1,
	/// <summary>
	/// Numeric keypad <c>2 ↓</c> on a keyboard.
	/// <c>2 ABC</c> on a phone or remote control.
	/// </summary>
	NumPad2,
	/// <summary>
	/// Numeric keypad <c>3 PgDn</c> on a keyboard.
	/// <c>3 DEF</c> on a phone or remote control.
	/// </summary>
	NumPad3,
	/// <summary>
	/// Numeric keypad <c>4 ←</c> on a keyboard.
	/// <c>4 GHI</c> on a phone or remote control.
	/// </summary>
	NumPad4,
	/// <summary>
	/// Numeric keypad <c>5</c> on a keyboard.
	/// <c>5 JKL</c> on a phone or remote control.
	/// </summary>
	NumPad5,
	/// <summary>
	/// Numeric keypad <c>6 →</c> on a keyboard.
	/// <c>6 MNO</c> on a phone or remote control.
	/// </summary>
	NumPad6,
	/// <summary>
	/// Numeric keypad <c>7 Home</c> on a keyboard.
	/// <c>7 PQRS</c> or <c>7 PRS</c> on a phone or remote control.
	/// </summary>
	NumPad7,
	/// <summary>
	/// Numeric keypad <c>8 ↑</c> on a keyboard.
	/// <c>8 TUV</c> on a phone or remote control.
	/// </summary>
	NumPad8,
	/// <summary>
	/// Numeric keypad <c>9 PgUp</c> on a keyboard.
	/// <c>9 WXYZ</c> or <c>9 WXY</c> on a phone or remote control.
	/// </summary>
	NumPad9,
	/// <summary>
	/// Numeric keypad <c>+</c>.
	/// </summary>
	NumPadAdd,
	/// <summary>
	/// Numeric keypad <c>C</c> or <c>AC</c> (All Clear).
	/// Also for use with numpads that have a <c>Clear</c> key that is separate from the <c>NumLock</c> key.
	/// On the Mac, the numpad <c>Clear</c> key is <see cref="F:Avalonia.Input.PhysicalKey.NumLock" />.
	/// </summary>
	NumPadClear,
	/// <summary>
	/// Numeric keypad <c>,</c> (thousands separator).
	/// For locales where the thousands separator is a "." (e.g., Brazil), this key may generate a <c>.</c>.
	/// </summary>
	NumPadComma,
	/// <summary>
	/// Numeric keypad <c>. Del</c>.
	/// For locales where the decimal separator is "," (e.g., Brazil), this key may generate a <c>,</c>.
	/// </summary>
	NumPadDecimal,
	/// <summary>
	/// Numeric keypad <c>/</c>.
	/// </summary>
	NumPadDivide,
	/// <summary>
	/// Numeric keypad <c>Enter</c>.
	/// </summary>
	NumPadEnter,
	/// <summary>
	/// Numeric keypad <c>=</c>.
	/// </summary>
	NumPadEqual,
	/// <summary>
	/// Numeric keypad <c>*</c> on a keyboard.
	/// For use with numpads that provide mathematical operations (<c>+</c>, <c>-</c>, <c>*</c> and <c>/</c>).
	/// </summary>
	NumPadMultiply,
	/// <summary>
	/// Numeric keypad <c>(</c>.
	/// Found on the Microsoft Natural Keyboard.
	/// </summary>
	NumPadParenLeft,
	/// <summary>
	/// Numeric keypad <c>)</c>.
	/// Found on the Microsoft Natural Keyboard.
	/// </summary>
	NumPadParenRight,
	/// <summary>
	/// Numeric keypad <c>-</c>.
	/// </summary>
	NumPadSubtract,
	/// <summary>
	/// <c>Esc</c> or <c>⎋</c>.
	/// </summary>
	Escape,
	/// <summary>
	/// <c>F1</c>.
	/// </summary>
	F1,
	/// <summary>
	/// <c>F2</c>.
	/// </summary>
	F2,
	/// <summary>
	/// <c>F3</c>.
	/// </summary>
	F3,
	/// <summary>
	/// <c>F4</c>.
	/// </summary>
	F4,
	/// <summary>
	/// <c>F5</c>.
	/// </summary>
	F5,
	/// <summary>
	/// <c>F6</c>.
	/// </summary>
	F6,
	/// <summary>
	/// <c>F7</c>.
	/// </summary>
	F7,
	/// <summary>
	/// <c>F8</c>.
	/// </summary>
	F8,
	/// <summary>
	/// <c>F9</c>.
	/// </summary>
	F9,
	/// <summary>
	/// <c>F10</c>.
	/// </summary>
	F10,
	/// <summary>
	/// <c>F11</c>.
	/// </summary>
	F11,
	/// <summary>
	/// <c>F12</c>.
	/// </summary>
	F12,
	/// <summary>
	/// <c>F13</c>.
	/// </summary>
	F13,
	/// <summary>
	/// <c>F14</c>.
	/// </summary>
	F14,
	/// <summary>
	/// <c>F15</c>.
	/// </summary>
	F15,
	/// <summary>
	/// <c>F16</c>.
	/// </summary>
	F16,
	/// <summary>
	/// <c>F17</c>.
	/// </summary>
	F17,
	/// <summary>
	/// <c>F18</c>.
	/// </summary>
	F18,
	/// <summary>
	/// <c>F19</c>.
	/// </summary>
	F19,
	/// <summary>
	/// <c>F20</c>.
	/// </summary>
	F20,
	/// <summary>
	/// <c>F21</c>.
	/// </summary>
	F21,
	/// <summary>
	/// <c>F22</c>.
	/// </summary>
	F22,
	/// <summary>
	/// <c>F23</c>.
	/// </summary>
	F23,
	/// <summary>
	/// <c>F24</c>.
	/// </summary>
	F24,
	/// <summary>
	/// <c>PrtScr SysRq</c> or <c>Print Screen</c>.
	/// </summary>
	PrintScreen,
	/// <summary>
	/// <c>Scroll Lock</c>.
	/// </summary>
	ScrollLock,
	/// <summary>
	/// <c>Pause Break</c>.
	/// </summary>
	Pause,
	/// <summary>
	/// Browser <c>Back</c>.
	/// Some laptops place this key to the left of the <c>↑</c> key.
	/// </summary>
	BrowserBack,
	/// <summary>
	/// Browser <c>Favorites</c>.
	/// </summary>
	BrowserFavorites,
	/// <summary>
	/// Browser <c>Forward</c>.
	/// Some laptops place this key to the right of the <c>↑</c> key.
	/// </summary>
	BrowserForward,
	/// <summary>
	/// Browser <c>Home</c>.
	/// </summary>
	BrowserHome,
	/// <summary>
	/// Browser <c>Refresh</c>.
	/// </summary>
	BrowserRefresh,
	/// <summary>
	/// Browser <c>Search</c>.
	/// </summary>
	BrowserSearch,
	/// <summary>
	/// Browser <c>Stop</c>.
	/// </summary>
	BrowserStop,
	/// <summary>
	/// <c>Eject</c> or <c>⏏</c>.
	/// This key is placed in the function section on some Apple keyboards.
	/// </summary>
	Eject,
	/// <summary>
	/// <c>App 1</c>.
	/// Sometimes labelled <c>My Computer</c> on the keyboard.
	/// </summary>
	LaunchApp1,
	/// <summary>
	/// <c>App 2</c>.
	/// Sometimes labelled <c>Calculator</c> on the keyboard.
	/// </summary>
	LaunchApp2,
	/// <summary>
	/// <c>Mail</c>.
	/// </summary>
	LaunchMail,
	/// <summary>
	/// Media <c>Play/Pause</c> or <c>⏵⏸</c>.
	/// </summary>
	MediaPlayPause,
	/// <summary>
	/// Media <c>Select</c>.
	/// </summary>
	MediaSelect,
	/// <summary>
	/// Media <c>Stop</c> or <c>⏹</c>.
	/// </summary>
	MediaStop,
	/// <summary>
	/// Media <c>Next</c> or <c>⏭</c>.
	/// </summary>
	MediaTrackNext,
	/// <summary>
	/// Media <c>Previous</c> or <c>⏮</c>.
	/// </summary>
	MediaTrackPrevious,
	/// <summary>
	/// <c>Power</c>.
	/// </summary>
	Power,
	/// <summary>
	/// <c>Sleep</c>.
	/// </summary>
	Sleep,
	/// <summary>
	/// <c>Volume Down</c>.
	/// </summary>
	AudioVolumeDown,
	/// <summary>
	/// <c>Mute</c>.
	/// </summary>
	AudioVolumeMute,
	/// <summary>
	/// <c>Volume Up</c>.
	/// </summary>
	AudioVolumeUp,
	/// <summary>
	/// <c>Wake Up</c>.
	/// </summary>
	WakeUp,
	/// <summary>
	/// <c>Again</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Again,
	/// <summary>
	/// <c>Copy</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Copy,
	/// <summary>
	/// <c>Cut</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Cut,
	/// <summary>
	/// <c>Find</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Find,
	/// <summary>
	/// <c>Open</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Open,
	/// <summary>
	/// <c>Paste</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Paste,
	/// <summary>
	/// <c>Props</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Props,
	/// <summary>
	/// <c>Select</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Select,
	/// <summary>
	/// <c>Undo</c>.
	/// Legacy.
	/// Found on Sun’s USB keyboard.
	/// </summary>
	Undo
}
