import os
import re
import shutil
import subprocess
import sys

from .base import SystemAPI


SLIDESHOW_WINDOW_TITLE_HINTS = (
    "slide show",
    "slideshow",
    "wps presentation slide show",
    "wps persentation slide show",
    "yozo slide show",
    "yozo slideshow",
    "yozo presentation",
    "幻灯片放映",
    "幻燈片放映",
    "投影片放映",
    "放映",
)
STRICT_SLIDESHOW_WINDOW_TITLE_HINTS = (
    "slide show",
    "slideshow",
    "slide-show",
    "wps presentation slide show",
    "wps persentation slide show",
    "幻灯片放映",
    "幻燈片放映",
    "投影片放映",
    "スライド ショー",
    "スライドショー",
    "슬라이드 쇼",
    "슬라이드쇼",
    "diaporama",
    "mode diaporama",
    "bildschirmprasentation",
    "bildschirmpräsentation",
    "presentacion con diapositivas",
    "presentación con diapositivas",
    "apresentacao de slides",
    "apresentação de slides",
)
SLIDESHOW_CLASS_HINTS = (
    "screenclass",
    "wppslideshowwindowclass",
    "wpp slideshow window",
)
EDITOR_WINDOW_EXACT_TITLES = (
    "wps 演示",
    "wps presentation",
    "wps 文字",
    "wps writer",
    "wps 表格",
    "wps spreadsheet",
)
EDITOR_WINDOW_SUFFIXES = (
    " - wps 演示",
    " - wps presentation",
    " - wps 文字",
    " - wps writer",
    " - wps 表格",
    " - wps spreadsheet",
)
PRESENTATION_KIND_HINTS = {
    "ppt": ("powerpoint", "powerpnt", "screenclass"),
    "wps": ("wps", "wpp", "kingsoft"),
    "yozo": ("yozo",),
}
SOFT_X11_FAILURE_HINTS = (
    "badwindow",
    "invalid window parameter",
    "window not found",
    "no such window",
)
_LINUX_TOOL_CAPABILITIES: dict | None = None


def _has_x11_display() -> bool:
    return bool(os.environ.get("DISPLAY"))


def _run_command_capture(args: list[str], timeout: float = 1.5) -> dict:
    try:
        result = subprocess.run(
            args,
            capture_output=True,
            text=True,
            timeout=timeout,
        )
    except Exception as exc:
        return {
            "ok": False,
            "returncode": -1,
            "stdout": "",
            "stderr": str(exc),
        }
    return {
        "ok": result.returncode == 0,
        "returncode": int(result.returncode),
        "stdout": (result.stdout or "").strip(),
        "stderr": (result.stderr or "").strip(),
    }


def _run_command(args: list[str], timeout: float = 1.5) -> tuple[bool, str]:
    result = _run_command_capture(args, timeout=timeout)
    if result["ok"]:
        return True, str(result["stdout"] or "")
    return False, str(result["stderr"] or result["stdout"] or "")


def _combine_output(result: dict) -> str:
    return "\n".join(
        part
        for part in (str(result["stdout"] or ""), str(result["stderr"] or ""))
        if part
    ).strip()


def _parse_window_ids(raw: str) -> list[int]:
    window_ids: list[int] = []
    for line in str(raw or "").splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            window_ids.append(int(line, 0))
        except Exception:
            pass
    return window_ids


def _read_xprop(window_id: int, property_name: str) -> str:
    if not window_id or shutil.which("xprop") is None:
        return ""
    result = _run_command_capture(
        ["xprop", "-id", str(int(window_id)), property_name],
        timeout=1.5,
    )
    combined = _combine_output(result)
    if not combined:
        return ""
    if any(token in combined.lower() for token in SOFT_X11_FAILURE_HINTS):
        return ""
    if result["returncode"] != 0 and "=" not in combined:
        return ""
    return str(result["stdout"] or combined)


def _parse_xprop_string(output: str) -> str:
    matches = [value.strip() for value in re.findall(r'"([^"]*)"', str(output or ""))]
    if matches:
        return " ".join(value for value in matches if value).strip()
    if "=" in str(output or ""):
        return str(output).split("=", 1)[1].strip()
    return ""


