from qfluentwidgets import (
    QConfig,
    ConfigItem,
    BoolValidator,
    RangeConfigItem,
    RangeValidator,
    OptionsConfigItem,
    OptionsValidator,
    Theme,
    qconfig,
    setThemeColor,
)
from qfluentwidgets.common.config import EnumSerializer
import os
import json
import sys

# Nuitka standalone detection and compatibility
if hasattr(sys, "nuitka_binary"):
    sys.frozen = True


def get_root_dir():
    """Get the root directory of the application, supporting both dev and packaged modes."""
    # If frozen (PyInstaller or Nuitka)
    if getattr(sys, "frozen", False):
        # PyInstaller temp dir
        if hasattr(sys, "_MEIPASS"):
            return sys._MEIPASS
        # Nuitka or other standalone folder: resources are usually relative to the exe or __file__
        # In Nuitka standalone, __file__ points to the source location in the dist folder
        # In Nuitka onefile, __file__ points to the temp directory
        # For our structure, the root is 3 levels up from ppt_assistant/core/config.py
        try:
            return os.path.dirname(
                os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            )
        except:
            return os.path.dirname(sys.executable)

    # Dev mode: root is 3 levels up from ppt_assistant/core/config.py
    return os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


_root_dir = get_root_dir()
ROOT_DIR = _root_dir  # For convenience

from ppt_assistant.core.platform_integration import (
    set_run_at_startup as platform_set_run_at_startup,
)

try:
    import winreg
except ImportError:
    winreg = None


