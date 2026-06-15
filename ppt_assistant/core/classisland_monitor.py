"""
ClassIsland 联动监测器 - 检测 ClassIsland.Desktop 运行状态，结合状态栏设置提供通知建议

触发条件：
  - "显示状态栏" 设置处于开启状态
  - "ClassIsland.Desktop" 应用程序正在运行中

两个条件同时满足时，通过 Windows 通知提醒用户关闭状态栏以获得更好体验。
"""

import sys
import time
import traceback
from typing import Optional

from PySide6.QtCore import QObject, Qt, QTimer, Slot

from ppt_assistant.core.config import cfg

# 防抖冷却时间（秒）- 两次通知之间最少间隔
_NOTIFY_COOLDOWN_SECONDS = 1800  # 30 分钟

# 进程检测轮询间隔（毫秒）
_POLL_INTERVAL_MS = 5000  # 5 秒

# 目标进程名（不含 .exe 后缀用于匹配）
_TARGET_PROCESS = "ClassIsland.Desktop"
_TARGET_PROCESS_EXE = f"{_TARGET_PROCESS}.exe"


class ClassIslandMonitor(QObject):
    """监测 ClassIsland.Desktop 与状态栏设置的联动"""

    def __init__(self, parent: Optional[QObject] = None):
        super().__init__(parent)
        self._timer: Optional[QTimer] = None
        self._was_triggered: bool = False
        self._last_notify_time: float = 0.0
        self._cooldown_seconds: float = _NOTIFY_COOLDOWN_SECONDS
        self._running: bool = False
        self._send_notification_callback = None

    # ------------------------------------------------------------------
    # 公共接口
    # ------------------------------------------------------------------

    def set_notification_callback(self, callback):
        """设置通知发送回调，回调签名为 callback(title, message, launch, buttons) -> bool"""
        self._send_notification_callback = callback

    def start(self):
        """启动监测"""
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
        """停止监测"""
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

    # ------------------------------------------------------------------
    # 轮询逻辑
    # ------------------------------------------------------------------

    @Slot()
    def _on_poll(self):
        if not self._running:
            return
        try:
            show_status_bar = self._cfg_show_status_bar()
            classisland_running = self._is_classisland_running()

            if show_status_bar and classisland_running:
                if not self._was_triggered:
                    # 刚进入触发状态
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

    # ------------------------------------------------------------------
    # 条件检查
    # ------------------------------------------------------------------

    @staticmethod
    def _cfg_show_status_bar() -> bool:
        """读取配置：状态栏是否开启"""
        try:
            return bool(cfg.showStatusBar.value)
        except Exception:
            return False

    @staticmethod
    def _is_classisland_running() -> bool:
        """检测 ClassIsland.Desktop 进程是否运行中"""
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

    # ------------------------------------------------------------------
    # 通知触发
    # ------------------------------------------------------------------

    def _can_notify(self) -> bool:
        """检查是否满足防抖条件"""
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
        """尝试发送通知（受防抖控制）"""
        if not self._can_notify():
            return
        self._send_notification()
        self._last_notify_time = time.time()

    def _send_notification(self):
        """构造并发送 Windows 通知，失败时打印详细错误"""
        title = "要关闭状态栏么？"
        message = (
            "检测到您正在使用 ClassIsland，"
            "将本软件状态栏功能关闭可保证您整体的体验更佳，"
            "要关闭本软件的状态栏么？"
        )
        launch_url = "luminalium://app/settings/statusbar"
        buttons = [
            {
                "content": "前往设置",
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