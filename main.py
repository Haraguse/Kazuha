import sys

from PyQt6.QtWidgets import QApplication
from PyQt6.QtCore import Qt

from controller import MainController


if __name__ == "__main__":
    QApplication.setHighDpiScaleFactorRoundingPolicy(Qt.HighDpiScaleFactorRoundingPolicy.PassThrough)
    app = QApplication(sys.argv)
    app.setQuitOnLastWindowClosed(False)
    controller = MainController()
    sys.exit(app.exec())