class Config(QConfig):
    themeMode = OptionsConfigItem(
        "Appearance",
        "ThemeMode",
        Theme.LIGHT,
        OptionsValidator(Theme),
        EnumSerializer(Theme),
        restart=False,
    )
    themeId = ConfigItem("Appearance", "ThemeId", "default", restart=False)
    overlayTheme = ConfigItem("Appearance", "OverlayTheme", "default", restart=False)

    runAtStartup = ConfigItem("General", "RunAtStartup", False, BoolValidator())
    autoShowOverlay = ConfigItem("General", "AutoShowOverlay", True, BoolValidator())
    disableAnimations = ConfigItem(
        "General", "DisableAnimations", False, BoolValidator(), restart=True
    )
    crashAutoHandleEnabled = ConfigItem(
        "General", "CrashAutoHandleEnabled", False, BoolValidator()
    )
    crashAutoHandleMode = OptionsConfigItem(
        "General",
        "CrashAutoHandleMode",
        "ShowAnalyzer",
        OptionsValidator(["ShowAnalyzer", "Exit", "RestartSilent", "Toast"]),
        restart=False,
    )
    showClear = ConfigItem("Toolbar", "ShowClear", True, BoolValidator())
    showSpotlight = ConfigItem("Toolbar", "ShowSpotlight", True, BoolValidator())
    showBoardInBoard = ConfigItem("Toolbar", "ShowBoardInBoard", True, BoolValidator())
    showTimer = ConfigItem("Toolbar", "ShowTimer", True, BoolValidator())
    showToolbarText = ConfigItem("Toolbar", "ShowToolbarText", False, BoolValidator())
    secRandomEnabled = ConfigItem(
        "Linkage", "SecRandomEnabled", False, BoolValidator()
    )

    showStatusBar = ConfigItem("Overlay", "ShowStatusBar", False, BoolValidator())
    statusBarShowTime = ConfigItem(
        "Overlay", "StatusBarShowTime", True, BoolValidator()
    )
    statusBarShowSeconds = ConfigItem(
        "Overlay", "StatusBarShowSeconds", False, BoolValidator()
    )
    statusBarShowBattery = ConfigItem(
        "Overlay", "StatusBarShowBattery", True, BoolValidator()
    )
    statusBarShowVolume = ConfigItem(
        "Overlay", "StatusBarShowVolume", True, BoolValidator()
    )
    statusBarShowNetwork = ConfigItem(
        "Overlay", "StatusBarShowNetwork", True, BoolValidator()
    )
    statusBarShowMusic = ConfigItem(
        "Overlay", "StatusBarShowMusic", True, BoolValidator()
    )
    clearMode = OptionsConfigItem(
        "Overlay",
        "ClearMode",
        "slide",
        OptionsValidator(["slide", "button"]),
        restart=False,
    )
    toolbarPosition = OptionsConfigItem(
        "Overlay",
        "ToolbarPosition",
        "bottom",
        OptionsValidator(["top", "bottom", "left", "right"]),
        restart=False,
    )
    flipperPosition = OptionsConfigItem(
        "Overlay",
        "FlipperPosition",
        "center",
        OptionsValidator(["center", "bottom"]),
        restart=False,
    )
    safeArea = RangeConfigItem(
        "Overlay", "SafeArea", 0, RangeValidator(0, 100), restart=False
    )
    scale = RangeConfigItem(
        "Overlay", "Scale", 1.0, RangeValidator(0.5, 2.0), restart=False
    )
    popWindowScale = RangeConfigItem(
        "Overlay", "PopWindowScale", 1.0, RangeValidator(0.5, 3.0), restart=False
    )
    toolbarOpacity = RangeConfigItem(
        "Overlay", "ToolbarOpacity", 1.0, RangeValidator(0.1, 1.0), restart=False
    )
    sidePageOpacity = RangeConfigItem(
        "Overlay", "SidePageOpacity", 1.0, RangeValidator(0.1, 1.0), restart=False
    )
    syncOpacity = ConfigItem(
        "Overlay", "SyncOpacity", False, BoolValidator(), restart=False
    )
    strictEdgeAlignment = ConfigItem(
        "Overlay", "StrictEdgeAlignment", False, BoolValidator(), restart=False
    )
    toolbarAutoHalfCollapse = ConfigItem(
        "Overlay", "ToolbarAutoHalfCollapse", False, BoolValidator(), restart=False
    )
    uiAccessTopmost = ConfigItem(
        "Overlay", "UIAccessTopmost", False, BoolValidator(), restart=False
    )
    allowRecording = ConfigItem(
        "Overlay", "AllowRecording", False, BoolValidator(), restart=True
    )
    zOrderCheckInterval = OptionsConfigItem(
        "Overlay",
        "ZOrderCheckInterval",
        200,
        OptionsValidator([50, 100, 200, 500, 1000]),
        restart=False,
    )

    autoHandleInk = ConfigItem("PPT", "AutoHandleInk", True, BoolValidator())
    penMode = OptionsConfigItem(
        "PPT",
        "PenMode",
        "com",
        OptionsValidator(["com", "self-developed"]),
        restart=False,
    )
    selfPenWidth = RangeConfigItem("SelfPen", "PenWidth", 3, RangeValidator(1, 12), restart=False)
    selfHighlightWidth = RangeConfigItem("SelfPen", "HighlightWidth", 24, RangeValidator(8, 48), restart=False)
    selfEraserWidth = RangeConfigItem("SelfPen", "EraserWidth", 30, RangeValidator(10, 80), restart=False)
    selfHighlightOpacity = RangeConfigItem("SelfPen", "HighlightOpacity", 0.45, RangeValidator(0.1, 1.0), restart=False)
    selfPenColor = ConfigItem("SelfPen", "PenColor", "#000000")
    selfHighlightColor = ConfigItem("SelfPen", "HighlightColor", "#FFFF00")
    selfPenFrameRateMode = OptionsConfigItem("SelfPen", "FrameRateMode", "adaptive", OptionsValidator(["low", "adaptive", "high"]), restart=False)
    selfPenPenEffect = OptionsConfigItem("SelfPen", "PenEffect", "limited", OptionsValidator(["off", "limited", "full"]), restart=False)
    selfPenPalmErase = ConfigItem("SelfPen", "PalmErase", False, BoolValidator())
    selfPenWidthPresetIndex = RangeConfigItem("SelfPen", "PenWidthPresetIndex", -1, RangeValidator(-1, 4), restart=False)
    selfEraserWidthPresetIndex = RangeConfigItem("SelfPen", "EraserWidthPresetIndex", -1, RangeValidator(-1, 4), restart=False)
    selfCustomPenColors = ConfigItem("SelfPen", "CustomPenColors", "[]")
    selfCustomHighlightColors = ConfigItem("SelfPen", "CustomHighlightColors", "[]")
    pageTurnRateLimit = RangeConfigItem(
        "PPT", "PageTurnRateLimit", 2, RangeValidator(1, 6), restart=False
    )
    compatibilityMode = ConfigItem(
        "General",
        "CompatibilityMode",
        True if sys.platform != "win32" else False,
        BoolValidator(),
    )
    registerUrlProtocol = ConfigItem(
        "General",
        "RegisterUrlProtocol",
        True if sys.platform == "win32" else False,
        BoolValidator(),
    )
    useNativeTitleBar = ConfigItem(
        "General", "UseNativeTitleBar", False, BoolValidator(), restart=True
    )
    hideOnClose = ConfigItem(
        "General", "HideOnClose", True, BoolValidator(), restart=False
    )

    overlayScreen = ConfigItem("Overlay", "OverlayScreen", "Auto", restart=False)

    splashMode = OptionsConfigItem(
        "General",
        "SplashMode",
        "Always",
        OptionsValidator(["Always", "Never", "HideOnAutoStart", "TimeRange"]),
        restart=False,
    )
    splashStyle = ConfigItem(
        "General",
        "SplashStyle",
        "default",
        restart=False,
    )
    showDetailedSplash = ConfigItem(
        "General", "ShowDetailedSplash", False, BoolValidator()
    )
    splashStartTime = ConfigItem("General", "SplashStartTime", "08:00", restart=False)
    splashEndTime = ConfigItem("General", "SplashEndTime", "20:00", restart=False)

    quickLaunchApps = ConfigItem("Toolbar", "QuickLaunchApps", [], restart=False)
    toolbarOrder = ConfigItem(
        "Toolbar",
        "ToolbarOrder",
        [
            "select",
            "pen",
            "eraser",
            "spotlight",
            "board_in_board",
            "timer",
            "clear",
            "apps",
        ],
        restart=False,
    )
    disabledTools = ConfigItem("Toolbar", "DisabledTools", [], restart=False)

    resourceMonitorInterval = OptionsConfigItem(
        "Notifications",
        "ResourceMonitorInterval",
        300,
        OptionsValidator([30, 300, 600, 1800, 2700, 3600, 0]),
        restart=False,
    )
    timerNotifyEnabled = ConfigItem(
        "Notifications", "TimerNotifyEnabled", True, BoolValidator()
    )


