import ctypes
import html
import os
import sys
from pathlib import Path

from ppt_assistant.core.app_icon import resolve_app_icon_path, resolve_project_root_dir
from ppt_assistant.core.i18n import get_language


WINDOWS_TOAST_APP_ID = "Kazuha.Luminalium"
WINDOWS_TOAST_ACTIVATOR_CLSID = "{E13C0D23-CCBC-4E12-931B-D9CC2EEE27E4}"
_SHORTCUT_FILENAME = "Luminalium.lnk"
_DISPLAY_NAMES = {
    "zh-CN": "荧素万演",
    "zh-TW": "Luminalium",
    "yue-HK": "Luminalium",
    "ja-JP": "ルマイナリウム",
    "en-US": "Luminalium",
    "ug-CN": "Luminalium",
}


def get_windows_notification_app_id() -> str:
    return WINDOWS_TOAST_APP_ID


def get_windows_notification_app_name(language: str | None = None) -> str:
    lang = str(language or get_language() or "zh-CN").strip()
    if lang in _DISPLAY_NAMES:
        return _DISPLAY_NAMES[lang]
    if lang.startswith("ja"):
        return _DISPLAY_NAMES["ja-JP"]
    if lang.startswith("en"):
        return _DISPLAY_NAMES["en-US"]
    return _DISPLAY_NAMES["zh-CN"]


def configure_current_process_for_notifications() -> bool:
    if sys.platform != "win32":
        return False
    try:
        ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID(
            WINDOWS_TOAST_APP_ID
        )
        return True
    except Exception:
        return False


def resolve_default_notification_shortcut_path() -> str:
    programs_dir = os.path.join(
        os.environ.get("APPDATA", ""),
        "Microsoft",
        "Windows",
        "Start Menu",
        "Programs",
    )
    return os.path.join(programs_dir, _SHORTCUT_FILENAME)


def resolve_default_notification_target() -> tuple[str, str | None, str | None, str | None]:
    icon_path = resolve_app_icon_path()
    if getattr(sys, "frozen", False):
        exe_path = os.path.abspath(sys.executable)
        work_dir = os.path.dirname(exe_path)
        return exe_path, work_dir, None, icon_path

    root_dir = resolve_project_root_dir()
    python_exe = os.path.abspath(sys.executable)
    main_py = os.path.join(root_dir, "main.py")
    args = f'"{main_py}"'
    return python_exe, root_dir, args, icon_path


def _set_shortcut_property(
    shortcut_path: str,
    key_name: str,
    value,
    value_type: int,
) -> None:
    import pythoncom
    from pywintypes import IID
    from win32com.propsys import propsys
    from win32com.shell import shellcon

    key = propsys.PSGetPropertyKeyFromName(key_name)
    store = propsys.SHGetPropertyStoreFromParsingName(
        shortcut_path,
        None,
        shellcon.GPS_READWRITE,
        propsys.IID_IPropertyStore,
    )
    prop_value = value
    if value_type == pythoncom.VT_CLSID and isinstance(value, str):
        prop_value = IID(value)
    store.SetValue(key, propsys.PROPVARIANTType(prop_value, value_type))
    store.Commit()


def create_windows_notification_shortcut(
    target_path: str,
    shortcut_path: str | None = None,
    work_dir: str | None = None,
    icon_path: str | None = None,
    args: str | None = None,
    app_name: str | None = None,
    app_id: str = WINDOWS_TOAST_APP_ID,
) -> str:
    import win32com.client

    display_name = str(app_name or get_windows_notification_app_name()).strip()
    shortcut_path = shortcut_path or resolve_default_notification_shortcut_path()
    shortcut_dir = os.path.dirname(shortcut_path)
    if shortcut_dir:
        os.makedirs(shortcut_dir, exist_ok=True)

    shell = win32com.client.Dispatch("WScript.Shell")
    shortcut = shell.CreateShortCut(shortcut_path)
    shortcut.TargetPath = target_path
    shortcut.Description = display_name
    if work_dir:
        shortcut.WorkingDirectory = work_dir
    if icon_path:
        shortcut.IconLocation = icon_path
    if args:
        shortcut.Arguments = args
    shortcut.Save()

    import pythoncom

    _set_shortcut_property(
        shortcut_path, "System.AppUserModel.ID", app_id, pythoncom.VT_BSTR
    )
    _set_shortcut_property(shortcut_path, "System.Title", display_name, pythoncom.VT_BSTR)
    _set_shortcut_property(
        shortcut_path,
        "System.AppUserModel.ToastActivatorCLSID",
        WINDOWS_TOAST_ACTIVATOR_CLSID,
        pythoncom.VT_CLSID,
    )
    return shortcut_path