def _parse_xprop_pid(output: str) -> int:
    match = re.search(r"=\s*(\d+)", str(output or ""))
    if not match:
        return 0
    try:
        return int(match.group(1))
    except Exception:
        return 0


def _title_looks_like_slideshow(title: str, *, strict: bool = False) -> bool:
    text = str(title or "").strip().lower()
    if not text:
        return False
    hints = (
        STRICT_SLIDESHOW_WINDOW_TITLE_HINTS if strict else SLIDESHOW_WINDOW_TITLE_HINTS
    )
    return any(hint in text for hint in hints)


def _class_looks_like_slideshow(class_name: str) -> bool:
    text = str(class_name or "").strip().lower()
    if not text:
        return False
    return any(token in text for token in SLIDESHOW_CLASS_HINTS)


def get_linux_tool_capabilities() -> dict:
    global _LINUX_TOOL_CAPABILITIES
    if _LINUX_TOOL_CAPABILITIES is not None:
        return dict(_LINUX_TOOL_CAPABILITIES)

    caps = {
        "display": _has_x11_display(),
        "xdotool": shutil.which("xdotool") is not None,
        "xprop": shutil.which("xprop") is not None,
        "getwindowfocus": False,
        "getwindowpid": False,
        "getwindowclassname": False,
        "search_pid": False,
    }

    if caps["xdotool"]:
        help_result = _run_command_capture(["xdotool", "help"], timeout=1.5)
        help_text = _combine_output(help_result).lower()
        caps["getwindowfocus"] = "getwindowfocus" in help_text
        caps["getwindowpid"] = "getwindowpid" in help_text
        caps["getwindowclassname"] = "getwindowclassname" in help_text

        search_help = _run_command_capture(["xdotool", "search", "--help"], timeout=1.5)
        search_text = _combine_output(search_help).lower()
        if "--pid" in search_text:
            caps["search_pid"] = True
        else:
            probe = _run_command_capture(
                ["xdotool", "search", "--pid", "0", ".*"],
                timeout=1.5,
            )
            probe_text = _combine_output(probe).lower()
            caps["search_pid"] = (
                "--pid" not in probe_text
                and "unknown option" not in probe_text
                and "unrecognized option" not in probe_text
                and "unknown command" not in probe_text
            )

    _LINUX_TOOL_CAPABILITIES = dict(caps)
    return dict(caps)


def describe_linux_tool_capabilities() -> str:
    caps = get_linux_tool_capabilities()
    return (
        "DISPLAY="
        f"{'set' if caps['display'] else 'missing'} "
        f"xdotool={'yes' if caps['xdotool'] else 'no'} "
        f"xprop={'yes' if caps['xprop'] else 'no'} "
        f"getwindowfocus={'yes' if caps['getwindowfocus'] else 'no'} "
        f"getwindowpid={'yes' if caps['getwindowpid'] else 'no'} "
        f"getwindowclassname={'yes' if caps['getwindowclassname'] else 'no'} "
        f"search_pid={'yes' if caps['search_pid'] else 'no'}"
    )


def can_use_xdotool() -> bool:
    caps = get_linux_tool_capabilities()
    return (
        sys.platform.startswith("linux")
        and bool(caps["display"])
        and bool(caps["xdotool"])
    )


def run_xdotool_command(*args: str, timeout: float = 1.5) -> tuple[bool, str]:
    if not can_use_xdotool():
        return False, ""
    return _run_command(["xdotool", *args], timeout=timeout)


def infer_linux_presentation_kind(title: str, class_name: str) -> str | None:
    title_lower = str(title or "").strip().lower()
    class_lower = str(class_name or "").strip().lower()
    combined = f"{title_lower} {class_lower}"
    for kind, hints in PRESENTATION_KIND_HINTS.items():
        if any(hint in combined for hint in hints):
            return kind
    return None


def window_looks_like_editor(
    window_id: int = 0,
    *,
    title: str = "",
    class_name: str = "",
) -> bool:
    if not title and not class_name and window_id:
        snapshot = get_window_snapshot(window_id)
        title = str(snapshot["title"] or "")
        class_name = str(snapshot["class"] or "")

    if _title_looks_like_slideshow(title, strict=False):
        return False

    title_lower = str(title or "").strip().lower()
    class_lower = str(class_name or "").strip().lower()
    if not title_lower and not class_lower:
        return False
    if title_lower in EDITOR_WINDOW_EXACT_TITLES:
        return True
    if any(title_lower.endswith(suffix) for suffix in EDITOR_WINDOW_SUFFIXES):
        return True
    if "wps 文字" in title_lower or "wps writer" in title_lower:
        return True
    return False