cfg = Config()

# Path configuration using the new ROOT_DIR
if getattr(sys, "frozen", False):
    # For packaged builds, settings.json lives next to the EXE (usually)
    # But for Nuitka onefile, we want it next to the EXE, not in temp dir.
    _exe_dir = os.path.dirname(sys.executable)
    SETTINGS_PATH = os.path.join(_exe_dir, "settings.json")
    PLUGINS_DIR = os.path.join(ROOT_DIR, "plugins")
else:
    # Dev mode: settings.json in root, external plugins in plugins/external/
    SETTINGS_PATH = os.path.join(ROOT_DIR, "settings.json")
    PLUGINS_DIR = os.path.join(ROOT_DIR, "plugins", "external")

if not os.path.exists(PLUGINS_DIR):
    try:
        os.makedirs(PLUGINS_DIR)
    except:
        pass

def get_active_settings_path():
    settings_path = SETTINGS_PATH
    settings_dir = os.path.dirname(settings_path)
    active_marker = os.path.join(settings_dir, "_active")
    if not os.path.exists(active_marker):
        return settings_path
    try:
        with open(active_marker, "r", encoding="utf-8") as f:
            profile_name = f.read().strip()
        if not profile_name or profile_name == "default":
            return settings_path
        profile_path = os.path.join(settings_dir, profile_name + ".json")
        if not os.path.exists(profile_path):
            return settings_path
        return profile_path
    except Exception:
        return settings_path

FIRST_RUN = not os.path.exists(SETTINGS_PATH)



def _load_settings_json():
    settings_path = SETTINGS_PATH
    if not os.path.exists(settings_path):
        return {}
    try:
        with open(settings_path, "rb") as f:
            raw = f.read()
    except Exception:
        return {}
    for enc in ("utf-8-sig", "utf-8", "gbk"):
        try:
            text = raw.decode(enc)
        except UnicodeDecodeError:
            continue
        try:
            data = json.loads(text)
        except Exception:
            continue
        return data if isinstance(data, dict) else {}
    return {}


def _set_run_at_startup(enabled: bool):
    """设置或取消开机自启 (Windows 注册表)"""
    if sys.platform != "win32":
        return

    app_name = "Luminalium"
    try:
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Run",
            0,
            winreg.KEY_SET_VALUE | winreg.KEY_QUERY_VALUE,
        ) as key:
            if enabled:
                if getattr(sys, "frozen", False):
                    app_path = sys.executable
                else:
                    return

                winreg.SetValueEx(
                    key, app_name, 0, winreg.REG_SZ, f'"{app_path}" --autostart'
                )
            else:
                return
    except Exception as e:
        print(f"Error setting startup: {e}")


