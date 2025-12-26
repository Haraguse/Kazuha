from PyQt6.QtCore import Qt, pyqtSignal, QTimer, QUrl
from PyQt6.QtWidgets import QWidget, QVBoxLayout, QLabel, QFrame, QHBoxLayout, QSizePolicy
from PyQt6.QtGui import QFont, QPainter, QColor, QLinearGradient, QDesktopServices
import json
import os

from qfluentwidgets import (
    FluentIcon as FIF,
    SwitchSettingCard, OptionsSettingCard, PushSettingCard,
    PrimaryPushSettingCard, SettingCard, PrimaryPushButton, PushButton,
    SmoothScrollArea, ExpandLayout, Theme, setTheme, setThemeColor,
    FluentWindow, NavigationItemPosition, isDarkTheme,
    LargeTitleLabel, TitleLabel, BodyLabel, CaptionLabel, IndeterminateProgressRing,
    InfoBar, InfoBarPosition
)
from ui.custom_settings import SchematicOptionsSettingCard, ScreenPaddingSettingCard
from controllers.business_logic import cfg
from ui.crash_dialog import CrashDialog, trigger_crash
from ui.visual_settings import ClockSettingCard


def _create_page(parent: QWidget):
    page = QWidget(parent)
    page.setStyleSheet("background-color: transparent;")
    layout = QVBoxLayout(page)
    layout.setContentsMargins(24, 24, 24, 24)
    layout.setSpacing(0)

    scroll = SmoothScrollArea(page)
    scroll.setObjectName("scrollInterface")
    scroll.setStyleSheet("SmoothScrollArea { background-color: transparent; border: none; }")

    content = QWidget()
    content.setStyleSheet("background-color: transparent;")
    content.setObjectName("scrollWidget")
    expand_layout = ExpandLayout(content)

    scroll.setWidget(content)
    scroll.setWidgetResizable(True)

    layout.addWidget(scroll)
    return page, content, expand_layout


def _apply_title_style(label: QLabel):
    f = label.font()
    f.setPointSize(22)
    f.setWeight(QFont.Weight.Bold)
    label.setFont(f)


def _apply_body_strong_style(label: QLabel):
    f = label.font()
    f.setPointSize(12)
    f.setWeight(QFont.Weight.Normal)
    label.setFont(f)


class VersionInfoCard(SettingCard):
    def __init__(self, icon, title, parent=None):
        super().__init__(icon, title, "", parent)
        box = QVBoxLayout()
        box.setContentsMargins(0, 0, 0, 0)
        box.setSpacing(4)
        self.latestLabel = BodyLabel("最新 Release 版本：尚未检查", self)
        self.currentLabel = BodyLabel("当前版本：未知", self)
        self.latestLabel.setWordWrap(True)
        self.currentLabel.setWordWrap(True)
        box.addWidget(self.latestLabel)
        box.addWidget(self.currentLabel)
        self.hBoxLayout.addLayout(box, 1)

    def set_versions(self, latest, current):
        self.latestLabel.setText(latest)
        self.currentLabel.setText(current)


class UpdateLogCard(SettingCard):
    def __init__(self, icon, title, parent=None):
        super().__init__(icon, title, "", parent)
        box = QVBoxLayout()
        box.setContentsMargins(0, 0, 0, 0)
        box.setSpacing(4)
        self.latestLogLabel = BodyLabel("最新更新日志：尚未加载", self)
        self.currentLogLabel = BodyLabel("当前版本更新日志：尚未加载", self)
        self.latestLogLabel.setWordWrap(True)
        self.currentLogLabel.setWordWrap(True)
        box.addWidget(self.latestLogLabel)
        box.addWidget(self.currentLogLabel)
        self.hBoxLayout.addLayout(box, 1)

    def set_logs(self, latest, current):
        self.latestLogLabel.setText(latest)
        self.currentLogLabel.setText(current)


