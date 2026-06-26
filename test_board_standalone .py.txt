#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Board.qml 独立测试脚本

用途：
- 独立测试Board.qml加载性能
- 不启动完整的Luminalium主程序
- 复用所有现有的配置和依赖
- 精确计时QML加载时间

使用方法：
1. 修改下面的 TEST_MODE 来切换渲染模式
2. 运行: python test_board_standalone.py
"""

import os
import sys
import time
import types

# ============================================================
# 配置区域 - 可以修改这里来测试不同场景
# ============================================================

# 渲染模式选择
# - "hardware": 使用硬件加速（OpenGL/D3D）
# - "software": 使用CPU软件渲染
# - "auto": 使用main.py的默认设置
TEST_MODE = "software"

# 是否启用Qt详细日志
ENABLE_QT_DEBUG_LOG = False

# 是否禁用日志捕获（让print直接输出）
DISABLE_LOG_CAPTURE = True

# ============================================================
# 环境变量设置
# ============================================================

print("=" * 70)
print("Board.qml 独立测试脚本")
print("=" * 70)

if TEST_MODE == "software":
    print("[Config] 渲染模式: 软件渲染 (CPU)")
    os.environ["QT_QUICK_BACKEND"] = "software"
    os.environ["QSG_RHI_BACKEND"] = "software"
elif TEST_MODE == "hardware":
    print("[Config] 渲染模式: 硬件加速 (GPU)")
    # 移除软件渲染的环境变量，让Qt使用默认的硬件加速
    os.environ.pop("QT_QUICK_BACKEND", None)
    os.environ.pop("QSG_RHI_BACKEND", None)
else:
    print("[Config] 渲染模式: 自动 (使用main.py设置)")

if ENABLE_QT_DEBUG_LOG:
    print("[Config] Qt调试日志: 启用")
    os.environ["QT_LOGGING_RULES"] = "qt.qml*=true;qt.quick*=true"
    os.environ["QSG_INFO"] = "1"
else:
    print("[Config] Qt调试日志: 禁用")

if DISABLE_LOG_CAPTURE:
    print("[Config] 日志捕获: 禁用 (print直接输出)")
    os.environ["LUMINALIUM_DISABLE_LOG_CAPTURE"] = "1"
else:
    print("[Config] 日志捕获: 启用")

print("-" * 70)
print(f"[Env] QT_QUICK_BACKEND = {os.environ.get('QT_QUICK_BACKEND', '(default)')}")
print(f"[Env] QSG_RHI_BACKEND = {os.environ.get('QSG_RHI_BACKEND', '(default)')}")
print("=" * 70)
print()

# ============================================================
# 模拟main模块 - 避免导入整个main.py
# ============================================================

def _init_trace(msg):
    """模拟 main._init_trace() 函数"""
    timestamp = time.strftime("%H:%M:%S")
    print(f"[{timestamp}] {msg}", flush=True)

# 创建假的main模块并注入到sys.modules
fake_main = types.ModuleType('main')
fake_main._init_trace = _init_trace
sys.modules['main'] = fake_main

print("[Setup] 模拟main模块完成")

# ============================================================
# 导入依赖
# ============================================================

print("[Setup] 导入PySide6...")
from PySide6.QtWidgets import QApplication
from PySide6.QtCore import Qt

print("[Setup] 导入ppt_assistant.core...")
try:
    from ppt_assistant.core.config import cfg
    print(f"[Setup] ✓ cfg已初始化")
except Exception as e:
    print(f"[Setup] ✗ 导入cfg失败: {e}")
    sys.exit(1)

print("[Setup] 导入board_window...")
try:
    from plugins.builtins.board.board_window import BoardWindow
    print(f"[Setup] ✓ BoardWindow已导入")
except Exception as e:
    print(f"[Setup] ✗ 导入BoardWindow失败: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)

print("[Setup] ✓ 所有依赖导入完成")
print()

# ============================================================
# 创建测试应用
# ============================================================

print("=" * 70)
print("开始测试")
print("=" * 70)

# 创建QApplication
print("[Test] 创建QApplication...")
app = QApplication(sys.argv)
print("[Test] ✓ QApplication已创建")
print()

# ============================================================
# 计时测试 - 创建BoardWindow（setSource在__init__中）
# ============================================================

print("-" * 70)
print("[Test] 开始创建BoardWindow...")
print("[Test] 注意: setSource()会在BoardWindow.__init__()中自动调用")
print("-" * 70)
print()

t_start = time.time()
creation_failed = False
board_window = None

try:
    # BoardWindow的__init__会调用setSource加载QML
    board_window = BoardWindow()
    
    t_end = time.time()
    elapsed = t_end - t_start
    
    print()
    print("=" * 70)
    print("测试结果 - 成功")
    print("=" * 70)
    print(f"✓ BoardWindow创建成功")
    print(f"✓ 总耗时: {elapsed:.3f} 秒")
    
    # 检查QML状态
    if board_window._qml:
        from PySide6.QtQuickWidgets import QQuickWidget
        status = board_window._qml.status()
        
        print(f"✓ QML状态: ", end="")
        if status == QQuickWidget.Status.Ready:
            print("Ready (加载成功)")
        elif status == QQuickWidget.Status.Loading:
            print("Loading (加载中)")
        elif status == QQuickWidget.Status.Error:
            print("Error (错误)")
            errors = board_window._qml.errors()
            if errors:
                print("\n  QML错误列表:")
                for err in errors:
                    print(f"    - {err.toString()}")
        elif status == QQuickWidget.Status.Null:
            print("Null (未初始化)")
        else:
            print(f"Unknown ({status})")
    
    print("=" * 70)
    print()
    
    # 性能分析
    print("性能分析:")
    print("-" * 70)
    if elapsed < 1.0:
        print(f"✓ 优秀! 加载时间 < 1秒")
    elif elapsed < 3.0:
        print(f"✓ 良好! 加载时间 < 3秒")
    elif elapsed < 5.0:
        print(f"⚠ 一般，加载时间在 3-5秒之间")
    elif elapsed < 10.0:
        print(f"⚠ 较慢，加载时间在 5-10秒之间")
    else:
        print(f"✗ 很慢! 加载时间 > 10秒")
    
    print()
    
    if TEST_MODE == "software":
        print("提示: 当前使用软件渲染，尝试硬件加速可能更快")
        print("      修改脚本中 TEST_MODE = 'hardware' 后重新运行")
    elif TEST_MODE == "hardware":
        print("提示: 当前使用硬件加速")
        if elapsed > 5.0:
            print("      如果加载仍然很慢，可能是QML文件本身的复杂度问题")
            print("      或者尝试软件渲染: TEST_MODE = 'software'")
    
    print("=" * 70)
    print()
    
except KeyboardInterrupt:
    print()
    print("[Test] ✗ 用户中断测试")
    sys.exit(130)
    
except Exception as e:
    t_end = time.time()
    elapsed = t_end - t_start
    creation_failed = True
    
    print()
    print("=" * 70)
    print("测试结果 - 失败")
    print("=" * 70)
    print(f"✗ BoardWindow创建失败")
    print(f"✗ 失败前耗时: {elapsed:.3f} 秒")
    print(f"✗ 错误信息: {e}")
    print("=" * 70)
    print()
    print("详细错误堆栈:")
    print("-" * 70)
    import traceback
    traceback.print_exc()
    print("=" * 70)
    print()

# ============================================================
# 显示窗口（如果创建成功）
# ============================================================

if board_window and not creation_failed:
    print("[Test] 显示Board窗口...")
    print("[Test] 提示: 关闭窗口以退出测试")
    print()
    
    try:
        board_window.show()
        exit_code = app.exec()
        
        print()
        print("[Test] ✓ 应用正常退出")
        sys.exit(exit_code)
        
    except Exception as e:
        print()
        print(f"[Test] ✗ 运行时错误: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
else:
    print("[Test] ✗ 跳过窗口显示（创建失败）")
    sys.exit(1)
