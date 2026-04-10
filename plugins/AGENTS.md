# PLUGINS KNOWLEDGE BASE

## OVERVIEW
Plugin infrastructure plus runtime helpers. This directory hosts the plugin contract, the webview runner, and built-in plugin packages.

## STRUCTURE
```text
plugins/
├── builtins/                  # shipped plugins
├── interface.py               # base plugin API
├── webview_runner.py          # plugin/webview host runtime
├── webview_window_utils.py    # helper utilities for hosted windows
├── monet_utils.py             # wallpaper/theme extraction support
└── in_process_window_handle.py# handle/window utility glue
```

## WHERE TO LOOK
| Task | Location | Notes |
|---|---|---|
| Add/change plugin API | `interface.py` | Shared contract |
| Debug hosted plugin window behavior | `webview_runner.py` | Very large runtime file |
| Window helper behavior | `webview_window_utils.py`, `in_process_window_handle.py` | Support layer |
| Dynamic theming from wallpaper | `monet_utils.py` | Used by settings/theme flows |
| Built-in plugin feature work | `builtins/` | See child AGENTS |

## CONVENTIONS
- Plugins are first-class features, not throwaway extensions.
- Runtime glue belongs in shared plugin infrastructure files, not copied into each plugin.
- Feature plugins should respect contracts from `interface.py` and existing manifest/layout patterns.

## ANTI-PATTERNS
- Do not add feature-specific hacks to `webview_runner.py` if they belong in one plugin only.
- Do not bypass `interface.py` conventions with one-off plugin bootstrap flows.
- Avoid treating `plugins_external/` as an internal source module unless it gains real contents.

## NOTES
- `webview_runner.py` is one of the largest files in the repo; changes there have wide blast radius.
- Plugin work often also requires `ppt_assistant/core` changes.
