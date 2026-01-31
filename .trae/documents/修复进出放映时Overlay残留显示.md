## 问题定位
- 截图表现为：退出放映后 Overlay 的左右翻页条仍残留在桌面/浏览器上，说明 Overlay 窗口没有被真正 hide（只是工具栏/布局变化）。
- 近期为“受限提示遮罩”加入了 `force_visible` 逻辑：当遮罩存在时会阻止 `_hide_if_inactive()` 隐藏窗口。
- 进/出放映切换时存在竞态：放映刚结束但遮罩/计时器仍在，导致 `set_active_on_slideshow(False)` 调用后 `_hide_if_inactive()` 被拦截，Overlay 留在屏幕上。

## 优化目标
- 退出放映时：无论当前遮罩/计时器状态如何，都必须立即清理并隐藏 Overlay（避免残留 UI）。
- 进入放映时：清理上一次会话遗留的遮罩状态，确保 UI 状态从干净开始。

## 实现方案
### 1) 给 OverlayWindow 增加“会话结束强制收尾”方法
- 新增 `on_slideshow_end_cleanup()`（或类似命名）：
  - 停止受限提示倒计时（ppt_restriction_tick）
  - 清空 `_mask_reasons` 并隐藏遮罩层
  - 复位 `_force_visible_mask=False`
  - 停止 `_hide_after_fade_timer`
  - 调用 `_apply_win32_slideshow_binding(False)`，并尽力把窗口从 TopMost 复位
  - 最后 `hide()`

### 2) 在 main.py 的 on_slideshow_end 中调用强制收尾
- 当前仅调用 `overlay.set_active_on_slideshow(False)`，改为：
  - 先 `overlay.on_slideshow_end_cleanup()`（强制收尾）
  - 再 `overlay.set_active_on_slideshow(False, animate=False)`（保持现有逻辑兼容）

### 3) 进入放映时做一次轻量清理
- 在 `on_slideshow_start` 触发时：调用 `overlay.on_slideshow_start_cleanup()`（可复用上面的部分逻辑），清掉上一轮残留遮罩/force 标志，避免“刚进入放映就被旧遮罩顶住不隐藏/不刷新”。

### 4) 加一道兜底：set_active_on_slideshow(False) 时允许隐藏
- 在 `OverlayWindow.set_active_on_slideshow(False)` 路径中：如果当前没有任何遮罩原因（`_mask_reasons` 为空），则强制 `_force_visible_mask=False`，避免历史状态导致偶发不 hide。

## 验证方式
- 连续多次：进入放映 → 立即退出放映（含受保护视图/只读场景）。
- 观察：退出放映后左右翻页条不会残留；进入放映后遮罩与页码提示正常。

## 涉及文件
- d:/Documents/Kazuha/ppt_assistant/ui/overlay.py
- d:/Documents/Kazuha/main.py