import sys
import winreg

def get_system_fonts():
    if sys.platform != "win32":
        print("Not win32")
        return []
    try:
        keys = [
            (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
            (winreg.HKEY_CURRENT_USER, r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
        ]
        fonts = set()
        suffixes = (" (TrueType)", " (OpenType)", " (Type 1)", " (All res)")
        for root, path in keys:
            try:
                with winreg.OpenKey(root, path) as k:
                    try:
                        count = winreg.QueryInfoKey(k)[1]
                    except Exception as e:
                        print(f"QueryInfoKey failed: {e}")
                        count = 0
                    for i in range(count):
                        try:
                            name, _, _ = winreg.EnumValue(k, i)
                        except Exception:
                            continue
                        if not isinstance(name, str):
                            continue
                        display = name.strip()
                        for suf in suffixes:
                            if display.endswith(suf):
                                display = display[: -len(suf)].strip()
                                break
                        if display:
                            fonts.add(display)
            except OSError as e:
                print(f"OpenKey failed for {path}: {e}")
                continue
        return sorted(fonts, key=lambda s: s.lower())
    except Exception as e:
        print(f"General exception: {e}")
        return []

fonts = get_system_fonts()
print(f"Found {len(fonts)} fonts")
if len(fonts) > 0:
    print(f"First 5: {fonts[:5]}")
