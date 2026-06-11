import sys
import time
from PySide6.QtCore import Qt, QTimer
from PySide6.QtGui import QColor, QSurfaceFormat
from PySide6.QtWidgets import QApplication, QWidget, QVBoxLayout, QPushButton
from PySide6.QtQuick import QQuickView
from PySide6.QtQml import QQmlApplicationEngine

QML = """
import QtQuick

Rectangle {
    width: 27
    height: 27
    color: "transparent"

    Rectangle {
        id: spinner
        width: 20
        height: 20
        anchors.centerIn: parent
        color: "transparent"
        border.color: "red"
        border.width: 3
        radius: 10
        
        Rectangle {
            width: 10
            height: 10
            color: "blue"
            radius: 5
            anchors.top: parent.top
            anchors.horizontalCenter: parent.horizontalCenter
        }
    }

    RotationAnimator {
        target: spinner
        from: 0
        to: 360
        duration: 800
        loops: Animation.Infinite
        running: true
    }
}
"""

class Main(QWidget):
    def __init__(self):
        super().__init__()
        self.resize(300, 300)
        self.setAttribute(Qt.WA_TranslucentBackground)
        layout = QVBoxLayout(self)

        self.view = QQuickView()
        self.view.setResizeMode(QQuickView.SizeRootObjectToView)
        self.view.setColor(QColor(Qt.transparent))
        
        format = QSurfaceFormat()
        format.setAlphaBufferSize(8)
        self.view.setFormat(format)
        self.view.setClearBeforeRendering(True)
        
        # Load QML
        from PySide6.QtCore import QByteArray
        self.view.engine().loadData(QByteArray(QML.encode('utf-8')))

        container = QWidget.createWindowContainer(self.view, self)
        container.setFixedSize(27, 27)
        container.setAttribute(Qt.WA_TranslucentBackground)
        
        layout.addWidget(container)
        
        btn = QPushButton("Block Thread for 2s")
        btn.clicked.connect(self.block)
        layout.addWidget(btn)

    def block(self):
        print("blocking...")
        time.sleep(2)
        print("unblocked!")

if __name__ == "__main__":
    app = QApplication(sys.argv)
    w = Main()
    w.show()
    sys.exit(app.exec())
