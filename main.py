import ctypes
import os
import shutil
import sys
from pathlib import Path


def register_url_protocol():
	if sys.platform != "win32":
		return
	try:
		import winreg

		key_path = r"Software\Classes\luminalium"

		# コマンドラインを構築
		if getattr(sys, "frozen", False):
			exe_path = os.path.abspath(sys.executable)
			new_cmd = f'"{exe_path}" "%1"'
			icon_path = f'"{exe_path}",0'
		else:
			main_py = os.path.abspath(__file__)
			python_exe = os.path.abspath(sys.executable)
			# pythonw.exe でCMD窓を出さずに起動
			pythonw_exe = python_exe.replace("python.exe", "pythonw.exe")
			if not os.path.exists(pythonw_exe):
				pythonw_exe = python_exe
			new_cmd = f'"{pythonw_exe}" "{main_py}" "%1"'
			icon_path = f'"{pythonw_exe}",0'

		# 既存のコマンドが最新かチェック、古い or なければ再登録
		try:
			cmd_key = winreg.OpenKey(
				winreg.HKEY_CURRENT_USER,
				key_path + r"\shell\open\command",
				0,
				winreg.KEY_READ,
			)
			old_cmd = winreg.QueryValue(cmd_key, "")
			winreg.CloseKey(cmd_key)
			if old_cmd == new_cmd:
				return  # すでに最新
		except FileNotFoundError:
			pass

		key = winreg.CreateKey(winreg.HKEY_CURRENT_USER, key_path)
		winreg.SetValue(key, "", winreg.REG_SZ, "URL:Luminalium Protocol")
		winreg.SetValueEx(key, "URL Protocol", 0, winreg.REG_SZ, "")

		icon_key = winreg.CreateKey(key, "DefaultIcon")
		winreg.SetValue(icon_key, "", winreg.REG_SZ, icon_path)
		winreg.CloseKey(icon_key)

		cmd_key = winreg.CreateKey(key, r"shell\open\command")
		winreg.SetValue(cmd_key, "", winreg.REG_SZ, new_cmd)
		winreg.CloseKey(cmd_key)

		winreg.CloseKey(key)
	except Exception as e:
		print(f"Failed to register URL protocol: {e}")


def unregister_url_protocol():
	if sys.platform != "win32":
		return
	try:
		import winreg

		key_path = r"Software\Classes\luminalium"
		winreg.DeleteKey(winreg.HKEY_CURRENT_USER, key_path + r"\shell\open\command")
		winreg.DeleteKey(winreg.HKEY_CURRENT_USER, key_path + r"\shell\open")
		winreg.DeleteKey(winreg.HKEY_CURRENT_USER, key_path + r"\shell")
		winreg.DeleteKey(winreg.HKEY_CURRENT_USER, key_path + r"\DefaultIcon")
		winreg.DeleteKey(winreg.HKEY_CURRENT_USER, key_path)
		print("[Protocol] URL protocol unregistered successfully")
	except FileNotFoundError:
		pass
	except Exception as e:
		print(f"Failed to unregister URL protocol: {e}")


def parse_luminalium_url(url: str) -> dict | None:
	if not url or not url.startswith("luminalium://"):
		return None
	try:
		from urllib.parse import parse_qs, urlparse

		parsed = urlparse(url)
		path = parsed.path.strip("/")
		parts = [p for p in path.split("/") if p]
		query = parse_qs(parsed.query or "")
		host = parsed.hostname or ""
		if host == "app" and len(parts) >= 1:
			if parts[0] == "settings":
				page = parts[1] if len(parts) >= 2 else "main"
				return {"action": "settings", "page": page}
		elif host == "intergrate" and len(parts) >= 1:
			target = parts[0]
			if target in ("timer", "board"):
				route = {"action": target}
				for key, values in query.items():
					if values:
						route[key] = values[-1]
				return route
	except Exception:
		pass
	return None


# Nuitka standalone detection and compatibility
if hasattr(sys, "nuitka_binary"):
	sys.frozen = True

import faulthandler
import importlib
import importlib.util
import json
import subprocess
import tempfile
import time
import traceback
import warnings
from typing import Optional

if sys.platform == "linux":
	_HAS_X11_DISPLAY = bool(os.environ.get("DISPLAY"))
	_HAS_WAYLAND_DISPLAY = bool(os.environ.get("WAYLAND_DISPLAY"))
	_HAS_DISPLAY = _HAS_X11_DISPLAY or _HAS_WAYLAND_DISPLAY
	_LINUX_QPA_OVERRIDE = str(os.environ.get("LUMINALIUM_QPA_PLATFORM", "")).strip()
	_FORCE_X11 = str(os.environ.get("LUMINALIUM_FORCE_X11", "")).strip().lower() in (
		"1",
		"true",
		"yes",
	)
	if "QT_QPA_PLATFORM" not in os.environ:
		if _LINUX_QPA_OVERRIDE:
			os.environ["QT_QPA_PLATFORM"] = _LINUX_QPA_OVERRIDE
		elif _HAS_X11_DISPLAY:
			os.environ["QT_QPA_PLATFORM"] = "xcb"
			if _FORCE_X11:
				print(
					"[Main] LUMINALIUM_FORCE_X11 is set, using X11/XWayland for WPS RPC compatibility"
				)
		elif _HAS_WAYLAND_DISPLAY:
			os.environ["QT_QPA_PLATFORM"] = "wayland"
		else:
			os.environ["QT_QPA_PLATFORM"] = "offscreen"
	_SELECTED_QPA_PLATFORM = str(os.environ.get("QT_QPA_PLATFORM", "")).strip().lower()
	if _SELECTED_QPA_PLATFORM.startswith("xcb") and _HAS_X11_DISPLAY:
		if _HAS_WAYLAND_DISPLAY:
			os.environ["LUMINALIUM_XWAYLAND_SESSION"] = "1"
			original_wayland_display = os.environ.get("WAYLAND_DISPLAY")
			if original_wayland_display:
				os.environ["LUMINALIUM_ORIGINAL_WAYLAND_DISPLAY"] = (
					original_wayland_display
				)
				os.environ.pop("WAYLAND_DISPLAY", None)
			if "LUMINALIUM_ORIGINAL_XDG_SESSION_TYPE" not in os.environ:
				current_session_type = os.environ.get("XDG_SESSION_TYPE")
				os.environ["LUMINALIUM_ORIGINAL_XDG_SESSION_TYPE"] = (
					current_session_type if current_session_type is not None else ""
				)
			os.environ["XDG_SESSION_TYPE"] = "x11"
			print(
				"[Main] Normalized Linux environment for QtWebEngine:"
				" xcb session will hide WAYLAND_DISPLAY and force XDG_SESSION_TYPE=x11",
				flush=True,
			)
		else:
			os.environ.pop("LUMINALIUM_XWAYLAND_SESSION", None)
	print(
		"[Main] Linux display detection:"
		f" wayland={_HAS_WAYLAND_DISPLAY}"
		f" x11={_HAS_X11_DISPLAY}"
		f" qpa={os.environ.get('QT_QPA_PLATFORM', '')}"
		f" force_x11={_FORCE_X11}"
	)
	# Add --no-sandbox to avoid zygote crash on some Linux environments
	# This must be done BEFORE any Qt import or QApp creation
	if "--no-sandbox" not in sys.argv:
		sys.argv.append("--no-sandbox")

# Delay heavy imports or move them inside if __name__ == "__main__" logic
# to allow --webview-runner to start fast and clean.

if __name__ == "__main__":
	if "--webview-runner" in sys.argv:
		idx = sys.argv.index("--webview-runner")
		import plugins.webview_runner as _wv

		sys.argv = ["webview_runner.py"] + sys.argv[idx + 1 :]
		_wv.main()
		sys.exit(0)

	if "--dialog" in sys.argv or "--crash-file" in sys.argv:
		import plugins.webview_runner as _wv

		_wv.main()
		sys.exit(0)

	if "--memory-cleaner" in sys.argv:
		from ppt_assistant.core.memory_cleaner import run_memory_cleaner

		parent_pid = int(os.environ.get("LUMINALIUM_PARENT_PID", "0"))
		interval = int(os.environ.get("LUMINALIUM_MEMCLEAN_INTERVAL", "30"))
		if parent_pid:
			run_memory_cleaner(parent_pid, interval)
		sys.exit(0)

from PySide6.QtCore import (
	QCoreApplication,
	QEasingCurve,
	QEvent,
	QEventLoop,
	QObject,
	QParallelAnimationGroup,
	QPoint,
	QPropertyAnimation,
	QRect,
	Qt,
	QTimer,
	Slot,
)
from PySide6.QtGui import (
	QBrush,
	QColor,
	QFont,
	QFontDatabase,
	QFontMetrics,
	QIcon,
	QPainter,
	QPen,
)
from PySide6.QtWidgets import (
	QApplication,
	QFrame,
	QLabel,
	QProgressBar,
	QWidget,
)

from ppt_assistant.core.config import (
	FIRST_RUN,
	PLUGINS_DIR,
	ROOT_DIR,
	Theme,
	_apply_theme_and_color,
	cfg,
	reload_cfg,
)
from ppt_assistant.core.config import (
	SETTINGS_PATH as _SETTINGS_PATH_ORIG,
)
from ppt_assistant.core.config_base import setThemeColor
from ppt_assistant.core.ppt_monitor import PPTMonitor
from ppt_assistant.core.system_theme_watcher import SystemThemeWatcherManager
from ppt_assistant.ui.overlay import create_overlay_window
from ppt_assistant.ui.tray import SystemTray, is_system_tray_supported


def _apply_resolved_theme_color(theme_value):
	"""Apply resolved theme to qfluentwidgets engine and accent color.
	Used by SystemThemeWatcher when AUTO mode resolves to actual system theme.
	Does NOT write cfg.themeMode (stays as AUTO)."""
	from qfluentwidgets import qconfig

	qconfig.theme = theme_value
	if theme_value == Theme.DARK:
		setThemeColor("#E1EBFF")
	else:
		setThemeColor("#3275F5")


def _get_active_settings_path():
	settings_dir = os.path.dirname(_SETTINGS_PATH_ORIG)
	active_marker = os.path.join(settings_dir, "_active")
	if os.path.exists(active_marker):
		try:
			with open(active_marker, "r", encoding="utf-8") as f:
				name = f.read().strip()
			if name and name != "default":
				profile_path = os.path.join(settings_dir, name + ".json")
				if os.path.exists(profile_path):
					return profile_path
		except Exception:
			pass
	return _SETTINGS_PATH_ORIG


SETTINGS_PATH = _get_active_settings_path()
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.classisland_monitor import ClassIslandMonitor
from ppt_assistant.core.i18n import t
from ppt_assistant.core.linux_focus_watcher import LinuxFocusWatcher
from ppt_assistant.core.platform_integration import open_path
from ppt_assistant.core.resource_monitor import SystemResourceMonitor
from ppt_assistant.core.timer_manager import TimerManager
from ppt_assistant.core.win_focus_watcher import WindowsFocusWatcher
from ppt_assistant.core.windows_notifications import (
	configure_current_process_for_notifications,
	send_windows_notification,
)


class WindowIconEventFilter(QObject):
	def __init__(self, icon: QIcon):
		super().__init__()
		self._icon = icon

	def eventFilter(self, obj, event):
		# Early-return on event type before touching `obj`. PySide must wrap
		# `obj` into a Python object (`getWrapperForQObject`) before invoking
		# this Python eventFilter; on Linux/xcb that wrapping has crashed for
		# short-lived QQuickItem objects in the QML hover delivery path. Even
		# though the application-level filter has been replaced with per-window
		# installs, this early-return is kept as a defensive backstop.
		et = event.type()
		if et != QEvent.Show and et != QEvent.Polish:
			return False
		if self._icon.isNull():
			return False
		try:
			if (
				isinstance(obj, QWidget)
				and obj.isWindow()
				and obj.windowIcon().isNull()
			):
				obj.setWindowIcon(self._icon)
		except Exception:
			return False
		return False


def _try_install_window_icon_filter(widget):
	"""Helper to install the icon filter on a widget from its constructor.

	This is called before QApplication.exec() starts, so we retrieve the
	installer function from `QApplication.instance()._install_window_icon_filter_on`
	which is set up in the main entry point.
	"""
	try:
		app = QApplication.instance()
		if app is not None and hasattr(app, "_install_window_icon_filter_on"):
			app._install_window_icon_filter_on(widget)
	except Exception:
		pass


SPLASH_I18N = {
	"zh-CN": {
		"initializing": "正在启动",
		"loading_config": "加载配置",
		"loading_fonts": "加载字体",
		"init_monitor": "启动监视器",
		"prepare_ui_env": "准备界面环境",
		"init_ui": "创建界面",
		"loading_plugins": "加载插件",
		"loading_settings": "加载设置",
		"loading_timer": "加载计时器",
		"init_tray": "创建托盘图标",
		"finalizing": "正在完成启动后操作",
		"watermark.1": "开发中版本",
		"watermark.2": "技术预览版",
		"watermark.3": "Release Preview",
		"watermark.4": "重新评估版本",
		"dev_watermark": "{type}\n不保证最终品质 （{version}）",
	},
	"zh-TW": {
		"initializing": "正在初始化",
		"loading_config": "載入設定",
		"loading_fonts": "載入字型",
		"init_monitor": "啟動監視器",
		"prepare_ui_env": "準備介面環境",
		"init_ui": "建立介面",
		"loading_plugins": "載入插件",
		"loading_settings": "載入設定",
		"loading_timer": "載入計時器",
		"init_tray": "建立系統匣圖示",
		"finalizing": "正在完成啓動後操作",
		"watermark.1": "開發中版本",
		"watermark.2": "技術預覽版",
		"watermark.3": "Release Preview",
		"watermark.4": "重新評估版本",
		"dev_watermark": "{type}\n不保證最終品質 （{version}）",
	},
	"yue-HK": {
		"initializing": "開工中",
		"loading_config": "撈緊設定",
		"loading_fonts": "撈緊字型",
		"init_monitor": "啟動監視器",
		"prepare_ui_env": "準備介面環境",
		"init_ui": "砌緊介面",
		"loading_plugins": "載入插件",
		"loading_settings": "載入設定",
		"loading_timer": "載入計時器",
		"init_tray": "整緊托盤圖示",
		"finalizing": "正喺完成啟動後操作",
		"watermark.1": "開發中版本",
		"watermark.2": "技術預覽版",
		"watermark.3": "Release Preview",
		"watermark.4": "重新評估版本",
		"dev_watermark": "{type}\n品質唔包（{version}）",
	},
	"ja-JP": {
		"initializing": "初期化中",
		"loading_config": "設定を読み込み中",
		"loading_fonts": "フォントを読み込み中",
		"init_monitor": "モニターを起動中",
		"prepare_ui_env": "UI環境を準備中",
		"init_ui": "UIを作成中",
		"loading_plugins": "プラグインを読み込み中",
		"loading_settings": "設定を読み込み中",
		"loading_timer": "タイマーを読み込み中",
		"init_tray": "トレイアイコンを作成中",
		"finalizing": "起動後の操作を実行中",
		"watermark.1": "開発中バージョン",
		"watermark.2": "テクニカルプレビュー",
		"watermark.3": "Release Preview",
		"watermark.4": "再評価バージョン",
		"dev_watermark": "{type}\n品質は保証されません （{version}）",
	},
	"en-US": {
		"initializing": "Initializing",
		"loading_config": "Loading config",
		"loading_fonts": "Loading fonts",
		"init_monitor": "Starting monitor",
		"prepare_ui_env": "Preparing UI environment",
		"init_ui": "Creating UI",
		"loading_plugins": "Loading plugins",
		"loading_settings": "Loading settings",
		"loading_timer": "Loading timer",
		"init_tray": "Creating system tray",
		"finalizing": "Completing post-startup operations",
		"watermark.1": "In-Development",
		"watermark.2": "Technical Preview",
		"watermark.3": "Release Preview",
		"watermark.4": "Re-evaluated Version",
		"dev_watermark": "{type}\nFinal quality not guaranteed ({version})",
	},
}


def _is_windows7():
	try:
		v = sys.getwindowsversion()
		return v.major == 6 and v.minor == 1
	except Exception:
		return False


def _get_screen_refresh_rate() -> int:
	try:
		user32 = ctypes.windll.user32
		hdc = user32.GetDC(0)
		rate = ctypes.windll.gdi32.GetDeviceCaps(hdc, 116)  # VREFRESH
		user32.ReleaseDC(0, hdc)
		return rate if rate > 1 else 60
	except Exception:
		return 60


def _env_flag_enabled(name: str, default: bool = False) -> bool:
	raw = os.environ.get(name)
	if raw is None:
		return default
	return str(raw).strip().lower() in {"1", "true", "yes", "on"}


def _is_compatibility_mode_enabled() -> bool:
	try:
		data = _load_settings_json()
		general = data.get("General", {}) if isinstance(data, dict) else {}
		return (
			bool(general.get("CompatibilityMode", False))
			if isinstance(general, dict)
			else False
		)
	except Exception:
		return False


_VIRTUAL_GPU = None


