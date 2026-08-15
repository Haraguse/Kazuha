# Luminalium 运行手册（Runbook）

> 适用版本：1.4.400.1（代号 RyouYamada，future Togeari）
> 计划：`.omo/plans/Plan.md` T25（文档）
> 配套：`docs/FEATURE_PARITY_MATRIX.md`、`docs/LEGACY_CAPABILITY_INVENTORY.md`、`docs/evidence/*`

本手册面向接手 C# 版本的维护者，覆盖：解决方案结构、SDK 与底层、命令、
发布、配置、排除项（延迟/排除功能及其警告）、COM 先决条件。所有命令均在
仓库根目录执行，工作分支为 `RyouYamada`。

---

## 1. 解决方案结构

```text
dotnet sln Luminalium.sln list
```

| 项目 | TFM | 职责 |
|---|---|---|
| `src/Luminalium.App` | `net10.0` | Avalonia 应用壳（入口、Shell、设置、托盘、启动画面、错误协调） |
| `src/Luminalium.Core` | `net10.0` | 跨平台核心：配置、版本、安全、本地化、身份 |
| `src/Luminalium.Platform.Windows` | `net10.0-windows10.0.17763.0` | Win32 平台服务（注册表、自启动、DWM、DPI、通知等） |
| `src/Luminalium.Plugins` | `net10.0` | 外部插件契约（不得引用 App 类型） |
| `src/Luminalium.Presentation` | `net10.0` | 演示功能跨平台契约与编排（Monitor/Errors/Models） |
| `src/Luminalium.Presentation.Windows` | `net10.0-windows10.0.17763.0` | PowerPoint COM + Win32 放映窗适配 |
| `src/Luminalium.Smtc.Windows` | `net10.0-windows10.0.19041.0` | SMTC 会话（可选加载） |
| `src/Luminalium.Theming` | `net10.0` | Monet/壁纸/强调色提取 |
| `src/Luminalium.Updater` | `net10.0` | 自更新（下载/校验/暂存/回滚/替换） |
| `src/Luminalium.Wps` | `net10.0` | WPS WebSocket 桥宿主 + JSON Schema 协议 |
| `tests/Luminalium.Tests` | `net10.0-windows10.0.17763.0` | xUnit 测试套件 |

架构要点：
- **内置/外部边界**：App 侧内置功能走强类型 Feature ID 路由（`BuiltInFeatureId`/`Descriptor`/`Catalog` + `BuiltInFeatureHost`）；`Luminalium.Plugins` 只保留给真正的外部插件，禁止引用 App。
- **单向兼容适配器**：原生目录 → 旧插件投影，保留现有 ID 与 `plugin:<id>` 输入别名（兼容期由 `CompatibilityReleaseMarker` / `NativeFeatureMigrationGate` 门控）。
- 参考边界由 `tools/ForbiddenReferenceCheck.ps1` 强制。

## 2. SDK 与底层

- **.NET SDK**：`10.0.302`（`global.json`，`rollForward=latestFeature`，禁预发布）。
- **最低 Windows**：`10.0.17763.0`（Windows 10 1809）。
- **包版本**（`Directory.Packages.props`，集中管理，全部精确固定）：
  - Avalonia 12.1.1（含 Desktop/Skia/ColorPicker/DataGrid）
  - FluentAvaloniaUI 3.0.2
  - SkiaSharp 3.119.4
  - CommunityToolkit.Mvvm 8.4.2
  - JsonSchema.Net 9.4.0
  - Microsoft.NET.Test.Sdk 17.14.1 / xunit 2.9.3 / xunit.runner.visualstudio 3.1.0 / coverlet.collector 6.0.4
- **构建治理**（`Directory.Build.props`）：`Nullable`、`ImplicitUsings`、`Deterministic`、`AnalysisLevel=latest`、`AnalysisMode=Recommended`、`EnableNETAnalyzers`。

## 3. 常用命令

```powershell
# 还原
dotnet restore Luminalium.sln

# 构建（Debug）
dotnet build Luminalium.sln

# 构建（Release，门禁：0 警告 0 错误）
dotnet build Luminalium.sln -c Release

# 全量测试（门禁：绿色确定性）
dotnet test Luminalium.sln -c Release

# 聚焦测试（按测试类/命名空间过滤）
dotnet test Luminalium.sln -c Release --filter "FullyQualifiedName~ConfigurationServiceTests"

# 发布 win-x64（自包含、目录输出、无符号）
dotnet publish src/Luminalium.App/Luminalium.App.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=WindowsNightly -p:PublishDir=artifacts/publish
```

## 4. 发布

- **发布契约**（`src/Luminalium.App/Properties/PublishProfiles/WindowsNightly.pubxml`）：Release、`win-x64`、自包含、**目录式输出**（`PublishSingleFile=false`，保留 Avalonia 原生库文件）、`PublishReadyToRun=false`、`DebugType=none`/`DebugSymbols=false`，并追加目标剥离全部 `.pdb`。
- **CI 工作流**（`.github/workflows/nightly-release.yml`，每日 00:00 + 手动触发，仅 Windows）按序执行：
  1. restore → `ForbiddenReferenceCheck.ps1` → build → test
  2. `Set-NightlyVersion.ps1`（改写 `version.json` 的 `version` 为 `yyyy.MM.dd-nightly`）
  3. `dotnet publish`
  4. `Assert-ReleaseVersion.ps1`（版本元数据门禁）
  5. `Assert-ForbiddenPayload.ps1`（Python/PySide/Qt/QML/WebView2/VSTO/外部插件负载扫描）
  6. `Invoke-DesktopQASmoke.ps1 -Scenario all`（桌面 UI 冒烟 8 场景）
  7. 上传 QA 证据（`Luminalium-DesktopQA`）
  8. `New-NightlyPackage.ps1`（确定性 ZIP：固定时间戳 + 排序条目）
  9. 上传 `Luminalium-Windows.zip` 并更新 `nightly` release