def snapshot_is_transient(snapshot: dict) -> bool:
    if not snapshot:
        return False
    window_id = int(snapshot.get("window_id", 0) or 0)
    title = str(snapshot.get("title", "") or "").strip()
    class_name = str(snapshot.get("class", "") or "").strip()
    return bool(window_id and not title and not class_name)


def format_window_snapshot(snapshot: dict | None = None) -> str:
    snapshot = dict(snapshot or {})
    window_id = int(snapshot.get("window_id", 0) or 0)
    title = str(snapshot.get("title", "") or "").strip() or "<empty>"
    class_name = str(snapshot.get("class", "") or "").strip() or "<empty>"
    pid = int(snapshot.get("pid", 0) or 0)
    source = str(snapshot.get("source", "") or "").strip()
    parts = [
        f"id={window_id}",
        f"title={title!r}",
        f"class={class_name!r}",
    ]
    if pid:
        parts.append(f"pid={pid}")
    if source:
        parts.append(f"source={source}")
    return " ".join(parts)


def get_window_snapshot(window_id: int) -> dict:
    snapshot = {
        "window_id": 0,
        "title": "",
        "class": "",
        "pid": 0,
        "kind": "",
        "is_wps": False,
        "is_editor": False,
        "is_slideshow": False,
        "is_transient": False,
        "source": "window",
    }
    try:
        window_id = int(window_id or 0)
    except Exception:
        window_id = 0
    snapshot["window_id"] = window_id
    if not window_id or not can_use_xdotool():
        return snapshot

    caps = get_linux_tool_capabilities()

    ok, output = run_xdotool_command("getwindowname", str(window_id))
    if ok:
        snapshot["title"] = output
    if not snapshot["title"]:
        snapshot["title"] = _parse_xprop_string(
            _read_xprop(window_id, "_NET_WM_NAME")
        ) or _parse_xprop_string(_read_xprop(window_id, "WM_NAME"))

    if caps["getwindowclassname"]:
        ok, output = run_xdotool_command("getwindowclassname", str(window_id))
        if ok:
            snapshot["class"] = output
    if not snapshot["class"]:
        snapshot["class"] = _parse_xprop_string(_read_xprop(window_id, "WM_CLASS"))

    if caps["getwindowpid"]:
        ok, output = run_xdotool_command("getwindowpid", str(window_id))
        if ok:
            window_ids = _parse_window_ids(output)
            if window_ids:
                snapshot["pid"] = int(window_ids[0])
    if not snapshot["pid"]:
        snapshot["pid"] = _parse_xprop_pid(_read_xprop(window_id, "_NET_WM_PID"))

    snapshot["kind"] = (
        infer_linux_presentation_kind(
            str(snapshot["title"] or ""),
            str(snapshot["class"] or ""),
        )
        or ""
    )
    snapshot["is_editor"] = window_looks_like_editor(
        title=str(snapshot["title"] or ""),
        class_name=str(snapshot["class"] or ""),
    )
    snapshot["is_wps"] = bool(
        snapshot["kind"] == "wps"
        or "wps" in str(snapshot["title"] or "").lower()
        or "wpp" in str(snapshot["class"] or "").lower()
    )
    snapshot["is_slideshow"] = window_looks_like_slideshow(
        title=str(snapshot["title"] or ""),
        class_name=str(snapshot["class"] or ""),
    )
    snapshot["is_transient"] = snapshot_is_transient(snapshot)
    return snapshot


def get_active_window_snapshot() -> dict:
    if not can_use_xdotool():
        return get_window_snapshot(0)

    caps = get_linux_tool_capabilities()
    commands = []
    if caps["getwindowfocus"]:
        commands.append("getwindowfocus")
    commands.append("getactivewindow")

    for command in commands:
        ok, output = run_xdotool_command(command)
        if not ok:
            continue
        window_ids = _parse_window_ids(output)
        if not window_ids:
            continue
        snapshot = get_window_snapshot(window_ids[0])
        snapshot["source"] = command
        if snapshot["window_id"] and (
            not snapshot_is_transient(snapshot) or command == commands[-1]
        ):
            return snapshot
    return get_window_snapshot(0)