def _is_virtual_gpu() -> bool:
	global _VIRTUAL_GPU
	if _VIRTUAL_GPU is not None:
		return _VIRTUAL_GPU

	names = []
	try:
		from ctypes import wintypes

		class DISPLAY_DEVICEW(ctypes.Structure):
			_fields_ = [
				("cb", wintypes.DWORD),
				("DeviceName", wintypes.WCHAR * 32),
				("DeviceString", wintypes.WCHAR * 128),
				("StateFlags", wintypes.DWORD),
				("DeviceID", wintypes.WCHAR * 128),
				("DeviceKey", wintypes.WCHAR * 128),
			]

		user32 = ctypes.windll.user32
		i = 0
		while True:
			dd = DISPLAY_DEVICEW()
			dd.cb = ctypes.sizeof(DISPLAY_DEVICEW)
			if not user32.EnumDisplayDevicesW(None, i, ctypes.byref(dd), 0):
				break
			for field in (dd.DeviceString, dd.DeviceID, dd.DeviceName):
				try:
					if field:
						names.append(str(field).lower())
				except Exception:
					continue
			i += 1
	except Exception:
		_VIRTUAL_GPU = False
		return _VIRTUAL_GPU

	hay = " ".join(names)
	keywords = [
		"vmware",
		"virtualbox",
		"vbox",
		"svga",
		"qxl",
		"virtio",
		"parallels",
		"hyper-v",
		"microsoft basic display",
		"basic display adapter",
		"remote display",
		"citrix",
		"xen",
		"bochs",
		"rdpdd",
		"rdp chain",
		"idriver",
		"vnc",
		"microsoft remote display",
	]
	_VIRTUAL_GPU = any(k in hay for k in keywords)
	if _VIRTUAL_GPU:
		print(
			f"[Main] Virtual GPU or headless environment detected: {hay[:200]}",
			flush=True,
		)
	return _VIRTUAL_GPU


def _force_directwrite_font_engine():
	if sys.platform != "win32":
		return
	qpa = os.environ.get("QT_QPA_PLATFORM", "windows")
	if ":fontengine=" not in qpa:
		os.environ["QT_QPA_PLATFORM"] = qpa + ":fontengine=directwrite"
	print("[Main] DirectWrite font engine forced.", flush=True)


def _apply_graphics_settings():
	if sys.platform == "linux":
		os.environ["QTWEBENGINE_DISABLE_SANDBOX"] = "1"
		os.environ.setdefault("NO_AT_BRIDGE", "1")
		os.environ.setdefault("QT_ACCESSIBILITY", "0")
		extra_chrome_flags = "--disable-renderer-accessibility"
		cur = os.environ.get("QTWEBENGINE_CHROMIUM_FLAGS", "")
		if extra_chrome_flags not in cur:
			os.environ["QTWEBENGINE_CHROMIUM_FLAGS"] = (
				cur + " " + extra_chrome_flags
			).strip()
		return

	if sys.platform == "win32":
		_force_directwrite_font_engine()

	use_software_webengine = (
		_is_compatibility_mode_enabled()
		or _env_flag_enabled("LUMINALIUM_WEBENGINE_SOFTWARE", False)
		or _is_virtual_gpu()
	)

	if use_software_webengine:
		os.environ["QSG_RHI_BACKEND"] = "software"
		os.environ["QT_QUICK_BACKEND"] = "software"
		os.environ["QT_OPENGL"] = "software"
		os.environ["QTWEBENGINE_DISABLE_GPU"] = "1"
		flags = [
			"--disable-gpu",
			"--disable-gpu-compositing",
			"--disable-gpu-rasterization",
			"--no-sandbox",
		]
	else:
		# Base flags tuned for smoother rendering and lower memory usage
		flags = [
			"--enable-gpu-rasterization",
			"--enable-zero-copy",
			"--enable-features=VaapiVideoDecoder,VaapiVideoEncoder",
			"--ignore-gpu-blocklist",
			"--enable-hardware-overlays",
			# Memory and performance optimizations
			"--js-flags=--max-old-space-size=64",
			"--disable-site-isolation-trials",
			"--renderer-process-limit=1",
			"--disable-features=Translate",
			"--disable-logging",
			"--enable-low-res-tiling",
			"--max-decoded-image-size-bytes=10485760",
			"--disk-cache-size=10485760",
			"--aggressive-cache-discard",
		]

		# Get refresh rate for target FPS
		rate = _get_screen_refresh_rate()
		target_fps = rate * 3
		os.environ["LUMINALIUM_TARGET_FPS"] = str(target_fps)

		if sys.platform == "win32":
			flags.append("--gpu-memory-buffer-budget=67108864")

	# Windows 7 Fallback
	if _is_windows7():
		candidates = [
			r"C:\Program Files\VxKex\Kex64",
			r"C:\Program Files\VxKex\Kex86",
			r"C:\Program Files (x86)\VxKex\Kex64",
			r"C:\Program Files (x86)\VxKex\Kex86",
		]
		existing = os.environ.get("PATH", "")
		for path in candidates:
			dll_path = os.path.join(path, "KxNt.dll")
			if os.path.exists(dll_path):
				if path not in existing.split(os.pathsep):
					os.environ["PATH"] = path + os.pathsep + existing
				break


def _should_enable_system_tray() -> bool:
	value = str(os.environ.get("LUMINALIUM_ENABLE_TRAY", "")).strip().lower()
	if value in ("0", "false", "no", "off"):
		return False
	if value in ("1", "true", "yes", "on"):
		return True
	return is_system_tray_supported()


_SETTINGS_CACHED_MTIME = 0
_SETTINGS_CACHED_DATA = {}


def _load_settings_json(force_reload=False):
	global _SETTINGS_CACHED_MTIME, _SETTINGS_CACHED_DATA
	if not force_reload and _SETTINGS_CACHED_MTIME > 0:
		return _SETTINGS_CACHED_DATA
	if not os.path.exists(SETTINGS_PATH):
		_SETTINGS_CACHED_DATA = {}
		_SETTINGS_CACHED_MTIME = 1
		return {}
	try:
		current_mtime = os.path.getmtime(SETTINGS_PATH)
		if (
			not force_reload
			and _SETTINGS_CACHED_MTIME > 0
			and current_mtime <= _SETTINGS_CACHED_MTIME
		):
			return _SETTINGS_CACHED_DATA
	except Exception:
		pass
	try:
		with open(SETTINGS_PATH, "rb") as f:
			raw = f.read()
	except Exception:
		return {}
	for enc in ("utf-8", "utf-8-sig", "gbk"):
		try:
			text = raw.decode(enc)
		except UnicodeDecodeError:
			continue
		try:
			data = json.loads(text)
		except Exception:
			continue
		if isinstance(data, dict):
			_SETTINGS_CACHED_DATA = data
			try:
				_SETTINGS_CACHED_MTIME = os.path.getmtime(SETTINGS_PATH)
			except Exception:
				_SETTINGS_CACHED_MTIME = 1
			return data
	_SETTINGS_CACHED_DATA = {}
	_SETTINGS_CACHED_MTIME = 1
	return {}


def _create_focus_watcher(parent):
	if sys.platform == "linux":
		return LinuxFocusWatcher(parent)
	return WindowsFocusWatcher(parent)


def _get_settings_reset_marker_path():
	return os.path.join(os.path.dirname(SETTINGS_PATH), "settings.reset")


def _get_restart_marker_path():
	return os.path.join(os.path.dirname(SETTINGS_PATH), "restart.marker")


def _write_restart_marker():
	try:
		path = _get_restart_marker_path()
		data = {"pid": os.getpid(), "ts": time.time()}
		with open(path, "w", encoding="utf-8") as f:
			json.dump(data, f, ensure_ascii=False)
	except Exception:
		pass


def _consume_restart_marker(max_age_seconds: float = 5.0) -> bool:
	path = _get_restart_marker_path()
	if not os.path.exists(path):
		return False
	data = {}
	try:
		with open(path, "r", encoding="utf-8") as f:
			data = json.load(f)
	except Exception:
		data = {}
	finally:
		try:
			os.remove(path)
		except Exception:
			pass
	try:
		ts = float(data.get("ts", 0))
	except Exception:
		ts = 0.0
	if ts <= 0:
		return False
	return (time.time() - ts) <= max_age_seconds


def _get_current_language():
	data = _load_settings_json()
	return data.get("General", {}).get("Language", "zh-CN")


def _normalize_font_weight_value(value):
	if isinstance(value, bool):
		return None
	if isinstance(value, float):
		if not value.is_integer():
			return None
		value = int(value)
	if isinstance(value, int):
		num = value
	elif isinstance(value, str):
		text = value.strip()
		if not text.isdigit():
			return None
		num = int(text)
	else:
		return None
	if num < 100 or num > 1000 or num % 100 != 0:
		return None
	return num


def _get_font_weight_from_settings(
	data, lang: str, scene: str, fallback_scene: str = ""
):
	fonts = data.get("Fonts", {}) or {}
	weights = fonts.get("Weights", {}) or {}
	lang_weights = weights.get(lang, {}) or {}
	value = _normalize_font_weight_value(lang_weights.get(scene))
	if value is not None:
		return value
	if fallback_scene:
		return _normalize_font_weight_value(lang_weights.get(fallback_scene))
	return None


_BUNDLED_FONT_FILES = {
	"google_sans_flex": "Google Sans Flex.ttf",
	"misans_vf": "MiSansVF.ttf",
	"misans_japanese_vf": "MiSansJapaneseVF.ttf",
	"misans_tc_vf": "MiSansTCVF.ttf",
}


_BUNDLED_FONT_CACHE = None


def _dedupe_font_families(families):
	seen = set()
	ordered = []
	for family in families or []:
		if not isinstance(family, str):
			continue
		name = family.strip()
		if not name or name in seen:
			continue
		seen.add(name)
		ordered.append(name)
	return ordered


def _load_bundled_font_families(root_dir: str):
	global _BUNDLED_FONT_CACHE
	if _BUNDLED_FONT_CACHE is not None:
		return _BUNDLED_FONT_CACHE
	families = {}
	fonts_dir = os.path.join(root_dir, "fonts")
	for key, file_name in _BUNDLED_FONT_FILES.items():
		font_path = os.path.join(fonts_dir, file_name)
		if not os.path.exists(font_path):
			continue
		try:
			font_id = QFontDatabase.addApplicationFont(font_path)
			if font_id == -1:
				continue
			loaded = QFontDatabase.applicationFontFamilies(font_id)
			if loaded:
				families[key] = loaded[0]
		except Exception:
			continue
	_BUNDLED_FONT_CACHE = families
	return families


def _get_default_font_family_stack(lang: str, bundled_families=None):
	if bundled_families is None:
		bundled_families = {}
	google = bundled_families.get("google_sans_flex", "Google Sans Flex")
	misans = bundled_families.get("misans_vf", "MiSans VF")
	misans_jp = bundled_families.get("misans_japanese_vf", "MiSans Japanese VF")
	misans_tc = bundled_families.get("misans_tc_vf", "MiSans TC VF")

	if lang in ("zh-TW", "ja-JP"):
		return _dedupe_font_families([google, misans_jp, misans_tc, "Segoe UI"])
	if lang == "ug-CN":
		return _dedupe_font_families([google, "Segoe UI", misans])
	return _dedupe_font_families([google, misans, "Segoe UI"])


def _build_css_font_family_value(families):
	parts = []
	for family in _dedupe_font_families(families):
		safe = family.replace("\\", "\\\\").replace("'", "\\'")
		parts.append(f"'{safe}'")
	if not parts:
		parts.append("sans-serif")
	else:
		parts.append("sans-serif")
	return ", ".join(parts)


