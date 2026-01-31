## 现象与根因推断
- 截图里右侧翻页条落在屏幕中间，典型是 Overlay 窗口宽度被算成了实际的一半。
- 代码链路：`PPTWorker._update_window_rect()` 采集放映窗口 raw rect（优先 Win32 GetWindowRect，失败则 COM 的 Left/Top/Width/Height）→ UI 线程 `PPTMonitor._on_geometry_changed()` 统一按“raw=物理像素”做 `/ dpr` 换算 → `OverlayWindow.update_geometry()` 直接 setGeometry。
- 当采集走到 COM 回退分支时，这些 Left/Width 在部分单屏/缩放环境中可能已经是“逻辑坐标”（已按 DPI 缩放过）。此时再 `/ dpr` 会导致宽高被缩小一半，从而出现截图的错位。

## 修复目标
- 不管 raw rect 来自 Win32 还是 COM，都能在单屏/不同 DPI 缩放下得到正确的 Overlay 窗口几何。

## 实现方案
### 1) 在 UI 线程做“raw 坐标单位”自动识别
- 修改 `PPTMonitor._on_geometry_changed()`：
  - 在拿到 `ppt_screen`、Win32 `m_width/m_height`（物理像素）和 `dpr` 后，计算两种假设的误差：
    - 假设 raw 是物理：`diff_physical = |w - m_width| + |h - m_height|`
    - 假设 raw 是逻辑：`diff_logical = |w*dpr - m_width| + |h*dpr - m_height|`
  - 当 `diff_logical` 显著更小（且 dpr>1）时，认为 raw 已经是逻辑坐标。

### 2) 按识别结果选择换算方式
- 若 raw=物理（现有逻辑不变）：继续用当前公式 `/ dpr` 计算 `rect_logical`。
- 若 raw=逻辑：
  - 直接使用 `QRect(x, y, w, h)` 作为 `rect_logical`（不再 `/ dpr`，也不再用 `ppt_p_origin` 做换算）。
  - 增加一次安全兜底：如果该 rect 与 `ppt_screen.geometry()` 几乎不相交，则回退到原来的物理换算公式（避免极端坐标系不一致）。

### 3) 兼容多种模式
- 仅在 `display_screen == ppt_screen`（跟随放映屏）时做上述识别与换算。
- 当 `display_screen != ppt_screen`（用户指定铺满某屏）保持原逻辑 `rect_logical = display_screen.geometry()`。

## 验证方式
- 单屏模式 + Windows 缩放 125%/150%/200%：进入放映后左右翻页条应贴边，工具栏/遮罩不偏移。
- 快速多次进入/退出放映：不出现半屏/错位。

## 涉及文件
- d:/Documents/Kazuha/ppt_assistant/core/ppt_monitor.py（`_on_geometry_changed`）