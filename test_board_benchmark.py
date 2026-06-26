#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Board.qml 最小对比测试

用途：
- 快速对比不同渲染模式的性能
- 自动运行多次测试并输出对比结果
- 不显示窗口，只测量加载时间

使用方法：
    python test_board_benchmark.py
"""

import os
import sys
import time
import types
import subprocess

# 禁用日志捕获和详细日志
os.environ["LUMINALIUM_DISABLE_LOG_CAPTURE"] = "1"
os.environ["QT_LOGGING_RULES"] = "*.critical=true"  # 只显示严重错误

print("=" * 70)
print("Board.qml 性能对比测试")
print("=" * 70)
print()

# 测试配置
TEST_CONFIGS = [
    {
        "name": "硬件加速 (GPU)",
        "env": {
            "QT_QUICK_BACKEND": None,  # None表示删除该环境变量
            "QSG_RHI_BACKEND": None,
        }
    },
    {
        "name": "软件渲染 (CPU)",
        "env": {
            "QT_QUICK_BACKEND": "software",
            "QSG_RHI_BACKEND": "software",
        }
    },
]

results = []

for config in TEST_CONFIGS:
    print(f"正在测试: {config['name']}")
    print("-" * 70)
    
    # 设置环境变量
    for key, value in config['env'].items():
        if value is None:
            os.environ.pop(key, None)
        else:
            os.environ[key] = value
    
    # 模拟main模块
    def _init_trace(msg):
        pass  # 静默模式
    
    fake_main = types.ModuleType('main')
    fake_main._init_trace = _init_trace
    sys.modules['main'] = fake_main
    
    try:
        # 导入（只在第一次测试时导入）
        if 'PySide6.QtWidgets' not in sys.modules:
            from PySide6.QtWidgets import QApplication
            from PySide6.QtCore import Qt
        
        if 'plugins.builtins.board.board_window' not in sys.modules:
            from plugins.builtins.board.board_window import BoardWindow
        
        # 创建QApplication（如果还没有）
        if QApplication.instance() is None:
            app = QApplication(sys.argv)
        else:
            app = QApplication.instance()
        
        # 计时测试（3次取平均）
        times = []
        for i in range(3):
            print(f"  第 {i+1}/3 次测试...", end=" ", flush=True)
            
            t_start = time.time()
            board = BoardWindow()
            t_end = time.time()
            
            elapsed = t_end - t_start
            times.append(elapsed)
            
            print(f"{elapsed:.3f}秒")
            
            # 立即销毁以准备下次测试
            board.close()
            board.deleteLater()
            app.processEvents()
            time.sleep(0.5)
        
        avg_time = sum(times) / len(times)
        min_time = min(times)
        max_time = max(times)
        
        results.append({
            "name": config['name'],
            "avg": avg_time,
            "min": min_time,
            "max": max_time,
            "times": times,
            "success": True,
        })
        
        print(f"  ✓ 平均: {avg_time:.3f}秒 (最快: {min_time:.3f}s, 最慢: {max_time:.3f}s)")
        
    except Exception as e:
        print(f"  ✗ 测试失败: {e}")
        results.append({
            "name": config['name'],
            "success": False,
            "error": str(e),
        })
    
    print()

# 输出汇总结果
print("=" * 70)
print("测试结果汇总")
print("=" * 70)
print()

if not results:
    print("✗ 没有成功的测试结果")
    sys.exit(1)

# 排序（按平均时间）
success_results = [r for r in results if r.get('success')]
if not success_results:
    print("✗ 所有测试都失败了")
    for r in results:
        print(f"  {r['name']}: {r.get('error', '未知错误')}")
    sys.exit(1)

success_results.sort(key=lambda x: x['avg'])

# 显示结果表格
print(f"{'模式':<20} {'平均时间':<12} {'最快':<10} {'最慢':<10} {'评级':<10}")
print("-" * 70)

for i, r in enumerate(success_results):
    avg = r['avg']
    
    # 评级
    if avg < 1.0:
        rating = "优秀 ⭐⭐⭐"
    elif avg < 3.0:
        rating = "良好 ⭐⭐"
    elif avg < 5.0:
        rating = "一般 ⭐"
    else:
        rating = "较慢 ⚠"
    
    # 标记最快的
    prefix = "👑 " if i == 0 else "   "
    
    print(f"{prefix}{r['name']:<17} {avg:>6.3f}秒     {r['min']:>6.3f}s   {r['max']:>6.3f}s   {rating}")

print()
print("=" * 70)
print("结论")
print("=" * 70)

fastest = success_results[0]
print(f"✓ 最快模式: {fastest['name']}")
print(f"✓ 平均加载时间: {fastest['avg']:.3f}秒")
print()

if len(success_results) > 1:
    slowest = success_results[-1]
    speedup = slowest['avg'] / fastest['avg']
    print(f"性能对比:")
    print(f"  {fastest['name']} 比 {slowest['name']} 快 {speedup:.1f}x")
    print()

# 建议
if fastest['avg'] > 5.0:
    print("⚠ 建议:")
    print("  所有渲染模式都比较慢 (>5秒)")
    print("  这可能是QML文件本身过于复杂导致的")
    print("  考虑优化Board.qml的结构")
elif fastest['name'].startswith("软件"):
    print("⚠ 注意:")
    print("  软件渲染比硬件加速更快，这很不寻常")
    print("  可能是GPU驱动问题或硬件不兼容")
    print("  建议检查显卡驱动")
else:
    print("✓ 推荐:")
    print(f"  使用 {fastest['name']} 以获得最佳性能")

print("=" * 70)
