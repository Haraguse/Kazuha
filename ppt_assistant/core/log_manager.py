"""
日志管理器 - 基于标准logging模块的规范化日志系统

使用方式:
    from ppt_assistant.core.log_manager import get_logger
    logger = get_logger(__name__)
    logger.info("This is a log message")
    
获取日志历史:
    from ppt_assistant.core.log_manager import get_log_manager
    manager = get_log_manager()
    logs = manager.get_logs()
"""
import os
import sys
import io
import logging
import threading
from datetime import datetime
from collections import deque
from typing import List, Dict, Literal, Optional

LogLevel = Literal["debug", "info", "warn", "error"]

# 日志级别映射
LEVEL_MAP = {
    "debug": logging.DEBUG,
    "info": logging.INFO,
    "warn": logging.WARNING,
    "error": logging.ERROR,
}

REVERSE_LEVEL_MAP = {
    logging.DEBUG: "debug",
    logging.INFO: "info",
    logging.WARNING: "warn",
    logging.ERROR: "error",
}


class LogEntry:
    """日志项"""

    def __init__(self, timestamp: str, level: LogLevel, message: str, index: int = None):
        self.timestamp = timestamp
        self.level = level
        self.message = message
        self._index = index

    def to_dict(self) -> Dict:
        return {"time": self.timestamp, "level": self.level, "message": self.message, "_idx": self._index}


class MemoryLogHandler(logging.Handler):
    """内存日志处理器 - 将日志保存到deque供UI查看"""

    def __init__(self, manager: "LogManager"):
        super().__init__()
        self.manager = manager

    def emit(self, record: logging.LogRecord):
        """处理日志记录"""
        try:
            # 转换logging级别到自定义级别
            level = REVERSE_LEVEL_MAP.get(record.levelno, "info")
            
            # 检查过滤器
            if not self.manager._filters.get(level, True):
                return
            
            # 格式化消息
            message = self.format(record)
            
            # 添加到内存
            with self.manager._lock:
                timestamp = datetime.fromtimestamp(record.created).strftime("%H:%M:%S")
                entry = LogEntry(timestamp, level, message, index=self.manager._total_count)
                self.manager.logs.append(entry)
                self.manager._total_count += 1
        except Exception:
            self.handleError(record)


class PrintCaptureHandler(io.TextIOBase):
    """
    捕获print输出的处理器 - 同时输出到console和logger
    这样既能在终端看到输出，也能在日志窗口中查看
    """
    
    def __init__(self, logger: logging.Logger, level: int, original_stream):
        self.logger = logger
        self.level = level
        self.original_stream = original_stream
        self.buffer = ""
    
    def write(self, s: str) -> int:
        if isinstance(s, str):
            # 1. 先输出到原始流（console）
            try:
                self.original_stream.write(s)
            except Exception:
                pass
            
            # 2. 然后捕获到logger（供日志窗口查看）
            self.buffer += s
            # 检查是否有完整的行
            if "\n" in self.buffer or "\r" in self.buffer:
                lines = self.buffer.split("\n")
                for line in lines[:-1]:
                    if line.strip():
                        self.logger.log(self.level, line.strip())
                self.buffer = lines[-1]
        return len(s) if isinstance(s, str) else 0
    
    def flush(self):
        # 1. flush原始流
        if hasattr(self.original_stream, 'flush'):
            try:
                self.original_stream.flush()
            except Exception:
                pass
        
        # 2. flush时处理剩余的缓冲到logger
        if self.buffer.strip():
            self.logger.log(self.level, self.buffer.strip())
            self.buffer = ""


class LogManager:
    """日志管理器 - 收集应用的所有日志输出"""

    MAX_LOGS = 10000  # 最大保存的日志数

    def __init__(self, max_logs: int = None):
        self.max_logs = max_logs or self.MAX_LOGS
        self.logs: deque = deque(maxlen=self.max_logs)
        self._lock = threading.RLock()
        self._filters = {"debug": True, "info": True, "warn": True, "error": True}
        self._total_count = 0
        self._logger = None
        self._memory_handler = None
        self._console_handler = None
        self._stdout_capture = None
        self._stderr_capture = None
        self._original_stdout = sys.stdout
        self._original_stderr = sys.stderr

    def setup_logging(self):
        """设置日志系统"""
        # 获取根logger
        self._logger = logging.getLogger("luminalium")
        self._logger.setLevel(logging.DEBUG)
        self._logger.propagate = False  # 不传播到root logger

        # 清除已有的handlers（避免重复）
        self._logger.handlers.clear()

        # 1. Console Handler - 输出到真实的stdout（不会被拦截）
        self._console_handler = logging.StreamHandler(sys.__stdout__)
        self._console_handler.setLevel(logging.DEBUG)
        console_formatter = logging.Formatter(
            "[%(levelname)s] %(message)s"
        )
        self._console_handler.setFormatter(console_formatter)
        self._logger.addHandler(self._console_handler)

        # 2. Memory Handler - 保存到内存供UI查看
        self._memory_handler = MemoryLogHandler(self)
        self._memory_handler.setLevel(logging.DEBUG)
        memory_formatter = logging.Formatter("%(message)s")  # 只保存消息本身
        self._memory_handler.setFormatter(memory_formatter)
        self._logger.addHandler(self._memory_handler)

    def start_capture(self):
        """
        启动日志捕获
        - 设置logging系统
        - 重定向stdout/stderr到logger（这样所有print调用都会被捕获）
        """
        self.setup_logging()
        
        # 重定向stdout/stderr到logger
        # 注意：这会捕获所有print输出，但Qt消息处理器已经使用sys.__stdout__，所以不会被捕获
        self._stdout_capture = PrintCaptureHandler(self._logger, logging.INFO, self._original_stdout)
        self._stderr_capture = PrintCaptureHandler(self._logger, logging.ERROR, self._original_stderr)
        
        sys.stdout = self._stdout_capture
        sys.stderr = self._stderr_capture

    def stop_capture(self):
        """停止日志捕获并恢复原始输出"""
        # 恢复原始stdout/stderr
        if self._stdout_capture:
            sys.stdout = self._original_stdout
            self._stdout_capture = None
        if self._stderr_capture:
            sys.stderr = self._original_stderr
            self._stderr_capture = None
        
        # 清理logger handlers
        if self._logger:
            self._logger.handlers.clear()

    def add_log(self, level: LogLevel, message: str):
        """
        添加日志项（兼容旧接口）
        新代码应该使用 get_logger(__name__).info() 而不是这个方法
        """
        if not message:
            return

        # 通过logger添加日志
        if self._logger:
            log_level = LEVEL_MAP.get(level, logging.INFO)
            self._logger.log(log_level, message)

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
_log_manager: Optional[LogManager] = None


def get_log_manager() -> LogManager:
    """获取全局日志管理器实例"""
    global _log_manager
    if _log_manager is None:
        _log_manager = LogManager()
    return _log_manager


def init_log_manager():
    """初始化并启动日志管理器"""
    manager = get_log_manager()
    disable_capture = os.environ.get("LUMINALIUM_DISABLE_LOG_CAPTURE", "").strip().lower()
    if disable_capture in ["1", "true"]:
        print("[LogManager] Log capture disabled by environment variable")
        return manager
    manager.start_capture()
    return manager


def get_logger(name: str = None) -> logging.Logger:
    """
    获取logger实例
    
    使用方式:
        logger = get_logger(__name__)
        logger.info("This is a log message")
    """
    if name:
        return logging.getLogger(f"luminalium.{name}")
    return logging.getLogger("luminalium")
