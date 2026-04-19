# Tasks

- [x] Task 1: 创建 LinuxOverlay.qml 基础结构和根布局
  - [x] SubTask 1.1: 创建 QML 文件基础结构，导入必要模块
  - [x] SubTask 1.2: 实现 root 级兄弟节点布局（statusBar, centerArea, flipper, toolbarContainer, pageSelector, colorPicker, eraserPopup, inkPrompt）
  - [x] SubTask 1.3: 实现与 LinuxOverlayBridge 的信号/槽连接

- [x] Task 2: 实现 StatusBar 组件
  - [x] SubTask 2.1: 实现状态栏基础 UI（时间、系统图标）
  - [x] SubTask 2.2: 实现系统状态显示（电池、网络、音量）
  - [x] SubTask 2.3: 实现音乐信息显示（可选动画效果）
  - [x] SubTask 2.4: 实现可见性控制（showStatusBar）

- [x] Task 3: 实现 Flipper 组件
  - [x] SubTask 3.1: 实现默认垂直布局（54x160）
  - [x] SubTask 3.2: 实现底部水平布局（160x54）
  - [x] SubTask 3.3: 实现翻页按钮和页面信息显示
  - [x] SubTask 3.4: 实现点击打开页面选择器

- [x] Task 4: 实现 Toolbar 组件
  - [x] SubTask 4.1: 实现 toolbar 基础 UI 和按钮渲染
  - [x] SubTask 4.2: 实现四种位置布局（top/bottom/left/right）
  - [x] SubTask 4.3: 实现 scale、safeArea、strictEdgeAlignment 支持
  - [x] SubTask 4.4: 实现折叠/展开功能和 handle 指示器
  - [x] SubTask 4.5: 实现兼容模式标签显示（"兼"）
  - [x] SubTask 4.6: 实现结束按钮红色样式

- [x] Task 5: 实现 PageSelector 组件
  - [x] SubTask 5.1: 实现页面选择器基础 UI（260px 宽，16:9 卡片）
  - [x] SubTask 5.2: 实现左右两侧显示逻辑
  - [x] SubTask 5.3: 实现缩略图加载和缓存（thumbnailSources）
  - [x] SubTask 5.4: 实现 loading spinner 和错误处理
  - [x] SubTask 5.5: 实现当前页边框高亮（accent 色）
  - [x] SubTask 5.6: 实现滚动加载策略（当前页±5，可见±3）
  - [x] SubTask 5.7: 实现点击跳页功能

- [x] Task 6: 实现 ColorPicker 组件
  - [x] SubTask 6.1: 实现颜色选择器基础 UI
  - [x] SubTask 6.2: 实现普通笔/荧光笔标签页切换
  - [x] SubTask 6.3: 实现颜色选择功能
  - [x] SubTask 6.4: 实现位置适配（根据 toolbarPosition）

- [x] Task 7: 实现 EraserPopup 组件
  - [x] SubTask 7.1: 实现橡皮设置弹窗基础 UI
  - [x] SubTask 7.2: 实现滑动清屏控件
  - [x] SubTask 7.3: 实现按钮清屏模式
  - [x] SubTask 7.4: 实现位置适配

- [x] Task 8: 实现 InkPrompt 组件
  - [x] SubTask 8.1: 实现墨迹确认弹窗 UI
  - [x] SubTask 8.2: 实现保留/不保留按钮逻辑

- [x] Task 9: 修改 linux_qml_overlay.py
  - [x] SubTask 9.1: 优化 _qml_color helper，确保 rgba 正确转换
  - [x] SubTask 9.2: 修改 _build_theme_payload，输出 QML 友好颜色格式
  - [x] SubTask 9.3: 优化 update_mask，正确处理 page-selector 区域

- [x] Task 10: 更新单元测试
  - [x] SubTask 10.1: 更新颜色转换测试
  - [x] SubTask 10.2: 更新缩略图 URL 测试
  - [x] SubTask 10.3: 运行测试确保通过

# Task Dependencies
- Task 2, 3, 4 可以并行开发（独立组件）
- Task 5 依赖 Task 3（flipper 打开 page selector）
- Task 6, 7, 8 可以并行开发（独立弹窗）
- Task 9 需要在 QML 基础结构完成后进行
- Task 10 最后执行
