# Linux PPT 缩略图生成 Spec

## Why
在 Linux 环境下，需要通过 WPS 演示的放映窗口找到对应的源文件路径，然后使用 LibreOffice 生成幻灯片缩略图。WPS 演示窗口标题中包含文件名（格式为 `[文件名]`），可以通过 `/proc/{pid}/fd` 查找进程打开的文件来定位完整路径。

## What Changes
- 新增 Linux 下通过窗口标题和 `/proc/{pid}/fd` 查找 PPT 源文件路径的功能
- 新增使用 LibreOffice 无头模式生成幻灯片缩略图的功能
- 在 `icon_helper.py` 中增加 Linux 缩略图支持
- 在 `linux.py` 中增加文件路径查找功能

## Impact
- Affected specs: Linux 系统 API、文件图标/缩略图生成
- Affected code: `ppt_assistant/core/system/linux.py`, `ppt_assistant/core/icon_helper.py`

## ADDED Requirements

### Requirement: 从 WPS 窗口标题解析文件名
The system SHALL 能够从 WPS 演示窗口标题中提取文件名（格式为 `[文件名]`）。

#### Scenario: 成功提取文件名
- **GIVEN** WPS 放映窗口标题为 `"[坚持] - WPS 演示"` 或 `"[presentation] WPS Presentation Slide Show"`
- **WHEN** 调用解析函数
- **THEN** 返回提取的文件名 `"坚持"` 或 `"presentation"`

### Requirement: 通过 /proc/{pid}/fd 查找文件路径
The system SHALL 能够通过进程 ID 在 `/proc/{pid}/fd` 中查找匹配文件名的完整路径。

#### Scenario: 成功查找文件路径
- **GIVEN** WPS 进程 PID 为 12345，文件名为 `"坚持"`
- **AND** `/proc/12345/fd` 目录下存在指向 `/home/user/坚持.pptx` 的符号链接
- **WHEN** 调用路径查找函数
- **THEN** 返回完整路径 `/home/user/坚持.pptx`

#### Scenario: 处理多个匹配结果
- **GIVEN** `/proc/{pid}/fd` 中有多个匹配文件名的条目（如原文件和临时文件）
- **WHEN** 调用路径查找函数
- **THEN** 优先返回非临时文件路径（不包含 `.~` 前缀的路径）

### Requirement: 使用 LibreOffice 生成缩略图
The system SHALL 能够使用 LibreOffice 无头模式将 PPT 转换为图片。

#### Scenario: 成功生成缩略图
- **GIVEN** PPT 文件路径为 `/home/user/test.pptx`
- **WHEN** 调用缩略图生成函数，指定输出路径和幻灯片索引
- **THEN** 使用 LibreOffice 生成对应幻灯片的 PNG 图片

#### Scenario: LibreOffice 不可用
- **GIVEN** 系统中未安装 LibreOffice
- **WHEN** 调用缩略图生成函数
- **THEN** 返回 None，不抛出异常

### Requirement: 集成到 icon_helper
The system SHALL 在 `icon_helper.py` 中集成 Linux 下的 PPT 缩略图支持。

#### Scenario: 获取 PPT 文件缩略图
- **GIVEN** Linux 系统，PPT 文件路径为 `/home/user/test.pptx`
- **WHEN** 调用 `get_file_icon_base64()`
- **THEN** 返回第一张幻灯片的缩略图 base64 编码

## MODIFIED Requirements
无

## REMOVED Requirements
无