def ensure_windows_notification_registration(
    target_path: str | None = None,
    work_dir: str | None = None,
    icon_path: str | None = None,
    args: str | None = None,
    app_name: str | None = None,
    app_id: str = WINDOWS_TOAST_APP_ID,
) -> str | None:
    if sys.platform != "win32":
        return None

    configure_current_process_for_notifications()

    if not target_path:
        target_path, work_dir, args, icon_path = resolve_default_notification_target()

    try:
        return create_windows_notification_shortcut(
            target_path=target_path,
            shortcut_path=resolve_default_notification_shortcut_path(),
            work_dir=work_dir,
            icon_path=icon_path,
            args=args,
            app_name=app_name,
            app_id=app_id,
        )
    except Exception:
        return None


def send_windows_notification(
    title: str,
    message: str,
    *,
    launch: str | None = None,
    buttons: list[dict[str, str]] | None = None,
    app_name: str | None = None,
    app_id: str = WINDOWS_TOAST_APP_ID,
    target_path: str | None = None,
    work_dir: str | None = None,
    icon_path: str | None = None,
    args: str | None = None,
) -> bool:
    if sys.platform != "win32":
        return False

    try:
        ensure_windows_notification_registration(
            target_path=target_path,
            work_dir=work_dir,
            icon_path=icon_path,
            args=args,
            app_name=app_name,
            app_id=app_id,
        )

        from winrt.windows.data.xml.dom import XmlDocument
        from winrt.windows.ui.notifications import (
            ToastNotification,
            ToastNotificationManager,
        )

        xml = XmlDocument()
        launch_attr = ""
        if launch:
            launch_attr = (
                f' activationType="protocol" launch="{html.escape(str(launch), quote=True)}"'
            )
        actions_xml = ""
        if buttons:
            action_items: list[str] = []
            for button in buttons:
                content = str((button or {}).get("content") or "").strip()
                arguments = str((button or {}).get("arguments") or "").strip()
                if not content or not arguments:
                    continue
                activation_type = str(
                    (button or {}).get("activationType") or "protocol"
                ).strip()
                action_items.append(
                    (
                        '<action'
                        f' content="{html.escape(content, quote=True)}"'
                        f' activationType="{html.escape(activation_type, quote=True)}"'
                        f' arguments="{html.escape(arguments, quote=True)}"'
                        " />"
                    )
                )
            if action_items:
                actions_xml = f"<actions>{''.join(action_items)}</actions>"
        xml.load_xml(
            (
                f"<toast{launch_attr}>"
                "<visual>"
                '<binding template="ToastGeneric">'
                f"<text>{html.escape(str(title or ''))}</text>"
                f"<text>{html.escape(str(message or ''))}</text>"
                "</binding>"
                "</visual>"
                f"{actions_xml}"
                "</toast>"
            )
        )
        notifier = ToastNotificationManager.create_toast_notifier(app_id)
        notifier.show(ToastNotification(xml))
        return True
    except Exception as e:
        print(f"[WindowsNotification] Failed to send native toast: {e}")
        return False


def resolve_update_marker_path() -> Path:
    if getattr(sys, "frozen", False):
        return Path(sys.executable).parent / "_internal" / ".version"
    return Path(resolve_project_root_dir()) / "_internal" / ".version"
