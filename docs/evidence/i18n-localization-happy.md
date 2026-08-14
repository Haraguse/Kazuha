# i18n - Multilingual Localization (user-required addition)

Branch: `RyouYamada`

## Requirement

The app must be Chinese-first with a runtime language switcher (user request during Task 17 planning).
Legacy language set preserved: zh-CN, zh-TW, yue-HK, ja-JP, en-US, ug-CN.

## Implementation

- `src/Luminalium.Core/Localization/`: `AppLanguage` enum, `ILocalizationService`
  (string indexer, `SetLanguage`, `LanguageChanged` event), `LocalizationService`
  loading six embedded JSON resources. Fallback chain: requested -> zh-CN -> key.
- Default language: **zh-CN**. zh-CN and en-US are complete for the full key set;
  zh-TW/ja-JP/yue-HK/ug-CN are best-effort and automatically fall back to zh-CN
  for missing keys (documented, never crashes).
- Config: `General.Language` (default "zh-CN") persisted through the Task 7
  configuration service; applied at shell startup.
- App: all user-visible shell strings (nav labels, pages, dialogs, splash, tray
  menu, overlay toolbar labels + disabled reasons) route through the service;
  language switches at runtime without restart. AutomationProperties.Name values
  (NextSlide/OpenOverlay/...) remain stable identifiers and are NOT localized.
- Settings page exposes a language ComboBox (简体中文/繁體中文/粵語/日本語/English/ئۇيغۇرچە).

## Verification

```
dotnet build Luminalium.sln -c Release   => 0 warnings, 0 errors
dotnet test  Luminalium.sln -c Release   => passed 102, failed 0, skipped 0
tools/ForbiddenReferenceCheck.ps1        => pass
```

Tests (`LocalizationTests`): six-language key lookup, zh-CN fallback for missing
keys, unknown key passthrough, LanguageChanged event, invalid code typed rejection,
config round-trip with language persistence, VM label switching on language change.

UI Automation smoke: app launches in zh-CN by default ("设置" nav item and CJK
plugin names verified via Windows UI Automation); full visual CJK rendering
evidence remains environment-dependent (Task 22 desktop harness).
