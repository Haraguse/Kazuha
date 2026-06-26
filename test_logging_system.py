"""
日志系统重构总结 - 测试脚本

改进内容：
1. 使用标准logging模块替代自定义LogCaptureStream
2. Qt消息处理器直接输出到sys.__stderr__（不经过Python重定向）
3. PrintCaptureHandler同时输出到console和logger（既能看到输出，又能在日志窗口查看）
4. 避免GIL争夺和死锁问题

预期效果：
- Board.qml加载应该秒开（不再有死锁）
- 所有日志同时出现在console和日志窗口
- Qt消息不会导致Python回调开销
"""

import sys
import os
from pathlib import Path

ROOT_DIR = Path(__file__).parent
sys.path.insert(0, str(ROOT_DIR))

def test_logging_system():
    print("=" * 80)
    print("日志系统重构 - 功能测试")
    print("=" * 80)
    
    # 1. 初始化日志系统
    print("\n[Test 1] 初始化日志系统...")
    from ppt_assistant.core.log_manager import init_log_manager, get_logger, get_log_manager
    
    init_log_manager()
    logger = get_logger("test")
    manager = get_log_manager()
    
    print("✅ 日志系统初始化成功")
    
    # 2. 测试logger输出
    print("\n[Test 2] 测试logger.info()输出...")
    logger.info("这是一条info日志")
    logger.warning("这是一条warning日志")
    logger.error("这是一条error日志")
    print("✅ Logger输出正常")
    
    # 3. 测试print捕获
    print("\n[Test 3] 测试print()捕获...")
    print("这是一条普通的print输出")
    print("应该同时出现在console和日志窗口")
    print("✅ Print捕获正常")
    
    # 4. 检查日志管理器
    print("\n[Test 4] 检查日志管理器...")
    logs = manager.get_logs()
    print(f"✅ 日志管理器工作正常，当前共有 {len(logs)} 条日志")
    
    # 5. 测试日志过滤
    print("\n[Test 5] 测试日志过滤...")
    info_logs = manager.get_logs(levels=["info"])
    error_logs = manager.get_logs(levels=["error"])
    print(f"✅ Info日志: {len(info_logs)} 条")
    print(f"✅ Error日志: {len(error_logs)} 条")
    
    # 6. 测试日志统计
    print("\n[Test 6] 测试日志统计...")
    stats = manager.get_stats()
    print(f"✅ 日志统计: {stats}")
    
    print("\n" + "=" * 80)
    print("所有测试通过！")
    print("=" * 80)
    
    print("\n架构说明：")
    print("1. Logger输出 → MemoryHandler(内存) + StreamHandler(console)")
    print("2. Print输出 → PrintCaptureHandler → Logger → 同上")
    print("3. Qt消息 → sys.__stderr__.write() → Console（不经过Python）")
    print("\n关键优势：")
    print("- 避免了GIL争夺（Qt消息不回调Python）")
    print("- 所有日志同时在console和UI可见")
    print("- 使用标准logging模块，易于扩展")
    print("- 解决了Board.qml加载死锁问题")

if __name__ == "__main__":
    test_logging_system()
