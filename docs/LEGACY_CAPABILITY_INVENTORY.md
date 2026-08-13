# Luminalium C# Migration — Legacy Capability Inventory

> **Purpose**: Catalog `plugins/webview_runner.py` capabilities and the remaining legacy runtime surfaces
> **before** they are removed, per plan Task 4: "Catalog `plugins/webview_runner.py` capabilities before
> deletion: page lifecycle, toolbar/window hosting, dialogs, crash UI, titlebar, timer, spotlight, settings,
> logs, onboarding, and bridge behavior."
> **Plan**: `../../.omo/plans/csharp-fluentavalonia-migration.md` Task 4 / Task 24 (retirement).
> **Companion**: [FEATURE_PARITY_MATRIX.md](./FEATURE_PARITY_MATRIX.md) (in-scope rows, exclusions, and deferrals).
>
> **Catalog policy**: every entry lists a legacy surface, its current behavior, its C# destination task, and
> its disposition. No entry here is a WebView port task — WebView/HTML/QML surfaces are cataloged and then
> retired (X-05). Visible behaviors migrate to native FluentAvalonia controls.

---

## 1. `plugins/webview_runner.py` — Capability Catalog (219,420 bytes, ~5,080 lines)

Two classes: `Api(QObject)` at line 798 (host→page QWebChannel bridge, ~170 methods) and
`MainWindow(QWebEngineView)` at line 3966 (hosted window). Module-level helpers support the runner.

### 1.1 Page lifecycle & web-engine management → native lifecycle (Task 12) / registry lifecycle (Task 11)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-01 | WebEngine profile configuration | `_configure_profile` (L347), `_get_shared_profile` (L472), `_cleanup_shared_profile` (L480) | Shared profile + cache isolation; no per-window state leakage | Task 12 window lifecycle; cache handling as native resource policy |
| W-02 | Memory trimming / warmup | `_trim_webengine_memory` (L494), `_release_warmup_placeholder` (L509), `_warmup_webengine` (L527), `_should_defer_initial_load` (L573), `MainWindow._setup_memory_timer/_stop_memory_timer/_on_memory_tick` (L4156-4202) | Deferred/lazy startup; memory pressure handling | Task 12 startup/error states; Task 8 resource policy |
| W-03 | Page load lifecycle | `MainWindow._ensure_pending_load` (L4202), `_on_load_started` (L4210), `_on_load_finished` (L4213), `_force_refresh` (L4951) | Deterministic load/failure states with retry semantics | Task 12 navigation/loading states |
| W-04 | Render-process crash handling | `MainWindow._on_render_process_terminated` (L4217) | Crash → restart flow without losing user context | Task 19 crash-safe error display; Task 12 error states |
| W-05 | Window tag detection | `MainWindow._detect_window_tag` (L4603) | Per-surface identity routing | Task 11 registry identity; Task 12 shell routing |

### 1.2 Toolbar / window hosting → native windows (Tasks 12, 16, 18)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-06 | Frameless hosting window | `MainWindow` (L3966): `_enable_frameless_aero` (L5002), `nativeEvent` (L4833), `_apply_backdrop` (L4940), `mouseMoveEvent` (L4793) | Borderless overlay/window style with DWM backdrop | Task 12 shell/titlebar; Task 16 overlay windows |
| W-07 | Mini mode / fullscreen / maximized | `MainWindow.set_mini_mode` (L4619), `set_fullscreen` (L4682), `Api.set_mini_mode` (L1068), `Api.set_fullscreen` (L1078), `Api.set_maximized` (L1094), `_push_maximized_state` (L5055) | Window state transitions with persisted restore state | Task 12 window management |
| W-08 | Drag / move / resize / placement | `MainWindow.start_window_drag` (L4776), `_center_on_screen` (L4824), move/resize/hide/close events (L5070-5080) | Client-area drag, centering, close-to-hide semantics | Task 12; Task 8 monitor/DPI services |
| W-09 | Custom titlebar injection | `MainWindow._inject_title_bar_html` (L4499), `_inject_title_bar_js` (L4551), `_set_custom_title_bar_visible` (L4763) | Custom titlebar with drag/controls | Task 12 native titlebar (no HTML) |
| W-10 | Toolbar quick-launch hosting | `Api.get_toolbar_icon` (L1177), `Api.get_quick_launch_apps` (L1320), `_attach_quick_launch_icons` (L1297), `get_taskbar_preview` (L1434) | Toolbar items with icons + previews | Task 18 app launcher; Task 16 overlay toolbar |
| W-11 | Window drag/control bridge | `Api.start_window_drag` (L870), `minimize_window` (L880), `toggle_maximize` (L889), `close_window` (L901), `force_close_window` (L941), `_spawn_window_killer` (L956), `_force_destroy_window` (L977), `flash_window` (L993), `set_in_process` (L930) | In-process window control incl. forced destroy fallback | Task 12 window management; Task 8 Win32 interop |

