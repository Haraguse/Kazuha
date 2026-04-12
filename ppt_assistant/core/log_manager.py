"""
日志管理器 - 收集和管理应用日志流
"""
import sys
import io
import threading
from datetime import datetime
from collections import deque
from typing import List, Dict, Literal

LogLevel = Literal["debug", "info", "warn", "error"]


class LogEntry:
    """日志项"""
    def __init__(self, timestamp: str, level: LogLevel, message: str):
        self.timestamp = timestamp
        self.level = level
        self.message = message

    def to_dict(self) -> Dict:
        return {
            "time": self.timestamp,
            "level": self.level,
            "message": self.message
        }


class LogCaptureStream(io.StringIO):
    """捕获输出流的自定义 StringIO"""
    def __init__(self, manager: 'LogManager', level: LogLevel):
        super().__init__()
        self.manager = manager
        self.level = level
        self.buffer = ""

    def write(self, s: str) -> int:
        if isinstance(s, str):
            self.buffer += s
            # 检查是否有完整的行
            if "\n" in self.buffer or "\r" in self.buffer:
                lines = self.buffer.split("\n")
                for line in lines[:-1]:
                    if line.strip():
                        self.manager.add_log(self.level, line.strip())
                self.buffer = lines[-1]
        return len(s) if isinstance(s, str) else 0

    def flush(self):
        # 在 flush 时处理剩余的缓冲
        if self.buffer.strip():
            self.manager.add_log(self.level, self.buffer.strip())
            self.buffer = ""


class LogManager:
    """日志管理器 - 收集应用的所有日志输出"""

    MAX_LOGS = 10000  # 最大保存的日志数

    def __init__(self, max_logs: int = None):
        self.max_logs = max_logs or self.MAX_LOGS
        self.logs: deque = deque(maxlen=self.max_logs)
        self._lock = threading.RLock()
        self._filters = {"debug": True, "info": True, "warn": True, "error": True}
        self._original_stdout = sys.stdout
        self._original_stderr = sys.stderr
        self._capture_streams = {}

    def start_capture(self):
        """开始捕获 stdout 和 stderr"""
        self._capture_streams["stdout"] = LogCaptureStream(self, "info")
        self._capture_streams["stderr"] = LogCaptureStream(self, "error")
        sys.stdout = self._capture_streams["stdout"]
        sys.stderr = self._capture_streams["stderr"]

    def stop_capture(self):
        """停止捕获并恢复原始输出"""
        # 刷新流中的剩余内容
        for stream in self._capture_streams.values():
            if hasattr(stream, 'flush'):
                stream.flush()

        sys.stdout = self._original_stdout
        sys.stderr = self._original_stderr
        self._capture_streams.clear()

    def add_log(self, level: LogLevel, message: str):
        """添加日志项"""
        if not message:
            return

        with self._lock:
            # 检查级别过滤
            if not self._filters.get(level, True):
                return

            timestamp = datetime.now().strftime("%H:%M:%S")
            entry = LogEntry(timestamp, level, message)
            self.logs.append(entry)

    def get_logs(self, levels: List[LogLevel] = None, search_text: str = "") -> List[Dict]:
        """
        获取日志
        :param levels: 要包含的日志级别列表，None 表示全部
        :param search_text: 搜索文本
        :return: 日志字典列表
        """
        with self._lock:
            if levels is None:
                levels = ["debug", "info", "warn", "error"]

            search_text = search_text.lower()
            result = []

            for entry in self.logs:
                if entry.level not in levels:
                    continue
                if search_text and search_text not in entry.message.lower():
                    continue
                result.append(entry.to_dict())

            return result

    def get_stats(self) -> Dict[str, int]:
        """获取日志统计信息"""
        with self._lock:
            stats = {"debug": 0, "info": 0, "warn": 0, "error": 0}
            for entry in self.logs:
                stats[entry.level] = stats.get(entry.level, 0) + 1
            return stats

    def set_filters(self, filters: Dict[LogLevel, bool]):
        """设置日志级别过滤"""
        with self._lock:
            self._filters.update(filters)

    def get_filters(self) -> Dict[LogLevel, bool]:
        """获取当前的日志级别过滤"""
        with self._lock:
            return dict(self._filters)

    def clear_logs(self):
        """清空所有日志"""
        with self._lock:
            self.logs.clear()

    def log(self, level: LogLevel, message: str):
        """直接添加日志（不通过 stdout/stderr）"""
        self.add_log(level, message)


# 全局日志管理器实例
_log_manager: LogManager = None


def get_log_manager() -> LogManager:
    """获取全局日志管理器实例"""
    global _log_manager
    if _log_manager is None:
        _log_manager = LogManager()
    return _log_manager


def init_log_manager():
    """初始化并启动日志管理器"""
    manager = get_log_manager()
    manager.start_capture()
    return manager
