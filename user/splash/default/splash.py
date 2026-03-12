from PySide6.QtWidgets import QFrame


def apply(splash):
    splash._container = QFrame(splash)
    splash._container.setObjectName("splashContainer")
    splash._build_ui()
    splash._apply_styles()
