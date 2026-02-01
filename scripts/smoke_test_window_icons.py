import sys
import os
import traceback

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

from PySide6.QtCore import QCoreApplication, Qt, QTimer
from PySide6.QtWidgets import QApplication, QDialog, QWidget

from ppt_assistant.core.app_icon import load_app_icon


def _icon_of_window(obj):
    if hasattr(obj, "windowIcon"):
        return obj.windowIcon()
    if hasattr(obj, "icon"):
        return obj.icon()
    return None


def _exercise_window(win):
    win.show()
    QApplication.processEvents()
    if hasattr(win, "showMinimized"):
        win.showMinimized()
        QApplication.processEvents()
    if hasattr(win, "showMaximized"):
        win.showMaximized()
        QApplication.processEvents()
    if hasattr(win, "showNormal"):
        win.showNormal()
        QApplication.processEvents()


def _check(name, win):
    icon = _icon_of_window(win)
    if icon is None or icon.isNull():
        raise RuntimeError(f"{name}: window icon is null")
    sizes = icon.availableSizes()
    print(f"{name}: icon ok, availableSizes={[(s.width(), s.height()) for s in sizes]}")


def main():
    QCoreApplication.setAttribute(Qt.AA_UseHighDpiPixmaps, True)
    app = QApplication(sys.argv)
    app_icon = load_app_icon()
    if not app_icon.isNull():
        app.setWindowIcon(app_icon)

    windows = []
    windows.append(("QWidget", QWidget()))
    windows.append(("QDialog", QDialog()))

    try:
        from plugins.builtins.board.board_window import BoardWindow
        windows.append(("BoardWindow", BoardWindow()))
    except Exception:
        print("BoardWindow: skipped")
        traceback.print_exc()

    try:
        from plugins.builtins.spotlight.spotlight_window import SpotlightWindow
        windows.append(("SpotlightWindow", SpotlightWindow()))
    except Exception:
        print("SpotlightWindow: skipped")
        traceback.print_exc()

    failures = 0
    for name, win in windows:
        try:
            _exercise_window(win)
            _check(name, win)
        except Exception:
            failures += 1
            print(f"{name}: FAILED")
            traceback.print_exc()

    QTimer.singleShot(0, app.quit)
    app.exec()

    if failures:
        raise SystemExit(f"{failures} windows failed icon checks")


if __name__ == "__main__":
    main()