def window_looks_like_slideshow(
    window_id: int = 0,
    *,
    title: str = "",
    class_name: str = "",
    strict: bool = False,
) -> bool:
    if not title and not class_name and window_id:
        snapshot = get_window_snapshot(window_id)
        title = str(snapshot["title"] or "")
        class_name = str(snapshot["class"] or "")
    if _title_looks_like_slideshow(title, strict=strict):
        return True
    if window_looks_like_editor(title=title, class_name=class_name):
        return False
    return _class_looks_like_slideshow(class_name)


def _search_visible_windows(pattern: str) -> list[int]:
    ok, output = run_xdotool_command("search", "--onlyvisible", str(pattern))
    if not ok:
        return []
    return _parse_window_ids(output)


def _search_visible_windows_by_pid(pid: int) -> list[int]:
    if not pid or not get_linux_tool_capabilities().get("search_pid"):
        return []
    ok, output = run_xdotool_command(
        "search", "--onlyvisible", "--pid", str(int(pid)), ".*"
    )
    if not ok:
        return []
    return _parse_window_ids(output)


def _copy_snapshot(snapshot: dict, **extra) -> dict:
    merged = dict(snapshot or {})
    merged.update(extra)
    return merged


def find_linux_slideshow_window(cached_window_id: int = 0) -> dict:
    if not can_use_xdotool():
        return get_window_snapshot(0)

    active_snapshot = get_active_window_snapshot()
    active_window_id = int(active_snapshot.get("window_id", 0) or 0)
    cached_snapshot = (
        get_window_snapshot(cached_window_id)
        if cached_window_id
        else get_window_snapshot(0)
    )

    if active_window_id and window_looks_like_slideshow(
        title=str(active_snapshot.get("title", "") or ""),
        class_name=str(active_snapshot.get("class", "") or ""),
        strict=False,
    ):
        return _copy_snapshot(active_snapshot, match_source="focus")

    cached_window = int(cached_snapshot.get("window_id", 0) or 0)
    if cached_window and window_looks_like_slideshow(
        title=str(cached_snapshot.get("title", "") or ""),
        class_name=str(cached_snapshot.get("class", "") or ""),
        strict=False,
    ):
        if snapshot_is_transient(active_snapshot):
            return _copy_snapshot(cached_snapshot, match_source="cached-transient")
        return _copy_snapshot(cached_snapshot, match_source="cached")

    candidates: list[dict] = []
    seen: set[int] = set()

    def add_candidate(window_id: int, source: str) -> None:
        try:
            window_id = int(window_id or 0)
        except Exception:
            window_id = 0
        if window_id <= 0 or window_id in seen:
            return
        seen.add(window_id)
        snapshot = get_window_snapshot(window_id)
        if not snapshot.get("window_id"):
            return
        candidates.append(_copy_snapshot(snapshot, match_source=source))

    search_terms = (
        "WPS Presentation Slide Show",
        "slide show",
        "slideshow",
        "幻灯片放映",
        "幻燈片放映",
        "投影片放映",
    )
    for term in search_terms:
        for window_id in _search_visible_windows(term):
            add_candidate(window_id, f"search:{term}")

    if get_linux_tool_capabilities().get("search_pid"):
        pid_candidates = [
            int(active_snapshot.get("pid", 0) or 0),
            int(cached_snapshot.get("pid", 0) or 0),
        ]
        for pid in pid_candidates:
            for window_id in _search_visible_windows_by_pid(pid):
                add_candidate(window_id, f"pid:{pid}")

    if not candidates:
        for window_id in _search_visible_windows(".*"):
            add_candidate(window_id, "search:visible")

    for snapshot in candidates:
        if window_looks_like_slideshow(
            title=str(snapshot.get("title", "") or ""),
            class_name=str(snapshot.get("class", "") or ""),
            strict=False,
        ):
            return snapshot
    return get_window_snapshot(0)


