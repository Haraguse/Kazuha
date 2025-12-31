from PyQt6.QtWidgets import QWidget, QVBoxLayout, QLabel, QRadioButton, QCheckBox, QLineEdit, QButtonGroup
from qfluentwidgets import MessageBoxBase, SubtitleLabel, LineEdit, PushButton, RadioButton
from PyQt6.QtCore import Qt, QTimer, QCoreApplication


def tr(text: str) -> str:
    return QCoreApplication.translate("CrashDialog", text)


class CrashDialog(MessageBoxBase):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.titleLabel = SubtitleLabel(tr("崩溃行为测试配置"), self)

        self.viewLayout.addWidget(self.titleLabel)
        
        self.radioGroup = QButtonGroup(self)
        self.rbNormal = RadioButton(tr("常规崩溃（抛出异常）"), self)
        self.rbStack = RadioButton(tr("堆栈溢出（递归调用）"), self)
        
        self.radioGroup.addButton(self.rbNormal, 0)
        self.radioGroup.addButton(self.rbStack, 1)
        self.rbNormal.setChecked(True)
        
        self.viewLayout.addWidget(self.rbNormal)
        self.viewLayout.addWidget(self.rbStack)
        
        self.textEdit = LineEdit(self)
        self.textEdit.setPlaceholderText(tr("自定义崩溃文本（仅在常规崩溃模式下生效）"))
        self.viewLayout.addWidget(self.textEdit)
        
        self.cbCountdown = QCheckBox(tr("触发前倒计时 3 秒"), self)
        self.cbCountdown.setChecked(True)
        self.viewLayout.addWidget(self.cbCountdown)
        
        self.yesButton.setText(tr("触发崩溃"))
        self.cancelButton.setText(tr("取消"))
        
        self.widget.setMinimumWidth(350)
        
    def get_settings(self):
        return {
            'type': 'stack' if self.rbStack.isChecked() else 'normal',
            'text': self.textEdit.text() or "Manual Crash Test",
            'countdown': self.cbCountdown.isChecked()
        }

def trigger_crash(settings):
    """ Helper to execute crash based on settings """
    crash_type = settings['type']
    text = settings['text']
    
    if crash_type == 'normal':
        raise Exception(text)
    elif crash_type == 'stack':
        def infinite_recursion(n):
            infinite_recursion(n+1)
        infinite_recursion(0)