### 1.3 Dialogs → native `ContentDialog` (Task 12)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-12 | Generic dialog lifecycle | `Api.create_dialog` (L2278), `get_dialog_data` (L2360), `on_confirm` (L2364), `on_confirm_with_value` (L2372), `on_cancel` (L2385) | Modal confirm/cancel/value dialogs with async result | Task 12 async `ContentDialog.ShowAsync()` |
| W-13 | Font warning dialog | `Api.show_font_warning` (L2312) | Missing-font warnings with actionable result | Task 12 dialogs; Task 19 font settings |
| W-14 | Host-side dialog helpers | `main.py::show_webview_dialog` (L1666), `ppt_assistant/ui/dialog.py`, `dialog_runtime.py` (13 KB), `dialog.html` (57 KB) | Dialog runtime shared across surfaces | Task 12 native dialog service |

### 1.4 Crash UI → native crash dialog (Task 19)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-15 | Crash dialog launch/result | `Api.restart_from_crash_dialog` (L2202), `Api.trigger_crash` (L2274), `main.py::CrashHandler._launch_crash_dialog` (L1744), `crash_dialog.html` (25 KB) | Post-crash user choice: restart / exit, silent restart | Task 19 crash-safe display; Task 12 startup error states |
| W-16 | Watchdog | `main.py::CrashHandler.start_watchdog` (L1903), `_run_watchdog_process` (L1990), `_check_hung_windows` (L2050) | Heartbeat + freeze detection + thread dump | Task 12/19 watchdog parity decision (native equivalent) |

### 1.5 Titlebar → native titlebar (Task 12)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-17 | Titlebar drag/controls/injection | `MainWindow._inject_title_bar_html` (L4499), `_inject_title_bar_js` (L4551), `_set_custom_title_bar_visible` (L4763), `titlebar.html` (10.8 KB), `ppt_assistant/ui/titlebar_manager.py` (9 KB), `webview_titlebar_window.py` (8.8 KB) | Custom titlebar with minimize/maximize/close and drag | Task 12 native titlebar |

### 1.6 Timer bridge → native timer (Task 18)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-18 | Timer state bridge | `Api.update_timer` (L1058); `plugins/builtins/timer/plugin.py::_InProcessTimerApiMixin` (L56-109: `get_timer_state/start_timer/pause_timer/resume_timer/stop_timer/finish_timer/add_time/update_timer`); `timer.html` (117 KB); `ppt_assistant/core/timer_manager.py::TimerManager` (L79) | Countdown lifecycle (start/pause/resume/stop/finish/add) with notification cues | Task 18 native timer view; Task 7 config for persistence |

### 1.7 Spotlight bridge → native spotlight (Task 18)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-19 | Spotlight overlay control | `overlay.py::OverlayBridge.toggleSpotlight` (L344), `spotlightSetTransparent` (L445), `spotlightDrawDemoFrame`/`spotlightClearDemoFrame` (L452/459), `overlay.py` spotlight mask logic; `plugins/builtins/spotlight/spotlight_window.py` (17.8 KB) | Region highlight, lights-off dimming, magnifier | Task 18 native spotlight window |

