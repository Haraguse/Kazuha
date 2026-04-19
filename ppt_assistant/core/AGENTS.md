# PPT_ASSISTANT CORE KNOWLEDGE BASE

## OVERVIEW
Shared backend/support layer for the desktop app: config, platform integration, monitoring, timers, theme, icons, i18n.

## WHERE TO LOOK
| Task | Location | Notes |
|---|---|---|
| Add/change app settings | `config.py` | QConfig-backed, central truth |
| Translate UI strings | `i18n.py` | Locale mapping and text helpers |
| PPT slideshow detection/control | `ppt_monitor.py` | High-complexity integration file |
| Theme palettes | `theme_data.py` | Mostly data, used widely |
| File/app icon behavior | `icon_helper.py`, `app_icon.py` | Shared asset resolution |
| Autostart / OS integration | `platform_integration.py` | Platform glue |
| Timer engine | `timer_manager.py` | Shared timer logic |
| Platform-specific APIs | `system/` | `base.py`, `windows.py`, `linux.py` |

## CONVENTIONS
- This directory is the shared dependency layer for both `main.py` and plugins.
- Prefer extending existing helpers over creating duplicate utility modules elsewhere.
- Keep platform-specific code behind `system/` abstractions when feasible.

## ANTI-PATTERNS
- Do not couple new UI rendering details into `core/` unless they are truly shared logic.
- Do not hardcode single-platform assumptions in shared modules.
- Avoid bypassing `config.py` with ad-hoc settings readers/writers.

## NOTES
- `ppt_monitor.py` is a complexity hotspot; bugfixes should be narrow.
- `theme_data.py` is configuration/data-heavy; treat format consistency as important.