def _apply_global_font(app: QApplication):
	root_dir = os.path.dirname(os.path.abspath(__file__))
	selected_family = ""
	data = _load_settings_json()
	lang = data.get("General", {}).get("Language", "zh-CN")
	profiles = (data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
	v = (profiles.get(lang, {}) or {}).get("qt", "")
	if isinstance(v, str) and v.strip():
		selected_family = v.strip()

	bundled_families = _load_bundled_font_families(root_dir)
	default_families = _get_default_font_family_stack(lang, bundled_families)

	if selected_family:
		font = QFont(selected_family)
	else:
		if not default_families:
			return
		font = QFont()
		font.setStyleHint(QFont.SansSerif)
		if default_families:
			font.setFamily(default_families[0])
		font.setFamilies(default_families)
	weight = _get_font_weight_from_settings(data, lang, "qt")
	if weight is not None:
		font.setWeight(weight)
	app.setFont(font)


def _load_version_info():
	root_dir = os.path.dirname(os.path.abspath(__file__))
	version_path = os.path.join(root_dir, "version.json")
	version = ""
	code_name = ""
	code_name_cn = ""
	if os.path.exists(version_path):
		try:
			with open(version_path, "r", encoding="utf-8") as f:
				data = json.load(f)
			version = data.get("version", "")
			raw_code_name = data.get("code_name", "")
			code_name_cn = data.get("code_name_CN", "")
			mapping = {
				"MomokaKawaragi": "Momoka Kawaragi",
				"NinaIseri": "Nina Iseri",
				"SubaruAwa": "Subaru Awa",
				"TomoEbizuka": "Tomo Ebizuka",
			}
			code_name = mapping.get(raw_code_name, raw_code_name)
		except Exception:
			pass
	return version, code_name, code_name_cn


def _format_version_display(version: str) -> str:
	if not version:
		return ""
	parts = str(version).strip().split(".")
	if len(parts) < 2:
		return str(version).strip()
	suffix = parts[-1]
	if suffix in ["5", "6", "7"]:
		patch_num = int(suffix) - 4
		base = ".".join(parts[:-1])
		return f"{base} Patch {patch_num}"
	return str(version).strip()


def _is_dev_preview_version(version: str) -> bool:
	if not version:
		return False
	parts = str(version).strip().split(".")
	if len(parts) < 2:
		return False
	# 除了 .0 是正式版，其他后缀 (.1, .2, .3, .4) 都带水印
	suffix = parts[-1]
	return suffix in ["1", "2", "3", "4"]


def _get_user_root_dir() -> str:
	return ROOT_DIR


def _ensure_user_dirs():
	"""Ensure user directories exist for themes and splash screens."""
	try:
		root_dir = _get_user_root_dir()
		# Prefer "user" but check for "users"
		user_dir = os.path.join(root_dir, "user")
		if not os.path.exists(user_dir) and os.path.exists(
			os.path.join(root_dir, "users")
		):
			user_dir = os.path.join(root_dir, "users")

		if not os.path.exists(user_dir):
			os.makedirs(user_dir)

		for sub in ["themes", "splash"]:
			path = os.path.join(user_dir, sub)
			if not os.path.exists(path):
				os.makedirs(path)
	except Exception as e:
		print(f"Error ensuring user directories: {e}")


def _resolve_user_splash_dir(splash_style: str) -> Optional[str]:
	if not splash_style:
		return None
	root_dir = _get_user_root_dir()
	# Support both "user" and "users" folder names
	splash_dir = os.path.join(root_dir, "user", "splash", splash_style)
	if not os.path.exists(splash_dir):
		splash_dir = os.path.join(root_dir, "users", "splash", splash_style)

	if os.path.exists(splash_dir):
		return splash_dir
	return None


def _is_valid_splash_package(splash_dir: str, splash_style: str) -> bool:
	if not splash_dir or not os.path.isdir(splash_dir):
		return False
	manifest_path = os.path.join(splash_dir, "manifest.json")
	preview_png = os.path.join(splash_dir, "preview.png")
	preview_jpg = os.path.join(splash_dir, "preview.jpg")
	splash_path = os.path.join(splash_dir, "splash.py")
	if not os.path.exists(splash_path):
		return False
	if not os.path.exists(manifest_path):
		return False
	if not (os.path.exists(preview_png) or os.path.exists(preview_jpg)):
		return False
	try:
		with open(manifest_path, "r", encoding="utf-8-sig") as f:
			data = json.load(f)
		# Remove strict name check to allow more flexible splash naming
		# if data.get("name") != splash_style:
		#     return False
	except Exception:
		return False
	return True


class StartupSplash(QWidget):
	def __init__(self, parent=None):
		super().__init__(parent)
		self._splash_style = cfg.splashStyle.value
		self._pixmap = None
		self._progress_value = 0
		self._status_text = ""

		icon = load_app_icon()
		if not icon.isNull():
			self.setWindowIcon(icon)
		self.setWindowFlags(Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Tool)
		self.setAttribute(Qt.WA_TranslucentBackground)

		if sys.platform == "win32":
			from ppt_assistant.core.platform_integration import (
				remove_window_border_delayed,
			)

			remove_window_border_delayed(self)

		self._version_raw, self._code_name_en, self._code_name_cn = _load_version_info()
		self._version_text = _format_version_display(self._version_raw)
		self._language = _get_current_language()
		self._is_first_run = FIRST_RUN

		# 确定主题
		theme_val = cfg.themeMode.value
		if theme_val == Theme.AUTO:
			from ppt_assistant.core.config import _get_system_is_dark

			self._is_dark = _get_system_is_dark()
		else:
			self._is_dark = theme_val == Theme.DARK

		applied_user = False
		if not (self._splash_style == "nina_iseri_1_2" and self._is_first_run):
			applied_user = self._apply_user_splash()

		if not applied_user:
			if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
				self._build_ui_nina()
			else:
				self._container = QFrame(self)
				self._container.setObjectName("splashContainer")
				self._build_ui()
				self._apply_styles()

		self._center_on_screen()
		self.set_progress(0, "initializing")

		if (
			self._splash_style != "nina_iseri_1_2"
			and not self._is_first_run
			and _is_dev_preview_version(self._version_raw)
		):
			self._dev_watermark = QLabel(self._container)
			i18n_table = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"])
			suffix = self._version_raw.split(".")[-1]
			w_type = i18n_table.get(f"watermark.{suffix}", "")
			tmpl = i18n_table.get("dev_watermark", "")
			self._dev_watermark.setText(
				tmpl.format(type=w_type, version=self._version_text)
			)
			font = QFont()
			font.setPixelSize(11)
			self._dev_watermark.setFont(font)
			self._dev_watermark.setAlignment(Qt.AlignRight | Qt.AlignBottom)

			watermark_color = (
				"rgba(255, 255, 255, 100)" if self._is_dark else "rgba(0, 0, 0, 100)"
			)
			self._dev_watermark.setStyleSheet(f"color: {watermark_color};")

			self._dev_watermark.resize(320, 36)
			self._dev_watermark.move(
				self._container.width() - self._dev_watermark.width() - 16,
				self._container.height() - self._dev_watermark.height() - 12,
			)
		_try_install_window_icon_filter(self)

	def _build_ui_nina(self):
		root_dir = _get_user_root_dir()
		icon_path = os.path.join(
			root_dir, "user", "splash", "nina_iseri_1_2", "1.2_Splash.png"
		)
		if os.path.exists(icon_path):
			from PySide6.QtGui import QPixmap

			original_pixmap = QPixmap(icon_path)
			if not original_pixmap.isNull():
				# For High DPI, we should NOT pre-scale the pixmap if possible, or scale it based on devicePixelRatio.
				# However, QPainter.drawPixmap with SmoothPixmapTransform is usually better than pre-scaling if we want dynamic resizing.
				# But here we are setting a fixed window size.

				# Let's keep the original high-res pixmap in memory and only resize the window logic.
				self._pixmap = original_pixmap

				# Logic to determine window size:
				# If image is very large, we define a "logical" size for the window (e.g. 860 width)
				# and let the paintEvent draw the high-res image scaled down into that rect.

				target_width = 860
				aspect_ratio = original_pixmap.height() / original_pixmap.width()
				target_height = int(target_width * aspect_ratio)

				self.resize(target_width, target_height)
			else:
				# Fallback
				self.resize(860, 480)
		else:
			self.resize(860, 480)

		# We don't use standard widgets, we paint in paintEvent

	def _apply_user_splash(self) -> bool:
		splash_dir = _resolve_user_splash_dir(self._splash_style)
		if not splash_dir or not _is_valid_splash_package(
			splash_dir, self._splash_style
		):
			return False
		module_path = os.path.join(splash_dir, "splash.py")
		try:
			module_name = f"user_splash_{self._splash_style}"
			spec = importlib.util.spec_from_file_location(module_name, module_path)
			if not spec or not spec.loader:
				return False
			module = importlib.util.module_from_spec(spec)
			spec.loader.exec_module(module)
			apply_fn = getattr(module, "apply", None)
			if not callable(apply_fn):
				return False
			apply_fn(self)
			return True
		except Exception:
			return False

	def paintEvent(self, event):
		if (
			self._splash_style == "nina_iseri_1_2"
			and not self._is_first_run
			and self._pixmap
		):
			painter = QPainter(self)
			painter.setRenderHint(QPainter.Antialiasing)
			painter.setRenderHint(QPainter.TextAntialiasing)
			painter.setRenderHint(QPainter.SmoothPixmapTransform)

			# Draw Background
			# Use drawPixmap with target rect to ensure it scales to window size
			painter.drawPixmap(self.rect(), self._pixmap)

			# Constants
			margin_left = 40
			margin_bottom = 40

			# Fonts
			splash_font_families = _get_default_font_family_stack(
				self._language, _load_bundled_font_families(_get_user_root_dir())
			)
			title_font = QFont()
			title_font.setStyleHint(QFont.SansSerif)
			if splash_font_families:
				title_font.setFamily(splash_font_families[0])
			title_font.setFamilies(splash_font_families)
			title_font.setPixelSize(36)
			title_font.setBold(True)

			sub_font = QFont()
			sub_font.setStyleHint(QFont.SansSerif)
			if splash_font_families:
				sub_font.setFamily(splash_font_families[0])
			sub_font.setFamilies(splash_font_families)
			sub_font.setPixelSize(14)

			# Calculate positions from bottom
			h = self.height()
			w = self.width()

			# Reduce width to ~65% to avoid character more aggressively
			content_width = w * 0.65

			progress_h = 6
			progress_y = h - margin_bottom - progress_h

			# Title "Luminalium"
			painter.setPen(QColor("#000000"))
			painter.setFont(title_font)
			# Calculate exact height to position tighter
			fm_title = QFontMetrics(title_font)
			title_height = fm_title.capHeight()

			# Subtitle
			painter.setFont(sub_font)
			fm_sub = QFontMetrics(sub_font)
			sub_height = fm_sub.capHeight()

			# Position calculations
			# Gap between Title baseline and Subtitle top: e.g. 8px
			# Gap between Subtitle baseline and Progress bar: e.g. 15px

			subtitle_baseline_y = progress_y - 15
			title_baseline_y = subtitle_baseline_y - sub_height - 16  # 20px gap

			# Draw Title
			brand_name_map = {
				"zh-CN": "Luminalium",
				"zh-TW": "Luminalium",
				"yue-HK": "Luminalium",
				"ja-JP": "ルマイナリウム",
				"en-US": "Luminalium",
			}
			brand_name = brand_name_map.get(self._language, "Luminalium")

			painter.setFont(title_font)
			painter.setPen(QColor("#000000"))
			painter.drawText(margin_left, title_baseline_y, brand_name)

			# Draw Subtitle
			painter.setFont(sub_font)
			painter.setPen(QColor("#888888"))
			subtitle = f"{self._version_text} // {self._code_name_en}"
			painter.drawText(margin_left, subtitle_baseline_y, subtitle)

			# Status Text (Right aligned relative to content width)
			status_text = f"{self._status_text}"
			status_rect = fm_sub.boundingRect(status_text)

			# Align status text to the end of the progress bar
			status_x = margin_left + content_width - status_rect.width()
			painter.drawText(status_x, subtitle_baseline_y, status_text)

			# Progress Bar Background
			painter.setPen(Qt.NoPen)
			painter.setBrush(QColor("#E0E0E0"))
			# Width is content_width
			painter.drawRoundedRect(
				margin_left, progress_y, content_width, progress_h, 3, 3
			)

			# Progress Bar Value
			if self._progress_value > 0:
				painter.setBrush(QColor("#404040"))
				prog_width = content_width * (self._progress_value / 100.0)
				painter.drawRoundedRect(
					margin_left, progress_y, prog_width, progress_h, 3, 3
				)

			painter.end()
		else:
			super().paintEvent(event)

	def _build_ui(self):
		if self._is_first_run:
			self._container.setFixedSize(960, 540)
			self._container.move(0, 0)

			# Center Logo (120x120)
			self._icon_label = QLabel(self._container)
			self._icon_label.setFixedSize(120, 120)
			icon_path = os.path.join(
				os.path.dirname(os.path.abspath(__file__)), "icons", "logo.svg"
			)
			if os.path.exists(icon_path):
				icon = QIcon(icon_path)
				pix = icon.pixmap(120, 120)
				self._icon_label.setPixmap(pix)

			# Center it
			cx = (960 - 120) // 2
			cy = (540 - 120) // 2
			self._icon_label.move(cx, cy)
			return

		# Redesigned based on QML spec
		# Width: 678, Height: 255
		self._container.setFixedSize(678, 255)
		self._container.move(0, 0)

		# Logo (kZHTXT_2.png equivalent) - x: 38, y: 37
		self._icon_label = QLabel(self._container)
		self._icon_label.setFixedSize(64, 64)
		icon_path = os.path.join(
			os.path.dirname(os.path.abspath(__file__)), "icons", "logo.svg"
		)
		if os.path.exists(icon_path):
			icon = QIcon(icon_path)
			pix = icon.pixmap(64, 64)
			self._icon_label.setPixmap(pix)
		self._icon_label.move(38, 37)

		brand_name_map = {
			"zh-CN": "Luminalium",
			"zh-TW": "Luminalium",
			"yue-HK": "Luminalium",
			"ja-JP": "ルマイナリウム",
			"en-US": "Luminalium",
		}
		brand_name = brand_name_map.get(self._language, "Luminalium")
		self._brand_label = QLabel(brand_name, self._container)
		splash_font_families = _get_default_font_family_stack(
			self._language, _load_bundled_font_families(_get_user_root_dir())
		)
		brand_font = QFont()
		if splash_font_families:
			brand_font.setFamily(splash_font_families[0])
		brand_font.setFamilies(splash_font_families)
		brand_font.setPixelSize(32)
		brand_font.setWeight(QFont.Black)
		self._brand_label.setFont(brand_font)
		self._brand_label.setFixedWidth(328)
		self._brand_label.setFixedHeight(32)
		self._brand_label.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
		self._brand_label.move(38, 111)

		# Version Info Container - x: 38, y: 143
		self._version_info_label = QLabel(self._container)

		ver_text = self._version_text or ""
		en_text = self._code_name_en or ""

		ver_color = "#FFFFFF" if self._is_dark else "#000000"
		en_color = (
			"rgba(255, 255, 255, 0.47)" if self._is_dark else "rgba(0, 0, 0, 0.47)"
		)

		version_font_css = _build_css_font_family_value(splash_font_families)
		html = f"""
		<div style="line-height: 20px;">
			<span style="font-family: {version_font_css}; font-size: 11px; font-weight: 500; color: {ver_color};">{ver_text}</span>
			<span style="font-family: {version_font_css}; font-size: 11px; font-weight: 300; color: {en_color}; margin-left: 2px;">{en_text}</span>
		</div>
		"""

		self._version_info_label.setText(html)
		self._version_info_label.adjustSize()
		self._version_info_label.move(38, 143)

		# Loading Spinner (Vector) - x: 38, y: 199
		spinner_color = QColor("#d9d9d9") if self._is_dark else QColor("#666666")
		self._spinner = IndeterminateSpinner(self._container, color=spinner_color)
		self._spinner.move(38, 199)
		self._spinner.start()

		# Status Text (element_2) - x: 76, y: 203
		init_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"])[
			"initializing"
		]
		self._percent_label = QLabel(f"{init_text} 0%", self._container)
		percent_font = QFont()
		percent_font.setFamilies(splash_font_families)
		percent_font.setPixelSize(15)
		percent_font.setBold(True)
		self._percent_label.setFont(percent_font)
		self._percent_label.setFixedWidth(221)
		self._percent_label.setFixedHeight(24)
		self._percent_label.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
		self._percent_label.move(76, 200)

		# Progress Bar Foreground (rectangle_31) - y: 247, h: 8, w: 678
		self._progress = QProgressBar(self._container)
		self._progress.setRange(0, 100)
		self._progress.setValue(0)
		self._progress.setTextVisible(False)
		self._progress.setFixedHeight(8)
		self._progress.setFixedWidth(678)
		self._progress.move(0, 247)

	def _apply_styles(self):
		if self._is_first_run:
			self.resize(960, 540)
			if self._is_dark:
				bg_color = "#121212"
			else:
				bg_color = "#f2f3f5"

			self._container.setStyleSheet(
				f"QFrame#splashContainer {{"
				f"background-color: {bg_color};"
				f"border: none;"
				f"border-radius: 0px;"
				f"}}"
			)
			return

		if self._is_dark:
			bg_color = "rgba(47, 47, 47, 240)"
			border_color = "rgba(255, 255, 255, 0.15)"
			brand_color = "#ffffff"
			percent_color = "#d9d9d9"
			progress_bg = "#454545"
			chunk_color = "#E1EBFF"
		else:
			bg_color = "rgba(255, 255, 255, 240)"
			border_color = "rgba(0, 0, 0, 0.08)"
			brand_color = "#000000"
			percent_color = "#666666"
			progress_bg = "#e5e5e5"
			chunk_color = "#3275F5"

		self.setFixedSize(678, 255)

		self._container.setStyleSheet(
			f"QFrame#splashContainer {{"
			f"background-color: {bg_color};"
			f"border: 1px solid {border_color};"
			"border-radius: 8px;"
			"}"
		)
		self._brand_label.setStyleSheet(f"color: {brand_color};")
		self._percent_label.setStyleSheet(f"color: {percent_color};")

		self._progress.setStyleSheet(
			"QProgressBar {"
			f"background-color: {progress_bg};"
			"border: none;"
			"border-bottom-left-radius: 8px;"
			"border-bottom-right-radius: 8px;"
			"}"
			"QProgressBar::chunk {"
			f"background-color: {chunk_color};"
			"border-bottom-left-radius: 8px;"
			"border-bottom-right-radius: 8px;"
			"}"
		)

	def refresh_theme(self):
		"""Recompute dark state and re-apply splash styles when system theme changes."""
		if not self.isVisible():
			return
		theme_val = cfg.themeMode.value
		if theme_val == Theme.AUTO:
			from ppt_assistant.core.config import _get_system_is_dark

			new_dark = _get_system_is_dark()
		else:
			new_dark = theme_val == Theme.DARK
		if new_dark == self._is_dark:
			return
		self._is_dark = new_dark
		applied_user = self._apply_user_splash()
		if not applied_user:
			if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
				self._build_ui_nina()
			else:
				self._apply_styles()

	def _center_on_screen(self):
		screen = QApplication.primaryScreen()
		if not screen:
			return
		screen_geo = screen.geometry()
		w = self.width()
		h = self.height()
		x = screen_geo.x() + (screen_geo.width() - w) // 2
		y = screen_geo.y() + (screen_geo.height() - h) // 2
		self.move(x, y)

	def show(self):
		super().show()

	def set_progress(self, value, text_key="initializing"):
		value = min(max(value, 0), 100)

		# Check if detailed splash is enabled
		if cfg.showDetailedSplash.value:
			display_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"]).get(
				text_key, text_key
			)
		else:
			# Always show "initializing" text if details are disabled
			init_key = "initializing"
			display_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"]).get(
				init_key, init_key
			)

		full_text = f"{display_text} {value}%"

		# Nina style
		if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
			self._progress_value = value
			self._status_text = full_text
			self.update()
			QApplication.processEvents()
			return

		# For first run splash (simple logo), we don't show progress
		if not hasattr(self, "_progress") or not hasattr(self, "_percent_label"):
			QApplication.processEvents()
			return

		self._progress.setValue(value)
		self._percent_label.setText(full_text)

		# Update spinner if needed, or it spins automatically
		QApplication.processEvents()

	def finish(self):
		if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
			self._progress_value = 100
			self._status_text = "100%"
			self.update()
			self.close()
			return

		if hasattr(self, "_progress"):
			self._progress.setValue(100)
		if hasattr(self, "_percent_label"):
			self._percent_label.setText("正在完成启动后操作")
		if hasattr(self, "_spinner"):
			self._spinner.stop()
		self.close()


class IndeterminateSpinner(QWidget):
	def __init__(self, parent=None, color=QColor("#d9d9d9")):
		super().__init__(parent)
		self.setFixedSize(27, 27)
		self._angle = 0
		self._color = color
		self._timer = QTimer(self)
		self._timer.timeout.connect(self._rotate)
		self._timer.start(16)  # ~60 FPS
		_try_install_window_icon_filter(self)

	def start(self):
		if not self._timer.isActive():
			self._timer.start()

	def stop(self):
		self._timer.stop()

	def _rotate(self):
		self._angle = (self._angle + 5) % 360
		self.update()

	def paintEvent(self, event):
		painter = QPainter(self)
		painter.setRenderHint(QPainter.Antialiasing)

		rect = self.rect()
		cx, cy = rect.center().x(), rect.center().y()

		# Outer ring (static)
		pen = QPen(self._color)
		pen.setWidth(3)
		painter.setPen(pen)
		painter.setBrush(Qt.NoBrush)

		# Draw ring. Adjust rect to account for pen width
		ring_radius = 10
		painter.drawEllipse(QPoint(cx, cy), ring_radius, ring_radius)

		# Inner rotating dot
		painter.save()
		painter.translate(cx, cy)
		painter.rotate(self._angle)

		# Draw small circle on the orbit
		dot_radius = 2.5
		orbit_radius = ring_radius - 3 - dot_radius + 1  # Fine tuned visual position

		painter.setPen(Qt.NoPen)
		painter.setBrush(QBrush(self._color))
		painter.drawEllipse(QPoint(0, -orbit_radius), dot_radius, dot_radius)

		painter.restore()
		painter.end()


def show_webview_dialog(
	title,
	text,
	confirm_text="确认",
	cancel_text="取消",
	is_error=False,
	hide_cancel=False,
	code=None,
	width=650,
	height=500,
):
	from ppt_assistant.ui.dialog_runtime import show_webview_dialog_in_process

	return show_webview_dialog_in_process(
		title=title,
		text=text,
		confirm_text=confirm_text,
		cancel_text=cancel_text,
		is_error=is_error,
		hide_cancel=hide_cancel,
		code=code,
		width=width,
		height=height,
	)