def find_linux_slideshow_window_id(cached_window_id: int = 0) -> int:
    snapshot = find_linux_slideshow_window(cached_window_id)
    return int(snapshot.get("window_id", 0) or 0)


def extract_filename_from_title(title: str) -> str | None:
    """
    从 WPS 演示窗口标题中提取文件名（格式为 `[文件名]`）。

    Args:
        title: 窗口标题，如 "[坚持] - WPS 演示" 或 "[presentation] WPS Presentation Slide Show"

    Returns:
        提取的文件名，如 "坚持" 或 "presentation"；如果未找到则返回 None
    """
    if not title:
        return None

    # 匹配 [文件名] 格式，支持中英文、空格、特殊字符
    match = re.search(r"\[([^\]]+)\]", str(title))
    if match:
        filename = match.group(1).strip()
        return filename if filename else None
    return None


def find_ppt_path_by_pid(pid: int, filename: str) -> str | None:
    """
    通过进程 ID 在 /proc/{pid}/fd 中查找匹配文件名的完整路径。

    Args:
        pid: 进程 ID
        filename: 要查找的文件名（不含扩展名）

    Returns:
        完整文件路径；如果未找到则返回 None
    """
    if not pid or not filename:
        return None

    fd_dir = f"/proc/{pid}/fd"
    if not os.path.isdir(fd_dir):
        return None

    try:
        # 执行 ls -la /proc/{pid}/fd
        result = subprocess.run(
            ["ls", "-la", fd_dir],
            capture_output=True,
            text=True,
            timeout=5.0,
        )
        if result.returncode != 0:
            return None

        filename_lower = filename.lower()
        candidates = []

        for line in result.stdout.splitlines():
            # 解析形如: lr-x------ 1 user user 64 Apr 11 18:03 66 -> /home/user/坚持.pptx
            match = re.search(r"->\s*(.+)$", line)
            if not match:
                continue

            file_path = match.group(1).strip()
            basename = os.path.basename(file_path)
            basename_lower = basename.lower()

            # 检查文件名是否匹配（支持部分匹配）
            if filename_lower in basename_lower:
                # 检查是否是 PPT 文件
                if basename_lower.endswith(
                    (".ppt", ".pptx", ".pps", ".ppsx", ".dps", ".dpt")
                ):
                    candidates.append(file_path)

        if not candidates:
            return None

        # 优先返回非临时文件路径（不包含 .~ 前缀的路径）
        for path in candidates:
            basename = os.path.basename(path)
            if not basename.startswith(".~"):
                return path

        # 如果没有非临时文件，返回第一个候选
        return candidates[0]

    except Exception:
        return None


def get_ppt_path_from_slideshow_window(window_id: int) -> str | None:
    """
    从 WPS 放映窗口获取对应的 PPT 文件路径。

    Args:
        window_id: 窗口 ID

    Returns:
        PPT 文件的完整路径；如果未找到则返回 None
    """
    if not window_id:
        return None

    # 获取窗口信息
    snapshot = get_window_snapshot(window_id)
    title = snapshot.get("title", "")
    pid = snapshot.get("pid", 0)

    if not title or not pid:
        return None

    # 从标题提取文件名
    filename = extract_filename_from_title(title)
    if not filename:
        return None

    # 通过 PID 查找文件路径
    return find_ppt_path_by_pid(pid, filename)


def has_libreoffice() -> bool:
    """检查系统是否安装了 LibreOffice。"""
    return (
        shutil.which("libreoffice") is not None or shutil.which("soffice") is not None
    )


def get_libreoffice_command() -> str:
    """获取 LibreOffice 命令路径。"""
    for cmd in ["libreoffice", "soffice"]:
        path = shutil.which(cmd)
        if path:
            return path
    return "libreoffice"


