# Tasks

- [x] Task 1: 修复 SVG 图标显示问题
  - [x] SubTask 1.1: 创建图标加载函数，根据工具名返回 SVG 文件路径
  - [x] SubTask 1.2: 修改翻页按钮，使用 Image 组件加载 Previous.svg 和 Next.svg
  - [x] SubTask 1.3: 修改工具栏按钮，使用 Image 组件加载对应工具图标
  - [x] SubTask 1.4: 确保图标颜色随主题变化（使用 ColorOverlay 或 tint）

- [x] Task 2: 修复翻页按钮功能
  - [x] SubTask 2.1: 检查翻页按钮的 onClicked 信号连接
  - [x] SubTask 2.2: 确保调用 bridge.prevPage() 和 bridge.nextPage()
  - [x] SubTask 2.3: 添加边界检查（当前页大于1才能上一页，小于总页数才能下一页）

- [x] Task 3: 接入所有 Bridge 信号接收
  - [x] SubTask 3.1: 实现 onConfigChanged 处理，更新所有配置属性
  - [x] SubTask 3.2: 实现 onThemeChanged 处理，更新所有颜色属性
  - [x] SubTask 3.3: 实现 onPageInfoChanged 处理，更新页码显示
  - [x] SubTask 3.4: 实现 onSystemStatusChanged 处理，更新状态栏
  - [x] SubTask 3.5: 实现 onThumbnailReady 处理，更新缩略图
  - [x] SubTask 3.6: 实现 onToolStateReset 处理，更新当前工具
  - [x] SubTask 3.7: 实现 onPenColorReset 处理，重置画笔颜色
  - [x] SubTask 3.8: 实现 onInkPromptVisibilityChanged 处理，显示/隐藏墨迹弹窗
  - [x] SubTask 3.9: 实现 onRestrictionsChanged 处理，更新限制状态

- [x] Task 4: 接入所有 Bridge 方法调用
  - [x] SubTask 4.1: 初始化时调用 bridge.requestInitState()
  - [x] SubTask 4.2: 工具按钮调用 bridge.setTool()
  - [x] SubTask 4.3: 颜色选择器调用 bridge.setPenColor()
  - [x] SubTask 4.4: 翻页按钮调用 bridge.prevPage() / nextPage()
  - [x] SubTask 4.5: 页面选择器调用 bridge.gotoSlide()
  - [x] SubTask 4.6: 清屏调用 bridge.clearScreen()
  - [x] SubTask 4.7: 结束按钮调用 bridge.endShow()
  - [x] SubTask 4.8: 插件按钮调用 bridge.toggleSpotlight() / toggleBoard() / toggleTimer()
  - [x] SubTask 4.9: 快速启动应用调用 bridge.launchApp()
  - [x] SubTask 4.10: UI 变化时调用 bridge.updateMask()
  - [x] SubTask 4.11: 弹窗关闭时调用 bridge.releaseFocus()
  - [x] SubTask 4.12: 页面选择器调用 bridge.requestThumbnail() 和 startBackgroundThumbnailCaching()
  - [x] SubTask 4.13: 墨迹确认调用 bridge.inkPromptResult()
  - [x] SubTask 4.14: 调试时调用 bridge.logMessage()

- [x] Task 5: 代码验证
  - [x] SubTask 5.1: 运行 ruff 检查确保 Python 代码无误
  - [x] SubTask 5.2: 检查 QML 语法
  - [x] SubTask 5.3: 验证所有信号连接正确

# Task Dependencies
- Task 1 和 Task 2 可以并行
- Task 3 和 Task 4 可以并行
- Task 5 最后执行
