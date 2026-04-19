# Fix Linux QML Overlay Issues Spec

## Why
- 当前 Linux QML Overlay 存在三个关键问题：
  1. 所有图标显示为方框（使用文本符号而非 SVG 图标）
  2. 翻页工具栏按钮无效（未正确连接 bridge 信号）
  3. Overlay 未利用 bridge 获取信息（初始化时未正确请求状态）
- 需要接入 Bridge 提供的**所有**功能

## What Changes
- 修改 `ppt_assistant/ui/LinuxOverlay.qml`，使用 SVG 图标替代文本符号
- 修复翻页按钮信号连接，确保 `prevPage()` 和 `nextPage()` 正确调用
- 修复初始化逻辑，确保在 `Component.onCompleted` 时正确请求初始状态
- 修复工具按钮点击逻辑，确保所有工具按钮正常工作
- **接入 Bridge 提供的所有功能**：
  - 信号接收：configChanged, themeChanged, pageInfoChanged, systemStatusChanged, thumbnailReady, toolStateReset, penColorReset, inkPromptVisibilityChanged, restrictionsChanged
  - 方法调用：requestInitState, setTool, setPenColor, prevPage, nextPage, gotoSlide, clearScreen, endShow, toggleSpotlight, toggleBoard, toggleTimer, launchApp, updateMask, releaseFocus, resizeNudge, requestThumbnail, startBackgroundThumbnailCaching, inkPromptResult, logMessage

## Impact
- Affected specs: Linux QML Overlay UI
- Affected code: `ppt_assistant/ui/LinuxOverlay.qml`

## ADDED Requirements

### Requirement: SVG 图标支持
The system SHALL 使用 SVG 图标替代文本符号。

#### Scenario: 翻页按钮图标
- **GIVEN** 翻页按钮需要显示上一页/下一页图标
- **THEN** 使用 `icons/Previous.svg` 和 `icons/Next.svg` 文件

#### Scenario: 工具栏图标
- **GIVEN** 工具栏需要显示工具图标
- **THEN** 根据工具类型加载对应的 SVG 文件：
  - select: `icons/Mouse.svg`
  - pen: `icons/Pen.svg`
  - eraser: `icons/Eraser.svg`
  - clear: `icons/Clear.svg`
  - spotlight: `icons/spotlight.svg`
  - board_in_board: `icons/board-in-board.svg`
  - timer: `icons/timer.svg`
  - apps: `icons/More.svg`
  - end: `icons/Minimize.svg`

#### Scenario: 状态栏图标
- **GIVEN** 状态栏需要显示系统状态图标
- **THEN** 使用内嵌 SVG 或使用 Qt 内置图标

### Requirement: 翻页按钮功能
The system SHALL 确保翻页按钮正常工作。

#### Scenario: 上一页按钮
- **GIVEN** 用户点击上一页按钮
- **WHEN** 当前页大于 1
- **THEN** 调用 `bridge.prevPage()`

#### Scenario: 下一页按钮
- **GIVEN** 用户点击下一页按钮
- **WHEN** 当前页小于总页数
- **THEN** 调用 `bridge.nextPage()`

### Requirement: Bridge 信号接收（全部）
The system SHALL 接收并处理 Bridge 发出的所有信号。

#### Scenario: configChanged
- **GIVEN** bridge 发送 `configChanged(config)` 信号
- **THEN** 更新所有配置属性：showStatusBar, disableAnimations, toolbarOrder, toolbarPosition, compatibilityMode, flipperPosition, showClear, clearMode, showSpotlight, showBoardInBoard, showTimer, uiScale, safeArea, toolbarOpacity, sidePageOpacity, strictEdgeAlignment, quickApps, disabledTools 等

#### Scenario: themeChanged
- **GIVEN** bridge 发送 `themeChanged(theme)` 信号
- **THEN** 更新所有主题颜色属性：themeId, themeMode, darkMode, accentColor, toolbarBg, toolbarBorder, toolbarFg, statusBg, statusFg, pageBg, pageBorder, pageFg, popupBg, popupBorder, textPrimary, textSecondary, dialogBg, dialogBorder 等