### 1.8 Settings bridge → native settings (Tasks 19, 7, 14)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-20 | Settings read/write bridge | `Api.update_settings` (L1021), `Api.get_settings` (L1114), `Api.get_self_pen_settings` (L1129), `Api.save_setting` (L1514), `_get_settings_path/_get_active_settings_path` (L1269/1279), `_get_settings_reset_marker_path` (L1293) | Category/key setting mutation with runtime refresh | Task 7 config service; Task 19 settings UI |
| W-21 | Theme preview | `Api.preview_theme` (L1608), `Api.get_overlay_themes` (L1169), `Api.get_splash_styles` (L1173) | Preview without committing | Task 14 theme service; Task 19 settings |
| W-22 | Diagnostic info | `Api.get_diagnostic_info` (L1157), `plugins/builtins/settings/diagnostic_info.py` (7.4 KB) | Version/platform/system diagnostics | Task 19 logs/settings; Task 6 version identity |
| W-23 | App control from settings | `Api.restart_app` (L2186), `restart_and_open_settings` (L2190), `quit_app` (L2194), `settings/plugin.py::quit_app_for_update` (L422), `navigate_to_section` (L435) | Restart/quit with deep-link to settings page | Task 12 app lifecycle; Task 19 settings navigation |
| W-24 | Onboarding state reset | `Api.clear_onboarding_pending_actions` (L2198), `reset_to_pre_onboarding_state` (L2221) | Onboarding completion + reset | Task 19 onboarding state (persisted) |
| W-25 | Import settings (legacy) | `Api.import_settings` (L2004) | **Excluded**: automatic Python config import (X-04) | None — deferred |

### 1.9 Logs bridge → native logs (Task 19)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-26 | Log viewing/export bridge | `plugins/builtins/logs/plugin.py::LogsPlugin`; `logs.html` (49 KB); `ppt_assistant/core/log_manager.py` (10.8 KB); `Api.get_diagnostic_info` (L1157) | Filter, view, export logs + system info | Task 19 native logs view |

### 1.10 Onboarding bridge → native onboarding (Task 19)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-27 | Onboarding flow | `plugins/builtins/onboarding/plugin.py::OnboardingPlugin` (execute/preview L166); `onboarding.html` (219 KB); `main.py::_start_onboarding_wait_loop/_check_onboarding_closed` (L2894/2903) | First-run guided setup with completion + restart markers | Task 19 native onboarding; Task 7 config for completion state |

### 1.11 Bridge protocol (QWebChannel) → native service contracts (Tasks 11, 12)

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-28 | Host↔page bridge | `Api(QObject)` (L798) exposed via `plugins/qwebchannel.js` (8 KB); `MainWindow` page wiring | Command routing with typed payloads and error isolation | Task 11 plugin contract; Task 12 view-model-first command routing |
| W-29 | Global API surface | All `Api` methods (window control, settings, profiles, backups, quick-launch, storage, fonts, screens, version) | Service boundaries with typed results | Distributed to Tasks 7, 8, 11, 12, 18, 19 per matrix rows |

### 1.12 Module-level helpers

| # | Capability | Legacy implementation | Behavior to preserve conceptually | C# destination |
|---|---|---|---|---|
| W-30 | Theme/window style helpers | `_get_windows_dark_mode` (L237), `_resolve_theme_dark` (L251), `_apply_window_theme` (L262), `_animations_disabled` (L304), `MainWindow.update_theme_mode` (L4956), `_apply_page_background` (L4271), `_inject_custom_border` (L4319) | Dark/light window styling + animation preference | Task 8 platform; Task 14 theme resources |
| W-31 | Asset resolution | `_resolve_logo_ico_path` (L84), `_resolve_logo_svg_path` (L101), `_resolve_misans_font_path` (L126), `_load_app_icon` (L230) | Product icon/font identity | Task 6 assets/identity; Task 12 resources |
| W-32 | User data discovery | `_get_user_root_dir` (L155), `_list_user_themes` (L162), `_list_user_splashes` (L198) | User themes/splashes discovery | Task 7 config; Task 12 splash; Task 14 themes |
| W-33 | Startup shortcuts | `_create_shortcut` (L647), `_set_run_at_startup` (L665), `_pin_to_start` (L688), `_pin_to_taskbar` (L710) | Autostart/pinning | Task 8 platform services |
| W-34 | Platform detection | `_maybe_add_vxkex_path` (L313), `_is_windows7` (L331), `_is_win11` (L339) | Version-specific behaviors (Win7 legacy compat) | Task 8; Task 2 floor validation (Win7 handling retired with WebEngine) |
| W-35 | Profiles & backups | `Api.list_profiles/create_profile/switch_profile/delete_profile/rename_profile/get_active_profile` (L1668-1831); `list_backups/create_backup/restore_backup/delete_backup/rename_backup` (L1863-2002); registry-backed profile index (L1644-1666) | Profile switching + settings backups | Task 7 config service (profiles/backups) |
| W-36 | Storage & screens | `Api.get_storage_info` (L2836), `clean_directory` (L2943), `get_screen_list` (L2975), `get_wallpaper_path` (L1507) | Storage accounting, cache cleanup, screen enumeration | Task 8 platform services; Task 7 config cache policy |
| W-37 | Fonts | `Api.get_system_fonts` (L1201), `get_font_preview_sample` (L1245) | System font enumeration + preview | Task 19 font settings |
| W-38 | Version/platform info | `Api.get_version` (L1153), `get_platform` (L1165) | Opaque version string display | Task 6 version metadata |

