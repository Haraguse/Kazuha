"""
日志管理器 - 收集和管理应用日志流
"""

import sys
import io
import logging
import threading
from datetime import datetime
from collections import deque
from typing import List, Dict, Literal, Optional

LogLevel = Literal["debug", "info", "warn", "error"]


class NullTextStream(io.TextIOBase):
    """Writable no-op stream used when a GUI process has no console stream."""

    encoding = "utf-8"
    errors = None

    def write(self, s: str) -> int:
        return len(s) if isinstance(s, str) else 0

    def flush(self):
        pass

    def writable(self):
        return True


class LogEntry:
    """日志项"""

    def __init__(self, timestamp: str, level: LogLevel, message: str, index: int = None):
        self.timestamp = timestamp
        self.level = level
        self.message = message
        self._index = index

    def to_dict(self) -> Dict:
        return {"time": self.timestamp, "level": self.level, "message": self.message, "_idx": self._index}


class LogCaptureStream(io.TextIOBase):
    """Tee stdout/stderr to the original stream and the in-memory log manager."""

    def __init__(self, manager: "LogManager", level: LogLevel, original_stream):
        super().__init__()
        self.manager = manager
        self.level = level
        self.original_stream = original_stream or NullTextStream()
        self.buffer = ""
        self._lock = threading.RLock()

    @property
    def encoding(self):
        return getattr(self.original_stream, "encoding", None) or "utf-8"

    @property
    def errors(self):
        return getattr(self.original_stream, "errors", None)

    @property
    def closed(self):
        return False

    def isatty(self):
        isatty = getattr(self.original_stream, "isatty", None)
        return bool(isatty()) if callable(isatty) else False

    def fileno(self):
        fileno = getattr(self.original_stream, "fileno", None)
        if callable(fileno):
            return fileno()
        raise OSError("underlying stream has no file descriptor")

    def write(self, s: str) -> int:
        if not isinstance(s, str):
            s = str(s)

        with self._lock:
            self._write_original(s)

            self.buffer += s
            self._drain_complete_lines()
        return len(s)

    def flush(self):
        with self._lock:
            try:
                self.original_stream.flush()
            except Exception:
                pass
            if self.buffer.strip():
                self.manager.add_log(self.level, self.buffer.strip())
                self.buffer = ""

    def _drain_complete_lines(self):
        start = 0
        lines = []
        i = 0
        while i < len(self.buffer):
            char = self.buffer[i]
            if char in "\r\n":
                lines.append(self.buffer[start:i])
                if char == "\r" and i + 1 < len(self.buffer) and self.buffer[i + 1] == "\n":
                    i += 1
                start = i + 1
            i += 1

        if not lines:
            return

        self.buffer = self.buffer[start:]
        for line in lines:
            if line.strip():
                self.manager.add_log(self.level, line.strip())

    def _write_original(self, s: str):
        try:
            self.original_stream.write(s)
            return
        except UnicodeEncodeError:
            encoding = self.encoding or "utf-8"
            safe_text = s.encode(encoding, errors="replace").decode(encoding, errors="replace")
            try:
                self.original_stream.write(safe_text)
            except Exception:
                pass
        except Exception:
            pass

    def writable(self):
        return True


class LogManagerHandler(logging.Handler):
    """Logging handler that stores formatted records in LogManager."""

    LEVEL_MAP = {
        logging.DEBUG: "debug",
        logging.INFO: "info",
        logging.WARNING: "warn",
        logging.ERROR: "error",
        logging.CRITICAL: "error",
    }

    def __init__(self, manager: "LogManager"):
        super().__init__()
        self.manager = manager

    def emit(self, record: logging.LogRecord):
        try:
            level = self.LEVEL_MAP.get(record.levelno, "info")
            self.manager.add_log(level, self.format(record))
        except Exception:
            self.handleError(record)


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
        self._total_count = 0
        self._logging_handler: Optional[LogManagerHandler] = None
        self._console_handler: Optional[logging.Handler] = None
        self._installed = False

    def start_capture(self):
        """开始捕获 stdout 和 stderr"""
        if self._installed:
            return

        self._original_stdout = sys.stdout
        self._original_stderr = sys.stderr
        self._capture_streams["stdout"] = LogCaptureStream(self, "info", self._original_stdout)
        self._capture_streams["stderr"] = LogCaptureStream(self, "error", self._original_stderr)
        sys.stdout = self._capture_streams["stdout"]
        sys.stderr = self._capture_streams["stderr"]

        root_logger = logging.getLogger()
        root_logger.setLevel(logging.DEBUG)

        self._logging_handler = LogManagerHandler(self)
        self._logging_handler.setLevel(logging.DEBUG)
        self._logging_handler.setFormatter(logging.Formatter("[%(name)s] %(message)s"))
        root_logger.addHandler(self._logging_handler)

        self._console_handler = logging.StreamHandler(self._original_stderr or NullTextStream())
        self._console_handler.setLevel(logging.DEBUG)
        self._console_handler.setFormatter(logging.Formatter("%(levelname)s:%(name)s:%(message)s"))
        root_logger.addHandler(self._console_handler)
        self._installed = True

    def stop_capture(self):
        """停止捕获并恢复原始输出"""
        # 刷新流中的剩余内容
        for stream in self._capture_streams.values():
            if hasattr(stream, "flush"):
                stream.flush()

        sys.stdout = self._original_stdout
        sys.stderr = self._original_stderr
        self._capture_streams.clear()

        root_logger = logging.getLogger()
        for handler in (self._logging_handler, self._console_handler):
            if handler is not None:
                root_logger.removeHandler(handler)
                handler.close()
        self._logging_handler = None
        self._console_handler = None
        self._installed = False

    def add_log(self, level: LogLevel, message: str):
        """添加日志项"""
        if not message:
            return

        with self._lock:
            if not self._filters.get(level, True):
                return

            timestamp = datetime.now().strftime("%H:%M:%S")
            entry = LogEntry(timestamp, level, message, index=self._total_count)
            self.logs.append(entry)
            self._total_count += 1

    def get_total_count(self) -> int:
        with self._lock:
            return self._total_count

    def get_logs_since(
        self, since_index: int, levels: List[LogLevel] = None, search_text: str = ""
    ) -> Dict:
        with self._lock:
            if levels is None:
                levels = ["debug", "info", "warn", "error"]

            search_lower = search_text.lower()
            new_entries = []

            for entry in self.logs:
                if entry._index is not None and entry._index <= since_index:
                    continue
                if entry.level not in levels:
                    continue
                if search_lower and search_lower not in entry.message.lower():
                    continue
                new_entries.append(entry.to_dict())

            return {"logs": new_entries, "total_count": self._total_count}

    def get_logs(
        self, levels: List[LogLevel] = None, search_text: str = ""
    ) -> List[Dict]:
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
            self._total_count = 0

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


def get_logger(name: str = "luminalium") -> logging.Logger:
    """获取标准 logging logger。"""
    return logging.getLogger(name)
