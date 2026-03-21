import os

from PySide6.QtCore import QFileInfo
from PySide6.QtWidgets import QFileDialog, QFileIconProvider, QMessageBox

from plugins.interface import AssistantPlugin
from ppt_assistant.core.config import _save_cfg, cfg
from ppt_assistant.core.platform_integration import get_quick_launch_dialog_filter, open_path


class AppLauncherPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)

    def get_name(self):
        return "工具栏固定项"

    def get_icon(self):
        return "apps.svg"

    def get_apps(self):
        return cfg.quickLaunchApps.value

    def add_app(self):
        file_path, _ = QFileDialog.getOpenFileName(
            None,
            "选择要添加的项目",
            "",
            get_quick_launch_dialog_filter(),
        )
        if not file_path:
            return
        name = os.path.splitext(os.path.basename(file_path))[0]
        apps = cfg.quickLaunchApps.value.copy()
        if any(app["path"] == file_path for app in apps):
            return

        apps.append({
            "name": name,
            "path": file_path,
            "icon": "",
        })
        cfg.quickLaunchApps.value = apps
        _save_cfg()

        if self.context and hasattr(self.context, "update_toolbar"):
            self.context.update_toolbar()

    def rename_app(self, path, new_name):
        apps = cfg.quickLaunchApps.value.copy()
        for app in apps:
            if app["path"] == path:
                app["name"] = new_name
                break
        cfg.quickLaunchApps.value = apps
        _save_cfg()
        if self.context and hasattr(self.context, "update_toolbar"):
            self.context.update_toolbar()

    def remove_app(self, path):
        apps = cfg.quickLaunchApps.value.copy()
        apps = [app for app in apps if app["path"] != path]
        cfg.quickLaunchApps.value = apps
        _save_cfg()
        if self.context and hasattr(self.context, "update_toolbar"):
            self.context.update_toolbar()

    def get_apps(self):
        return cfg.quickLaunchApps.value

    def get_app_icon(self, path):
        if not os.path.exists(path):
            return None
        file_info = QFileInfo(path)
        icon_provider = QFileIconProvider()
        icon = icon_provider.icon(file_info)
        if not icon.isNull():
            return icon.pixmap(32, 32)
        return None

    def execute_app(self, path):
        try:
            if os.path.exists(path):
                open_path(path)
            else:
                QMessageBox.warning(None, "错误", f"找不到文件: {path}")
        except Exception as e:
            QMessageBox.critical(None, "错误", f"打开失败: {str(e)}")

    def execute(self):
        self.add_app()
