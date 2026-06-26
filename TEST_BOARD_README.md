# Board.qml 测试工具集

本目录包含用于诊断 Board.qml 加载性能问题的独立测试脚本。

## 📁 文件说明

### `test_board_standalone.py` - 完整功能测试
**用途**: 详细测试单次Board窗口加载过程
**特点**:
- 完整的环境复用（配置、主题、国际化等）
- 精确的加载时间测量
- 详细的Qt调试日志输出
- 可以切换软件/硬件渲染模式
- 测试完成后显示窗口可交互

**使用方法**:
```powershell
# 基本使用
python test_board_standalone.py

# 修改测试模式：编辑文件中的 TEST_MODE 变量
# - "hardware": 硬件加速（GPU）
# - "software": 软件渲染（CPU）
# - "auto": 使用main.py的默认设置
```

### `test_board_benchmark.py` - 性能对比测试
**用途**: 自动对比不同渲染模式的性能
**特点**:
- 自动测试多种渲染模式
- 每个模式运行3次取平均值
- 生成性能对比报告
- 不显示窗口（纯性能测试）
- 自动给出优化建议

**使用方法**:
```powershell
python test_board_benchmark.py
```

## 🚀 快速开始

### 步骤1：基础诊断

首先运行独立测试，了解当前的加载性能：

```powershell
python test_board_standalone.py
```

**观察输出**:
- 如果加载时间 < 3秒 → 性能正常
- 如果加载时间 3-10秒 → 性能一般，可以优化
- 如果加载时间 > 10秒 → 性能较差，需要排查

### 步骤2：性能对比

运行性能对比测试，找出最快的渲染模式：

```powershell
python test_board_benchmark.py
```

**查看结果**:
- 对比硬件加速 vs 软件渲染的性能差异
- 找出最适合当前系统的渲染模式

### 步骤3：应用优化

根据测试结果，在 `main.py` 中调整渲染设置：

**如果硬件加速更快**:
```python
# 在 main.py 的 _apply_graphics_settings() 函数中
# 移除或注释掉强制软件渲染的代码：
# os.environ["QSG_RHI_BACKEND"] = "software"
# os.environ["QT_QUICK_BACKEND"] = "software"
```

**如果软件渲染更快**（通常在虚拟机或老旧硬件上）:
```python
# 保持现有的软件渲染设置
os.environ["QSG_RHI_BACKEND"] = "software"
os.environ["QT_QUICK_BACKEND"] = "software"
```

## 🔍 故障排查

### 问题1：import错误

**错误信息**: `ModuleNotFoundError: No module named 'ppt_assistant'`

**解决方法**: 确保在项目根目录运行脚本
```powershell
cd D:\sectl\Luminalium
python test_board_standalone.py
```

### 问题2：QML加载失败

**错误信息**: `QML Status: Error`

**解决方法**: 
1. 检查 `test_board_standalone.py` 输出的QML错误详情
2. 确认 `plugins/builtins/board/Board.qml` 文件存在
3. 检查是否缺少依赖的QML模块

### 问题3：创建BoardWindow时崩溃

**可能原因**:
- GPU驱动不兼容（尝试软件渲染模式）
- QML中有死循环或无限递归
- Context属性未正确设置

**诊断步骤**:
1. 在 `test_board_standalone.py` 中设置 `TEST_MODE = "software"`
2. 设置 `ENABLE_QT_DEBUG_LOG = True` 查看详细日志
3. 检查输出中的Qt错误信息

### 问题4：加载时间始终很长（>15秒）

**可能原因**:
1. QML文件本身过于复杂（1671行代码）
2. 大量的属性绑定和JavaScript函数
3. 软件渲染模式性能低下

**优化建议**:
1. 尝试硬件加速模式
2. 考虑拆分Board.qml为多个小文件
3. 使用Loader延迟加载非核心UI组件
4. 增加watchdog超时时间（临时方案）

## 📊 性能基准参考

| 渲染模式 | 预期加载时间 | 评级 |
|---------|------------|------|
| 硬件加速（现代GPU） | < 2秒 | 优秀 ⭐⭐⭐ |
| 硬件加速（集成显卡） | 2-5秒 | 良好 ⭐⭐ |
| 软件渲染 | 5-15秒 | 一般 ⭐ |
| 软件渲染（低配机器） | > 15秒 | 较慢 ⚠ |

## 🛠 高级用法

### 自定义测试场景

编辑 `test_board_standalone.py` 中的配置：

```python
# 渲染模式
TEST_MODE = "hardware"  # 或 "software", "auto"

# 启用/禁用详细日志
ENABLE_QT_DEBUG_LOG = True

# 禁用日志捕获（让print输出）
DISABLE_LOG_CAPTURE = True
```

### 分析Qt内部日志

启用详细日志后，查找关键信息：

```powershell
python test_board_standalone.py > board_test.log 2>&1
```

在 `board_test.log` 中搜索：
- `"compile"` - QML编译耗时
- `"shader"` - GPU着色器编译
- `"texture"` - 纹理创建
- `"error"` 或 `"warning"` - 错误和警告

### 测试特定QML文件

如果想测试简化版的QML，可以临时修改Board.qml或创建测试版本：

1. 复制 `Board.qml` 为 `Board_test.qml`
2. 注释掉部分复杂组件
3. 临时修改 `board_window.py:1291` 加载测试文件
4. 运行 `test_board_standalone.py`

## 📝 输出示例

### 成功的测试输出

```
======================================================================
Board.qml 独立测试脚本
======================================================================
[Config] 渲染模式: 硬件加速 (GPU)
[Config] Qt调试日志: 启用
[Config] 日志捕获: 禁用 (print直接输出)
----------------------------------------------------------------------
[Env] QT_QUICK_BACKEND = (default)
[Env] QSG_RHI_BACKEND = (default)
======================================================================

[Setup] 模拟main模块完成
[Setup] 导入PySide6...
[Setup] 导入ppt_assistant.core...
[Setup] ✓ cfg已初始化
[Setup] 导入board_window...
[Setup] ✓ BoardWindow已导入
[Setup] ✓ 所有依赖导入完成

======================================================================
开始测试
======================================================================
[Test] 创建QApplication...
[Test] ✓ QApplication已创建

----------------------------------------------------------------------
[Test] 开始创建BoardWindow...
[Test] 注意: setSource()会在BoardWindow.__init__()中自动调用
----------------------------------------------------------------------

[BoardWindow] Creating QQuickWidget
[BoardWindow] QQuickWidget created
[BoardWindow] Calling QQuickWidget.setSource()...
[BoardWindow] setSource() returned, status=1
[BoardWindow] __init__ END (SUCCESS)

======================================================================
测试结果 - 成功
======================================================================
✓ BoardWindow创建成功
✓ 总耗时: 2.345 秒
✓ QML状态: Ready (加载成功)
======================================================================

性能分析:
----------------------------------------------------------------------
✓ 良好! 加载时间 < 3秒

提示: 当前使用硬件加速
======================================================================
```

## 🤝 贡献

如果发现测试脚本的问题或有改进建议，请在项目中反馈。

## 📄 许可

这些测试脚本随Luminalium项目一起发布，遵循相同的许可协议。
