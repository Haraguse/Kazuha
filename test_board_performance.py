"""
测试Board加载性能 - 验证新日志系统是否解决了死锁问题
"""
import sys
import os
import time
from pathlib import Path

# Add project root to path
ROOT_DIR = Path(__file__).parent
sys.path.insert(0, str(ROOT_DIR))

def test_board_loading():
    print("=" * 80)
    print("Board加载性能测试")
    print("=" * 80)
    
    # 设置软件渲染（避免GPU问题）
    os.environ["QSG_RHI_BACKEND"] = "software"
    os.environ["QT_QUICK_BACKEND"] = "software"
    
    print("\n1. 启动Qt应用...")
    start_time = time.time()
    
    from PySide6.QtWidgets import QApplication
    app = QApplication(sys.argv)
    
    qt_init_time = time.time() - start_time
    print(f"   Qt应用启动耗时: {qt_init_time:.2f}秒")
    
    # 初始化日志系统
    print("\n2. 初始化新的日志系统...")
    log_start = time.time()
    
    from ppt_assistant.core.log_manager import init_log_manager, get_logger
    init_log_manager()
    logger = get_logger("test")
    
    log_init_time = time.time() - log_start
    print(f"   日志系统初始化耗时: {log_init_time:.2f}秒")
    logger.info("日志系统已就绪")
    
    # 注册QML类型
    print("\n3. 注册QML类型...")
    register_start = time.time()
    
    from PySide6.QtQml import qmlRegisterType
    from plugins.builtins.board.board_window import NativeBoardItem
    qmlRegisterType(NativeBoardItem, "KazuhaBoard", 1, 0, "NativeBoardItem")
    
    register_time = time.time() - register_start
    print(f"   QML类型注册耗时: {register_time:.2f}秒")
    
    # 创建BoardWindow
    print("\n4. 创建BoardWindow...")
    board_start = time.time()
    
    try:
        from plugins.builtins.board.board_window import BoardWindow
        board = BoardWindow()
        
        board_create_time = time.time() - board_start
        print(f"   BoardWindow创建耗时: {board_create_time:.2f}秒")
        logger.info(f"Board窗口创建成功，耗时: {board_create_time:.2f}秒")
        
        # 显示窗口
        print("\n5. 显示Board窗口...")
        board.show()
        
        total_time = time.time() - start_time
        
        print("\n" + "=" * 80)
        print("性能测试结果:")
        print("=" * 80)
        print(f"Qt初始化:       {qt_init_time:.2f}秒")
        print(f"日志系统初始化: {log_init_time:.2f}秒")
        print(f"QML类型注册:    {register_time:.2f}秒")
        print(f"Board窗口创建:  {board_create_time:.2f}秒")
        print(f"总耗时:         {total_time:.2f}秒")
        print("=" * 80)
        
        if board_create_time < 5:
            print("\n✅ 测试通过！Board加载速度正常（< 5秒）")
        elif board_create_time < 10:
            print("\n⚠️  警告：Board加载较慢（5-10秒）")
        else:
            print("\n❌ 失败：Board加载过慢（> 10秒）")
        
        # 保持窗口打开3秒以便查看
        print("\n窗口将在3秒后关闭...")
        from PySide6.QtCore import QTimer
        QTimer.singleShot(3000, app.quit)
        
        return app.exec()
        
    except Exception as e:
        print(f"\n❌ 错误: {e}")
        import traceback
        traceback.print_exc()
        return 1

if __name__ == "__main__":
    sys.exit(test_board_loading())
