# Task 17 - Native Drawing Board (positive evidence)

Branch: `RyouYamada`

## What was built

- `src/Luminalium.App/Board/`: `BoardDocument` (ordered strokes + deterministic
  bbox-intersection erase + clear), `BoardSaveService` (JSON stroke-record export
  with typed Success/NothingToSave/Failure results; never mutates the document),
  `BoardBenchmark` (deterministic synthetic load/save round-trip timing).
- `ViewModels/BoardViewModel`: pen color (incl. #FF0000 preset), width, eraser
  toggle (width 24 in eraser mode), save/status/error state, all strings localized.
- `Views/BoardWindow`: standalone FluentAvalonia window reusing the Task 16
  `AnnotationCanvas`; toolbar automation names `BoardColorRed`, `BoardPen`(via
  canvas), `BoardEraser`, `BoardClear`, `BoardSave`, `BoardClose`; canvas
  automation name `BoardCanvas` (automation peer added to AnnotationCanvas).
- Shell wiring: selecting the `board` navigation item opens the native board
  window (other plugins keep the placeholder page until Tasks 18-19).
- i18n: `Board.*` keys added to zh-CN and en-US resources.

## Verification

```
dotnet build Luminalium.sln -c Release   => 0 warnings, 0 errors
dotnet test  Luminalium.sln -c Release   => passed 109, failed 0, skipped 0
tools/ForbiddenReferenceCheck.ps1        => pass
```

`BoardTests` covers: 3-stroke #FF0000 save round-trip with file content assertions,
empty-board NothingToSave with no file written, invalid-path typed failure with
strokes retained, bbox erase removing only intersecting strokes, clear, pen/eraser
VM gestures, and the benchmark. Benchmark baseline (this machine, 100 strokes x
16 points, cold suite run): ~37 ms total test duration including fixture setup.

## UI Automation smoke (this machine)

Selecting `板中板 - Luminalium` opens the native board; UIA observed:
`BoardCanvas | BoardColorRed | BoardEraser | BoardClear | BoardSave | BoardClose`.

Live pen/touch drawing and pixel-level visual verification remain
environment-dependent (Task 22 desktop harness).
