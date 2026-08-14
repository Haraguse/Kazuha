# i18n - Localization Negative / Fallback Evidence

Branch: `RyouYamada`

## Guarantees under test

- A deliberately missing key in a non-default language (ja-JP) falls back to
  zh-CN and NEVER throws or returns an empty string.
- An unknown key returns the key itself (visible failure instead of silent blank).
- `SetLanguage` with an invalid code returns a typed rejection and leaves the
  current language unchanged.
- Language switching raises `LanguageChanged` exactly once with the selected
  language; view-model label properties refresh (asserted via a VM test).
- The overlay disabled-state help text and the version-unavailable fallback are
  localized and match the localization service output byte-for-byte (tests were
  updated to assert against the service rather than hardcoded English).

All cases pass in `tests/Luminalium.Tests/LocalizationTests.cs`
(full suite: passed 102, failed 0, skipped 0).

## Honest limitations

zh-TW / ja-JP / yue-HK / ug-CN resources are best-effort translations; any missing
key in those languages falls back to zh-CN. Completeness of zh-CN and en-US is
guaranteed for the current shell key set; new keys added by later tasks must add
zh-CN and en-US entries.