class CrashHandler:
	# Heartbeat file path - shared with watchdog subprocess
	_heartbeat_path = None
	_heartbeat_thread = None
	_heartbeat_running = False

	def __init__(self, app=None):
		self.app = app
		self.app_instance = None
		self._handling = False
		sys.excepthook = self.handle_exception
		import threading

		threading.excepthook = self.handle_thread_exception

	def set_app_instance(self, instance):
		self.app_instance = instance

	def _resolve_crash_action(self):
		mode = "ShowAnalyzer"
		enabled = False
		try:
			if hasattr(cfg, "crashAutoHandleMode"):
				mode = cfg.crashAutoHandleMode.value
		except Exception:
			mode = "ShowAnalyzer"
		try:
			if hasattr(cfg, "crashAutoHandleEnabled"):
				enabled = bool(cfg.crashAutoHandleEnabled.value)
		except Exception:
			enabled = False

		# Legacy fallback: if no explicit toggle exists, treat non-analyzer mode as enabled
		if not enabled and not hasattr(cfg, "crashAutoHandleEnabled"):
			if mode in ("Exit", "RestartSilent", "Toast"):
				enabled = True

		if not enabled:
			return "ShowAnalyzer"
		if mode in ("Exit", "RestartSilent", "Toast"):
			return mode
		return "RestartSilent"

	def _parse_crash_dialog_result(self, stdout: str):
		if not stdout:
			return None
		if "CRASH_DIALOG_IGNORED" in stdout:
			return "ignored"
		if "CRASH_DIALOG_EXIT" in stdout:
			return "exit"
		return None

	def _launch_crash_dialog(self, error_msg: str, wait_for_result: bool = False):
		try:
			base_dir = os.path.dirname(os.path.abspath(__file__))
			root_dir = base_dir
			main_path = os.path.join(root_dir, "main.py")
			env = os.environ.copy()
			env["CRASH_PARENT_PID"] = str(os.getpid())

			with tempfile.NamedTemporaryFile(
				mode="w", suffix=".log", delete=False, encoding="utf-8"
			) as f:
				f.write(error_msg)
				temp_path = f.name

			creationflags = (
				0x08000000 | 0x00000008
			)  # CREATE_NO_WINDOW | DETACHED_PROCESS

			if getattr(sys, "frozen", False):
				cmd = [sys.executable, "--webview-runner", "--crash-file", temp_path]
			else:
				cmd = [
					sys.executable,
					main_path,
					"--webview-runner",
					"--crash-file",
					temp_path,
				]

			if not wait_for_result:
				subprocess.Popen(
					cmd, env=env, creationflags=creationflags, close_fds=True
				)
				return None

			proc = subprocess.Popen(
				cmd,
				env=env,
				creationflags=creationflags,
				close_fds=True,
				stdin=subprocess.DEVNULL,
				stdout=subprocess.PIPE,
				stderr=subprocess.PIPE,
				text=True,
				encoding="utf-8",
				errors="replace",
			)
			try:
				stdout, _ = proc.communicate()
			except Exception:
				return None
			return self._parse_crash_dialog_result(stdout)
		except Exception as e:
			print(f"Failed to launch crash dialog: {e}", file=sys.stderr)
		return None

	def _restart_silent(self):
		try:
			base_dir = os.path.dirname(os.path.abspath(__file__))
			main_path = os.path.join(base_dir, "main.py")
			filtered_args = [
				a
				for a in sys.argv[1:]
				if a
				not in (
					"--silent",
					"--webview-runner",
					"--dialog",
					"--crash-file",
					"--memory-cleaner",
				)
			]
			if "--silent" not in filtered_args:
				filtered_args.append("--silent")

			if getattr(sys, "frozen", False):
				cmd = [sys.executable] + filtered_args
			else:
				cmd = [sys.executable, main_path] + filtered_args

			creationflags = 0x08000000 | 0x00000008
			env = os.environ.copy()
			env["LUMINALIUM_RESTART"] = "1"
			env["LUMINALIUM_RESTART_PID"] = str(os.getpid())
			_write_restart_marker()
			subprocess.Popen(cmd, env=env, creationflags=creationflags, close_fds=True)
		except Exception as e:
			print(f"Failed to restart silently: {e}", file=sys.stderr)
		try:
			if (
				self.app_instance is not None
				and hasattr(self.app_instance, "tray")
				and self.app_instance.tray
			):
				self.app_instance.tray.show_message(
					t("crash.toast.title"), t("crash.toast.body")
				)
		except Exception as e:
			print(f"Failed to show crash toast: {e}", file=sys.stderr)

	def handle_thread_exception(self, args):
		self.handle_exception(args.exc_type, args.exc_value, args.exc_traceback)

	def handle_exception(self, exc_type, exc_value, exc_traceback):
		if self._handling:
			sys.__excepthook__(exc_type, exc_value, exc_traceback)
			return
		self._handling = True
		if issubclass(exc_type, KeyboardInterrupt):
			sys.__excepthook__(exc_type, exc_value, exc_traceback)
			return

		error_msg = "".join(
			traceback.format_exception(exc_type, exc_value, exc_traceback)
		)
		print(f"CRASH DETECTED:\n{error_msg}", file=sys.stderr)

		# Write persistent crash log to disk FIRST, before any dialog/cleanup
		# that might itself fail or hang.  This ensures we always have a
		# crash record on disk even if the rest of the handler breaks.
		try:
			crash_log_dir = os.path.join(
				os.environ.get("APPDATA", tempfile.gettempdir()),
				"Luminalium",
				"crash_logs",
			)
			os.makedirs(crash_log_dir, exist_ok=True)
			crash_log_path = os.path.join(
				crash_log_dir, f"crash_{os.getpid()}_{int(time.time())}.log"
			)
			with open(crash_log_path, "w", encoding="utf-8") as f:
				f.write(f"=== CRASH LOG ===\n")
				f.write(f"PID: {os.getpid()}\n")
				f.write(f"Time: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
				f.write(f"Exception: {exc_type.__name__}\n\n")
				f.write(error_msg)
			print(f"[CrashHandler] Crash log written to {crash_log_path}", flush=True)
		except Exception as e:
			print(f"[CrashHandler] Failed to write crash log: {e}", flush=True)

		action = self._resolve_crash_action()
		if action == "ShowAnalyzer":
			result = self._launch_crash_dialog(error_msg, wait_for_result=True)
			if result == "ignored":
				self._handling = False
				return
		elif action == "RestartSilent":
			self._restart_silent()
		elif action == "Toast":
			self._show_crash_toast()

		try:
			if self.app_instance is not None:
				self.app_instance.cleanup()
		except Exception as e:
			print(f"Error during crash cleanup: {e}", file=sys.stderr)

		import time

		time.sleep(0.5)
		os._exit(1)

	# ---- Watchdog & Heartbeat ----

	def start_watchdog(self):
		"""Start heartbeat writer thread and watchdog subprocess.

		The watchdog is a *separate process* so it survives if the main
		process deadlocks or segfaults.  It uses two detection methods:

		1. ``IsHungAppWindow`` (primary) – checks if any visible window
		   belonging to the main process is not responding to messages.
		   This works even when the GIL is held by a frozen thread.

		2. Heartbeat file (secondary) – the main process writes a
		   timestamp every 2 s.  If the file goes stale the watchdog
		   treats it as a freeze.  This catches non-GUI freezes where
		   the event loop is fine but the app is logically stuck.
		"""
		import threading

		# 1. Set up heartbeat file
		tmp_dir = tempfile.gettempdir()
		self._heartbeat_path = os.path.join(
			tmp_dir, f"luminalium_heartbeat_{os.getpid()}.txt"
		)
		self._heartbeat_running = True

		# Write initial heartbeat immediately
		try:
			with open(self._heartbeat_path, "w", encoding="utf-8") as f:
				f.write(str(time.time()))
		except Exception:
			pass

		# 2. Start heartbeat writer thread (daemon, plain Python thread)
		def _heartbeat_loop():
			while self._heartbeat_running:
				try:
					with open(self._heartbeat_path, "w", encoding="utf-8") as f:
						f.write(str(time.time()))
				except Exception:
					pass
				time.sleep(2)

		self._heartbeat_thread = threading.Thread(
			target=_heartbeat_loop, daemon=True, name="CrashHandlerHeartbeat"
		)
		self._heartbeat_thread.start()

		# 3. Launch watchdog subprocess
		self._launch_watchdog_subprocess()

	def stop_watchdog(self):
		"""Stop heartbeat and clean up."""
		self._heartbeat_running = False
		if self._heartbeat_path:
			try:
				os.remove(self._heartbeat_path)
			except Exception:
				pass
			self._heartbeat_path = None

	def _launch_watchdog_subprocess(self):
		"""Launch a tiny watchdog process that monitors the main process."""
		try:
			base_dir = os.path.dirname(os.path.abspath(__file__))
			main_path = os.path.join(base_dir, "main.py")

			env = os.environ.copy()
			env["LUMINALIUM_WATCHDOG_FOR"] = str(os.getpid())
			env["LUMINALIUM_HEARTBEAT_PATH"] = self._heartbeat_path or ""

			if sys.platform == "win32":
				creationflags = (
					0x08000000 | 0x00000008
				)  # CREATE_NO_WINDOW | DETACHED_PROCESS
			else:
				creationflags = 0

			if getattr(sys, "frozen", False):
				cmd = [sys.executable, "--watchdog"]
			else:
				cmd = [sys.executable, main_path, "--watchdog"]

			subprocess.Popen(cmd, env=env, creationflags=creationflags, close_fds=True)
			print("[Watchdog] Watchdog subprocess launched", flush=True)
		except Exception as e:
			print(f"[Watchdog] Failed to launch watchdog subprocess: {e}", flush=True)


def _run_watchdog_process():
	"""Entry point for the watchdog subprocess.

	Runs in a completely independent process.  Detects freezes via:
	  - IsHungAppWindow (primary, GIL-independent)
	  - Heartbeat file staleness (secondary)
	"""
	pid_str = os.environ.get("LUMINALIUM_WATCHDOG_FOR", "")
	heartbeat_path = os.environ.get("LUMINALIUM_HEARTBEAT_PATH", "")

	if not pid_str:
		return

	main_pid = int(pid_str)
	HUNG_TIMEOUT = 6  # consecutive hung checks before declaring freeze
	HEARTBEAT_TIMEOUT = 75  # seconds without heartbeat = frozen
	CHECK_INTERVAL = 5  # check every 5 seconds

	print(f"[Watchdog] Monitoring PID {main_pid}", flush=True)

	_is_windows = sys.platform == "win32"

	# --- Windows API helpers (only on Windows) ---
	if _is_windows:
		import ctypes
		from ctypes import wintypes

		kernel32 = ctypes.windll.kernel32
		user32 = ctypes.windll.user32
	else:
		kernel32 = None
		user32 = None
		wintypes = None  # type: ignore[assignment]

	def _is_process_alive(pid):
		if _is_windows:
			handle = kernel32.OpenProcess(0x100000, False, pid)  # SYNCHRONIZE
			if handle:
				kernel32.CloseHandle(handle)
				return True
			return False
		# POSIX: signal 0 probes existence without delivering anything.
		try:
			os.kill(pid, 0)
			return True
		except ProcessLookupError:
			return False
		except PermissionError:
			return True

	def _check_hung_windows(pid):
		"""Use IsHungAppWindow to detect if any visible window of the
		process is not responding.  This is GIL-independent – it checks
		the Windows message queue state from outside the process.

		On non-Windows platforms there is no equivalent API, so we report
		(found=True, hung=False) and rely solely on heartbeat staleness.
		"""
		if not _is_windows:
			return True, False

		found = False
		hung = False

		WNDENUMPROC = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

		def _enum_cb(hwnd, _):
			nonlocal found, hung
			wnd_pid = wintypes.DWORD()
			user32.GetWindowThreadProcessId(hwnd, ctypes.byref(wnd_pid))
			if wnd_pid.value == pid and user32.IsWindowVisible(hwnd):
				found = True
				if user32.IsHungAppWindow(hwnd):
					hung = True
				return False  # stop
			return True  # continue

		user32.EnumWindows(WNDENUMPROC(_enum_cb), 0)
		return found, hung

	def _check_heartbeat():
		"""Check heartbeat file staleness."""
		if not heartbeat_path:
			return False, 0.0
		try:
			with open(heartbeat_path, "r", encoding="utf-8") as f:
				last_beat = float(f.read().strip())
			elapsed = time.time() - last_beat
			return elapsed >= HEARTBEAT_TIMEOUT, elapsed
		except (FileNotFoundError, ValueError):
			return False, 0.0

	def _dump_thread_info(pid):
		"""Try to dump thread info of the frozen process."""
		try:
			import psutil

			proc = psutil.Process(pid)
			threads_info = []
			for t in proc.threads():
				threads_info.append(f"  tid={t.id}")
			return f"Process threads ({len(threads_info)}):\n" + "\n".join(threads_info)
		except Exception as e:
			return f"Failed to enumerate threads: {e}"

	def _handle_freeze(reason, detail=""):
		"""Handle a detected freeze: log, notify, kill."""
		print(
			f"[Watchdog] FREEZE DETECTED: {reason} (PID {main_pid})",
			flush=True,
		)

		stack_info = _dump_thread_info(main_pid)
		error_msg = (
			f"Application freeze detected\n\n"
			f"Reason: {reason}\n"
			f"Main PID: {main_pid}\n"
			f"{detail}\n\n"
			f"{stack_info}"
		)

		# Write crash log
		try:
			crash_log_dir = os.path.join(
				os.environ.get("APPDATA", tempfile.gettempdir()),
				"Luminalium",
				"crash_logs",
			)
			os.makedirs(crash_log_dir, exist_ok=True)
			crash_log_path = os.path.join(
				crash_log_dir, f"freeze_{main_pid}_{int(time.time())}.log"
			)
			with open(crash_log_path, "w", encoding="utf-8") as f:
				f.write(error_msg)
			print(f"[Watchdog] Freeze log written to {crash_log_path}", flush=True)
		except Exception as e:
			print(f"[Watchdog] Failed to write freeze log: {e}", flush=True)

		# Launch crash dialog
		try:
			base_dir = os.path.dirname(os.path.abspath(__file__))
			main_path = os.path.join(base_dir, "main.py")

			with tempfile.NamedTemporaryFile(
				mode="w", suffix=".log", delete=False, encoding="utf-8"
			) as f:
				f.write(error_msg)
				temp_path = f.name

			env = os.environ.copy()
			env["CRASH_PARENT_PID"] = str(main_pid)
			if _is_windows:
				creationflags = 0x08000000 | 0x00000008
			else:
				creationflags = 0

			if getattr(sys, "frozen", False):
				cmd = [sys.executable, "--webview-runner", "--crash-file", temp_path]
			else:
				cmd = [
					sys.executable,
					main_path,
					"--webview-runner",
					"--crash-file",
					temp_path,
				]

			subprocess.Popen(cmd, env=env, creationflags=creationflags, close_fds=True)
		except Exception as e:
			print(f"[Watchdog] Failed to launch crash dialog: {e}", flush=True)

		# Kill the frozen process
		try:
			import psutil

			proc = psutil.Process(main_pid)
			proc.kill()
			print(f"[Watchdog] Killed frozen process {main_pid}", flush=True)
		except Exception:
			if _is_windows:
				try:
					handle = kernel32.OpenProcess(
						1, False, main_pid
					)  # PROCESS_TERMINATE
					if handle:
						kernel32.TerminateProcess(handle, 1)
						kernel32.CloseHandle(handle)
						print(
							f"[Watchdog] Killed frozen process {main_pid} via WinAPI",
							flush=True,
						)
				except Exception as e2:
					print(f"[Watchdog] Failed to kill frozen process: {e2}", flush=True)
			else:
				try:
					os.kill(main_pid, 9)  # SIGKILL
					print(
						f"[Watchdog] Killed frozen process {main_pid} via SIGKILL",
						flush=True,
					)
				except Exception as e2:
					print(f"[Watchdog] Failed to kill frozen process: {e2}", flush=True)

		# Clean up
		if heartbeat_path:
			try:
				os.remove(heartbeat_path)
			except Exception:
				pass

	# --- Main watchdog loop ---
	hung_count = 0

	while True:
		time.sleep(CHECK_INTERVAL)

		# If main process exited, clean up and exit
		if not _is_process_alive(main_pid):
			if heartbeat_path:
				try:
					os.remove(heartbeat_path)
				except Exception:
					pass
			print("[Watchdog] Main process exited, stopping watchdog", flush=True)
			break

		# Check 1: IsHungAppWindow (GIL-independent)
		found_window, is_hung = _check_hung_windows(main_pid)
		if found_window and is_hung:
			hung_count += 1
			if hung_count >= HUNG_TIMEOUT:
				_handle_freeze("Window not responding (IsHungAppWindow)")
				break
		else:
			hung_count = 0

		# Check 2: Heartbeat staleness (secondary)
		heartbeat_stale, elapsed = _check_heartbeat()
		if heartbeat_stale:
			_handle_freeze(f"Heartbeat stale for {elapsed:.1f}s")
			break


if __name__ == "__main__" and "--watchdog" in sys.argv:
	_run_watchdog_process()
	sys.exit(0)


def _create_global_mutex():
	if sys.platform == "win32":
		import ctypes

		global _LUMINALIUM_MUTEX
		kernel32 = ctypes.windll.kernel32
		_LUMINALIUM_MUTEX = kernel32.CreateMutexW(
			None, False, "Global\\Luminalium_Mutex"
		)
		if not _LUMINALIUM_MUTEX:
			print("[Main] Failed to create global mutex", flush=True)


def _handle_multi_instance(app: QApplication):
	if str(os.environ.get("LUMINALIUM_DISABLE_MULTI_INSTANCE", "")).strip().lower() in (
		"1",
		"true",
		"yes",
		"on",
	):
		print(
			"[Main] Multi-instance check disabled by LUMINALIUM_DISABLE_MULTI_INSTANCE.",
			flush=True,
		)
		return

	try:
		import psutil
	except ImportError:
		return

	restart_flag = os.environ.pop("LUMINALIUM_RESTART", None)
	restart_marker = _consume_restart_marker()

	current_pid = os.getpid()
	parent_pid = os.getppid()
	current_entry = os.path.abspath(
		sys.executable if getattr(sys, "frozen", False) else __file__
	)
	pids = []
	for p in psutil.process_iter(["pid", "cmdline"]):
		try:
			pid = p.info.get("pid")
			if pid in (None, 0, current_pid, parent_pid):
				continue
			cmd = p.info.get("cmdline") or []

			if "--webview-runner" in cmd:
				continue
			if "--memory-cleaner" in cmd:
				continue
			if "--watchdog" in cmd:
				continue
			if cmd:
				launcher = os.path.basename(str(cmd[0])).lower()
				if launcher in ("uv", "uv.exe") and "run" in cmd:
					continue
				# Skip if parent process is uv (spawned by uv run)
				try:
					parent = psutil.Process(pid).parent()
					if parent and os.path.basename(parent.name()).lower() in (
						"uv",
						"uv.exe",
					):
						continue
				except (psutil.NoSuchProcess, psutil.AccessDenied, OSError):
					pass

			try:
				proc_cwd = p.cwd()
			except (psutil.NoSuchProcess, psutil.AccessDenied, OSError):
				proc_cwd = None

			for part in cmd:
				if not isinstance(part, str) or not part:
					continue
				if part.startswith("-"):
					continue
				if os.path.isabs(part):
					normalized = os.path.abspath(part)
				elif proc_cwd:
					normalized = os.path.abspath(os.path.join(proc_cwd, part))
				else:
					continue
				if normalized == current_entry:
					pids.append(p.info.get("pid"))
					break
		except (psutil.NoSuchProcess, psutil.AccessDenied, OSError):
			continue

	if not pids:
		return

	# 过滤掉遍历过程中已经死亡的进程
	pids = [pid for pid in pids if psutil.pid_exists(pid)]

	print(f"[Main] Existing Luminalium instance candidates: {pids}", flush=True)

	if restart_flag or restart_marker:
		try:
			deadline = time.time() + 1.2
			alive = list(pids)
			while time.time() < deadline:
				alive = [pid for pid in alive if psutil.pid_exists(pid)]
				if not alive:
					return
				time.sleep(0.05)
			for pid in alive:
				try:
					p_obj = psutil.Process(pid)
					p_obj.kill()
				except Exception:
					pass
			return
		finally:
			os.environ.pop("LUMINALIUM_RESTART_PID", None)

	if sys.platform.startswith("linux"):
		print(
			"[Main] Existing instance detected on Linux; verifying if process is actually alive...",
			flush=True,
		)
		# Verify the process is actually our instance, not a PID reuse
		alive_verified = []
		for pid in pids:
			try:
				proc = psutil.Process(pid)
				# Check if it's actually python running main.py
				cmdline = proc.cmdline()
				if any("main.py" in arg for arg in cmdline):
					alive_verified.append(pid)
			except (psutil.NoSuchProcess, psutil.AccessDenied):
				pass

		if not alive_verified:
			print(
				"[Main] No verified Luminalium instances found, continuing startup.",
				flush=True,
			)
			return

		print(
			f"[Main] Verified instances: {alive_verified}. Continuing without multi-instance dialog.",
			flush=True,
		)
		return

	lang = _get_current_language()
	_WINDOW_TITLES = {
		"zh-CN": "荧素万演已在运行",
		"zh-TW": "Luminalium 已在執行",
		"yue-HK": "Luminalium 喺度跑緊",
		"ja-JP": "ルマイナリウムが実行中です",
		"en-US": "Luminalium is already running",
		"ug-CN": "Luminalium ئىجرا قىلىنىۋاتىدۇ",
	}
	window_title = _WINDOW_TITLES.get(lang, _WINDOW_TITLES["zh-CN"])

	proc = show_webview_dialog(
		title=window_title, text="", code="multi_instance", width=738, height=577
	)
	stdout, _ = proc.communicate()

	if 'DIALOG_VALUE:"RESTART_OLD"' in stdout:
		for pid in pids:
			try:
				p_obj = psutil.Process(pid)
				p_obj.kill()
			except Exception:
				pass
		time.sleep(0.5)
		return
	elif 'DIALOG_VALUE:"CONTINUE_NEW"' in stdout:
		return
	else:
		sys.exit(0)


def _t(key):
	return key  # Simple fallback if i18n is missing


def _init_trace(msg: str):
	"""Write initialization trace to temp file for debugging freezes."""
	try:
		import datetime

		trace_path = os.path.join(tempfile.gettempdir(), "lumi_init_trace.log")
		with open(trace_path, "a", encoding="utf-8") as f:
			ts = datetime.datetime.now().strftime("%H:%M:%S.%f")[:-3]
			f.write(f"[{ts}] {msg}\n")
			f.flush()
	except Exception:
		pass


class PPTAssistantApp:
	def __init__(self, app: QApplication, splash=None):
		self.app = app
		self.app.setQuitOnLastWindowClosed(False)
		self._splash = splash
		self.tray = None
		self._timer_manager = TimerManager()
		self._focus_watcher = _create_focus_watcher(self.app)
		self._focus_watcher.start()
		self._last_timer_notify_at = 0.0
		self._reloading_overlay = False

		# System theme watcher - detect OS-level dark/light changes and refresh all windows
		self._theme_watcher = SystemThemeWatcherManager(
			apply_theme_callback=_apply_resolved_theme_color,
			get_config_value=lambda: cfg.themeMode.value,
			on_theme_changed_callback=self._handle_system_theme_refresh,
		)
		self._theme_watcher.start()

		self._slideshow_running = False
		self._last_slideshow_rect = None
		self._last_slideshow_screen = None
		self._reload_timer = QTimer()
		self._reload_timer.setSingleShot(True)
		self._reload_timer.setInterval(150)
		self._reload_timer.timeout.connect(self._reload_overlay)
		self._onboarding_wait_timer = None
		self._onboarding_restart_started = False
		self._resource_monitor = None
		self._classisland_monitor = None
		self._memory_cleaner_process = None
		self._gc_timer = None
		self._open_settings_after_startup = False
		self._pending_protocol_url = None
		self._pending_overlay_focus = None
		self._overlay_focus_timer = QTimer(self.app)
		self._overlay_focus_timer.setSingleShot(True)
		self._overlay_focus_timer.timeout.connect(self._apply_pending_overlay_focus)

		# Flag watcher for external settings requests
		self._flag_timer = QTimer(self.app)
		self._flag_timer.timeout.connect(self._check_flags)
		self._flag_timer.start(5000)

		# Start async initialization
		self._init_gen = self._init_steps()
		QTimer.singleShot(0, self._perform_init_step)

	def _check_flags(self):
		try:
			app_dir = (
				Path(sys.executable).parent
				if getattr(sys, "frozen", False)
				else Path(__file__).parent
			)
			flag_file = app_dir / "_internal" / ".open_settings"
			if flag_file.exists():
				flag_file.unlink()
				if hasattr(self, "settings_plugin"):
					self.settings_plugin.execute()
					QTimer.singleShot(500, lambda: self._switch_to_update_tab())
		except Exception:
			pass
		try:
			app_dir = (
				Path(sys.executable).parent
				if getattr(sys, "frozen", False)
				else Path(__file__).parent
			)
			protocol_flag = app_dir / "_internal" / ".protocol_url"
			if protocol_flag.exists():
				url = protocol_flag.read_text(encoding="utf-8").strip()
				protocol_flag.unlink()
				if url:
					self.handle_protocol_url(url)
		except Exception:
			pass

	def handle_protocol_url(self, url: str):
		route = parse_luminalium_url(url)
		if route is None:
			print(f"[Protocol] Unrecognized URL: {url}")
			return
		action = route.get("action")
		if action == "settings":
			page = route.get("page", "main")
			if hasattr(self, "settings_plugin"):
				self.settings_plugin.execute()
				QTimer.singleShot(500, lambda: self._navigate_settings_page(page))
		elif action == "timer":
			command = str(route.get("command") or "open").strip().lower()
			if command == "restart":
				self._restart_timer_from_notification()
			elif command == "stop_all":
				self._stop_all_timers_from_notification()
			elif hasattr(self, "timer_plugin"):
				self.timer_plugin.execute()
		elif action == "board":
			if hasattr(self, "board_plugin"):
				print(f"[Protocol] Executing board_plugin.execute()", flush=True)
				try:
					self.board_plugin.execute()
					print(f"[Protocol] board_plugin.execute() returned OK", flush=True)
				except Exception as e:
					print(f"[Protocol] board_plugin.execute() raised: {e}", flush=True)
					import traceback

					traceback.print_exc()

	def _restart_timer_from_notification(self):
		seconds = 0
		try:
			seconds = int(max(0, self._timer_manager.total_seconds))
		except Exception:
			seconds = 0
		if seconds <= 0:
			return
		try:
			self._timer_manager.start(seconds)
		except Exception as e:
			print(f"[Protocol] Failed to restart timer from notification: {e}")
			return
		if hasattr(self, "timer_plugin"):
			try:
				self.timer_plugin.execute()
			except Exception:
				pass

	def _stop_all_timers_from_notification(self):
		try:
			self._timer_manager.stop()
		except Exception as e:
			print(f"[Protocol] Failed to stop timer from notification: {e}")
		if hasattr(self, "timer_plugin"):
			try:
				self.timer_plugin.terminate()
			except Exception:
				pass

	def _open_settings_about(self):
		"""Open settings window and navigate to the About page."""
		try:
			self.settings_plugin.execute()
			QTimer.singleShot(600, lambda: self._navigate_settings_page("about"))
		except Exception:
			pass

	def _navigate_settings_page(self, page: str, retries=8):
		try:
			if (
				hasattr(self.settings_plugin, "_window")
				and self.settings_plugin._window
			):
				self.settings_plugin._window.page().runJavaScript(
					f"if(typeof navigateToSection === 'function') {{ navigateToSection('{page}'); 'OK'; }} else {{ 'WAIT'; }}",
					lambda result: (
						QTimer.singleShot(
							400, lambda: self._navigate_settings_page(page, retries - 1)
						)
						if result == "WAIT" and retries > 0
						else None
					),
				)
		except Exception:
			pass

	def _switch_to_update_tab(self, retries=5):
		try:
			if (
				hasattr(self.settings_plugin, "_window")
				and self.settings_plugin._window
			):
				self.settings_plugin._window.page().runJavaScript(
					"if(typeof showUpdate === 'function') { showUpdate(); 'OK'; } else { 'WAIT'; }",
					lambda result: (
						QTimer.singleShot(
							500, lambda: self._switch_to_update_tab(retries - 1)
						)
						if result == "WAIT" and retries > 0
						else None
					),
				)
		except Exception:
			pass

	def _init_steps(self):
		_init_trace("_init_steps: START")
		# Step 1: Basic Config
		yield 10, "loading_config"
		_init_trace("_init_steps: step 10 - applying theme")
		reload_cfg()  # Load config from JSON first, otherwise cfg uses defaults!
		_apply_theme_and_color(cfg.themeMode.value)
		_init_trace("_init_steps: theme applied")

		# Step 2: Fonts (loading already done before splash)
		yield 20, "loading_fonts"
		_init_trace("_init_steps: step 20 - loading fonts")
		self._current_language = _get_current_language()
		data = _load_settings_json()
		profiles = (data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
		lang_profile = profiles.get(self._current_language, {}) or {}
		qt_font = lang_profile.get("qt", "")
		overlay_font = lang_profile.get("overlay", "") or qt_font
		qt_weight = _get_font_weight_from_settings(data, self._current_language, "qt")
		self._current_qt_font = qt_font.strip() if isinstance(qt_font, str) else ""
		self._current_overlay_font = (
			overlay_font.strip() if isinstance(overlay_font, str) else ""
		)
		self._current_qt_font_weight = qt_weight
		self._overlay_rebuild_at = (data.get("Overlay", {}) or {}).get(
			"RecreateOverlayAt"
		)

		self._settings_mtime = (
			os.path.getmtime(SETTINGS_PATH) if os.path.exists(SETTINGS_PATH) else 0
		)
		self._settings_timer = QTimer()
		self._settings_timer.setInterval(200)
		self._settings_timer.timeout.connect(self._check_settings_changed)
		self._settings_timer.start()

		self.app.aboutToQuit.connect(self.cleanup)

		# Step 3: Monitor (Non-UI logic)
		yield 30, "init_monitor"
		_init_trace("_init_steps: step 30 - creating PPTMonitor")
		self.monitor = PPTMonitor()
		_init_trace("_init_steps: PPTMonitor created")

		# Step 3.5: Preload WebEngine + settings page (block until page fully loaded)
		yield 35, "prepare_ui_env"
		_init_trace("_init_steps: step 35 - warming up WebEngine + settings page")
		try:
			import plugins.webview_runner as _wv_mod
			from plugins.webview_runner import _warmup_webengine

			_warmup_webengine(retain_placeholder=True)

			# Create settings plugin early and preload settings.html
			if not hasattr(self, "settings_plugin") or self.settings_plugin is None:
				from plugins.builtins.settings.plugin import SettingsPlugin

				self.settings_plugin = SettingsPlugin()
				self.settings_plugin.set_context(self)

			sp = self.settings_plugin
			sp.prewarm(shell=True)
			sp.prewarm_load_content()

			# Wait for the settings page to finish loading
			_ui_env_loop = QEventLoop()
			_ui_env_deadline = 15000  # ms safety cap
			_ui_env_timeout = QTimer()
			_ui_env_timeout.setSingleShot(True)
			_ui_env_timeout.timeout.connect(_ui_env_loop.quit)
			_ui_env_poll = QTimer()
			_ui_env_poll.setInterval(100)
			_ui_env_loaded = {"done": False}

			def _check_page_loaded():
				w = getattr(sp, "_window", None)
				if w is None:
					return
				# Check if the page has finished loading via the window's load state
				page = None
				try:
					page = w.page()
				except Exception:
					page = None
				if page is None:
					return

				# Use runJavaScript to check document.readyState
				def _on_ready(result):
					if isinstance(result, str) and result == "complete":
						_ui_env_loaded["done"] = True
						_ui_env_poll.stop()
						_ui_env_loop.quit()

				try:
					page.runJavaScript("document.readyState", _on_ready)
				except Exception:
					pass

			_ui_env_poll.timeout.connect(_check_page_loaded)
			_ui_env_timeout.start(_ui_env_deadline)
			_ui_env_poll.start()
			_ui_env_loop.exec()
			_ui_env_poll.stop()
			_ui_env_timeout.stop()
			_init_trace(
				"_init_steps: settings page loaded"
				if _ui_env_loaded["done"]
				else "_init_steps: settings page load timeout"
			)
		except Exception as e:
			print(f"[Main] UI env preload skipped: {e}", flush=True)
			_init_trace(f"_init_steps: UI env preload error - {e}")

		# Step 4: Overlay (UI creation - expensive)
		yield 40, "init_ui"
		_init_trace("_init_steps: step 40 - creating overlay")
		# Yield to event loop BEFORE creating heavy UI to prevent freeze
		# We can split Overlay creation if needed, but yielding before is key
		pass

		print("[Main] Creating overlay window...", flush=True)
		self.overlay = create_overlay_window()
		print(
			f"[Main] Overlay window created: {type(self.overlay).__name__}", flush=True
		)
		_init_trace(f"_init_steps: overlay created ({type(self.overlay).__name__})")

		# Step 5: Plugins (IO/Process - expensive)
		yield 60, "loading_plugins"
		_init_trace("_init_steps: step 60 - loading plugins")
		self._load_plugins()

		# Wait for plugins to finish loading (async via QTimer.singleShot)
		_init_trace("_init_steps: waiting for plugins to load...")
		while self._plugin_index < len(self._plugin_paths):
			QCoreApplication.processEvents()
			import time

			time.sleep(0.01)
		_init_trace("_init_steps: all builtin plugins loaded")

		_init_trace("_init_steps: plugins loaded")
		try:
			if not hasattr(self, "onboarding_plugin") or self.onboarding_plugin is None:
				from plugins.builtins.onboarding.plugin import OnboardingPlugin

				plugin = OnboardingPlugin()
				plugin.set_context(self)
				self.onboarding_plugin = plugin
			if FIRST_RUN:
				p = self.onboarding_plugin
				p.execute(preview=False)
				if self._splash:
					QTimer.singleShot(200, self._splash.hide)
				self._start_onboarding_wait_loop()
				return
		except Exception:
			pass

		if hasattr(self, "settings_plugin") and self.settings_plugin is not None:
			# Settings page already preloaded during cold-start (step 35)
			pass
		# Timer plugin prewarming disabled to save memory (~50-100MB per hidden WebEngine window)
		# It will be created on-demand when the user opens the timer

		self._open_settings_after_startup = (
			self._consume_open_settings_pending_flag()
			or self._open_settings_after_startup
		)
		yield 80, "init_tray"
		_init_trace("_init_steps: step 80 - creating tray")
		print("[Main] Initializing tray...", flush=True)
		if _should_enable_system_tray():
			self.tray = SystemTray()
		else:
			print(
				"[Main] System tray disabled or unavailable. Set LUMINALIUM_ENABLE_TRAY=1 to force-enable it."
			)
		print("[Main] Tray initialization finished.", flush=True)
		_init_trace("_init_steps: tray created")

		# Step 7: Finalize connections
		yield 85, "finalizing"
		_init_trace("_init_steps: step 85 - binding overlay")
		print("[Main] Binding overlay to monitor...", flush=True)
		self.overlay.set_monitor(self.monitor)
		if hasattr(self.overlay, "set_timer_manager"):
			self.overlay.set_timer_manager(self._timer_manager)
		print("[Main] Overlay bound to monitor.", flush=True)
		_init_trace("_init_steps: overlay bound")

		yield 90, "finalizing"
		_init_trace("_init_steps: step 90 - connecting signals")
		print("[Main] Connecting app signals...", flush=True)
		self._connect_signals()
		print("[Main] App signals connected.", flush=True)
		_init_trace("_init_steps: signals connected")

		yield 95, "finalizing"
		_init_trace("_init_steps: step 95 - starting services")
		print("[Main] Starting PPT monitor...", flush=True)
		self.monitor.start_monitoring()
		_init_trace("_init_steps: monitor started")
		print(
			f"[Main] PPT monitor start requested. compatibilityMode={cfg.compatibilityMode.value}",
			flush=True,
		)
		self._start_resource_monitor()
		_init_trace("_init_steps: resource monitor started")
		self._start_classisland_monitor()
		_init_trace("_init_steps: classisland monitor started")
		if not self._start_memory_cleaner():
			self._setup_gc_timer()
		_init_trace("_init_steps: memory cleaner / GC done")

		# Start Update Service
		try:
			from ppt_assistant.core.update_service import start_update_server

			start_update_server(28423)
			print("[Main] Update service started on port 28423", flush=True)
		except Exception as e:
			print(f"[Main] Failed to start update service: {e}", flush=True)
		_init_trace("_init_steps: update service done")

		if cfg.compatibilityMode.value:
			print("[APP] Showing overlay in compatibility mode")
			self.overlay.show()
			_init_trace("_init_steps: overlay shown (compat mode)")

		if self._splash is not None:
			print("[Main] Finishing splash...", flush=True)
			self._splash.finish()
			print("[Main] Splash finished.", flush=True)
		_init_trace("_init_steps: splash finished")

		if getattr(self, "_open_settings_after_startup", False) and hasattr(
			self, "settings_plugin"
		):
			QTimer.singleShot(200, self.settings_plugin.execute)
			protocol_url = getattr(self, "_pending_protocol_url", None)
			if protocol_url:
				route = parse_luminalium_url(protocol_url)
				if route and route.get("page") == "update":
					QTimer.singleShot(800, lambda: self._switch_to_update_tab())

		if getattr(self, "_pending_protocol_url", None):
			url = self._pending_protocol_url
			QTimer.singleShot(600, lambda: self.handle_protocol_url(url))
		_init_trace("_init_steps: DONE")

	def _perform_init_step(self):
		try:
			_init_trace(f"_perform_init_step: calling next()")
			progress, text = next(self._init_gen)
			_init_trace(f"_perform_init_step: got ({progress}, {text})")
			self.update_splash(progress, text)
			_init_trace(f"_perform_init_step: splash updated")
			# Schedule next step immediately but allow event loop to breathe
			QTimer.singleShot(0, self._perform_init_step)
		except StopIteration:
			_init_trace("_perform_init_step: StopIteration - init complete")
			pass  # Done
		except Exception as e:
			print(f"Initialization error: {e}")
			_init_trace(f"_perform_init_step: ERROR - {e}")
			sys.exit(1)

	def _start_onboarding_wait_loop(self):
		if self._onboarding_wait_timer is not None:
			self._onboarding_wait_timer.stop()
			self._onboarding_wait_timer.deleteLater()
		self._onboarding_wait_timer = QTimer(self.app)
		self._onboarding_wait_timer.setInterval(100)
		self._onboarding_wait_timer.timeout.connect(self._check_onboarding_closed)
		self._onboarding_wait_timer.start()

	def _check_onboarding_closed(self):
		plugin = getattr(self, "onboarding_plugin", None)
		handle = getattr(plugin, "process", None) if plugin is not None else None

		# Wait at least 3 seconds before checking to allow window to fully initialize
		if not hasattr(self, "_onboarding_start_time"):
			self._onboarding_start_time = time.time()
			return

		elapsed = time.time() - self._onboarding_start_time
		if elapsed < 3.0:  # Minimum 3 seconds before checking
			return

		try:
			finished = handle is None or handle.poll() is not None
		except Exception:
			finished = True
		if not finished:
			return
		if self._onboarding_wait_timer is not None:
			self._onboarding_wait_timer.stop()
			self._onboarding_wait_timer.deleteLater()
			self._onboarding_wait_timer = None
		if self._onboarding_restart_started:
			return
		self._onboarding_restart_started = True

		should_quit = False
		try:
			if os.path.exists(SETTINGS_PATH):
				with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
					temp_data = json.load(f)
				if temp_data.get("_quit_pending"):
					should_quit = True
					del temp_data["_quit_pending"]
					with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
						json.dump(temp_data, f, indent=4, ensure_ascii=False)
		except Exception:
			pass

		if should_quit:
			self._prepare_shutdown(restarting=False)
			self.app.quit()
			return

		# Give the onboarding process a short buffer to finish flushing settings.json.
		def _restart_after_onboarding_close():
			try:
				reload_cfg()
			except Exception:
				pass
			try:
				if os.path.exists(SETTINGS_PATH):
					self._settings_mtime = os.path.getmtime(SETTINGS_PATH)
			except Exception:
				pass
			if not os.path.exists(SETTINGS_PATH):
				try:
					os.makedirs(os.path.dirname(SETTINGS_PATH), exist_ok=True)
					with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
						json.dump({}, f)
				except Exception:
					pass
			self.restart()

		QTimer.singleShot(220, _restart_after_onboarding_close)

	def _load_plugins(self):
		"""Dynamic plugin loading from builtins and external directory."""
		self.plugins = []

		builtin_plugins = [
			"plugins.builtins.settings.plugin.SettingsPlugin",
			"plugins.builtins.onboarding.plugin.OnboardingPlugin",
			"plugins.builtins.board.plugin.BoardPlugin",
			"plugins.builtins.timer.plugin.TimerPlugin",
			"plugins.builtins.spotlight.plugin.SpotlightPlugin",
			"plugins.builtins.app_launcher.plugin.AppLauncherPlugin",
			"plugins.builtins.logs.plugin.LogsPlugin",
		]

		self._plugin_paths = builtin_plugins
		self._plugin_index = 0
		self._load_next_builtin_plugin()

	def _load_next_builtin_plugin(self):
		if self._plugin_index >= len(self._plugin_paths):
			self._load_external_plugins()
			return
		p_path = self._plugin_paths[self._plugin_index]
		self._plugin_index += 1
		try:
			mod_name, cls_name = p_path.rsplit(".", 1)
			mod = importlib.import_module(mod_name)
			cls = getattr(mod, cls_name)
			plugin = cls()
			plugin.set_context(self)
			self.plugins.append(plugin)

			if cls_name == "SettingsPlugin":
				if getattr(self, "settings_plugin", None) is not None:
					# Already created during cold-start preload; just register it
					self.plugins.append(self.settings_plugin)
					QTimer.singleShot(0, self._load_next_builtin_plugin)
					return
				self.settings_plugin = plugin
			elif cls_name == "OnboardingPlugin":
				self.onboarding_plugin = plugin
			elif cls_name == "BoardPlugin":
				self.board_plugin = plugin
			elif cls_name == "TimerPlugin":
				self.timer_plugin = plugin
			elif cls_name == "SpotlightPlugin":
				self.spotlight_plugin = plugin
			elif cls_name == "LogsPlugin":
				self.logs_plugin = plugin
		except Exception as e:
			print(f"Failed to load builtin plugin {p_path}: {e}")
		QTimer.singleShot(0, self._load_next_builtin_plugin)

	def _load_external_plugins(self):
		# 2. Load External Plugins from PLUGINS_DIR
		if not os.path.exists(PLUGINS_DIR):
			return
		for item in os.listdir(PLUGINS_DIR):
			p_dir = os.path.join(PLUGINS_DIR, item)
			if not os.path.isdir(p_dir):
				continue

			# Check for manifest.json
			manifest_path = os.path.join(p_dir, "manifest.json")
			if not os.path.exists(manifest_path):
				continue

			try:
				with open(manifest_path, "r", encoding="utf-8-sig") as f:
					manifest = json.load(f)

				entry_point = manifest.get("entry")
				if not entry_point:
					continue

				if PLUGINS_DIR not in sys.path:
					sys.path.insert(0, PLUGINS_DIR)

				mod_name, cls_name = entry_point.rsplit(".", 1)
				spec = importlib.util.spec_from_file_location(
					f"external_plugin_{item}", os.path.join(p_dir, mod_name + ".py")
				)
				mod = importlib.util.module_from_spec(spec)
				spec.loader.exec_module(mod)

				cls = getattr(mod, cls_name)
				plugin = cls()
				plugin.set_context(self)
				plugin.manifest = manifest
				self.plugins.append(plugin)
				print(f"Loaded external plugin: {manifest.get('name', item)}")
			except Exception as e:
				print(f"Failed to load external plugin from {p_dir}: {e}")
				traceback.print_exc()

	def update_splash(self, value, text):
		if self._splash:
			self._splash.set_progress(value, text)

	def _start_resource_monitor(self):
		"""启动系统资源监测线程"""
		try:
			if self._resource_monitor is None:
				# 创建资源监测器，设置回调函数为显示托盘通知
				self._resource_monitor = SystemResourceMonitor(
					on_alert_callback=self._on_resource_alert
				)
				self._resource_monitor.start()
				print("[APP] Resource monitor started")
		except Exception as e:
			print(f"[APP] Failed to start resource monitor: {e}")

	def _start_classisland_monitor(self):
		"""启动 ClassIsland 联动监测"""
		if sys.platform != "win32":
			return
		try:
			if self._classisland_monitor is None:
				self._classisland_monitor = ClassIslandMonitor(self.app)
				self._classisland_monitor.set_notification_callback(
					send_windows_notification
				)
				self._classisland_monitor.start()
				print("[APP] ClassIsland monitor started")
		except Exception as e:
			print(f"[APP] Failed to start ClassIsland monitor: {e}")

	def _stop_resource_monitor(self):
		try:
			if self._resource_monitor is not None:
				self._resource_monitor.stop()
				self._resource_monitor = None
				print("[APP] Resource monitor stopped")
		except Exception as e:
			print(f"[APP] Error stopping resource monitor: {e}")

	def _start_memory_cleaner(self):
		try:
			if self._memory_cleaner_process is not None:
				if self._memory_cleaner_process.poll() is None:
					return True
				self._memory_cleaner_process = None
			if sys.platform != "win32":
				return False
			base_dir = os.path.dirname(os.path.abspath(__file__))
			main_path = os.path.join(base_dir, "main.py")
			env = os.environ.copy()
			env["LUMINALIUM_PARENT_PID"] = str(os.getpid())
			env["LUMINALIUM_MEMCLEAN_INTERVAL"] = "30"
			creationflags = 0x08000000 | 0x00000008
			if getattr(sys, "frozen", False):
				cmd = [sys.executable, "--memory-cleaner"]
			else:
				cmd = [sys.executable, main_path, "--memory-cleaner"]
			self._memory_cleaner_process = subprocess.Popen(
				cmd, env=env, creationflags=creationflags, close_fds=True
			)
			print("[APP] Memory cleaner process started", flush=True)
			return True
		except Exception as e:
			print(f"[APP] Failed to start memory cleaner: {e}", flush=True)
			return False

	def _stop_memory_cleaner(self):
		try:
			proc = self._memory_cleaner_process
			if proc is not None and proc.poll() is None:
				proc.terminate()
				try:
					proc.wait(timeout=2.0)
				except Exception:
					proc.kill()
				print("[APP] Memory cleaner process stopped", flush=True)
			self._memory_cleaner_process = None
		except Exception as e:
			print(f"[APP] Error stopping memory cleaner: {e}", flush=True)

	def _setup_gc_timer(self):
		try:
			if self._gc_timer is not None:
				return
			if (
				self._memory_cleaner_process is not None
				and self._memory_cleaner_process.poll() is None
			):
				return
			self._gc_timer = QTimer(self.app)
			self._gc_timer.setInterval(120000)
			self._gc_timer.timeout.connect(self._on_gc_tick)
			self._gc_timer.start()
			print("[APP] GC timer started (120s interval)", flush=True)
		except Exception as e:
			print(f"[APP] Failed to setup GC timer: {e}", flush=True)

	def _stop_gc_timer(self):
		try:
			if self._gc_timer is not None:
				self._gc_timer.stop()
				self._gc_timer.deleteLater()
				self._gc_timer = None
				print("[APP] GC timer stopped", flush=True)
		except Exception as e:
			print(f"[APP] Error stopping GC timer: {e}", flush=True)

	def _on_gc_tick(self):
		try:
			import gc

			collected = gc.collect(2)
			if collected > 0:
				print(f"[APP] GC collected {collected} objects", flush=True)
		except Exception as e:
			print(f"[APP] GC tick error: {e}", flush=True)

	def _on_resource_alert(self, title: str, message: str):
		try:
			if hasattr(self, "tray") and self.tray:
				if getattr(sys, "frozen", False):
					base_dir = os.path.dirname(sys.executable)
				else:
					base_dir = os.path.dirname(os.path.abspath(__file__))
				image_path = os.path.join(
					base_dir, "icons", "performance-warning-image.png"
				)
				if not os.path.exists(image_path):
					image_path = None
				self.tray.show_message(title, message, image_path=image_path)
		except Exception as e:
			print(f"[APP] Error sending resource alert: {e}")

	def _connect_signals(self):
		_init_trace("_connect_signals: ENTRY")
		self.monitor.slideshow_started.connect(self.on_slideshow_start)
		self.monitor.slideshow_ended.connect(self.on_slideshow_end)
		self.monitor.slideshow_started.connect(
			lambda: self._focus_watcher.set_slideshow_running(True)
		)
		self.monitor.slideshow_ended.connect(
			lambda: self._focus_watcher.set_slideshow_running(False)
		)
		self.monitor.slideshow_hwnd_changed.connect(
			self._focus_watcher.set_slideshow_hwnd
		)
		self._focus_watcher.focus_on_slideshow_changed.connect(
			self._on_focus_on_slideshow_changed
		)

		self.overlay.request_next.connect(self.monitor.go_next, Qt.QueuedConnection)
		self.overlay.request_prev.connect(self.monitor.go_previous, Qt.QueuedConnection)
		self.overlay.request_goto.connect(self.monitor.go_to_slide, Qt.QueuedConnection)
		self.overlay.request_clear.connect(
			self.monitor.clear_screen, Qt.QueuedConnection
		)
		self.overlay.request_end.connect(self.monitor.end_show, Qt.QueuedConnection)

		self.overlay.request_ptr_arrow.connect(
			lambda: self.monitor.set_pointer_type(1), Qt.QueuedConnection
		)
		self.overlay.request_ptr_pen.connect(
			lambda: self.monitor.set_pointer_type(2), Qt.QueuedConnection
		)
		self.overlay.request_ptr_highlighter.connect(
			lambda: self.monitor.set_pointer_type(3), Qt.QueuedConnection
		)
		self.overlay.request_ptr_eraser.connect(
			lambda: self.monitor.set_pointer_type(5), Qt.QueuedConnection
		)
		self.overlay.request_pen_color.connect(self.monitor.set_pen_color)
		self.overlay.request_thumbnail.connect(
			lambda idx: self.monitor.export_slide_thumbnail(
				idx,
				os.path.join(
					tempfile.gettempdir(), "luminalium_ppt_thumbs", f"thumb_{idx}.png"
				),
			)
		)

		if self.tray is not None:
			_init_trace(
				f"_connect_signals: tray is not None, board_plugin exists: {hasattr(self, 'board_plugin')}, timer_plugin exists: {hasattr(self, 'timer_plugin')}"
			)
			print(
				f"[Tray] Connecting signals, board_plugin exists: {hasattr(self, 'board_plugin')}, timer_plugin exists: {hasattr(self, 'timer_plugin')}",
				flush=True,
			)
			if hasattr(self, "settings_plugin"):
				self.tray.show_settings.connect(self.settings_plugin.execute)
				self.tray.show_about.connect(self._open_settings_about)
			if hasattr(self, "board_plugin"):
				_init_trace("_connect_signals: connecting board_plugin")
				print(f"[Tray] Connecting board_plugin signal", flush=True)

				def _board_exec_wrapped():
					print(f"[Tray] board_plugin.execute() called from tray", flush=True)
					try:
						self.board_plugin.execute()
						print(f"[Tray] board_plugin.execute() returned OK", flush=True)
					except Exception as e:
						print(f"[Tray] board_plugin.execute() raised: {e}", flush=True)
						import traceback

						traceback.print_exc()

				self.tray.show_board.connect(_board_exec_wrapped)
				_init_trace("_connect_signals: board_plugin signal connected")
				print(f"[Tray] board_plugin signal connected", flush=True)
			if hasattr(self, "timer_plugin"):
				print(f"[Tray] Connecting timer_plugin signal", flush=True)

				def _timer_exec_wrapped():
					print(f"[Tray] timer_plugin.execute() called from tray", flush=True)
					try:
						self.timer_plugin.execute()
						print(f"[Tray] timer_plugin.execute() returned OK", flush=True)
					except Exception as e:
						print(f"[Tray] timer_plugin.execute() raised: {e}", flush=True)
						import traceback

						traceback.print_exc()

				self.tray.show_timer.connect(_timer_exec_wrapped)
				print(f"[Tray] timer_plugin signal connected", flush=True)
			if hasattr(self, "spotlight_plugin"):
				self.tray.show_spotlight.connect(self.spotlight_plugin.execute)
			if hasattr(self, "logs_plugin"):
				self.tray.show_logs.connect(self.logs_plugin.execute)
			self.tray.toggle_overlay.connect(self.toggle_overlay_visibility)
			self.tray.open_program_dir.connect(self._open_program_directory)
			self.tray.open_user_dir.connect(self._open_user_directory)
			self.tray.restart_app.connect(self._restart_from_tray)
			self.tray.exit_app.connect(self._exit_from_tray)

		if hasattr(self, "timer_plugin"):
			self.timer_plugin.background_mode_entered.connect(
				self._on_timer_background_mode
			)

		self._timer_manager.finished.connect(self._on_timer_finished)

		self.monitor.slide_changed.connect(self.overlay.update_page_info)
		self.monitor.window_geometry_changed.connect(self.overlay.update_geometry)
		self.monitor.window_geometry_changed.connect(self._cache_slideshow_geometry)
		self.monitor.slideshow_hwnd_changed.connect(self.overlay.set_slideshow_hwnd)
		self.monitor.restrictions_changed.connect(self.overlay.set_ppt_restrictions)
		self.monitor.thumbnail_generated.connect(self.overlay.on_thumbnail_ready)
		self.monitor.pen_color_changed.connect(self._broadcast_pen_color)

	@Slot()
	def toggle_overlay_visibility(self):
		if self.overlay:
			if self.overlay.isVisible():
				self.overlay.hide()
			else:
				self.overlay.show()

	def _prepare_shutdown(self, restarting=False):
		try:
			self._stop_resource_monitor()
			self._stop_memory_cleaner()
			self._stop_gc_timer()
		except Exception:
			pass
		try:
			if hasattr(self, "tray") and self.tray:
				self.tray.prepare_shutdown()
		except Exception:
			pass
		if restarting:
			try:
				os.environ["LUMINALIUM_RESTART"] = "1"
				os.environ["LUMINALIUM_RESTART_PID"] = str(os.getpid())
				_write_restart_marker()
			except Exception:
				pass
		try:
			self.app.processEvents()
		except Exception:
			pass

	@Slot()
	def _exit_from_tray(self):
		self._prepare_shutdown(restarting=False)
		self.app.quit()

	@Slot()
	def _restart_from_tray(self):
		self._prepare_shutdown(restarting=True)
		self._launch_new_instance()
		self.app.quit()

	@Slot()
	def _open_program_directory(self):
		open_path(ROOT_DIR)

	@Slot()
	def _open_user_directory(self):
		user_dir = os.path.join(ROOT_DIR, "user")
		if not os.path.exists(user_dir):
			os.makedirs(user_dir)
		open_path(user_dir)

	@Slot()
	def _on_timer_finished(self):
		now = time.monotonic()
		if now - self._last_timer_notify_at < 1.0:
			return
		self._last_timer_notify_at = now
		try:
			if not cfg.timerNotifyEnabled.value:
				return
		except Exception:
			pass
		title = t("timer.notify.title")
		body = t("timer.notify.body")
		if sys.platform == "win32":
			if send_windows_notification(
				title,
				body,
				launch="luminalium://intergrate/timer?command=open",
				buttons=[
					{
						"content": t("timer.notify.action.open"),
						"arguments": "luminalium://intergrate/timer?command=open",
					},
					{
						"content": t("timer.notify.action.restart"),
						"arguments": "luminalium://intergrate/timer?command=restart",
					},
					{
						"content": t("timer.notify.action.stop_all"),
						"arguments": "luminalium://intergrate/timer?command=stop_all",
					},
				],
			):
				return
		if hasattr(self, "tray") and self.tray:
			self.tray.show_message(title, body)

	@Slot()
	def _on_timer_background_mode(self):
		if hasattr(self, "tray") and self.tray:
			self.tray.show_message(
				t("timer.background.title"), t("timer.background.body")
			)

	@Slot()
	def on_slideshow_start(self):
		try:
			print(
				"[Main] slideshow started: "
				f"kind={getattr(self.monitor, '_active_kind', None) or 'unknown'}, "
				f"hwnd={int(getattr(self.monitor, '_slideshow_hwnd', 0) or 0)}",
				flush=True,
			)
		except Exception:
			pass
		self._slideshow_running = True
		print("[APP] Slideshow started")
		try:
			self.overlay.on_slideshow_start_cleanup()
		except Exception as e:
			print(f"[APP] Error in on_slideshow_start_cleanup: {e}")
		# Cleanup slide thumbnails from previous session
		temp_dir = os.path.join(tempfile.gettempdir(), "luminalium_ppt_thumbs")
		if os.path.exists(temp_dir):
			try:
				shutil.rmtree(temp_dir)
			except Exception:
				pass
		try:
			self._focus_watcher.set_slideshow_running(True)
		except Exception:
			pass
		try:
			active_kind = getattr(self.monitor, "_active_kind", None)
			print(
				f"[APP] autoShowOverlay={cfg.autoShowOverlay.value}, compatibilityMode={cfg.compatibilityMode.value}, active_kind={active_kind}"
			)
			if cfg.autoShowOverlay.value and not cfg.compatibilityMode.value:
				print(
					"[APP] Calling set_active_on_slideshow(True) - autoShowOverlay path"
				)
				if self._last_slideshow_rect is not None:
					try:
						self.overlay.update_geometry(
							self._last_slideshow_rect, self._last_slideshow_screen
						)
					except Exception as e:
						print(f"[APP] Error updating geometry: {e}")
				self.overlay.set_active_on_slideshow(True, animate=False)
			elif active_kind == "yozo" and not cfg.compatibilityMode.value:
				print("[APP] Calling set_active_on_slideshow(True) - yozo path")
				self.overlay.set_active_on_slideshow(True, animate=False)
			else:
				print("[APP] Not showing overlay - conditions not met")
		except Exception as e:
			print(f"[APP] Error in on_slideshow_start: {e}")
			import traceback

			traceback.print_exc()

	@Slot()
	def on_slideshow_end(self):
		try:
			print("[Main] slideshow ended.", flush=True)
		except Exception:
			pass
		self._slideshow_running = False
		try:
			self._focus_watcher.set_slideshow_running(False)
		except Exception:
			pass
		# Check if ink prompt is pending - if so, keep overlay visible and let the
		# ink prompt flow complete naturally (user will click keep/discard)
		try:
			if (
				hasattr(self.monitor, "_pending_ink_prompt")
				and self.monitor._pending_ink_prompt
			):
				print("[Main] Ink prompt pending, keeping overlay visible", flush=True)
				# Don't reset _pending_ink_prompt, don't dismiss the dialog.
				# The ink_prompt_result callback will handle cleanup and end_show_with_ink_choice.
				# Keep overlay active so the user can interact with the dialog.
				self.overlay.set_active_on_slideshow(True, animate=False)
			else:
				self.overlay.on_slideshow_end_cleanup()
				self.overlay.set_active_on_slideshow(False, animate=False)
		except Exception:
			pass

	def _should_keep_overlay_visible(self) -> bool:
		if cfg.compatibilityMode.value:
			return True
		if not self._slideshow_running or not cfg.autoShowOverlay.value:
			return False
		try:
			hwnd = int(getattr(self.monitor, "_slideshow_hwnd", 0) or 0)
		except Exception:
			hwnd = 0
		if hwnd:
			return True
		rect = self._last_slideshow_rect
		if rect is not None:
			try:
				if not rect.isEmpty():
					return True
			except Exception:
				return True
		return getattr(self.monitor, "_active_kind", None) == "yozo"

	@Slot(object, object)
	def _cache_slideshow_geometry(self, rect, screen):
		try:
			if rect is not None and hasattr(rect, "isEmpty") and not rect.isEmpty():
				self._last_slideshow_rect = rect
				self._last_slideshow_screen = screen
		except Exception:
			pass

	@Slot(bool)
	def _on_focus_on_slideshow_changed(self, focused: bool):
		try:
			print(f"[Main] focus_on_slideshow -> {bool(focused)}", flush=True)
		except Exception:
			pass
		self._pending_overlay_focus = bool(focused)
		delay_ms = 120 if focused else 260
		self._overlay_focus_timer.start(delay_ms)

	def _apply_pending_overlay_focus(self):
		try:
			focused = bool(self._pending_overlay_focus)
			if cfg.compatibilityMode.value:
				return
			if not self._slideshow_running or not cfg.autoShowOverlay.value:
				if (
					hasattr(self.monitor, "_pending_ink_prompt")
					and self.monitor._pending_ink_prompt
				):
					return
				self.overlay.set_active_on_slideshow(False, animate=True)
				return
			active_kind = getattr(self.monitor, "_active_kind", None)
			if active_kind == "yozo":
				if self._last_slideshow_rect is not None:
					try:
						self.overlay.update_geometry(
							self._last_slideshow_rect, self._last_slideshow_screen
						)
					except Exception:
						pass
				self.overlay.set_active_on_slideshow(True, animate=False)
				return
			if focused and self._last_slideshow_rect is not None:
				try:
					self.overlay.update_geometry(
						self._last_slideshow_rect, self._last_slideshow_screen
					)
				except Exception:
					pass
			self.overlay.set_active_on_slideshow(bool(focused), animate=True)
		except Exception:
			pass

	def _check_settings_changed(self):
		reset_marker = _get_settings_reset_marker_path()
		if os.path.exists(reset_marker):
			try:
				os.remove(reset_marker)
			except Exception:
				pass
			self._settings_mtime = 0
			self.restart()
			return

		pending_path = os.path.join(os.path.dirname(SETTINGS_PATH), "_pending_action")
		if os.path.exists(pending_path):
			try:
				with open(pending_path, "r", encoding="utf-8") as f:
					pending_data = json.load(f)
				if pending_data.get("_quit_pending"):
					os.remove(pending_path)
					self._prepare_shutdown(restarting=False)
					self.app.quit()
					return
				if pending_data.get("_restart_pending"):
					os.remove(pending_path)
					self.restart()
					return
			except Exception:
				pass

		if not os.path.exists(SETTINGS_PATH):
			return
		mtime = os.path.getmtime(SETTINGS_PATH)
		if mtime != self._settings_mtime:
			self._settings_mtime = mtime

			# Check for restart flag
			try:
				with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
					temp_data = json.load(f)
				if temp_data.get("_quit_pending"):
					del temp_data["_quit_pending"]
					with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
						json.dump(temp_data, f, indent=4, ensure_ascii=False)
					self._prepare_shutdown(restarting=False)
					self.app.quit()
					return
				if temp_data.get("_restart_pending"):
					del temp_data["_restart_pending"]
					with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
						json.dump(temp_data, f, indent=4, ensure_ascii=False)
					self.restart()
					return
			except Exception:
				pass

			old_theme = cfg.themeMode.value
			old_theme_id = cfg.themeId.value if hasattr(cfg, "themeId") else "default"
			old_lang = getattr(self, "_current_language", "zh-CN")
			old_qt_font = getattr(self, "_current_qt_font", "")
			old_overlay_font = getattr(self, "_current_overlay_font", "")
			old_qt_weight = getattr(self, "_current_qt_font_weight", None)
			old_toolbar_text = cfg.showToolbarText.value
			old_status_bar = cfg.showStatusBar.value
			old_status_bar_show_time = cfg.statusBarShowTime.value
			old_status_bar_show_seconds = cfg.statusBarShowSeconds.value
			old_status_bar_show_battery = cfg.statusBarShowBattery.value
			old_status_bar_show_volume = cfg.statusBarShowVolume.value
			old_status_bar_show_network = cfg.statusBarShowNetwork.value
			old_status_bar_show_music = cfg.statusBarShowMusic.value
			old_clear = cfg.showClear.value
			old_spotlight = cfg.showSpotlight.value
			old_timer = cfg.showTimer.value
			old_toolbar_order = cfg.toolbarOrder.value
			old_safe_area = cfg.safeArea.value
			old_scale = cfg.scale.value
			old_overlay_screen = cfg.overlayScreen.value
			old_rebuild_at = getattr(self, "_overlay_rebuild_at", None)
			old_compat = cfg.compatibilityMode.value
			# old_layout_mode = cfg.toolbarLayout.value

			reload_cfg()

			data = _load_settings_json()
			if cfg.overlayScreen.value != old_overlay_screen:
				try:
					self.monitor.force_update_geometry()
				except Exception:
					pass

			new_lang = (data.get("General", {}) or {}).get("Language", "zh-CN")
			profiles = (data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
			lang_profile = profiles.get(new_lang, {}) or {}
			qt_font = lang_profile.get("qt", "")
			overlay_font = lang_profile.get("overlay", "") or qt_font
			new_qt_weight = _get_font_weight_from_settings(data, new_lang, "qt")
			new_qt_font = qt_font.strip() if isinstance(qt_font, str) else ""
			new_overlay_font = (
				overlay_font.strip() if isinstance(overlay_font, str) else ""
			)
			new_rebuild_at = (data.get("Overlay", {}) or {}).get("RecreateOverlayAt")

			self._current_language = new_lang
			self._current_qt_font = new_qt_font
			self._current_overlay_font = new_overlay_font
			self._current_qt_font_weight = new_qt_weight

			if new_qt_font != old_qt_font or new_qt_weight != old_qt_weight:
				_apply_global_font(self.app)

			theme_mode_changed = cfg.themeMode.value != old_theme
			theme_id_changed = (
				hasattr(cfg, "themeId") and cfg.themeId.value != old_theme_id
			)

			should_reload = (
				new_lang != old_lang
				or new_overlay_font != old_overlay_font
				or cfg.compatibilityMode.value != old_compat
				or cfg.showClear.value != old_clear
				or cfg.showSpotlight.value != old_spotlight
				or cfg.showTimer.value != old_timer
				or cfg.showToolbarText.value != old_toolbar_text
				or cfg.toolbarOrder.value != old_toolbar_order
				or cfg.safeArea.value != old_safe_area
				or cfg.scale.value != old_scale
			)

			if should_reload:
				if not self._reloading_overlay:
					self._reload_timer.start()
			else:
				overlay = getattr(self, "overlay", None)
				if overlay:
					if new_rebuild_at and new_rebuild_at != old_rebuild_at:
						if not self._reloading_overlay:
							self._reload_timer.start()

					# Check for any status bar config changes
					status_bar_changed = (
						cfg.showStatusBar.value != old_status_bar
						or cfg.statusBarShowTime.value != old_status_bar_show_time
						or cfg.statusBarShowSeconds.value != old_status_bar_show_seconds
						or cfg.statusBarShowBattery.value != old_status_bar_show_battery
						or cfg.statusBarShowVolume.value != old_status_bar_show_volume
						or cfg.statusBarShowNetwork.value != old_status_bar_show_network
						or cfg.statusBarShowMusic.value != old_status_bar_show_music
					)

					if status_bar_changed:
						overlay.update_config()

					if theme_mode_changed or theme_id_changed:
						overlay.update_theme()
						self._notify_settings_theme_changed()

					if cfg.compatibilityMode.value != old_compat:
						overlay.update_config()
						if cfg.compatibilityMode.value:
							overlay.show()
						elif not self._slideshow_running:
							overlay.hide()

			# Layout mode change is now handled by auto-reload above, no restart prompt needed

			if theme_mode_changed:
				if self.tray is not None:
					self.tray.refresh_menu()
				if self._splash is not None and self._splash.isVisible():
					try:
						self._splash.refresh_theme()
					except Exception:
						pass

			if new_lang != old_lang or cfg.compatibilityMode.value != old_compat:
				if self.tray is not None:
					self.tray.refresh_menu()

			if new_rebuild_at is not None:
				self._overlay_rebuild_at = new_rebuild_at

	def _consume_open_settings_pending_flag(self):
		if not os.path.exists(SETTINGS_PATH):
			return False
		try:
			with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
				data = json.load(f)
			if not data.get("_open_settings_pending"):
				return False
			del data["_open_settings_pending"]
			with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
				json.dump(data, f, indent=4, ensure_ascii=False)
			return True
		except Exception:
			return False

	def _reload_overlay(self):
		"""Recreate the overlay window to apply language and layout changes."""
		if self._reloading_overlay:
			return
		self._reloading_overlay = True
		was_visible = self.overlay.isVisible()

		try:
			# Import overlay again to refresh module-level LANGUAGE
			import ppt_assistant.ui.overlay as overlay_mod

			importlib.reload(overlay_mod)
			from ppt_assistant.ui.overlay import create_overlay_window

			# Create new overlay first (prevent crash if creation fails)
			new_overlay = create_overlay_window()
			new_overlay.set_monitor(self.monitor)
			if hasattr(new_overlay, "set_timer_manager"):
				new_overlay.set_timer_manager(self._timer_manager)

			# Re-connect signals
			new_overlay.request_next.connect(self.monitor.go_next, Qt.QueuedConnection)
			new_overlay.request_prev.connect(
				self.monitor.go_previous, Qt.QueuedConnection
			)
			new_overlay.request_goto.connect(
				self.monitor.go_to_slide, Qt.QueuedConnection
			)
			new_overlay.request_clear.connect(
				self.monitor.clear_screen, Qt.QueuedConnection
			)
			new_overlay.request_end.connect(self.monitor.end_show, Qt.QueuedConnection)
			new_overlay.request_ptr_arrow.connect(
				lambda: self.monitor.set_pointer_type(1), Qt.QueuedConnection
			)
			new_overlay.request_ptr_pen.connect(
				lambda: self.monitor.set_pointer_type(2), Qt.QueuedConnection
			)
			new_overlay.request_ptr_highlighter.connect(
				lambda: self.monitor.set_pointer_type(3), Qt.QueuedConnection
			)
			new_overlay.request_ptr_eraser.connect(
				lambda: self.monitor.set_pointer_type(5), Qt.QueuedConnection
			)
			new_overlay.request_pen_color.connect(self.monitor.set_pen_color)
			new_overlay.request_thumbnail.connect(
				lambda idx: self.monitor.export_slide_thumbnail(
					idx,
					os.path.join(
						tempfile.gettempdir(),
						"luminalium_ppt_thumbs",
						f"thumb_{idx}.png",
					),
				)
			)

			# Disconnect old overlay slots before connecting new ones
			with warnings.catch_warnings():
				warnings.simplefilter("ignore", RuntimeWarning)
				try:
					self.monitor.slide_changed.disconnect(self.overlay.update_page_info)
				except Exception:
					pass
				try:
					self.monitor.window_geometry_changed.disconnect(
						self.overlay.update_geometry
					)
				except Exception:
					pass
				try:
					self.monitor.slideshow_hwnd_changed.disconnect(
						self.overlay.set_slideshow_hwnd
					)
				except Exception:
					pass
				try:
					self.monitor.thumbnail_generated.disconnect(
						self.overlay.on_thumbnail_ready
					)
				except Exception:
					pass
			self.monitor.slide_changed.connect(new_overlay.update_page_info)
			self.monitor.window_geometry_changed.connect(new_overlay.update_geometry)
			self.monitor.slideshow_hwnd_changed.connect(new_overlay.set_slideshow_hwnd)
			self.monitor.thumbnail_generated.connect(new_overlay.on_thumbnail_ready)

			# Swap overlay
			old_overlay = self.overlay
			self.overlay = new_overlay

			# Cleanup old overlay
			old_overlay.cleanup()  # Stop threads safely
			old_overlay.hide()
			old_overlay.deleteLater()

			if was_visible and self._should_keep_overlay_visible():
				self.overlay.show()
				self.overlay.raise_()
			else:
				self.overlay.hide()

			# Update current page info immediately
			if self.monitor:
				curr, total = self.monitor.get_page_info()
				self.overlay.update_page_info(curr, total)
				self.monitor.force_update_geometry()

		except Exception as e:
			print(f"Error reloading overlay: {e}")
			# If failed, keep using the old overlay if it's still alive
			if was_visible and not self.overlay.isVisible():
				self.overlay.show()
		finally:
			self._reloading_overlay = False

	def _launch_new_instance(self):
		"""Start a fresh copy of the application as a detached child process."""
		try:
			base_dir = os.path.dirname(os.path.abspath(__file__))
			main_path = os.path.join(base_dir, "main.py")
			filtered_args = [
				a
				for a in sys.argv[1:]
				if a
				not in (
					"--silent",
					"--webview-runner",
					"--dialog",
					"--crash-file",
					"--memory-cleaner",
				)
			]
			if getattr(sys, "frozen", False):
				cmd = [sys.executable] + filtered_args
			else:
				cmd = [sys.executable, main_path] + filtered_args
			env = os.environ.copy()
			env["LUMINALIUM_RESTART"] = "1"
			env["LUMINALIUM_RESTART_PID"] = str(os.getpid())
			creationflags = (
				0x08000000 | 0x00000008
			)  # CREATE_NO_WINDOW | DETACHED_PROCESS
			subprocess.Popen(cmd, env=env, creationflags=creationflags, close_fds=True)
		except Exception as e:
			print(f"Failed to launch new instance: {e}", file=sys.stderr)

	def restart(self):
		"""Restart for internal callers (settings reset, onboarding, etc.)."""
		self._prepare_shutdown(restarting=True)
		self._launch_new_instance()
		self.app.quit()

	@Slot(int, int, int, str)
	def _broadcast_pen_color(self, r, g, b, hex_color):
		"""Broadcast pen color change to Board plugin only (NOT theme accent color)."""
		for plugin in getattr(self, "plugins", []):
			if plugin.get_name() == "板中板" or plugin.get_name() == "Board":
				if hasattr(plugin, "set_pen_color"):
					plugin.set_pen_color(r, g, b)

	def _handle_system_theme_refresh(self):
		"""Called when system theme changes and app is in AUTO mode.
		Refreshes all open windows to reflect the new theme."""
		print(f"[Main] System theme refresh triggered", flush=True)
		if self._splash and self._splash.isVisible():
			try:
				self._splash.refresh_theme()
			except Exception:
				pass
		if self.overlay:
			try:
				self.overlay.update_theme()
			except Exception:
				pass
		if self.tray:
			try:
				self.tray.refresh_menu()
			except Exception:
				pass
		self._notify_settings_theme_changed()

	def _notify_settings_theme_changed(self):
		"""Push theme update to the settings window if it is open."""
		try:
			plugin = getattr(self, "settings_plugin", None)
			if plugin is None:
				return
			win = getattr(plugin, "_window", None)
			if win is None:
				return
			import json as _json

			theme_mode_raw = cfg.themeMode.value
			theme_mode_str = (
				"Light"
				if theme_mode_raw == Theme.LIGHT
				else "Dark"
				if theme_mode_raw == Theme.DARK
				else "Auto"
			)
			theme_id = cfg.themeId.value
			js = (
				f"if (typeof updateTheme === 'function') "
				f"updateTheme({_json.dumps(theme_mode_str)}, {_json.dumps(theme_id)});"
				f"if(typeof window.__applyUnifiedTheme==='function')window.__applyUnifiedTheme();"
			)
			win.page().runJavaScript(js)
		except Exception:
			pass

	def cleanup(self):
		"""Cleanup app resources and terminate subprocesses."""
		if hasattr(self, "_theme_watcher"):
			try:
				self._theme_watcher.stop()
			except Exception:
				pass
		if hasattr(self, "monitor"):
			self.monitor.stop_monitoring()
		try:
			if hasattr(self, "_focus_watcher") and self._focus_watcher:
				self._focus_watcher.stop()
		except Exception:
			pass
		if hasattr(self, "settings_plugin"):
			self.settings_plugin.terminate()
		if hasattr(self, "_classisland_monitor") and self._classisland_monitor:
			try:
				self._classisland_monitor.stop()
			except Exception:
				pass
		if hasattr(self, "overlay"):
			self.overlay.cleanup()
		# Stop watchdog heartbeat so the watchdog subprocess exits cleanly
		try:
			if hasattr(self, "app") and hasattr(self.app, "_crash_handler"):
				self.app._crash_handler.stop_watchdog()
		except Exception:
			pass

	def run(self):
		# sys.exit(self.app.exec())
		pass


def _check_post_update():
	try:
		app_dir = (
			Path(sys.executable).parent
			if getattr(sys, "frozen", False)
			else Path(__file__).parent
		)
		version_file = app_dir / "_internal" / ".version"
		if version_file.exists():
			with open(version_file, "r", encoding="utf-8") as f:
				new_ver = f.read().strip()
			version_file.unlink()

			if sys.platform == "win32":
				try:
					send_windows_notification(
						"更新完成",
						f"应用已更新到 {new_ver}，点击以查看详细信息",
						launch="luminalium://settings/update",
					)
				except Exception as e:
					print(f"Failed to send update notification: {e}")
	except Exception as e:
		print(f"Error in post update check: {e}")


if __name__ == "__main__":
	# Platform settings moved to top of file to ensure they apply before any Qt import
	if sys.platform == "win32":
		configure_current_process_for_notifications()

	_ensure_user_dirs()

	_pending_protocol_url = None
	for arg in sys.argv:
		route = parse_luminalium_url(arg)
		if route is not None:
			_pending_protocol_url = arg
			# 既存インスタンスにURLを転送、新インスタンス起動を防止
			forwarded = False
			import socket
			import urllib.request
			from urllib.parse import quote

			# ポート疎通確認＋HTTP転送（最大5回リトライ、指数バックオフ）
			for attempt in range(5):
				try:
					# まずポートが開いてるか簡易チェック
					sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
					sock.settimeout(0.3)
					result = sock.connect_ex(("127.0.0.1", 28423))
					sock.close()
					if result != 0:
						raise ConnectionRefusedError("port not ready")

					encoded_url = quote(arg, safe="")
					timeout = 1.0 + attempt * 0.5  # 1.0, 1.5, 2.0, 2.5, 3.0
					urllib.request.urlopen(
						f"http://127.0.0.1:28423/api/protocol/handle?url={encoded_url}",
						timeout=timeout,
					)
					forwarded = True
					break
				except Exception:
					time.sleep(0.2 * (attempt + 1))

			if forwarded:
				sys.exit(0)
			else:
				# ポートが開いてる=既存インスタンスがいるがHTTP応答なし → フラグファイルに書き込んで終了
				# ポートが閉じてる=既存インスタンスなし → このまま新規起動
				sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
				sock.settimeout(0.3)
				port_open = sock.connect_ex(("127.0.0.1", 28423)) == 0
				sock.close()
				if port_open:
					print(
						"[Main] HTTP forwarding failed but port is open, writing protocol URL to flag file",
						flush=True,
					)
					try:
						app_dir = (
							Path(sys.executable).parent
							if getattr(sys, "frozen", False)
							else Path(__file__).parent
						)
						protocol_flag = app_dir / "_internal" / ".protocol_url"
						protocol_flag.parent.mkdir(exist_ok=True)
						protocol_flag.write_text(arg, encoding="utf-8")
					except Exception as fe:
						print(f"[Main] Failed to write protocol flag: {fe}", flush=True)
					sys.exit(0)
				else:
					print(
						"[Main] No existing instance detected, starting new instance",
						flush=True,
					)
			break

	if cfg.registerUrlProtocol.value:
		register_url_protocol()
	_check_post_update()
	_apply_graphics_settings()
	print("[Main] Creating QApplication...", flush=True)
	app = QApplication(sys.argv)
	print("[Main] QApplication created.", flush=True)
	app_icon = load_app_icon()
	if not app_icon.isNull():
		app.setWindowIcon(app_icon)
		app._window_icon_filter = WindowIconEventFilter(app_icon)

		# Per-toplevel-window install instead of app-level installEventFilter.
		# An application-level filter would force PySide to wrap every QObject
		# (including short-lived QQuickItem children inside QQuickWidget hover
		# delivery) into a Python wrapper before reaching the Python early-
		# return; that wrapping has SEGV'd in libpyside6 on Linux/xcb.
		# See `D:\sectl\DeathLogX11.log` for the reference stack.
		def _install_window_icon_filter_on(target):
			if target is None or not target.isWindow():
				return
			try:
				target.installEventFilter(app._window_icon_filter)
			except Exception:
				pass

		# Install on already-existing top-level widgets.
		for w in app.topLevelWidgets():
			_install_window_icon_filter_on(w)

		# Backstop for new top-level windows: focusWindowChanged carries the
		# QWindow of whichever window just got focus; map it back to the
		# owning QWidget and install the filter on that toplevel.
		def _on_focus_window_changed(win):
			if win is None:
				return
			try:
				wid = QWidget.find(win.winId())
			except Exception:
				wid = None
			if wid is not None:
				_install_window_icon_filter_on(wid.window())

		app.focusWindowChanged.connect(_on_focus_window_changed)
		# Expose so other modules can call `app._install_window_icon_filter_on(self)`
		app._install_window_icon_filter_on = _install_window_icon_filter_on
	_apply_global_font(app)
	print("[Main] Global font applied.", flush=True)
	crash_handler = CrashHandler(app)
	app._crash_handler = crash_handler  # Store ref for cleanup
	crash_handler.start_watchdog()

	# Enable faulthandler to capture segfaults / SIGABRT etc.
	# Writes stack trace to crash log directory on fatal signal.
	try:
		crash_log_dir = os.path.join(
			os.environ.get("APPDATA", tempfile.gettempdir()), "Luminalium", "crash_logs"
		)
		os.makedirs(crash_log_dir, exist_ok=True)
		fh_path = os.path.join(crash_log_dir, "faulthandler.log")
		faulthandler.enable(file=open(fh_path, "a", encoding="utf-8"), all_threads=True)
		print(f"[Main] faulthandler enabled -> {fh_path}", flush=True)
	except Exception as e:
		print(f"[Main] Failed to enable faulthandler: {e}", flush=True)

	# Install Qt message handler to catch Qt-level fatal errors
	# (e.g. "Must construct a QApplication before a QWidget")
	def _qt_message_handler(mode, context, message):
		"""
		Qt消息处理器 - 直接输出到真实的stderr/stdout，不经过Python的日志重定向
		这样可以避免GIL争夺和死锁问题
		"""
		msg_str = str(message) if message else ""
		
		# 使用 sys.__stderr__ 和 sys.__stdout__ 直接输出，绕过任何Python层的拦截
		try:
			if mode == 0:  # QtDebugMsg
				# 调试消息输出到stdout（通常不显示，除非设置了环境变量）
				sys.__stdout__.write(f"[Qt-Debug] {msg_str}\n")
				sys.__stdout__.flush()
			elif mode == 1:  # QtInfoMsg
				sys.__stdout__.write(f"[Qt-Info] {msg_str}\n")
				sys.__stdout__.flush()
			elif mode == 2:  # QtWarningMsg
				sys.__stderr__.write(f"[Qt-Warning] {msg_str}\n")
				sys.__stderr__.flush()
			elif mode == 3:  # QtCriticalMsg
				sys.__stderr__.write(f"[Qt-Critical] {msg_str}\n")
				sys.__stderr__.flush()
			elif mode == 4:  # QtFatalMsg
				error_msg = f"Qt Fatal Error: {msg_str}\n\nFile: {context.file}\nLine: {context.line}\nFunction: {context.function}"
				sys.__stderr__.write(f"[Qt-Fatal] {error_msg}\n")
				sys.__stderr__.flush()
				
				# Write to crash log immediately
				try:
					cld = os.path.join(
						os.environ.get("APPDATA", tempfile.gettempdir()),
						"Luminalium",
						"crash_logs",
					)
					os.makedirs(cld, exist_ok=True)
					clp = os.path.join(cld, f"qtfatal_{os.getpid()}_{int(time.time())}.log")
					with open(clp, "w", encoding="utf-8") as f:
						f.write(error_msg)
					sys.__stdout__.write(f"[CrashHandler] Qt fatal log written to {clp}\n")
					sys.__stdout__.flush()
				except Exception:
					pass
		except Exception:
			# 如果输出失败，静默忽略（避免递归错误）
			pass

	from PySide6.QtCore import qInstallMessageHandler

	qInstallMessageHandler(_qt_message_handler)
	print("[Main] Qt message handler installed", flush=True)
	_init_trace("main: creating global mutex")
	print("[Main] Creating global mutex...", flush=True)
	_create_global_mutex()
	_init_trace("main: global mutex created")
	print("[Main] Checking multi-instance state...", flush=True)
	_handle_multi_instance(app)
	_init_trace("main: multi-instance check done")
	print("[Main] Multi-instance check finished.", flush=True)

	# Initialize log manager to capture application logs
	from ppt_assistant.core.log_manager import get_log_manager, init_log_manager, get_logger

	init_log_manager()
	# Load log level settings from config
	log_manager = get_log_manager()
	log_filters = {
		"debug": cfg.showDebug.value if hasattr(cfg, "showDebug") else True,
		"info": cfg.showInfo.value if hasattr(cfg, "showInfo") else True,
		"warn": cfg.showWarn.value if hasattr(cfg, "showWarn") else True,
		"error": cfg.showError.value if hasattr(cfg, "showError") else True,
	}
	log_manager.set_filters(log_filters)
	
	# Get logger for main module
	logger = get_logger("main")
	logger.info("Log manager initialized with filters: %s", log_filters)

	from ppt_assistant.core.config import reload_cfg

	reload_cfg()  # Load settings.json so all cfg values reflect user config

	show_splash = True
	try:
		mode = cfg.splashMode.value
		if mode == "Never":
			show_splash = False
		elif mode == "HideOnAutoStart":
			args = [a.lower() for a in sys.argv]
			if "--autostart" in args or "-autostart" in args or "--silent" in args:
				show_splash = False
		elif mode == "TimeRange":
			from PySide6.QtCore import QTime

			start_str = cfg.splashStartTime.value
			end_str = cfg.splashEndTime.value
			start_t = QTime.fromString(start_str, "HH:mm")
			end_t = QTime.fromString(end_str, "HH:mm")
			now = QTime.currentTime()

			if start_t.isValid() and end_t.isValid():
				if start_t <= end_t:
					if not (start_t <= now <= end_t):
						show_splash = False
				else:
					if not (now >= start_t or now <= end_t):
						show_splash = False
	except Exception as e:
		print(f"Error determining splash visibility: {e}")
		show_splash = True

	splash = None
	if show_splash:
		print("[Main] Creating startup splash...", flush=True)
		splash = StartupSplash()
		splash.show()
		app.processEvents()
		print("[Main] Startup splash shown.", flush=True)

	print("[Main] Creating PPTAssistantApp...", flush=True)
	_init_trace("main: creating PPTAssistantApp")
	app_instance = PPTAssistantApp(app, splash)
	_init_trace("main: PPTAssistantApp created")

	if _pending_protocol_url:
		app_instance._pending_protocol_url = _pending_protocol_url
		route = parse_luminalium_url(_pending_protocol_url)
		if route and route.get("action") == "settings":
			app_instance._open_settings_after_startup = True
			if route.get("page") == "update":
				app_instance._open_settings_after_startup = True

	print("[Main] PPTAssistantApp created.", flush=True)
	crash_handler.set_app_instance(app_instance)

	# === Diagnostic: instrument app.exec() return ===
	print("[Main] Entering app.exec()...", flush=True)
	_exec_rc = app.exec()
	print(f"[Main] app.exec() returned rc={_exec_rc}", flush=True)
	sys.exit(_exec_rc)