def _set_run_at_startup(enabled: bool):
    platform_set_run_at_startup(bool(enabled))


def _on_run_at_startup_changed(enabled):
    platform_set_run_at_startup(bool(enabled))
    _save_cfg()


def _on_register_url_protocol_changed():
    _save_cfg()
    try:
        enabled = cfg.registerUrlProtocol.value
        if enabled:
            from main import register_url_protocol
            register_url_protocol()
        else:
            from main import unregister_url_protocol
            unregister_url_protocol()
    except Exception as e:
        print(f"Error changing URL protocol registration: {e}")


import sys as _sys


def _get_system_is_dark() -> bool:
    """Read the actual OS-level dark/light setting from Windows registry.
    Completely independent of qfluentwidgets' qconfig.theme."""
    if _sys.platform == "win32":
        try:
            import winreg
            key = winreg.OpenKey(
                winreg.HKEY_CURRENT_USER,
                r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            )
            value, _ = winreg.QueryValueEx(key, "AppsUseLightTheme")
            winreg.CloseKey(key)
            return value == 0  # 0 = dark, 1 = light
        except Exception:
            pass
    # Fallback for non-Windows or registry read failure
    try:
        from qfluentwidgets import isDarkTheme
        return isDarkTheme()
    except Exception:
        return False


def _apply_theme_and_color(theme_value):
    if isinstance(theme_value, Theme):
        qconfig.theme = theme_value
    else:
        try:
            qconfig.theme = Theme(theme_value)
        except Exception:
            qconfig.theme = Theme.LIGHT

    # Resolve AUTO to actual system theme for color AND qfluentwidgets engine
    if qconfig.theme == Theme.AUTO:
        qconfig.theme = Theme.DARK if _get_system_is_dark() else Theme.LIGHT
        print(f"[Config] _apply_theme_and_color: AUTO resolved to qconfig.theme={qconfig.theme}", flush=True)

    if qconfig.theme == Theme.DARK:
        setThemeColor("#E1EBFF")
    else:
        setThemeColor("#3275F5")


_apply_theme_and_color(cfg.themeMode.value)


_save_debounce_timer = None


def _do_save_cfg():
    settings_path = SETTINGS_PATH
    old_data = _load_settings_json()

    qconfig.save()

    new_data = _load_settings_json()

    merged = old_data if isinstance(old_data, dict) else {}
    if isinstance(new_data, dict):
        for cat, cat_val in new_data.items():
            if not isinstance(cat_val, dict):
                merged[cat] = cat_val
                continue
            if cat not in merged or not isinstance(merged[cat], dict):
                merged[cat] = {}
            for key, value in cat_val.items():
                merged[cat][key] = value

    try:
        with open(settings_path, "w", encoding="utf-8") as f:
            json.dump(merged, f, indent=4, ensure_ascii=False)
    except Exception:
        pass


def _ensure_save_debounce_timer():
    global _save_debounce_timer
    if _save_debounce_timer is not None:
        return
    from PySide6.QtCore import QTimer
    from PySide6.QtWidgets import QApplication
    app = QApplication.instance()
    if app is None:
        return
    _save_debounce_timer = QTimer(app)
    _save_debounce_timer.setSingleShot(True)
    _save_debounce_timer.setInterval(500)
    _save_debounce_timer.timeout.connect(_do_save_cfg)


def _save_cfg():
    _ensure_save_debounce_timer()
    if _save_debounce_timer is None:
        _do_save_cfg()
        return
    _save_debounce_timer.start()


def _on_theme_changed(theme):
    _apply_theme_and_color(theme)
    _save_cfg()


