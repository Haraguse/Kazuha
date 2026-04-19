# BUILTIN PLUGINS KNOWLEDGE BASE

## OVERVIEW
Shipped feature plugins. Each subdirectory is a mini-domain with its own UI/assets/manifest patterns.

## STRUCTURE
```text
plugins/builtins/
├── app_launcher/
├── board/
├── onboarding/
├── settings/
├── spotlight/
├── status_bar/
├── timer/
└── plugin_clock.py
```

## WHERE TO LOOK
| Task | Location | Notes |
|---|---|---|
| Launch external apps/files | `app_launcher/` | Launcher feature |
| Whiteboard/drawing | `board/` | Includes large QML/Python UI pieces |
| First-run flows | `onboarding/` | Guided setup UX |
| User preferences UI | `settings/` | Often touches shared config/theming |
| Search/spotlight behavior | `spotlight/` | Discovery/navigation feature |
| Status/toolbar extras | `status_bar/` | Lightweight shell UI |
| Timer feature | `timer/` | Usually collaborates with core timer manager |
| Clock-only helper | `plugin_clock.py` | Single-file built-in |

## CONVENTIONS
- Follow the existing per-plugin packaging pattern before inventing a new layout.
- Shared logic belongs in parent `plugins/` or `ppt_assistant/core/`, not duplicated across plugins.
- Settings/theme-sensitive plugins should reuse shared config/theme helpers.

## ANTI-PATTERNS
- Do not turn every plugin tweak into a `webview_runner.py` change.
- Do not duplicate timer/config/theme logic already present in shared layers.
- Avoid cross-plugin imports unless there is a proven shared abstraction need.

## NOTES
- `board/` is heavier than most built-ins and may need extra context before edits.
- Many plugin tasks span plugin UI + shared runtime + core settings.
