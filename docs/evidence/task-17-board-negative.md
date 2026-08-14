# Task 17 - Native Drawing Board (negative / fail-safe evidence)

Branch: `RyouYamada`

## Guarantees under test

- Empty board save => typed `NothingToSave` and NO file is created.
- Invalid save path (missing directory) => typed `Failure` with the exception
  kind; the in-memory document still holds all strokes (user work never lost).
- Erase only removes strokes whose bounding box intersects the eraser path;
  untouched strokes keep their original color/points.
- Clear empties the document deterministically.
- The board window never depends on QML/WebView; the drawing surface is the
  shared native `AnnotationCanvas`.

All cases pass in `tests/Luminalium.Tests/BoardTests.cs`
(full suite: passed 109, failed 0, skipped 0).

## Deliberately NOT claimed here

Pixel-level rendering quality, real pen/touch latency, and large-document
performance on target hardware are environment-dependent and belong to the
Task 22 desktop QA harness. The recorded benchmark (~37 ms synthetic load/save)
is a baseline note, not a performance regression gate.
