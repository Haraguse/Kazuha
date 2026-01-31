## 问题定位（已完成调研）
- 监控层目前只用 `PowerPoint.Application.SlideShowWindows` 来判定“正在放映”，并且大量 COM 调用异常被 `except: pass` 吞掉，导致在只读/受保护视图（Protected View）下出现“检测不到放映/按钮无响应”的现象。
- 关键位置：PPT 状态检测与控制入口在 [ppt_monitor.py](file:///d:/Documents/Kazuha/ppt_assistant/core/ppt_monitor.py)；overlay 是否激活严格依赖 `slideshow_started/slideshow_ended`，见 [main.py](file:///d:/Documents/Kazuha/main.py)。

## 修复目标
- PPT 以“只读/受保护视图”打开时，进入放映后 overlay 仍能正常激活。
- 至少保证“上一页/下一页/结束放映/清屏”等核心控制可用；对于在只读/受保护下不允许的能力（如画笔颜色/指针类型/导出缩略图），给出明确提示而不是静默失败。

## 实施方案
### 1) 增加只读/受保护视图识别
- 在 [ppt_monitor.py](file:///d:/Documents/Kazuha/ppt_assistant/core/ppt_monitor.py) 的 `_check_ppt_state` 中：
  - 尝试读取 `ppt_app.ProtectedViewWindows.Count`（存在则标记为“受保护视图”）。
  - 若已拿到 `ss_win.Presentation`，尝试读取 `Presentation.ReadOnly`（存在则标记为“只读演示文稿”）。
  - 这些读取失败时不再直接吞掉，而是进入“降级模式”（见第 2 点）。

### 2) 引入 Win32 降级模式（绕过 COM）
- 当 `SlideShowWindows` 不可用或关键 COM 调用频繁失败时：
  - 用 Win32 枚举窗口，按窗口类名（常见 `screenClass`）+ 进程名 `powerpnt.exe` 定位放映窗口 hwnd。
  - 以 hwnd 作为“放映存在”的依据：触发 `slideshow_started`、持续更新窗口 rect；让 overlay 绑定 hwnd（overlay 已支持 hwnd 绑定，见 [overlay.py](file:///d:/Documents/Kazuha/ppt_assistant/ui/overlay.py)）。
  - 控制指令（下一页/上一页/结束放映/清屏）在 COM 失败时改为对该 hwnd 发送按键（PgDn/PgUp/Esc/E）。

### 3) UI 反馈（只读/受保护能力限制提示）
- 复用 overlay 的 mask 机制（见 [overlay.py](file:///d:/Documents/Kazuha/ppt_assistant/ui/overlay.py) 的 `_set_mask_reason`）：
  - 新增 reason（如 `ppt_readonly` / `ppt_protected_view`），在检测到只读/受保护且某功能不可用时显示短提示（例如“PPT 处于受保护视图，部分功能受限；如需画笔/缩略图请点击‘启用编辑’”）。
  - 不影响核心翻页等可降级能力。

### 4) 最小化可观测性（用于后续定位）
- 将关键控制函数的 `except: pass` 改为：只在首次/低频打印错误摘要（不输出敏感信息），或发一个轻量 signal 给主线程显示“当前受限/降级”。

## 验证方式
- 手动回归（Windows + PowerPoint）：
  - 正常模式打开 PPT → 放映 → overlay 激活 → 翻页/结束/清屏/画笔正常。
  - 受保护视图/只读方式打开 PPT（从下载文件/邮件打开）→ 放映 → overlay 仍激活 → 翻页/结束/清屏可用；画笔/缩略图若不可用则有明确提示。
  - Presenter View、多屏场景：确认 hwnd 选择仍稳定（优先 `screenClass`）。

## 涉及文件
- [ppt_monitor.py](file:///d:/Documents/Kazuha/ppt_assistant/core/ppt_monitor.py)
- [main.py](file:///d:/Documents/Kazuha/main.py)
- [overlay.py](file:///d:/Documents/Kazuha/ppt_assistant/ui/overlay.py)

确认这个方案后，我会开始改代码并做一次完整手动回归，优先保证“只读/受保护视图下翻页与结束放映可用”。