# Linux QML Overlay 重写 Spec

## Why
- 当前 Linux QML Overlay 存在三个关键问题：缩略图接收/显示异常、QML 颜色解析导致黑底、页面选择器与弹窗被错误锚定造成 UI 错位
- 需要以 `ppt_assistant/ui/overlay.html` 作为唯一视觉和行为参考，重写 `ppt_assistant/ui/LinuxOverlay.qml`
- 保留现有 `LinuxOverlayBridge` 信号/槽接口，确保 Python ↔ QML 通信不变

## What Changes
- 新建 `ppt_assistant/ui/LinuxOverlay.qml`，完全复刻 Web overlay 的视觉结构和交互行为
- 修改 `ppt_assistant/ui/linux_qml_overlay.py`，优化颜色转换 helper 和 theme payload 构建
- 修复缩略图加载：正确处理 `data:image/png;base64,...` URL，添加 loading spinner 和错误处理
- 修复黑底问题：所有颜色通过 `_qml_color()` 转换为 QML 可识别的 `#AARRGGBB` 或 `#RRGGBB` 格式
- 修复 UI 锚定：弹窗和页面选择器作为 root 级兄弟节点，不再嵌套在 toolbarContainer 内
- **BREAKING**: 旧版 QML 文件不存在，本次为全新创建

## Impact
- Affected specs: Linux 演示模式 Overlay UI
- Affected code: `ppt_assistant/ui/LinuxOverlay.qml` (新建), `ppt_assistant/ui/linux_qml_overlay.py` (修改)

## ADDED Requirements

### Requirement: QML Overlay 根布局结构
The system SHALL 实现与 Web overlay 相同的根级布局结构。

#### Scenario: 根级兄弟节点布局
- **GIVEN** Web overlay 的 DOM 结构
- **WHEN** QML 加载时
- **THEN** 所有元素作为 root 的兄弟节点：`statusBar`、全屏 `centerArea`、左右 `flipper`、`toolbarContainer`、`pageSelector`、`colorPicker`、`eraserPopup`、`inkPrompt`

### Requirement: Toolbar 容器和定位
The system SHALL 支持 toolbar 的多种位置和尺寸配置。

#### Scenario: Toolbar 位置切换
- **GIVEN** `toolbarPosition` 配置为 top/bottom/left/right
- **WHEN** 配置变更时
- **THEN** toolbar 正确切换位置，支持 `scale`、`safeArea`、`strictEdgeAlignment` 参数

#### Scenario: Toolbar 折叠/展开
- **GIVEN** toolbar 处于展开状态
- **WHEN** 双击分隔线或点击 handle
- **THEN** toolbar 平滑折叠/展开，使用 iOS 风格的 handle 指示器

### Requirement: Flipper 组件
The system SHALL 实现左右翻页按钮组件。

#### Scenario: Flipper 默认布局
- **GIVEN** `flipperPosition` 为 center (默认)
- **THEN** flipper 尺寸为 `54x160`，垂直居中显示

#### Scenario: Flipper 底部布局
- **GIVEN** `flipperPosition` 为 bottom
- **THEN** flipper 尺寸为 `160x54`，水平显示，位置在屏幕底部

#### Scenario: Flipper 页面信息显示
- **GIVEN** 当前页码和总页数
- **THEN** flipper 中间显示当前页码，点击可打开页面选择器

### Requirement: 页面选择器
The system SHALL 实现页面缩略图选择器。

#### Scenario: 页面选择器显示
- **GIVEN** 用户点击 flipper 中间区域
- **THEN** 页面选择器从右侧滑入，宽度 `260px`，顶部/底部 `24px`，右侧 `16px`

#### Scenario: 页面选择器左侧显示
- **GIVEN** 用户点击左侧 flipper
- **THEN** 页面选择器从左侧滑入，左侧 `16px`

#### Scenario: 缩略图加载策略
- **GIVEN** 页面选择器打开
- **THEN** 优先请求当前页 ±5 页的缩略图，滚动时请求可见范围 ±3 页

#### Scenario: 缩略图显示
- **GIVEN** 缩略图 URL 到达
- **THEN** 显示图片，隐藏 loading spinner，当前页显示 accent 色边框

### Requirement: 颜色选择器弹窗
The system SHALL 实现画笔颜色选择弹窗。

#### Scenario: 颜色选择器显示
- **GIVEN** 用户点击 pen 工具按钮（已选中状态再次点击）
- **THEN** 颜色选择器弹出，包含普通笔/荧光笔两个标签页

#### Scenario: 颜色选择
- **GIVEN** 用户点击颜色选项
- **THEN** 触发 `setPenColor` 信号，关闭弹窗

### Requirement: 橡皮设置弹窗
The system SHALL 实现橡皮工具设置弹窗。

#### Scenario: 橡皮弹窗显示
- **GIVEN** 用户点击 eraser 工具按钮（已选中状态再次点击）
- **THEN** 橡皮设置弹窗弹出，包含滑动清屏或按钮清屏

### Requirement: 墨迹确认弹窗
The system SHALL 实现墨迹保留确认弹窗。

#### Scenario: 墨迹确认显示
- **GIVEN** Python 触发 `inkPromptVisibilityChanged(true)`
- **THEN** 全屏 mask 显示确认对话框，询问是否保留墨迹

#### Scenario: 墨迹确认结果
- **GIVEN** 用户点击"保留"或"不保留"
- **THEN** 触发 `inkPromptResult(bool)` 信号，关闭弹窗

### Requirement: 状态栏
The system SHALL 实现顶部状态栏。

#### Scenario: 状态栏显示
- **GIVEN** `showStatusBar` 为 true
- **THEN** 顶部显示状态栏，包含时间、系统状态、音乐信息

#### Scenario: 数字动画
- **GIVEN** 时间或状态数值变化
- **THEN** 数字使用翻转动画效果更新（可选，受 `disableAnimations` 控制）

### Requirement: 颜色系统
The system SHALL 正确处理主题颜色，避免黑底问题。

#### Scenario: CSS rgba 转换
- **GIVEN** 主题颜色值为 `rgba(0, 0, 0, 0.12)`
- **THEN** 转换为 `#1F000000` (ARGB 格式)，而非显示为黑色

#### Scenario: 透明度处理
- **GIVEN** `toolbarOpacity` 或 `sidePageOpacity` 小于 1.0
- **THEN** 仅背景色应用透明度，图标、文字、边框保持不透明

### Requirement: 缩略图 URL 处理
The system SHALL 正确处理多种缩略图 URL 格式。

#### Scenario: Data URL 原样传递
- **GIVEN** URL 为 `data:image/png;base64,...`
- **THEN** 原样传给 `Image.source`，不添加 `file://`

#### Scenario: 文件路径转换
- **GIVEN** URL 为普通文件路径
- **THEN** 转换为 `file://` URL

## MODIFIED Requirements

### Requirement: _build_theme_payload 输出格式
The system SHALL 输出 Web overlay 同语义的 theme token。

**修改内容**:
- 所有颜色值通过 `_qml_color()` 转换为 `#AARRGGBB` 或 `#RRGGBB`
- 不再直接传递 CSS `rgba(...)` 字符串给 QML
- 保持 token 命名与 Web overlay CSS 变量对应

### Requirement: update_mask 区域计算
The system SHALL 正确计算可交互区域。

**修改内容**:
- QML 上报 root 坐标系中的真实可交互矩形
- Python `update_mask()` 对 `page-selector` 角色扩展为全高交互区域
- 避免穿透和边缘错位

## REMOVED Requirements
无
