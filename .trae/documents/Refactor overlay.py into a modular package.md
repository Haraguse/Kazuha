# 重构 `overlay.py` 计划

## 1. 结构模块化 (Package Conversion)
将 `ppt_assistant\ui\overlay.py` (3000+行) 拆分为一个包目录 `ppt_assistant\ui\overlay\`，并按照职责划分模块：

- **`__init__.py`**: 导出 `OverlayWindow` 主窗口类，保持对外接口一致。
- **`constants.py`**: 定义全局常量（如 `ICON_DIR`, `PLUGIN_DIR`）和本地翻译字典。
- **`utils.py`**: 
    - 颜色处理：`_hex_to_rgb`, `_parse_color`, `_mix_color` 等。
    - 主题逻辑：`_get_palette`, `_build_monet_palette` 等。
    - Win32 辅助：提取窗口置顶、DPI 适配等底层 API 调用。
- **`components.py`**: 提取通用基础组件：
    - `GlobalIconCache`: 全局图标缓存。
    - `ClickableLabel`, `MarqueeLabel`: 交互式标签。
    - `CustomToolButton`, `IndeterminateSpinner`: 状态指示器。
    - `ReloadMask`: 刷新遮罩。
- **`status_bar.py`**: 
    - `StatusBarWidget`: 状态栏逻辑。
    - `NetworkCheckThread`: 网络状态监测线程。
- **`toolbar.py`**: 
    - `ToolbarWidget`: 悬浮工具栏。
    - `PageFlipWidget`, `PageFlipButton`: 翻页控件。
- **`popups.py`**: 
    - `PenColorPopup`: 画笔颜色选择弹窗。
    - `SlidePreviewPopup`: 幻灯片预览弹窗。
- **`watchdog.py`**: `UiBlockWatchdog` 界面阻塞监控线程。
- **`window.py`**: `OverlayWindow` 主窗口类，负责布局管理和全局事件分发。

## 2. 逻辑优化 (Logic Improvements)
- **配置访问**: 消除所有 `json.load(SETTINGS_PATH)` 的手动文件读取，统一使用 `ppt_assistant.core.config.cfg` 对象，提升性能并减少 I/O。
- **国际化**: 将翻译项整合到 `ppt_assistant.core.i18n` 或在该目录下建立统一的翻译管理机制。
- **代码清理**:
    - 统一信号槽装饰器 (`@Slot`)。
    - 移除注释掉的死代码（如 `_update_color_from_screen`）。
    - 改进 `try...except Exception: pass` 为带有日志记录的异常处理。
- **动画封装**: 简化 `QPropertyAnimation` 的手动管理，通过辅助方法统一处理 UI 元素的淡入淡出和滑动效果。

## 3. 兼容性与稳定性 (Compatibility & Stability)
- 封装 Win32 特有 API 调用，增加非 Windows 平台的降级处理。
- 优化 `GlobalIconCache` 的缓存策略，确保图标加载的高效与正确。

## 4. 验证计划 (Verification)
- 确保重构后程序能正常启动并正确显示悬浮工具栏。
- 验证所有交互（画笔、聚光灯、翻页、状态栏显示）功能完备。
- 检查切换语言和主题时 UI 是否能实时响应。
