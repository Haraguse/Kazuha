import os
import sys
import winsound


def icon_path(name):
    base_dir = getattr(sys, "_MEIPASS", os.path.dirname(os.path.abspath(__file__)))
    return os.path.join(base_dir, "icons", name)


def play_click_sound():
    try:
        system_root = os.environ.get("SystemRoot", r"C:\Windows")
        sound_path = os.path.join(system_root, "Media", "Windows Default.wav")
        winsound.PlaySound(sound_path, winsound.SND_FILENAME | winsound.SND_ASYNC | winsound.SND_NODEFAULT)
    except Exception:
        pass

