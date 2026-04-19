# SCRIPTS KNOWLEDGE BASE

## OVERVIEW
Build/integration/support tooling. Mixed Python and .NET helper area; not part of normal app runtime.

## STRUCTURE
```text
scripts/
├── smoke_test_window_icons.py   # manual smoke test
├── ppt_vsto_bridge/             # PowerPoint/VSTO bridge artifacts + deploy bits
└── smtc_helper/                 # .NET helper for system media transport controls
```

## WHERE TO LOOK
| Task | Location | Notes |
|---|---|---|
| Manual UI smoke check | `smoke_test_window_icons.py` | Only test-like script in repo |
| PowerPoint bridge work | `ppt_vsto_bridge/` | Integration/tooling boundary |
| Windows media controls helper | `smtc_helper/` | C# project |
| Packaging logic | root `build_pyinstaller.py`, `build_linux.py` | Outside this dir but same domain |

## CONVENTIONS
- Treat this directory as tooling/support, not app feature code.
- Generated outputs (`bin/`, `obj/`, deploy artifacts) are not authoritative sources.
- Cross-language changes here often need matching packaging/release updates.

## ANTI-PATTERNS
- Do not edit compiled/generated outputs instead of source project files.
- Do not assume scripts here run cross-platform; some are Windows-specific.
- Avoid adding application business logic to tooling helpers.

## NOTES
- Repo has no formal automated test suite; this directory contains the only smoke-test-style script.
- Nightly CI publishes artifacts from root build scripts, not from ad-hoc shell wrappers here.