def generate_thumbnail_with_libreoffice(
    ppt_path: str,
    output_path: str,
    slide_index: int = 1,
    width: int = 320,
    height: int = 180,
) -> bool:
    """
    使用 LibreOffice 无头模式生成 PPT 幻灯片缩略图。

    Args:
        ppt_path: PPT 文件路径
        output_path: 输出图片路径
        slide_index: 幻灯片索引（从1开始）
        width: 输出图片宽度
        height: 输出图片高度

    Returns:
        是否成功生成缩略图
    """
    if not has_libreoffice():
        return False

    if not os.path.exists(ppt_path):
        return False

    # 确保输出目录存在
    output_dir = os.path.dirname(output_path)
    if output_dir and not os.path.exists(output_dir):
        try:
            os.makedirs(output_dir, exist_ok=True)
        except Exception:
            return False

    import tempfile

    try:
        with tempfile.TemporaryDirectory() as tmpdir:
            libreoffice = get_libreoffice_command()

            # LibreOffice 导出所有幻灯片为图片
            # 使用 --headless 模式避免启动 GUI
            cmd = [
                libreoffice,
                "--headless",
                "--convert-to",
                "png",
                "--outdir",
                tmpdir,
                ppt_path,
            ]

            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                timeout=60.0,
            )

            if result.returncode != 0:
                return False

            # LibreOffice 生成的文件名格式: 原文件名-幻灯片索引.png
            # 例如: test.pptx -> test-1.png, test-2.png, ...
            base_name = os.path.splitext(os.path.basename(ppt_path))[0]
            generated_name = f"{base_name}-{slide_index}.png"
            generated_path = os.path.join(tmpdir, generated_name)

            # 如果指定索引的文件不存在，尝试找第一个生成的文件
            if not os.path.exists(generated_path):
                # 查找所有生成的 png 文件
                png_files = [
                    f
                    for f in os.listdir(tmpdir)
                    if f.endswith(".png") and f.startswith(base_name)
                ]
                if not png_files:
                    return False
                # 按名称排序，取第一个
                png_files.sort()
                generated_path = os.path.join(tmpdir, png_files[0])

            # 使用 PIL 调整图片大小
            from PIL import Image

            with Image.open(generated_path) as img:
                # 保持宽高比，缩放到指定尺寸
                img.thumbnail((width, height), Image.Resampling.LANCZOS)
                img.save(output_path, format="PNG")

            return True

    except Exception:
        return False


class LinuxSystemAPI(SystemAPI):
    def __init__(self):
        self._last_slideshow_window_id = 0

    def get_media_info(self):
        if shutil.which("playerctl"):
            try:
                status = subprocess.check_output(
                    ["playerctl", "status"], text=True
                ).strip()
                title = subprocess.check_output(
                    ["playerctl", "metadata", "title"], text=True
                ).strip()
                artist = subprocess.check_output(
                    ["playerctl", "metadata", "artist"], text=True
                ).strip()
                return {
                    "title": title,
                    "artist": artist,
                    "status": status,
                    "position_ms": 0,
                    "duration_ms": 0,
                }
            except Exception:
                pass
        return {
            "title": "",
            "artist": "",
            "status": "Stopped",
            "position_ms": 0,
            "duration_ms": 0,
        }

    def get_file_icon(self, path):
        return None

    def get_ppt_slideshow_hwnd(self):
        window_id = find_linux_slideshow_window_id(self._last_slideshow_window_id)
        if window_id:
            self._last_slideshow_window_id = int(window_id)
        return int(window_id or 0)

    def start_focus_watcher(self, callback):
        pass

    def stop_focus_watcher(self):
        pass

    def get_system_fonts(self):
        if shutil.which("fc-list"):
            try:
                output = subprocess.check_output(["fc-list", ":", "family"], text=True)
                fonts = set()
                for line in output.splitlines():
                    for font_name in line.split(","):
                        fonts.add(font_name.strip())
                return sorted(list(fonts))
            except Exception:
                pass
        return []

    def get_display_scale(self):
        return 1.0

    def find_wps_process(self) -> bool:
        try:
            result = subprocess.run(
                ["pgrep", "-x", "wpp"],
                capture_output=True,
                text=True,
            )
            if result.returncode == 0 and result.stdout.strip():
                return True
            result = subprocess.run(
                ["pgrep", "-f", "wps"],
                capture_output=True,
                text=True,
            )
            return result.returncode == 0 and bool(result.stdout.strip())
        except Exception:
            return False

    def get_active_window_info(self) -> dict:
        snapshot = get_active_window_snapshot()
        return {
            "title": snapshot["title"],
            "class": snapshot["class"],
            "pid": snapshot["pid"],
            "is_wps": snapshot["is_wps"],
            "is_slideshow": snapshot["is_slideshow"],
        }
