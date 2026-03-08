import sys

class SystemAPI:
    """
    Abstract base class for system-level operations.
    """
    def get_media_info(self):
        """
        Get current media playing info (title, artist, status).
        Returns: dict with keys 'title', 'artist', 'status', 'thumbnail' (optional)
        """
        return {"title": "", "artist": "", "status": "Stopped"}

    def get_file_icon(self, path):
        """
        Get file icon/thumbnail as base64 string.
        """
        return None

    def get_ppt_slideshow_hwnd(self):
        """
        Get the HWND of the PowerPoint slideshow window.
        """
        return 0
    
    def start_focus_watcher(self, callback):
        """
        Start watching for window focus changes.
        """
        pass
    
    def stop_focus_watcher(self):
        """
        Stop watching for window focus changes.
        """
        pass

    def get_display_scale(self):
        """
        Get display scaling factor.
        """
        return 1.0

    def pin_to_taskbar(self, enable):
        """
        Pin/unpin app to taskbar.
        """
        pass

    def pin_to_start(self, enable):
        """
        Pin/unpin app to start menu.
        """
        pass

    def get_system_fonts(self):
        """
        Get list of installed system fonts.
        """
        return []

    def set_window_attribute(self, hwnd, attribute, value):
        """
        Set platform-specific window attribute (e.g. DWM).
        """
        pass