#### Scenario: pageInfoChanged
- **GIVEN** bridge 发送 `pageInfoChanged(current, total)` 信号
- **THEN** 更新 currentPage 和 totalPage，刷新页码显示

#### Scenario: systemStatusChanged
- **GIVEN** bridge 发送 `systemStatusChanged(status)` 信号
- **THEN** 更新 systemStatus 对象，刷新状态栏显示（电池、网络、音量、音乐等）

#### Scenario: thumbnailReady
- **GIVEN** bridge 发送 `thumbnailReady(index, url)` 信号
- **THEN** 将缩略图 URL 存入 thumbnails 数组，刷新页面选择器显示

#### Scenario: toolStateReset
- **GIVEN** bridge 发送 `toolStateReset(tool)` 信号
- **THEN** 更新 currentTool 为指定工具

#### Scenario: penColorReset
- **GIVEN** bridge 发送 `penColorReset()` 信号
- **THEN** 重置画笔颜色到默认状态

#### Scenario: inkPromptVisibilityChanged
- **GIVEN** bridge 发送 `inkPromptVisibilityChanged(visible)` 信号
- **THEN** 显示或隐藏墨迹确认弹窗

#### Scenario: restrictionsChanged
- **GIVEN** bridge 发送 `restrictionsChanged(protectedView, readonly)` 信号
- **THEN** 更新演示限制状态，禁用相应功能

### Requirement: Bridge 方法调用（全部）
The system SHALL 在适当时机调用 Bridge 提供的所有方法。

#### Scenario: 初始化请求
- **GIVEN** QML 组件加载完成
- **WHEN** `Component.onCompleted` 触发
- **THEN** 调用 `bridge.requestInitState()`

#### Scenario: 设置工具
- **GIVEN** 用户点击工具按钮
- **THEN** 调用 `bridge.setTool(tool_name)`（"arrow", "pen", "eraser"）

#### Scenario: 设置画笔颜色
- **GIVEN** 用户在颜色选择器中选择颜色
- **THEN** 调用 `bridge.setPenColor(r, g, b)`

#### Scenario: 翻页
- **GIVEN** 用户点击翻页按钮
- **THEN** 调用 `bridge.prevPage()` 或 `bridge.nextPage()`

#### Scenario: 跳转到指定页
- **GIVEN** 用户在页面选择器中点击某页
- **THEN** 调用 `bridge.gotoSlide(index)`

#### Scenario: 清屏
- **GIVEN** 用户滑动或点击清屏按钮
- **THEN** 调用 `bridge.clearScreen()`

#### Scenario: 结束放映
- **GIVEN** 用户点击结束按钮
- **THEN** 调用 `bridge.endShow()`

#### Scenario: 切换插件
- **GIVEN** 用户点击 spotlight/board/timer 按钮
- **THEN** 调用 `bridge.toggleSpotlight()`, `bridge.toggleBoard()`, `bridge.toggleTimer()`

#### Scenario: 启动应用
- **GIVEN** 用户点击快速启动应用按钮
- **THEN** 调用 `bridge.launchApp(path)`

#### Scenario: 更新 Mask
- **GIVEN** UI 布局变化或弹窗显示/隐藏
- **THEN** 调用 `bridge.updateMask(rects)` 上报可交互区域

#### Scenario: 释放焦点
- **GIVEN** 弹窗关闭或点击空白区域
- **THEN** 调用 `bridge.releaseFocus()`

#### Scenario: 请求缩略图
- **GIVEN** 页面选择器打开或滚动到某区域
- **THEN** 调用 `bridge.requestThumbnail(index)`

#### Scenario: 启动后台缩略图缓存
- **GIVEN** 页面选择器打开
- **THEN** 调用 `bridge.startBackgroundThumbnailCaching(total_pages)`

#### Scenario: 墨迹确认结果
- **GIVEN** 用户在墨迹确认弹窗中选择保留/不保留
- **THEN** 调用 `bridge.inkPromptResult(keep)`

#### Scenario: 日志输出
- **GIVEN** 需要输出调试信息
- **THEN** 调用 `bridge.logMessage(message)`

## MODIFIED Requirements
无

## REMOVED Requirements
无
