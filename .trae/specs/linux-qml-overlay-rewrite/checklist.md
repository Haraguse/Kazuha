# Checklist

## 基础结构
- [x] LinuxOverlay.qml 文件创建成功
- [x] QML 语法检查通过，无错误
- [x] 根布局结构符合 Web overlay 设计（兄弟节点而非嵌套）
- [x] 与 LinuxOverlayBridge 的信号/槽连接正常

## StatusBar
- [x] 状态栏正确显示时间和系统信息
- [x] 电池、网络、音量图标正确显示
- [x] 音乐信息显示正常（如果有）
- [x] showStatusBar 配置生效

## Flipper
- [x] 默认垂直布局（54x160）正确显示
- [x] 底部水平布局（160x54）正确显示
- [x] 翻页按钮功能正常
- [x] 页面信息正确显示
- [x] 点击打开页面选择器

## Toolbar
- [x] 四种位置（top/bottom/left/right）切换正常
- [x] scale 参数正确应用
- [x] safeArea 参数正确应用
- [x] strictEdgeAlignment 参数正确应用
- [x] 折叠/展开功能正常
- [x] 兼容模式显示"兼"标签
- [x] 结束按钮显示红色
- [x] 工具按钮根据配置正确渲染

## PageSelector
- [x] 页面选择器正确滑入/滑出
- [x] 左右两侧显示逻辑正确
- [x] 缩略图正确加载显示
- [x] Loading spinner 正常显示/隐藏
- [x] 当前页边框高亮（accent 色）
- [x] 滚动加载策略生效
- [x] 点击跳页功能正常

## ColorPicker
- [x] 颜色选择器正确弹出/关闭
- [x] 普通笔/荧光笔标签页切换正常
- [x] 颜色选择功能正常
- [x] 位置根据 toolbarPosition 正确适配

## EraserPopup
- [x] 橡皮弹窗正确弹出/关闭
- [x] 滑动清屏控件正常
- [x] 按钮清屏模式正常
- [x] 位置正确适配

## InkPrompt
- [x] 墨迹确认弹窗正确显示
- [x] 保留/不保留按钮功能正常

## 颜色和主题
- [x] rgba 颜色正确转换，无黑底问题
- [x] toolbarOpacity 只影响背景，不影响图标文字
- [x] sidePageOpacity 只影响 flipper 背景
- [x] 浅色/深色主题切换正常

## 缩略图处理
- [x] data:image/png;base64 URL 原样传递
- [x] 普通路径正确转换为 file:// URL
- [x] Image.Error 状态正确处理

## Mask 和交互
- [x] update_mask 正确上报可交互区域
- [x] page-selector 区域扩展为全高
- [x] 点击穿透问题修复

## 测试
- [x] 颜色转换单元测试通过
- [x] 缩略图 URL 单元测试通过
- [x] WPS bridge 协议测试通过
- [x] Python 语法检查通过
- [x] QML 语法/组件加载检查通过