def _bind_auto_save():
    cfg.themeMode.valueChanged.connect(_on_theme_changed)
    cfg.themeId.valueChanged.connect(lambda *_: _save_cfg())
    cfg.runAtStartup.valueChanged.connect(_on_run_at_startup_changed)
    cfg.autoShowOverlay.valueChanged.connect(lambda *_: _save_cfg())
    cfg.disableAnimations.valueChanged.connect(lambda *_: _save_cfg())
    cfg.crashAutoHandleEnabled.valueChanged.connect(lambda *_: _save_cfg())
    cfg.crashAutoHandleMode.valueChanged.connect(lambda *_: _save_cfg())
    cfg.showClear.valueChanged.connect(lambda *_: _save_cfg())
    cfg.showSpotlight.valueChanged.connect(lambda *_: _save_cfg())
    cfg.showBoardInBoard.valueChanged.connect(lambda *_: _save_cfg())
    cfg.showTimer.valueChanged.connect(lambda *_: _save_cfg())
    cfg.showToolbarText.valueChanged.connect(lambda *_: _save_cfg())
    cfg.secRandomEnabled.valueChanged.connect(lambda *_: _save_cfg())
    cfg.showStatusBar.valueChanged.connect(lambda *_: _save_cfg())
    cfg.statusBarShowTime.valueChanged.connect(lambda *_: _save_cfg())
    cfg.statusBarShowSeconds.valueChanged.connect(lambda *_: _save_cfg())
    cfg.statusBarShowBattery.valueChanged.connect(lambda *_: _save_cfg())
    cfg.statusBarShowVolume.valueChanged.connect(lambda *_: _save_cfg())
    cfg.statusBarShowNetwork.valueChanged.connect(lambda *_: _save_cfg())
    cfg.statusBarShowMusic.valueChanged.connect(lambda *_: _save_cfg())
    cfg.toolbarPosition.valueChanged.connect(lambda *_: _save_cfg())
    cfg.flipperPosition.valueChanged.connect(lambda *_: _save_cfg())
    cfg.safeArea.valueChanged.connect(lambda *_: _save_cfg())
    cfg.scale.valueChanged.connect(lambda *_: _save_cfg())
    cfg.popWindowScale.valueChanged.connect(lambda *_: _save_cfg())
    cfg.toolbarOpacity.valueChanged.connect(lambda *_: _save_cfg())
    cfg.sidePageOpacity.valueChanged.connect(lambda *_: _save_cfg())
    cfg.syncOpacity.valueChanged.connect(lambda *_: _save_cfg())
    cfg.strictEdgeAlignment.valueChanged.connect(lambda *_: _save_cfg())
    cfg.toolbarAutoHalfCollapse.valueChanged.connect(lambda *_: _save_cfg())
    cfg.uiAccessTopmost.valueChanged.connect(lambda *_: _save_cfg())
    cfg.allowRecording.valueChanged.connect(lambda *_: _save_cfg())
    cfg.zOrderCheckInterval.valueChanged.connect(lambda *_: _save_cfg())
    cfg.autoHandleInk.valueChanged.connect(lambda *_: _save_cfg())
    cfg.penMode.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfPenWidth.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfHighlightWidth.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfEraserWidth.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfHighlightOpacity.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfPenColor.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfHighlightColor.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfPenFrameRateMode.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfPenPenEffect.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfPenPalmErase.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfPenWidthPresetIndex.valueChanged.connect(lambda *_: _save_cfg())
    cfg.selfEraserWidthPresetIndex.valueChanged.connect(lambda *_: _save_cfg())
    cfg.pageTurnRateLimit.valueChanged.connect(lambda *_: _save_cfg())
    cfg.overlayScreen.valueChanged.connect(lambda *_: _save_cfg())
    cfg.splashMode.valueChanged.connect(lambda *_: _save_cfg())
    cfg.splashStyle.valueChanged.connect(lambda *_: _save_cfg())
    cfg.splashStartTime.valueChanged.connect(lambda *_: _save_cfg())
    cfg.splashEndTime.valueChanged.connect(lambda *_: _save_cfg())
    cfg.disabledTools.valueChanged.connect(lambda *_: _save_cfg())
    cfg.resourceMonitorInterval.valueChanged.connect(lambda *_: _save_cfg())
    cfg.timerNotifyEnabled.valueChanged.connect(lambda *_: _save_cfg())
    cfg.registerUrlProtocol.valueChanged.connect(lambda *_: _on_register_url_protocol_changed())
    cfg.useNativeTitleBar.valueChanged.connect(lambda *_: _save_cfg())
    # cfg.toolbarLayout.valueChanged.connect(lambda *_: _save_cfg())


_bind_auto_save()
try:
    if cfg.runAtStartup.value:
        platform_set_run_at_startup(True)
except Exception:
    pass


def reload_cfg():
    qconfig.load(SETTINGS_PATH, cfg)
    _apply_theme_and_color(cfg.themeMode.value)