- **产物**：`Luminalium-Windows.zip`（`ProductIdentity.WindowsArtifactName`）。

## 5. 配置

- **版本元数据**：仓库根 `version.json`，字段为不透明字符串：`code_name`、`code_name_CN`、`version`、`versionnm`、`build`、`future_codename`。读写不得解析为数值。
- **应用设置**（`ConfigurationService`）：
  - 路径：`%LOCALAPPDATA%\Luminalium\settings.json`（`ProductIdentity.DisplayName`）。
  - 新 schema：`LuminaliumConfig`，`SchemaVersion=1`，分节 `Appearance`/`General`/`Toolbar`/`Linkage`/`Overlay`/`PPT`/`SelfPen`/`Notifications`/`Security`。
  - 配置文件：`settings.json` 为默认；`_active` 标记文件指定具名 `<profile>.json`。
  - 持久化：原子保存（同目录临时文件 + 原子移动）；损坏时备份为 `.bak` 并返回类型化 `ConfigurationLoadResult` / `ConfigurationLoadWarning`（`MalformedJson`/`InvalidRoot`/`InvalidValue` 等），绝不把未处理 JSON 异常抛给调用方。
  - 密码：`Security.PasswordHash` 仅存算法前缀加盐哈希（`pbkdf2`+`sha256`，PBKDF2-HMAC-SHA256，随机盐、固定时间比较），无明文密码 API。
  - Python 配置**不自动导入**（X-04），未来导入边界为 `IConfigImporter`。

## 6. 排除项（延迟 / 排除功能及其警告）

见 `docs/FEATURE_PARITY_MATRIX.md` 排除项与 `docs/LEGACY_CAPABILITY_INVENTORY.md` 退役映射。摘要：

| 项 | 内容 | 状态 |
|---|---|---|
| X-01 | Linux 平台 / Linux overlay / Linux CI | **延迟**：Windows-first 交付后分期；Linux CI 不作为发布输入 |
| X-02 | 外部插件发现（`plugins/external/`） | **排除**：只保留内置注册；`Luminalium.Plugins` 仅为外部契约 |
| X-04 | Python 配置导入 | **排除**：不自动迁移旧配置 |
| X-05 | WebView/HTML/QML 表面 | **退役**：改原生 FluentAvalonia 控件 |
| X-06 | 实验性渲染后端 | **排除** |
| X-07 | Python/PyInstaller 运行时 | **移除**：仅 C# 发布（T24 全树扫描 0 遗留） |

**运行时延迟/不可用警告**（类型化，不崩溃）：
- 无 Office/WPS 时演示功能返回 `HostUnavailable`；放映关闭返回 `SlideshowClosed`；更新源不可达返回 `FeedUnavailable`（见 `src/Luminalium.Presentation/PresentationErrors.cs`）。
- 插件回退描述保留在本地化键 `Plugin.FallbackDescription`（“插件画面预留”）。
- 冒烟/环境结论见 `docs/evidence/task-15-monitor-environment-dependent.md`。

## 7. COM 先决条件

- **操作系统**：Windows 10 1809（10.0.17763.0）或更高。
- **PowerPoint**：Office PowerPoint 2010 或更高 / Microsoft 365 PowerPoint；适配器编译期延迟绑定、版本无关。
- **WPS**：WPS 2019 或更高，配合 `Luminalium2WPS` 桥客户端连接宿主 `ws://127.0.0.1:{port}/ws`（端口 3892-3902；协议 Schema 位于 `src/Luminalium.Wps/Protocol/*.schema.json`）。
- **无宿主行为**：缺少 Office/WPS 时必须呈现类型化 `HostUnavailable`/`SlideshowClosed`，外壳保持可运行；桌面 QA 冒烟在无 Office/WPS 的头less 安全模式下验证 no-slideshow 路径。
- **SMTC**：`Luminalium.Smtc.Windows` 在运行时可选加载。

---

## T25 QA（命令验证）

以下“干净检出”命令序列在仓库根按顺序执行并核对输出；延迟/排除功能警告按第 6 节断言。

```text
dotnet restore Luminalium.sln                      -> 还原成功
dotnet build Luminalium.sln -c Release             -> 0 警告 0 错误
dotnet test Luminalium.sln -c Release              -> 全量通过（347/347）
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ForbiddenReferenceCheck.ps1
                                                   -> 引用边界通过
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-ForbiddenPayload.ps1 -PublishDir artifacts/publish
                                                   -> 发布树 0 禁止负载
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-DesktopQASmoke.ps1 -AppPath artifacts/publish/Luminalium.exe -Scenario all -EvidenceDir artifacts/qa-evidence -LaunchTimeoutSeconds 90
                                                   -> 8/8 场景通过（含 noslide / corrupt 延迟功能路径）
```

证据：`docs/evidence/task-25-docs-happy.md`。
