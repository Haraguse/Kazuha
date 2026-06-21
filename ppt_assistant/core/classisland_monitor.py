import sys
import time
import traceback
from typing import Optional

from PySide6.QtCore import QObject, Qt, QTimer, Slot

from ppt_assistant.core.config import cfg
from ppt_assistant.core.i18n import get_language

_NOTIFICATION_TEXTS = {
    "ja-JP": {
        "title": "ステータスバーをオフにしますか？",
        "message": (
            "ClassIsland / Class Widgets をご利用中のようです。"
            "本ソフトのステータスバー機能をオフにすると、"
            "より快適な表示体験が得られます。"
            "ステータスバーをオフにしますか？"
        ),
        "button": "設定を開く",
    },
    "zh-TW": {
        "title": "要關閉狀態列嗎？",
        "message": (
            "偵測到您正在使用 ClassIsland 或 Class Widgets，"
            "將本軟體狀態列功能關閉可確保您整體體驗更佳，"
            "要關閉本軟體的狀態列嗎？"
        ),
        "button": "前往設定",
    },
    "yue-HK": {
        "title": "你個腦係咪有問題呀？",
        "message": (
            "ClassIsland / Class Widgets 都行緊啦，你仲唔閂咗 Luminalium 個狀態欄佢？"
            "你若果唔想條賓周被我割咗去，就快啲開設置將 Luminalium 個狀態欄閂咗佢！"
            "如果唔係，等住被啲垃圾體驗割走你條賓周啦！"
        ),
        "button": "前往設定",
    },
    "en-US": {
        "title": "Turn off status bar?",
        "message": (
            "It looks like you're using ClassIsland or Class Widgets. "
            "Turning off the status bar feature in this app "
            "can improve your overall experience. "
            "Would you like to turn it off?"
        ),
        "button": "Open Settings",
    },
}

_DEFAULT_NOTIFICATION = {
    "title": "要关闭状态栏么？",
    "message": (
        "检测到您正在使用 ClassIsland 或 Class Widgets，"
        "将本软件状态栏功能关闭可保证您整体的体验更佳，"
        "要关闭本软件的状态栏么？"
    ),
    "button": "前往设置",
}

def _get_notification_texts():
    lang = get_language()
    return _NOTIFICATION_TEXTS.get(lang, _DEFAULT_NOTIFICATION)

_NOTIFY_COOLDOWN_SECONDS = 1800

_POLL_INTERVAL_MS = 5000

_TARGET_PROCESS = "ClassIsland.Desktop"
_TARGET_PROCESS_EXE = f"{_TARGET_PROCESS}.exe"


class ClassIslandMonitor(QObject):

    def __init__(self, parent: Optional[QObject] = None):
        super().__init__(parent)
        self._timer: Optional[QTimer] = None
        self._was_triggered: bool = False
        self._last_notify_time: float = 0.0
        self._cooldown_seconds: float = _NOTIFY_COOLDOWN_SECONDS
        self._running: bool = False
        self._send_notification_callback = None

    def set_notification_callback(self, callback):
        self._send_notification_callback = callback

    def start(self):
        if self._running:
            return
        self._running = True
        self._was_triggered = False
        self._last_notify_time = 0.0

        self._timer = QTimer(self)
        self._timer.setTimerType(Qt.TimerType.VeryCoarseTimer)
        self._timer.timeout.connect(self._on_poll)
        self._timer.start(_POLL_INTERVAL_MS)

        print(
            "[ClassIslandMonitor] 监测已启动 "
            f"(目标进程={_TARGET_PROCESS_EXE}, "
            f"轮询间隔={_POLL_INTERVAL_MS}ms, "
            f"冷却时间={self._cooldown_seconds}s)"
        )

    def stop(self):
        if not self._running:
            return
        self._running = False
        if self._timer is not None:
            self._timer.stop()
            self._timer.deleteLater()
            self._timer = None
        self._was_triggered = False
        print("[ClassIslandMonitor] 监测已停止")

    @property
    def is_running(self) -> bool:
        return self._running

    @Slot()
    def _on_poll(self):
        if not self._running:
            return
        try:
            show_status_bar = self._cfg_show_status_bar()
            classisland_running = self._is_classisland_running()

            if show_status_bar and classisland_running:
                if not self._was_triggered:
                    self._was_triggered = True
                    self._try_notify()
            else:
                if self._was_triggered:
                    print(
                        "[ClassIslandMonitor] 触发条件解除 "
                        f"(status_bar={show_status_bar}, "
                        f"classisland={classisland_running})"
                    )
                self._was_triggered = False
        except Exception as e:
            print(f"[ClassIslandMonitor] 轮询异常: {e}")

    @staticmethod
    def _cfg_show_status_bar() -> bool:
        try:
            return bool(cfg.showStatusBar.value)
        except Exception:
            return False

    @staticmethod
    def _is_classisland_running() -> bool:
        try:
            import psutil
        except ImportError:
            print("[ClassIslandMonitor] psutil 不可用，跳过进程检测")
            return False

        try:
            for proc in psutil.process_iter(["name"]):
                try:
                    if proc.info["name"] == _TARGET_PROCESS_EXE:
                        return True
                except (psutil.NoSuchProcess, psutil.AccessDenied):
                    continue
        except Exception as e:
            print(f"[ClassIslandMonitor] 进程枚举异常: {e}")
        return False

    def _can_notify(self) -> bool:
        now = time.time()
        elapsed = now - self._last_notify_time
        if elapsed < self._cooldown_seconds:
            remain = int(self._cooldown_seconds - elapsed)
            print(
                f"[ClassIslandMonitor] 冷却中，距离下次可通知还有 {remain}s"
            )
            return False
        return True

    def _try_notify(self):
        if not self._can_notify():
            return
        self._send_notification()
        self._last_notify_time = time.time()

    def _send_notification(self):
        texts = _get_notification_texts()
        title = texts["title"]
        message = texts["message"]
        launch_url = "luminalium://app/settings/statusbar"
        buttons = [
            {
                "content": texts["button"],
                "arguments": launch_url,
            }
        ]

        print(f"[ClassIslandMonitor] 触发通知: {title}")

        if self._send_notification_callback is None:
            print("[ClassIslandMonitor] 未设置通知回调，跳过通知发送")
            return

        try:
            ok = self._send_notification_callback(
                title, message, launch=launch_url, buttons=buttons
            )
            if ok:
                print("[ClassIslandMonitor] 通知已发送")
            else:
                print(
                    "[ClassIslandMonitor] 通知发送失败（回调返回 False），"
                    "请检查上方 [WindowsNotification] 错误日志获取详细原因"
                )
        except Exception as e:
            print(f"[ClassIslandMonitor] 通知发送异常: {e}")
            traceback.print_exc()