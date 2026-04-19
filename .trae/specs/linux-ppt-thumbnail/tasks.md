# Tasks

- [x] Task 1: 实现从 WPS 窗口标题解析文件名功能
  - [x] SubTask 1.1: 在 `linux.py` 中添加 `extract_filename_from_title()` 函数，支持从 `[文件名]` 格式中提取文件名
  - [x] SubTask 1.2: 添加单元测试验证各种标题格式的解析

- [x] Task 2: 实现通过 /proc/{pid}/fd 查找文件路径功能
  - [x] SubTask 2.1: 在 `linux.py` 中添加 `find_ppt_path_by_pid()` 函数，执行 `ls -la /proc/{pid}/fd` 并解析输出
  - [x] SubTask 2.2: 实现过滤逻辑，优先返回非临时文件路径
  - [x] SubTask 2.3: 添加错误处理（PID 不存在、权限不足等情况）

- [x] Task 3: 实现 LibreOffice 缩略图生成功能
  - [x] SubTask 3.1: 在 `linux.py` 或新模块中添加 `generate_thumbnail_with_libreoffice()` 函数
  - [x] SubTask 3.2: 实现使用 `--headless --convert-to png` 命令生成图片
  - [x] SubTask 3.3: 添加幻灯片索引支持（LibreOffice 导出所有幻灯片，选择指定索引）
  - [x] SubTask 3.4: 使用 PIL 调整图片大小到目标尺寸

- [x] Task 4: 集成到 icon_helper.py
  - [x] SubTask 4.1: 在 `icon_helper.py` 中添加 Linux 平台检测
  - [x] SubTask 4.2: 在 `get_file_icon_base64()` 中添加 PPT 文件类型处理分支
  - [x] SubTask 4.3: 实现完整的缩略图获取流程：窗口标题 → PID → 文件路径 → LibreOffice 生成缩略图

- [x] Task 5: 添加缓存机制
  - [x] SubTask 5.1: 实现缩略图缓存，避免重复生成
  - [x] SubTask 5.2: 缓存键使用文件路径 + 修改时间 + 幻灯片索引

# Task Dependencies
- Task 2 depends on Task 1
- Task 4 depends on Task 2 and Task 3
- Task 5 can be done in parallel with Task 4
