# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-06 Asia/Shanghai
**Commit:** 5ddbb35
**Branch:** linux-adapt

## OVERVIEW
Luminalium = PySide6 desktop app for touchscreen PowerPoint assistance on Windows/Linux. Core split: `main.py` bootstraps app, `ppt_assistant/` holds app logic/UI, `plugins/` provides built-in tools, `scripts/` handles build/integration helpers.

## STRUCTURE
```text
./
├── main.py                 # single large bootstrap/orchestrator
├── ppt_assistant/          # app core, UI, settings/assets
├── plugins/                # plugin runtime + built-in plugins
├── scripts/                # build helpers, smoke test, .NET helpers
├── user/                   # user themes/splash content
├── icons/                  # shipped UI/app assets
├── fonts/                  # bundled fonts
└── .github/workflows/      # nightly release pipeline
```

## WHERE TO LOOK
| Task | Location | Notes |
|---|---|---|
| App startup / lifecycle | `main.py` | Large entrypoint; loads Qt, tray, plugins, overlay |
| PPT/window detection | `ppt_assistant/core/ppt_monitor.py` | Slideshow tracking, integration-heavy |
| Overlay / annotation UI | `ppt_assistant/ui/overlay.py` | Main presentation overlay |
| Tray UI / menus | `ppt_assistant/ui/tray.py` | System tray, quick actions |
| Shared config / theme / i18n | `ppt_assistant/core/` | Cross-cutting support layer |
| Plugin contract / runtime | `plugins/interface.py`, `plugins/webview_runner.py` | Base API + webview host |
| Built-in feature plugins | `plugins/builtins/` | Board, timer, settings, spotlight, etc. |
| Build / release | `build_pyinstaller.py`, `build_linux.py`, `scripts/`, `.github/workflows/nightly-release.yml` | Windows/Linux packaging |
| User-customizable splash/theme content | `user/` | Content, not app core |

## CODE MAP
| Symbol / File | Role |
|---|---|
| `main.py` | Global bootstrap; central control flow |
| `plugins/webview_runner.py` | Biggest runtime hotspot; plugin/webview window host |
| `ppt_assistant/core/ppt_monitor.py` | Presentation-monitoring core |
| `ppt_assistant/ui/overlay.py` | Touch overlay / drawing / zoom surface |
| `ppt_assistant/ui/tray.py` | Tray panel and menu flow |
| `ppt_assistant/core/config.py` | QConfig-backed settings hub |

## CONVENTIONS
- Python 3.11+ project; packaging from `pyproject.toml`, lockfile via `uv.lock`.
- Dev tooling: `ruff` + `pyright`; no pytest/unittest setup in repo.
- UI stack is hybrid: Python + HTML/QML assets, not pure Qt Widgets-only layouts.
- `ppt_assistant` + `plugins` are the two packaged Python domains.
- Plugin architecture is real: feature work often spans `main.py` + `plugins/` + `ppt_assistant/core`.

## ANTI-PATTERNS (THIS PROJECT)
- Do not treat `.venv/`, `.nuget/`, `__pycache__/`, `bin/`, `obj/`, `deploy/Release/` as source domains.
- In `main.py`, Linux sandbox flags must be set **before** Qt imports/app creation.
- `main.py` explicitly favors custom painting over standard widgets in at least one UI path; check surrounding code before swapping in stock widgets.
- `tray.py` uses fresh menu recreation to avoid stale sizing; avoid “clear and reuse” shortcuts there.

## UNIQUE STYLES
- Repo mixes Python, webview assets, QML, and small .NET helpers in one app.
- Large files are normal here; hotspots are not automatically good refactor targets during bugfixes.
- Built-in plugins are organized as mini-domains under `plugins/builtins/`, usually with their own assets/manifests.

## COMMANDS
```bash
uv sync
ruff check .
pyright
python scripts/smoke_test_window_icons.py
python build_pyinstaller.py
python build_linux.py
```

## NOTES
- CI nightly workflow builds Windows + Linux and publishes `nightly` release artifacts.
- Workflow installs from `requirements.txt`, but dependency truth in repo is `pyproject.toml`/`uv.lock`; verify before changing release flow.
- No existing AGENTS hierarchy was present before this initialization.
