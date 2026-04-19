"""
系统资源监测器 - 检测CPU频率和内存占用，发送通知
"""
import threading
import time
import psutil
from typing import Optional, Callable


class SystemResourceMonitor:
    """系统资源监测器 - 后台线程定期检测资源占用"""

    # 资源警告阈值
    CPU_FREQ_THRESHOLD_GHZ = 1.25  # CPU主频低于此值（GHz）
    MEMORY_USAGE_THRESHOLD = 15  # 内存可用率不超过此值（百分比）
    CHECK_INTERVAL_SECONDS = 300  # 检测间隔（秒）

    def __init__(self, on_alert_callback: Optional[Callable[[str, str], None]] = None):
        """
        初始化资源监测器

        Args:
            on_alert_callback: 告警回调函数，签名为 (title, message)
        """
        self.on_alert = on_alert_callback
        self._thread: Optional[threading.Thread] = None
        self._running = False
        self._lock = threading.RLock()
        self._last_alert_time = 0.0
        self._alert_cooldown_seconds = 300  # 相同告警5分钟内只显示一次

    def start(self) -> None:
        """启动资源监测线程"""
        with self._lock:
            if self._running:
                return
            self._running = True
            self._thread = threading.Thread(target=self._monitor_loop, daemon=True)
            self._thread.start()

    def stop(self) -> None:
        """停止资源监测线程"""
        with self._lock:
            self._running = False

    def _monitor_loop(self) -> None:
        """监测循环 - 运行在后台线程"""
        while self._running:
            try:
                self._check_resources()
            except Exception as e:
                print(f"[ResourceMonitor] Error during resource check: {e}")

            # 等待下一个检测周期
            time.sleep(self.CHECK_INTERVAL_SECONDS)

    def _check_resources(self) -> None:
        """检查系统资源占用"""
        try:
            # 获取CPU主频（单位：GHz）
            cpu_freq = psutil.cpu_freq()
            if cpu_freq is None:
                return

            current_freq_ghz = cpu_freq.current / 1000.0  # 转换为GHz

            # 获取内存信息
            mem_info = psutil.virtual_memory()
            mem_available_percent = mem_info.percent  # 已用百分比
            mem_free_percent = 100 - mem_available_percent  # 可用百分比

            # 检查是否需要告警
            should_alert = False
            alert_reason = ""

            if current_freq_ghz < self.CPU_FREQ_THRESHOLD_GHZ:
                should_alert = True
                alert_reason = f"CPU频率过低({current_freq_ghz:.2f}GHz < {self.CPU_FREQ_THRESHOLD_GHZ}GHz)"

            if mem_free_percent <= self.MEMORY_USAGE_THRESHOLD:
                should_alert = True
                if alert_reason:
                    alert_reason += "；"
                alert_reason += f"可用内存不足({mem_free_percent:.1f}% <= {self.MEMORY_USAGE_THRESHOLD}%)"

            if should_alert:
                self._trigger_alert()

        except Exception as e:
            print(f"[ResourceMonitor] Error checking resources: {e}")

    def _trigger_alert(self) -> None:
        """触发告警 - 检查冷却时间后发送通知"""
        now = time.time()

        # 检查冷却时间 - 避免告警过于频繁
        if now - self._last_alert_time < self._alert_cooldown_seconds:
            return

        self._last_alert_time = now

        if self.on_alert:
            try:
                from ppt_assistant.core.i18n import t
                title = t("resource.monitor.title")
                body = t("resource.monitor.body")
                self.on_alert(title, body)
            except Exception as e:
                print(f"[ResourceMonitor] Error triggering alert: {e}")