class SettingsWindow(FluentWindow):
    configChanged = pyqtSignal()
    checkUpdateClicked = pyqtSignal()
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("界面设置 - Kazuha")
        self.resize(900, 640)

        try:
            self.setMicaEffectEnabled(True)
        except Exception:
            pass

        font = QFont("Bahnschrift", 14)
        font.setStyleStrategy(QFont.StyleStrategy.PreferAntialias)
        self.setFont(font)

        self.generalInterface, generalContent, generalLayout = _create_page(self)
        self.generalInterface.setObjectName("settings-general")

        self.personalInterface, personalContent, personalLayout = _create_page(self)
        self.personalInterface.setObjectName("settings-personal")

        self.clockInterface, clockContent, clockLayout = _create_page(self)
        self.clockInterface.setObjectName("settings-clock")
        self.clockLayout = clockLayout
        self.clockConflictInfoBar = None

        self.updateInterface, updateContent, updateLayout = _create_page(self)
        self.updateInterface.setObjectName("settings-update")

        self.aboutInterface, aboutContent, aboutLayout = _create_page(self)
        self.aboutInterface.setObjectName("settings-about")

        self._scrollWidgets = [generalContent, personalContent, clockContent, updateContent, aboutContent]

        self.generalPageTitle = LargeTitleLabel("常规", generalContent)
        _apply_title_style(self.generalPageTitle)
        generalLayout.addWidget(self.generalPageTitle)

        self.generalHeader = TitleLabel("基础设置", generalContent)
        _apply_body_strong_style(self.generalHeader)
        generalLayout.addWidget(self.generalHeader)

        self.startupCard = SwitchSettingCard(
            FIF.POWER_BUTTON,
            "开机自启",
            "跟随系统启动自动运行",
            configItem=cfg.enableStartUp,
            parent=generalContent
        )

        generalLayout.addWidget(self.startupCard)
        
        self.systemNotificationCard = SwitchSettingCard(
            FIF.MESSAGE,
            "系统通知",
            "显示系统通知消息",
            configItem=cfg.enableSystemNotification,
            parent=generalContent
        )
        generalLayout.addWidget(self.systemNotificationCard)
        
        self.globalSoundCard = SwitchSettingCard(
            FIF.MUSIC,
            "全局音效",
            "启用全局音效反馈",
            configItem=cfg.enableGlobalSound,
            parent=generalContent
        )
        generalLayout.addWidget(self.globalSoundCard)
        
        self.globalAnimationCard = SwitchSettingCard(
            FIF.SPEED_MEDIUM,
            "全局过渡动画",
            "启用界面过渡动画",
            configItem=cfg.enableGlobalAnimation,
            parent=generalContent
        )
        generalLayout.addWidget(self.globalAnimationCard)

        self.personalPageTitle = LargeTitleLabel("个性化", personalContent)
        _apply_title_style(self.personalPageTitle)
        personalLayout.addWidget(self.personalPageTitle)

        self.themeHeader = TitleLabel("主题与颜色", personalContent)
        _apply_body_strong_style(self.themeHeader)
        personalLayout.addWidget(self.themeHeader)

        self.themeCard = SchematicOptionsSettingCard(
            cfg.themeMode,
            FIF.BRUSH,
            "应用主题",
            "调整应用外观",
            texts=["浅色", "深色", "跟随系统"],
            schematic_type="theme",
            parent=personalContent
        )

        personalLayout.addWidget(self.themeCard)

        self.layoutHeader = TitleLabel("布局与位置", personalContent)
        _apply_body_strong_style(self.layoutHeader)
        personalLayout.addWidget(self.layoutHeader)
        
        self.navPosCard = SchematicOptionsSettingCard(
            cfg.navPosition,
            FIF.ALIGNMENT,
            "翻页导航位置",
            "调整翻页按钮在屏幕上的位置",
            texts=["底部两端", "中部两侧"],
            schematic_type="nav_pos",
            parent=personalContent
        )
        
        self.paddingCard = ScreenPaddingSettingCard(
            FIF.FULL_SCREEN,
            "组件屏幕边距",
            "调整组件距离屏幕边缘的内边距",
            parent=personalContent
        )
        
        self.timerPosCard = OptionsSettingCard(
            cfg.timerPosition,
            FIF.SPEED_HIGH, 
            "计时器位置",
            "调整倒计时窗口的显示位置",
            texts=["屏幕中央", "左上角", "右上角", "左下角", "右下角"],
            parent=personalContent
        )
        
        personalLayout.addWidget(self.navPosCard)
        personalLayout.addWidget(self.paddingCard)
        personalLayout.addWidget(self.timerPosCard)

        self.clockPageTitle = LargeTitleLabel("时钟组件", clockContent)
        _apply_title_style(self.clockPageTitle)
        clockLayout.addWidget(self.clockPageTitle)

        self.clockHeader = TitleLabel("显示与样式", clockContent)
        _apply_body_strong_style(self.clockHeader)
        clockLayout.addWidget(self.clockHeader)

        self.clockEnableCard = SwitchSettingCard(
            FIF.DATE_TIME,
            "显示时钟",
            "是否显示桌面悬浮时钟组件",
            configItem=cfg.enableClock,
            parent=clockContent
        )

        self.clockPosCard = OptionsSettingCard(
            cfg.clockPosition,
            FIF.HISTORY,
            "时钟位置",
            "调整悬浮时钟的显示位置",
            texts=["左上角", "右上角", "左下角", "右下角"],
            parent=clockContent
        )

        self.clockSettingCard = ClockSettingCard(
            FIF.DATE_TIME,
            "时钟样式",
            "自定义悬浮时钟的显示内容和外观",
            parent=clockContent
        )

        clockLayout.addWidget(self.clockEnableCard)
        clockLayout.addWidget(self.clockPosCard)
        clockLayout.addWidget(self.clockSettingCard)

        self._update_clock_settings_for_cicw()

        self.dangerHeader = TitleLabel("危险功能", generalContent)
        _apply_body_strong_style(self.dangerHeader)
        generalLayout.addWidget(self.dangerHeader)
        self.crashCard = PushSettingCard(
            "触发",
            FIF.DELETE,
            "崩溃测试",
            "仅用于开发调试，请勿在演示时点击",
            parent=generalContent
        )
        generalLayout.addWidget(self.crashCard)

        self.updatePageTitle = LargeTitleLabel("更新", updateContent)
        _apply_title_style(self.updatePageTitle)
        updateLayout.addWidget(self.updatePageTitle)

        self.updateHeader = TitleLabel("版本更新", updateContent)
        _apply_body_strong_style(self.updateHeader)
        updateLayout.addWidget(self.updateHeader)
        
        version = "Unknown"
        try:
            import sys
            base_dir = getattr(sys, "_MEIPASS", os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
            v_path = os.path.join(base_dir, "config", "version.json")
            with open(v_path, 'r', encoding='utf-8') as f:
                info = json.load(f)
                version = info.get("versionName", "Unknown")
        except:
            pass

        self.currentVersion = version

        self.updateCard = PushSettingCard(
            "检查更新",
            FIF.SYNC,
            "检查最新 Release 版本",
            "",
            parent=updateContent
        )

        updateLayout.addWidget(self.updateCard)

        self.versionInfoHeader = TitleLabel("版本信息", updateContent)
        _apply_body_strong_style(self.versionInfoHeader)
        updateLayout.addWidget(self.versionInfoHeader)
        self.versionInfoCard = VersionInfoCard(
            FIF.INFO,
            "版本状态",
            parent=updateContent
        )
        updateLayout.addWidget(self.versionInfoCard)

        self.logInfoHeader = TitleLabel("更新日志", updateContent)
        _apply_body_strong_style(self.logInfoHeader)
        updateLayout.addWidget(self.logInfoHeader)
        self.logInfoCard = UpdateLogCard(
            FIF.DOCUMENT,
            "更新内容",
            parent=updateContent
        )
        updateLayout.addWidget(self.logInfoCard)

        self.latestReleaseVersionLabel = self.versionInfoCard.latestLabel
        self.currentVersionLabel = self.versionInfoCard.currentLabel
        self.latestReleaseLogLabel = self.logInfoCard.latestLogLabel
        self.currentVersionLogLabel = self.logInfoCard.currentLogLabel

        self.currentVersionLabel.setText(f"当前版本：{version}")

        self.updateLoadingWidget = QWidget(updateContent)
        loadingLayout = QHBoxLayout(self.updateLoadingWidget)
        loadingLayout.setContentsMargins(0, 12, 0, 0)
        loadingLayout.setSpacing(8)

        self.updateRing = IndeterminateProgressRing(self.updateLoadingWidget)
        self.updateRing.setFixedSize(20, 20)
        self.updateRing.setStrokeWidth(3)

        self.updateLoadingLabel = BodyLabel("正在检查更新...", self.updateLoadingWidget)

        loadingLayout.addWidget(self.updateRing, 0, Qt.AlignmentFlag.AlignVCenter)
        loadingLayout.addWidget(self.updateLoadingLabel, 0, Qt.AlignmentFlag.AlignVCenter)
        loadingLayout.addStretch()

        self.updateLoadingWidget.hide()
        updateLayout.addWidget(self.updateLoadingWidget)
        
        self.aboutContentWidget = QWidget(aboutContent)
        aboutContentLayout = QVBoxLayout(self.aboutContentWidget)
        aboutContentLayout.setContentsMargins(24, 24, 24, 24)
        aboutContentLayout.setSpacing(20)
        
        self.aboutTitle = LargeTitleLabel("Kazuha 万叶演示助手", self.aboutContentWidget)
        _apply_title_style(self.aboutTitle)
        aboutContentLayout.addWidget(self.aboutTitle)
        
        self.aboutSubtitleLabel = BodyLabel("一款能平替希沃演示助手部分功能的演示工具。", self.aboutContentWidget)
        self.aboutSubtitleLabel.setWordWrap(True)
        aboutContentLayout.addWidget(self.aboutSubtitleLabel)
        
        self.aboutVersionLabel = CaptionLabel(f"当前版本：{version}", self.aboutContentWidget)
        self.aboutVersionLabel.setWordWrap(True)
        aboutContentLayout.addWidget(self.aboutVersionLabel)
        
        self.aboutDevCard = SettingCard(
            FIF.PEOPLE,
            "开发与维护",
            "",
            parent=self.aboutContentWidget
        )
        dev_layout = QVBoxLayout()
        dev_layout.setContentsMargins(0, 0, 0, 0)
        dev_layout.setSpacing(4)
        self.aboutAuthorLabel = BodyLabel("作者：Seirai Studio / @Haraguse", self.aboutDevCard)
        self.aboutAuthorLabel.setWordWrap(True)
        self.aboutThanksLabel = CaptionLabel("感谢所有参与开发与测试的贡献者。", self.aboutDevCard)
        self.aboutThanksLabel.setWordWrap(True)
        dev_layout.addWidget(self.aboutAuthorLabel)
        dev_layout.addWidget(self.aboutThanksLabel)
        self.aboutDevCard.hBoxLayout.addLayout(dev_layout, 1)
        aboutContentLayout.addWidget(self.aboutDevCard)
        
        self.aboutLinkHeader = TitleLabel("项目与社区", self.aboutContentWidget)
        _apply_body_strong_style(self.aboutLinkHeader)
        aboutContentLayout.addWidget(self.aboutLinkHeader)
        
        self.aboutProjectCard = PrimaryPushSettingCard(
            "打开",
            FIF.GITHUB,
            "GitHub 仓库",
            "在 GitHub 上查看项目主页",
            parent=self.aboutContentWidget
        )
        self.aboutContribCard = PrimaryPushSettingCard(
            "查看",
            FIF.PEOPLE,
            "贡献者",
            "在 GitHub 上查看贡献者列表",
            parent=self.aboutContentWidget
        )
        self.aboutDiscussQqCard = PrimaryPushSettingCard(
            "加入",
            FIF.CHAT,
            "交流 QQ 群",
            "通过 README 中二维码加入交流群",
            parent=self.aboutContentWidget
        )
        self.aboutWebsiteCard = PrimaryPushSettingCard(
            "访问",
            FIF.GLOBE,
            "官方网站",
            "（预留）",
            parent=self.aboutContentWidget
        )
        aboutContentLayout.addWidget(self.aboutProjectCard)
        aboutContentLayout.addWidget(self.aboutContribCard)
        aboutContentLayout.addWidget(self.aboutDiscussQqCard)
        aboutContentLayout.addWidget(self.aboutWebsiteCard)

        aboutLayout.addWidget(self.aboutContentWidget)

        self.aboutProjectCard.clicked.connect(
            lambda: QDesktopServices.openUrl(QUrl("https://github.com/TCYKyousen/Kazuha"))
        )
        self.aboutContribCard.clicked.connect(
            lambda: QDesktopServices.openUrl(QUrl("https://github.com/TCYKyousen/Kazuha/graphs/contributors"))
        )
        self.aboutDiscussQqCard.clicked.connect(
            lambda: QDesktopServices.openUrl(QUrl("https://github.com/TCYKyousen/Kazuha#readme"))
        )

        self.updateCard.clicked.connect(self._on_update_card_clicked)
        QTimer.singleShot(0, self._auto_check_update)
        
        self.crashCard.clicked.connect(self.show_crash_dialog)

        cfg.themeMode.valueChanged.connect(self.on_config_changed)
        cfg.navPosition.valueChanged.connect(self.on_config_changed)
        cfg.clockPosition.valueChanged.connect(self.on_config_changed)
        cfg.clockFontWeight.valueChanged.connect(self.on_config_changed)
        cfg.clockShowSeconds.valueChanged.connect(self.on_config_changed)
        cfg.clockShowDate.valueChanged.connect(self.on_config_changed)
        cfg.clockShowLunar.valueChanged.connect(self.on_config_changed)
        cfg.timerPosition.valueChanged.connect(self.on_config_changed)
        cfg.enableStartUp.valueChanged.connect(self.on_config_changed)
        cfg.enableClock.valueChanged.connect(self.on_config_changed)
        cfg.enableSystemNotification.valueChanged.connect(self.on_config_changed)
        cfg.enableGlobalSound.valueChanged.connect(self.on_config_changed)
        cfg.enableGlobalAnimation.valueChanged.connect(self.on_config_changed)
        cfg.screenPadding.valueChanged.connect(self.on_config_changed)

        self.addSubInterface(self.generalInterface, FIF.SETTING, "常规")
        self.addSubInterface(self.personalInterface, FIF.BRUSH, "个性化")
        self.addSubInterface(self.clockInterface, FIF.DATE_TIME, "时钟组件")
        self.addSubInterface(self.updateInterface, FIF.SYNC, "更新")
        self.addSubInterface(self.aboutInterface, FIF.INFO, "关于", NavigationItemPosition.BOTTOM)
        
    def on_config_changed(self):
        self.configChanged.emit()
        
    def show_crash_dialog(self):
        w = CrashDialog(self)
        if w.exec():
            settings = w.get_settings()
            if settings['countdown']:
                QTimer.singleShot(3000, lambda: trigger_crash(settings))
            else:
                trigger_crash(settings)
    
    def _update_clock_settings_for_cicw(self):
        running = False
        try:
            import subprocess
            startupinfo = subprocess.STARTUPINFO()
            startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
            output = subprocess.check_output('tasklist', startupinfo=startupinfo).decode('gbk', errors='ignore').lower()
            running = ('classisland' in output) or ('classwidgets' in output)
        except Exception:
            running = False
        
        widgets = [
            getattr(self, "clockEnableCard", None),
            getattr(self, "clockPosCard", None),
            getattr(self, "clockSettingCard", None),
        ]
        for w in widgets:
            if w is not None:
                w.setEnabled(not running)
        
        if running:
            if not self.clockConflictInfoBar:
                bar = InfoBar.warning(
                    title='检测到 ClassIsland/Class Widgets 正在运行',
                    content="ClassIsland/Class Widgets 一部分具有和时钟组件相同的功能，且另一部分功能甚至可以超出时钟组件所能做到的范围。\n故此，时钟组件现在是不可用的。",
                    orient=Qt.Orientation.Horizontal,
                    isClosable=False,
                    position=InfoBarPosition.BOTTOM,
                    duration=-1,
                    parent=self.clockInterface
                )
                for label in bar.findChildren(QLabel):
                    label.setWordWrap(True)
                self.clockConflictInfoBar = bar
                bar.show()
            else:
                self.clockConflictInfoBar.show()
        else:
            if self.clockConflictInfoBar:
                self.clockConflictInfoBar.hide()
                
    def set_theme(self, theme):
        pass

    def _on_update_card_clicked(self):
        if hasattr(self, "latestReleaseVersionLabel"):
            self.latestReleaseVersionLabel.setText("最新 Release 版本：正在检查...")
        if hasattr(self, "updateLoadingWidget"):
            self.updateLoadingWidget.show()
        self.checkUpdateClicked.emit()

    def _auto_check_update(self):
        self._on_update_card_clicked()

    def set_update_info(self, latest_version, latest_log):
        v = latest_version or "未知"
        if hasattr(self, "latestReleaseVersionLabel"):
            self.latestReleaseVersionLabel.setText(f"最新 Release 版本：{v}")
        if hasattr(self, "latestReleaseLogLabel"):
            text = latest_log.strip() if latest_log else "无更新日志"
            self.latestReleaseLogLabel.setText(f"最新更新日志：\n{text}")
        if hasattr(self, "currentVersionLogLabel"):
            if self.currentVersion and latest_version and self.currentVersion == latest_version:
                self.currentVersionLogLabel.setText(f"当前版本更新日志：\n{text}")
            else:
                self.currentVersionLogLabel.setText("当前版本更新日志：请在 GitHub Releases 查看对应版本说明")
        if hasattr(self, "updateLoadingWidget"):
            self.updateLoadingWidget.hide()

    def stop_update_loading(self):
        if hasattr(self, "updateLoadingWidget"):
            self.updateLoadingWidget.hide()