---

## 2. Legacy Surface Inventory (outside `webview_runner.py`)

| # | Legacy surface | File (size) | Key behavior | C# destination | Disposition |
|---|---|---|---|---|---|
| L-01 | Presentation overlay | `ppt_assistant/ui/overlay.py` (111 KB): `OverlayBridge` (L206: prevPage/nextPage/gotoSlide/clearScreen/endShow/mediaPlayPause/mediaPrev/mediaNext/inkPromptResult/toggleSpotlight/toggleBoard/toggleTimer/secRandomQuickDraw/launchApp/updateMask/showStrokeBg/requestThumbnail/startBackgroundThumbnailCaching), `InkPromptWindow` (L522), `LastSlideExitPromptWindow` (L688) | Slideshow overlay: navigation, annotation, zoom, spotlight/board/timer toggles, ink-keep prompt, media control, thumbnails | Task 16 (native overlay); ink prompt → Task 12 dialogs | In scope (native); `WaylandFallbackOverlayWindow` (L803) deferred (X-01) |
| L-02 | Overlay HTML surface | `ppt_assistant/ui/overlay.html` (246 KB) | Touch toolbar + canvas markup (HTML/CSS/JS) | Task 16 | Redesign natively; never embedded (X-05) |
| L-03 | PPT/WPS COM boundary | `ppt_assistant/core/ppt_monitor.py` (131 KB): `PPTWorker` (L139: go_next L2456, go_previous L2526, clear_screen L2588, end_show L2711, end_show_with_ink_choice L2749, set_pointer_type L2802, set_pen_color L2942, go_to_slide L2992, export_slide_thumbnail L3033), `PPTMonitor` (L3056), slideshow window discovery (L390-531), COM message filter (L1564), WPS bridge integration (L1182-1456) | Active app detection, slide navigation, ink/annotation, zoom, thumbnails, video state | Task 15 (C# boundary with fake adapters) | In scope; Linux xdotool paths (L878-1091) deferred (X-01) |
| L-04 | Windows system/SMTC API | `ppt_assistant/core/system/windows.py` (24 KB): `WindowsSystemAPI` (L77), SMTC worker launch/read (L176-485), slideshow HWND, focus watcher, fonts | SMTC JSON-line worker, window discovery | Task 8 (platform), Task 9 (SMTC absorb) | In scope |
| L-05 | System API base | `ppt_assistant/core/system/base.py` (2.3 KB) | Platform abstraction shape | Task 8 service interfaces | In scope (C# interfaces) |
| L-06 | Linux system API | `ppt_assistant/core/system/linux.py` (27 KB) | Linux-specific behaviors | None | Deferred (X-01): staged after Windows-first delivery |
| L-07 | Tray | `ppt_assistant/ui/tray.py` (40 KB): `SystemTray` (L545), native/fallback menus (L689/794), confirm flyouts (L479-999), `show_message` (L1083) | Tray menu, timer text, restart/exit confirm, notifications | Task 12 (tray) | In scope |
| L-08 | Splash | `main.py::StartupSplash` (L1077), `SplashProgressBar` (L1011), `user/splash/*`, `ppt_assistant/ui/splash.html` (8.5 KB) | Progress splash with user packages | Task 12/19 (native splash) | In scope |
| L-09 | Crash + watchdog | `main.py::CrashHandler` (L1692), `_run_watchdog_process` (L1990) | Crash dialog, watchdog, freeze dump | Task 19/12 | In scope |
| L-10 | Update service | `ppt_assistant/core/update_service.py` (16.6 KB): `_is_newer` (L78), `trigger_updater` (L222), `start_update_server` (L327) | Release lookup, download progress, updater trigger | Task 13 | In scope |
| L-11 | Updater app | `scripts/updater/updater.py`: `UpdaterWindow` (L154), `backup_app` (L112), `restore_backup` (L127), `wait_for_mutex` (L59), `force_kill_process_tree` (L97) | Standalone updater with progress UI | Task 13 (native status UI) | In scope |
| L-12 | Notifications | `ppt_assistant/core/windows_notifications.py` (13 KB): AUMID config (L41), registration (L147), WinRT/PowerShell toasts (L189/241), `send_windows_notification` (L298) | Windows toasts | Task 8 | In scope |
| L-13 | WPS bridge host | `ppt_assistant/core/wps_bridge/host.py` (5.9 KB): `WpsBridgeHost` (L12, ports 3892-3902), `protocol.py` (7.6 KB) | WebSocket/JSON-schema host | Task 10 (C# host) | In scope |
| L-14 | System theme watcher | `ppt_assistant/core/system_theme_watcher.py` (5.7 KB): `SystemThemeWatcherManager` (L89) | Dark/light detection | Task 8/14 | In scope |
| L-15 | Monet/theme data | `plugins/monet_utils.py` (5 KB), `ppt_assistant/core/theme_data.py` (25 KB) | Wallpaper/accent extraction, theme tokens | Task 14 | In scope |
| L-16 | Config | `ppt_assistant/core/config.py` (21 KB): `Config(QConfig)` (L58), `reload_cfg` (L560), `config/config.json`, `config/rin_ui.json` | Settings model, profiles, autostart | Task 7 (new schema) | In scope; Python format not imported (X-04) |
| L-17 | Timer manager | `ppt_assistant/core/timer_manager.py` (5.5 KB): `TimerManager` (L79) | Singleton countdown state | Task 18 | In scope |
| L-18 | Plugin contract | `plugins/interface.py` (33 lines): `AssistantPlugin` | Plugin API (name/icon/widget/execute/terminate/context/type) | Task 11 (C# contract) | In scope (translated, not ported) |
| L-19 | Plugin loading | `main.py::_load_plugins` (L2970), `_load_next_builtin_plugin` (L2988), `_load_external_plugins` (L3023) | Builtin + external loading | Task 11 registry (builtins only; external excluded X-02) | In scope / X-02 |
| L-20 | URL protocol/autostart | `main.py::register_url_protocol` (L7), `unregister_url_protocol` (L61), `parse_luminalium_url` (L79), `config.py::_set_run_at_startup` (L333), `platform_integration.py` (9.2 KB) | `luminalium://` protocol, autostart, DWM styles | Task 8 | In scope |
| L-21 | SMTC helper (C#) | `scripts/smtc_helper/Program.cs` (JSON line protocol), `SmtcHelper.csproj` | WinRT media session polling | Task 9 (absorb into solution) | In scope |
| L-22 | QML board | `plugins/builtins/board/Board.qml` (68 KB), `SaveStrokesDialog.qml` (7.2 KB), `board_window.py` (67 KB) | Drawing canvas + save dialog | Task 17 (native canvas) | In scope (replaced, never embedded) |
| L-23 | Linux overlays | `ppt_assistant/ui/LinuxOverlay.qml` (71 KB), `linux_qml_overlay.py` (35.8 KB), `linux_widget_overlay.py` (31.9 KB) | Linux overlay parity | None | Deferred (X-01): QML overlay retired; native Avalonia replacement staged with Linux support |
| L-24 | Experimental rendering | `ppt_assistant/rendering/` (directx9/, directx12/, opengl/, manager, integration_layer, ui, abstract, config, debug, examples/guides) | DirectX/OpenGL capture backends | None | Excluded (X-06; `Luminalium.spec` already excludes `ppt_assistant.rendering`) |
| L-25 | VSTO bridge deploy | `scripts/ppt_vsto_bridge/deploy/Release/20260322_131816/` (`Kazuha.PowerPointBridge.dll`, `Newtonsoft.Json.dll`) | Source-less VSTO artifacts | None | Excluded (X-03) |
| L-26 | External plugins dir | `plugins/external/` (empty) | Dynamic external discovery | None | Excluded (X-02) |
| L-27 | ClassIsland monitor | `ppt_assistant/core/classisland_monitor.py` (7.5 KB); `main.py::_start_classisland_monitor` (L3082) | Third-party companion notification hook | Task 8 (notification service decision); not a README feature | Catalog note — parity decision deferred to Task 8 |
| L-28 | Resource/memory monitors | `ppt_assistant/core/resource_monitor.py` (4.2 KB), `memory_cleaner.py` (10.7 KB); `main.py::_start_resource_monitor` (L3069) | System resource alerts, GC/memory hygiene | Task 8 (optional services) | Catalog note — not a README feature |

---

## 3. `Luminalium.spec` — Packaging Exclusions and Payload Map

`Luminalium.spec` (PyInstaller) is the legacy release manifest. Its content maps to retirement decisions:

| spec element | Value | C# migration disposition |
|---|---|---|
| `datas` — `version.json`, `config`, `plugins`, `icons`, `fonts`, `user` | Product data payloads | Task 6 (version/identity), Task 7 (config), Tasks 12/19 (splash/user data); assets move to `avares://` or publish layout |
| `datas` — `ppt_assistant/ui/*.html` (crash_dialog, dialog, splash, titlebar, titlebar_demo) + `LinuxOverlay.qml` | HTML/QML UI payloads | Redesigned natively (Tasks 12, 16, 19); `LinuxOverlay.qml` retired under deferral X-01 (native Avalonia replacement staged with Linux); HTML never embedded (X-05) |
| `datas` — `ppt_assistant/assets` | Core assets | Task 12 asset wiring |
| `hiddenimports` — `PySide6.QtXml`, `winrt.*` | Python/WinRT runtime imports | Removed with the Python runtime (X-07); WinRT usage absorbed into C# (Task 8 notifications, Task 9 SMTC) |
| `excludes` — `ppt_assistant.rendering` | Experimental rendering backends | Retained as exclusion (X-06); Task 24 removes from release path |
| `excludes` — `PyQt5`, `PyQt6`, `setuptools`, `numpy`, `scipy` | Unused Python deps | N/A (Python runtime removed, X-07) |
| `exe`/`COLLECT` name `Luminalium`, icon `icons/logo.ico` | Product identity | Task 6 preserves name/icon/`Luminalium-Windows.zip` artifact |
| `build_pyinstaller.py` | PyInstaller build driver | Replaced by `dotnet publish` (Task 5, Task 23); never the release source of truth |

---

## 4. Retirement Map (feeds Task 24)

| Legacy path | Retirement action |
|---|---|
| `main.py`, `build_pyinstaller.py`, `Luminalium.spec`, `pyproject.toml`, `requirements.txt`, `uv.lock` | Removed/quarantined from production wiring (X-07) |
| `plugins/webview_runner.py`, `plugins/qwebchannel.js`, `plugins/webview_window_utils.py`, `plugins/in_process_window_handle.py` | Cataloged (this document), then removed from production wiring (X-05, Task 24) |
| `ppt_assistant/ui/*.html`, `plugins/builtins/*/*.html` | Redesigned natively; HTML payloads retired (X-05) |
| `plugins/builtins/board/*.qml`, `plugins/WindowLoadingOverlay.qml`, `ppt_assistant/ui/LinuxOverlay.qml` | QML production/overlay assets retired (Task 17); Linux overlay QML retired, native Avalonia replacement staged with Linux support (X-01) |
| `ppt_assistant/rendering/` | Excluded experimental backends removed from release path (X-06) |
| `plugins/external/`, `main.py::_load_external_plugins` | External discovery removed (X-02) |
| `scripts/ppt_vsto_bridge/deploy/` | Deprecated artifacts remain documented, never consumed (X-03) |
| `.github/workflows/nightly-release.yml` Python/Linux steps | Replaced by Windows-only .NET workflow (Task 5, Task 23); Linux CI staged with Linux deferral (X-01) |
| **Retained** | `Luminalium2WPS/` (protocol client + schemas, unchanged), `version.json`, `scripts/smtc_helper/` (absorbed into solution, Task 9), parity matrix + inventory (this task), C# migration tests |

---

## 5. Validation

- Catalog completeness: 38 `webview_runner` capability rows (W-01..W-38) + 28 legacy surface rows (L-01..L-28), all with a C# destination, explicit exclusion, or deferral.
- Every in-scope destination referenced here exists in the parity matrix with acceptance criteria and evidence slugs.
- Exclusion/deferral terms (`WebView`, `QWebChannel`, `plugins/external`, `Kazuha.PowerPointBridge`, `Python config import`, `Linux QML`, `VSTO`) appear in this document only as catalog notes, exclusions, or deferrals, never as implementation deliverables (scan: `.omo/evidence/task-4-parity-negative.txt`).
