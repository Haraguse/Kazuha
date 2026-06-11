"""
System theme watcher - detect system color mode changes and apply them in real-time
"""
import sys
from typing import Optional, Callable
from PySide6.QtCore import QObject, QThread, QTimer, Signal
from PySide6.QtWidgets import QWidget
from qfluentwidgets import Theme


class SystemThemeWatcher(QObject):
    """Monitor system theme changes and emit signals when detected"""
    
    theme_changed = Signal(bool)  # True for dark, False for light
    
    def __init__(self, parent: Optional[QObject] = None):
        super().__init__(parent)
        self._timer = QTimer(self)
        self._timer.timeout.connect(self._check_theme)
        self._last_is_dark: Optional[bool] = None
        self._check_interval = 500  # Check every 500ms
        
        # Platform-specific initialization
        self._startup_delay = 1000  # 1 second delay before starting monitoring
        QTimer.singleShot(self._startup_delay, self._start_polling)
        
    def _start_polling(self):
        """Start the polling timer after startup delay"""
        if sys.platform == "win32":
            self._setup_windows_listener()
        else:
            self._setup_unix_listener()
            
    def _setup_windows_listener(self):
        """Setup Windows-specific theme change detection via WM_SETTINGCHANGE"""
        try:
            # For Windows, we'll use polling with isDarkTheme()
            # A more advanced approach would be to use WM_SETTINGCHANGE with native messages
            self._timer.start(self._check_interval)
            print("[SystemThemeWatcher] Windows theme watcher started (polling)", flush=True)
        except Exception as e:
            print(f"[SystemThemeWatcher] Failed to setup Windows listener: {e}", flush=True)
            
    def _setup_unix_listener(self):
        """Setup Linux/Unix-specific theme change detection via D-Bus or file monitoring"""
        try:
            # For Linux, we'll use polling with isDarkTheme()
            # A more advanced approach would use D-Bus signals from desktop environments
            self._timer.start(self._check_interval)
            print("[SystemThemeWatcher] Unix theme watcher started (polling)", flush=True)
        except Exception as e:
            print(f"[SystemThemeWatcher] Failed to setup Unix listener: {e}", flush=True)
            
    def _check_theme(self):
        """Check if system theme has changed"""
        try:
            from ppt_assistant.core.config import _get_system_is_dark
            current_is_dark = _get_system_is_dark()
            
            if self._last_is_dark is None:
                # First check, store the value
                self._last_is_dark = current_is_dark
                return
                
            # If theme changed, emit signal
            if current_is_dark != self._last_is_dark:
                self._last_is_dark = current_is_dark
                print(
                    f"[SystemThemeWatcher] System theme changed to {'dark' if current_is_dark else 'light'}",
                    flush=True
                )
                self.theme_changed.emit(current_is_dark)
        except Exception as e:
            print(f"[SystemThemeWatcher] Error checking theme: {e}", flush=True)
            
    def start(self):
        """Start watching for theme changes"""
        if not self._timer.isActive():
            print("[SystemThemeWatcher] Starting theme watcher", flush=True)
            self._timer.start(self._check_interval)
            
    def stop(self):
        """Stop watching for theme changes"""
        if self._timer.isActive():
            self._timer.stop()
            print("[SystemThemeWatcher] Theme watcher stopped", flush=True)


class SystemThemeWatcherManager:
    """Manager for system theme watching integrated with app config"""
    
    def __init__(self, apply_theme_callback: Callable, get_config_value: Callable, on_theme_changed_callback: Callable = None):
        """
        Initialize the theme watcher manager
        
        Args:
            apply_theme_callback: Function to apply theme (called when system theme changes)
            get_config_value: Function to get current theme mode config value
            on_theme_changed_callback: Optional callback to call when theme changes
        """
        self._watcher = SystemThemeWatcher()
        self._apply_theme_callback = apply_theme_callback
        self._get_config_value = get_config_value
        self._on_theme_changed_callback = on_theme_changed_callback
        self._watcher.theme_changed.connect(self._on_system_theme_changed)
        
    def _on_system_theme_changed(self, is_dark: bool):
        """Handle system theme change"""
        try:
            from ppt_assistant.core.config import cfg, Theme
            
            # Only apply if theme mode is set to AUTO
            if cfg.themeMode.value != Theme.AUTO:
                return
                
            # Apply the new theme
            new_theme = Theme.DARK if is_dark else Theme.LIGHT
            self._apply_theme_callback(new_theme)
            
            # Call the custom callback if provided
            if self._on_theme_changed_callback:
                self._on_theme_changed_callback()
            
        except Exception as e:
            print(f"[SystemThemeWatcherManager] Error on theme change: {e}", flush=True)
            
    def start(self):
        """Start theme watching"""
        self._watcher.start()
        
    def stop(self):
        """Stop theme watching"""
        self._watcher.stop()
