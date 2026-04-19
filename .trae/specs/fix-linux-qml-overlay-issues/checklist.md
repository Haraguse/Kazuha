# Checklist

## SVG 图标
- [x] 翻页按钮使用 Previous.svg 和 Next.svg
- [x] 工具栏按钮使用对应的 SVG 图标（Mouse.svg, Pen.svg, Eraser.svg, Clear.svg, spotlight.svg, board-in-board.svg, timer.svg, More.svg, Minimize.svg）
- [x] 图标颜色随主题变化（使用 ColorOverlay）
- [x] 图标正确显示（不是方框或文本符号）

## 翻页按钮功能
- [x] 上一页按钮调用 bridge.prevPage()
- [x] 下一页按钮调用 bridge.nextPage()
- [x] 边界检查正确（第一页不能上一页，最后一页不能下一页）

## Bridge 信号接收（全部9个）
- [x] onConfigChanged 更新所有配置属性
- [x] onThemeChanged 更新所有颜色属性
- [x] onPageInfoChanged 更新页码显示
- [x] onSystemStatusChanged 更新状态栏
- [x] onThumbnailReady 更新缩略图
- [x] onToolStateReset 更新当前工具
- [x] onPenColorReset 重置画笔颜色
- [x] onInkPromptVisibilityChanged 显示/隐藏墨迹弹窗
- [x] onRestrictionsChanged 更新限制状态

## Bridge 方法调用（全部14个）
- [x] bridge.requestInitState() - 初始化
- [x] bridge.setTool() - 设置工具
- [x] bridge.setPenColor() - 设置画笔颜色
- [x] bridge.prevPage() / nextPage() - 翻页
- [x] bridge.gotoSlide() - 跳转到指定页
- [x] bridge.clearScreen() - 清屏
- [x] bridge.endShow() - 结束放映
- [x] bridge.toggleSpotlight() / toggleBoard() / toggleTimer() - 切换插件
- [x] bridge.launchApp() - 启动应用
- [x] bridge.updateMask() - 更新可交互区域
- [x] bridge.releaseFocus() - 释放焦点
- [x] bridge.requestThumbnail() - 请求缩略图
- [x] bridge.startBackgroundThumbnailCaching() - 启动后台缩略图缓存
- [x] bridge.inkPromptResult() - 墨迹确认结果
- [x] bridge.logMessage() - 日志输出

## 代码质量
- [x] Python 代码通过 ruff 检查
- [x] QML 语法正确
- [x] 所有信号连接正确
