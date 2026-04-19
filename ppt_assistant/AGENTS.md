# PPT_ASSISTANT KNOWLEDGE BASE

## OVERVIEW
Main application package. Split into shared core logic and user-facing UI surfaces.

## STRUCTURE
```text
ppt_assistant/
├── core/      # config, monitoring, platform glue, theme/i18n
├── ui/        # overlay, tray, dialogs, splash html
├── settings/  # settings-related package area
└── assets/    # shipped media assets
```

## WHERE TO LOOK
| Task | Location | Notes |
|---|---|---|
| Settings/config changes | `core/config.py` | Central settings model |
| Theme/color behavior | `core/theme_data.py`, `core/config.py` | Shared theme definitions |
| PPT integration | `core/ppt_monitor.py` | Heavy integration code |
| Platform-specific behavior | `core/system/`, `core/platform_integration.py` | Windows/Linux split |
| Overlay interactions | `ui/overlay.py` | Drawing, spotlight, zoom |
| Tray/menu behavior | `ui/tray.py` | Main shell controls |
| Dialog/web content | `ui/*.html`, `ui/dialog.py`, `ui/dialog_runtime.py` | Hybrid Python + HTML |

## CONVENTIONS
- `core/` is shared infrastructure; prefer keeping feature-specific UI logic out of it.
- `ui/` is not pure template storage; Python controllers and HTML assets evolve together.
- Platform branching belongs near `core/system/` or dedicated integration modules, not scattered across unrelated files.

## ANTI-PATTERNS
- Avoid adding new cross-cutting state directly in `main.py` when `core/` already owns the concern.
- Do not duplicate theme/config/i18n logic inside plugins or UI files.
- Do not assume Windows-only behavior; repo declares Windows + Linux support.

## NOTES
- Biggest hotspots here: `core/ppt_monitor.py`, `ui/overlay.py`, `ui/tray.py`.
- If work touches both `core/` and `ui/`, read both local AGENTS files first.
