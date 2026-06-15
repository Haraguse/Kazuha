import ctypes
import html
import os
import subprocess
import sys
import traceback
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


def configure_current_process_for_notifications(
    app_id: str = WINDOWS_TOAST_APP_ID,
) -> bool:
    if sys.platform != "win32":
        return False
    try:
        ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID(app_id)
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

    configure_current_process_for_notifications(app_id=app_id)

    if not target_path:
        target_path, work_dir, args, icon_path = resolve_default_notification_target()

    shortcut_path = resolve_default_notification_shortcut_path()
    try:
        result = create_windows_notification_shortcut(
            target_path=target_path,
            shortcut_path=shortcut_path,
            work_dir=work_dir,
            icon_path=icon_path,
            args=args,
            app_name=app_name,
            app_id=app_id,
        )
        if result and os.path.isfile(result):
            print(f"[WindowsNotification] ショートカット作成完了: {result}")
        else:
            print(
                f"[WindowsNotification] ショートカット作成が空かファイル不在: {shortcut_path}"
            )
        return result
    except Exception as e:
        print(
            f"[WindowsNotification] ショートカット作成失敗 ({shortcut_path}): {e}"
        )
        traceback.print_exc()
        return None


def _send_toast_via_winrt(
    app_id: str,
    xml_content: str,
) -> bool:
    """winrt 経由で toast を送信。失敗時は False を返す"""
    try:
        from winrt.windows.data.xml.dom import XmlDocument
        from winrt.windows.ui.notifications import (
            ToastNotification,
            ToastNotificationManager,
        )

        # winrt のバージョンによって create_toast_notifier の呼び出し方が異なる
        # エラー情報を全部出すために全パターン試す
        notifier = None
        errors: list[str] = []
        for pattern, fn in enumerate(
            [
                lambda: ToastNotificationManager.create_toast_notifier(app_id),
                lambda: ToastNotificationManager.create_toast_notifier(),
            ],
            start=1,
        ):
            try:
                notifier = fn()
                break
            except TypeError as e:
                errors.append(f"  pattern {pattern} (TypeError): {e}")
            except OSError as e:
                errors.append(f"  pattern {pattern} (OSError): {e}")
            except Exception as e:
                errors.append(f"  pattern {pattern} ({type(e).__name__}): {e}")

        if notifier is None:
            print("[WindowsNotification] winrt create_toast_notifier 全滅:")
            for err in errors:
                print(err)
            return False

        xml = XmlDocument()
        xml.load_xml(xml_content)
        notifier.show(ToastNotification(xml))
        return True
    except ImportError:
        print("[WindowsNotification] winrt パッケージ未導入、PowerShell にフォールバック")
        return False
    except Exception as e:
        print(f"[WindowsNotification] winrt toast 送信失敗: {e}")
        traceback.print_exc()
        return False


def _send_toast_via_powershell(
    app_id: str,
    xml_content: str,
) -> bool:
    """PowerShell 経由で toast を送信。Windows 10/11 の WinRT API を直接使う"""
    try:
        escaped_xml = xml_content.replace("'", "''")
        # PowerShell スクリプト: WinRT API を直接叩いて toast を表示
        ps_script = f'''
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml('{escaped_xml}')

$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{app_id}')
$toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
$notifier.Show($toast)
'''
        result = subprocess.run(
            [
                "powershell.exe",
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                ps_script,
            ],
            capture_output=True,
            text=True,
            timeout=15,
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        if result.returncode != 0:
            stderr = (result.stderr or "").strip()
            stdout = (result.stdout or "").strip()
            print(
                "[WindowsNotification] PowerShell toast 失敗 "
                f"(exit={result.returncode})"
            )
            if stderr:
                print(f"  stderr: {stderr[:500]}")
            if stdout:
                print(f"  stdout: {stdout[:500]}")
            return False
        return True
    except FileNotFoundError:
        print("[WindowsNotification] PowerShell が見つからん")
        return False
    except subprocess.TimeoutExpired:
        print("[WindowsNotification] PowerShell toast がタイムアウト")
        return False
    except Exception as e:
        print(f"[WindowsNotification] PowerShell toast 例外: {e}")
        traceback.print_exc()
        return False


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

    # ショートカット登録を先に済ませる
    try:
        ensure_windows_notification_registration(
            target_path=target_path,
            work_dir=work_dir,
            icon_path=icon_path,
            args=args,
            app_name=app_name,
            app_id=app_id,
        )
    except Exception as e:
        print(f"[WindowsNotification] 登録失敗: {e}")
        traceback.print_exc()

    # Toast XML を構築
    escaped_title = html.escape(str(title or ""), quote=True)
    escaped_message = html.escape(str(message or ""), quote=True)
    launch_attr = ""
    if launch:
        launch_attr = (
            f' activationType="protocol"'
            f' launch="{html.escape(str(launch), quote=True)}"'
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

    xml_content = (
        f"<toast{launch_attr}>"
        "<visual>"
        '<binding template="ToastGeneric">'
        f"<text>{escaped_title}</text>"
        f"<text>{escaped_message}</text>"
        "</binding>"
        "</visual>"
        f"{actions_xml}"
        "</toast>"
    )

    # 送信: winrt → PowerShell の順にフォールバック
    if _send_toast_via_winrt(app_id, xml_content):
        return True
    if _send_toast_via_powershell(app_id, xml_content):
        return True

    print("[WindowsNotification] すべての通知手段が失敗した")
    return False


def resolve_update_marker_path() -> Path:
    if getattr(sys, "frozen", False):
        return Path(sys.executable).parent / "_internal" / ".version"
    return Path(resolve_project_root_dir()) / "_internal" / ".version"
