# Native Built-in Feature Migration

## TL;DR
> **Summary**: Separate Luminalium's app-owned built-in capabilities from the external plugin contract by introducing an authoritative native feature catalog and centralized feature host, then migrate shell/pages/windows incrementally through a one-release compatibility adapter.
> **Deliverables**:
> - Typed, app-owned built-in feature identity, descriptors, routing, activation, lifecycle, diagnostics, and tests.
> - Native ShellViewModel/MainWindow/page/window paths that no longer depend on built-in plugin registrations or placeholder factories.
> - Read-only native-to-legacy projection preserving existing IDs and `plugin:<id>` input aliases for one compatibility release.
> - External plugin contracts and registry retained independently.
> **Effort**: Large
> **Parallel**: YES - 4 waves
> **Critical Path**: T1 identity/catalog -> T2 host/routing -> T3 pages/windows -> T4 compatibility removal gate

## Context
### Original Request
The built-in functionality is still represented and routed as plugins. Produce a better solution before implementation.

### Interview Summary
- This plan covers architecture migration only; the previously paused WPS/startup/onboarding/settings/log bug-fix batch is excluded.
- Built-ins are `settings`, `onboarding`, `logs`, `board`, `timer`, `spotlight`, `app_launcher`, and `status_bar`.
- Existing IDs must remain stable. `plugin:<id>` remains accepted at input boundaries for one compatibility release.
- New internal state uses typed native feature IDs and never emits `plugin:<id>`.
- Window behavior defaults to current behavior: fresh activation per current invocation unless an existing feature already has explicit singleton semantics; shell pages remain shared instances.
- External plugin support remains available and is not redesigned.

### Metis Review (gaps addressed)
- Added exact identity normalization, descriptor invariants, lifecycle/UI-thread/concurrency/shutdown rules, structured activation failures, diagnostics, persistence normalization, and a measurable compatibility-removal gate.
- Guarded against dual writable catalogs, alias leakage, duplicate window ownership, placeholder fallback, and external-plugin coupling.

## Work Objectives
### Core Objective
Make native built-in features first-class application capabilities owned by `Luminalium.App`, while retaining `Luminalium.Plugins` solely for genuine external extensions during a measured compatibility period.

### Deliverables
- `BuiltInFeatureId`, descriptors, catalog, route parser/normalizer, activation result/error codes, host, and diagnostics in `Luminalium.App`.
- Typed ShellViewModel navigation and app-owned page/window activation.
- Centralized feature host replacing direct built-in window construction in `MainWindow.axaml.cs`.
- Native catalog to legacy projection adapter with no reverse dependency.
- Migrated overview/page/window tests and external plugin regression tests.
- Compatibility removal evidence and explicit deprecation boundary.

### Definition of Done (verifiable conditions with commands)
- `dotnet test Luminalium.sln --configuration Release --no-restore` passes.
- `dotnet build Luminalium.sln --configuration Release --no-restore` reports 0 warnings and 0 errors.
- Static searches show no production `BuiltInPluginCatalog.CreateDefaultRegistry()` use from Shell/MainWindow/native feature activation.
- Static searches show no direct built-in window construction in `MainWindow.axaml.cs`.
- All eight existing IDs normalize correctly, malformed/unknown routes return structured failure, and external plugin IDs remain untouched.
- Compatibility projection is read-only and native-catalog-to-legacy only.
- Runtime smoke tests prove page navigation, window activation/close, duplicate activation policy, shutdown rejection, and external plugin registration.

### Must Have
- Canonical IDs: `settings`, `onboarding`, `logs`, `board`, `timer`, `spotlight`, `app_launcher`, `status_bar`.
- Canonical internal representation: string-backed `BuiltInFeatureId` value object with ordinal, case-sensitive canonical values; parsing trims only the route wrapper, never changes canonical IDs.
- Accepted input forms: bare canonical ID and `plugin:<canonical-id>`. Optional `feature:<id>` is accepted only as a forward-compatible input alias and is never emitted.
- Unknown, empty, malformed, conflicting, or external IDs return `BuiltInFeatureActivationErrorCode.NotFound` without throwing from UI navigation.
- Descriptor fields: canonical ID, display localization key, icon key, surface (`ShellPage`, `Window`, `Toolbar`, `StatusBar`), activation mode (`SharedPage`, `FreshWindow`, `SingletonWindow`), route/navigation key, availability predicate, and stable diagnostics key.
- Catalog is immutable after construction and is the sole built-in metadata source.
- Host is application-scoped, owns activation/close/dispose, serializes activation per feature, marshals UI-bound creation to the Avalonia UI dispatcher, rejects activation after shutdown begins, and converts construction failures to structured results.
- Persistence normalizes legacy IDs on read and writes only canonical native IDs; external plugin identifiers are not rewritten.
- Compatibility adapter is one-way: native catalog -> legacy metadata/projection. Legacy registry cannot add or mutate built-ins.
- Every migration slice has focused tests and static usage evidence.

### Must NOT Have
- No `Luminalium.Plugins` reference to `Luminalium.App`, native view models, windows, or feature host types.
- No second independently editable built-in catalog.
- No internal `plugin:<id>` state after a route is normalized.
- No silent fallback to `PlaceholderPluginCommand`, `PlaceholderPluginViewFactory`, or a different feature.
- No external plugin discovery redesign, marketplace, event bus, service locator, or broad UI redesign.
- No removal of external plugin contracts merely because built-ins leave the registry.
- No behavior changes to onboarding/logs semantics, timer behavior, or window visuals beyond ownership/routing required by this migration.

## Verification Strategy
> ZERO HUMAN INTERVENTION - all verification is agent-executed.
- Test decision: tests-after for the initial seam, then regression tests before each migration slice; xUnit via the existing `Luminalium.Tests` project.
- QA policy: every task includes unit/integration and runtime-oriented scenarios.
- Evidence: `.omo/evidence/task-{N}-{slug}.{ext}` and `artifacts/feature-migration/{slice}/` for runtime/static reports.
- Required final commands: `dotnet test Luminalium.sln --configuration Release --no-restore`, `dotnet build Luminalium.sln --configuration Release --no-restore`, repository searches for remaining compatibility consumers, and Release app startup smoke test.

## Execution Strategy
### Parallel Execution Waves
Wave 1: T1 identity/descriptor/catalog, T2 activation result/diagnostics contracts, T3 compatibility route parser, T4 test fixtures and static reference baseline.
Wave 2: T5 feature host/lifecycle, T6 typed ShellViewModel routing, T7 MainWindow host delegation, T8 native-to-legacy projection.
Wave 3: T9 shell page migration, T10 native window migration, T11 overview/navigation projection migration, T12 external plugin isolation regression tests.
Wave 4: T13 compatibility-release diagnostics/removal gate, T14 delete built-in placeholder registrations after zero-usage evidence, T15 final integration/static/runtime verification.

### Dependency Matrix (full, all tasks)
| Task | Depends on | Blocks |
|---|---|---|
| T1 | none | T5,T6,T8,T9,T10,T11 |
| T2 | none | T5,T10,T13 |
| T3 | T1 | T6,T7,T8,T11,T13 |
| T4 | none | T13,T15 |
| T5 | T1,T2 | T6,T7,T9,T10,T13 |
| T6 | T1,T3,T5 | T7,T9,T11 |
| T7 | T5,T6 | T10,T15 |
| T8 | T1,T3 | T11,T13 |
| T9 | T1,T5,T6 | T11,T15 |
| T10 | T1,T2,T5,T7 | T13,T15 |
| T11 | T1,T6,T8,T9 | T13,T15 |
| T12 | T5,T6,T7 | T13,T15 |
| T13 | T4,T8,T9,T10,T11,T12 | T14 |
| T14 | T13 | T15 |
| T15 | T14 | none |

### Agent Dispatch Summary
- Wave 1: 4 tasks, ultrabrain/quick/unspecified-high.
- Wave 2: 4 tasks, deep/visual-engineering/unspecified-high.
- Wave 3: 4 tasks, deep/visual-engineering/unspecified-high.
- Wave 4: 3 tasks, deep/unspecified-high/oracle.

## TODOs
> Implementation + Test = ONE task. Never separate.

- [ ] T1. Add authoritative built-in feature identity and immutable catalog

  **What to do**: Add `BuiltInFeatureId` as a string-backed value object with the eight canonical IDs, `BuiltInFeatureSurface`, `BuiltInFeatureActivationMode`, `BuiltInFeatureDescriptor`, and immutable `BuiltInFeatureCatalog` under `src/Luminalium.App/Features/`. Register descriptors in the exact current catalog order and map existing localization/icon/navigation metadata without changing user-visible text.
  **Must NOT do**: Do not create `BuiltInPlugin` instances, do not move native windows into `Luminalium.Plugins`, and do not add new features.
  **Recommended Agent Profile**: Category `ultrabrain` - identity and metadata are the foundation of all later slices. Skills: none. Omitted: `playwright` because this is non-browser model work.
  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: T5,T6,T8,T9,T10,T11 | Blocked By: none
  **References**: `src/Luminalium.Plugins/BuiltInPluginCatalog.cs:13-65` for current IDs/order/metadata; `src/Luminalium.Plugins/PluginType.cs` for current surface concepts; `src/Luminalium.App/ViewModels/ShellPageViewModel.cs` for navigation key conventions.
  **Acceptance Criteria**:
  - [ ] Unit tests assert exactly eight descriptors, unique canonical IDs, stable order, and expected surfaces/modes.
  - [ ] Invalid/duplicate descriptors fail at catalog construction with deterministic errors.
  - [ ] `dotnet test tests/Luminalium.Tests/Luminalium.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~BuiltInFeatureCatalog` passes.
  **QA Scenarios**:
  ```
  Scenario: Catalog exposes all native features
    Tool: Bash
    Steps: Run the focused BuiltInFeatureCatalog test filter.
    Expected: Eight canonical IDs and metadata assertions pass.
    Evidence: .omo/evidence/task-1-feature-catalog.txt

  Scenario: Duplicate or malformed feature registration fails
    Tool: Bash
    Steps: Run invalid descriptor tests with duplicate, empty, and non-canonical IDs.
    Expected: Structured construction failure; no partial catalog is exposed.
    Evidence: .omo/evidence/task-1-feature-catalog-error.txt
  ```
  **Commit**: YES | Message: `refactor: define native built-in feature catalog` | Files: `src/Luminalium.App/Features/*`, matching tests

- [ ] T2. Define structured activation results, lifecycle policy, and diagnostics

  **What to do**: Add stable activation result/error types and diagnostic records for success, not found, unavailable, initialization failure, shutdown in progress, duplicate/coalesced activation, and UI-dispatch failure. Include canonical ID, source route, correlation ID, and non-sensitive failure detail. Define `FreshWindow`, `SingletonWindow`, and `SharedPage` semantics in tests.
  **Must NOT do**: Do not implement a generalized event bus or leak exception messages containing passwords/tokens/config secrets.
  **Recommended Agent Profile**: Category `quick` - bounded contracts and tests. Skills: none. Omitted: `debugging` because no runtime failure is being diagnosed here.
  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: T5,T10,T13 | Blocked By: none
  **References**: `src/Luminalium.Core/Platform/PlatformOperationResult.cs` for result/error style; `src/Luminalium.App/Services/StartupErrorCoordinator.cs` for sanitized user-facing failure handling.
  **Acceptance Criteria**:
  - [ ] Every required failure has a stable enum/code and tests assert no throwing from route activation.
  - [ ] Diagnostic serialization excludes raw exception details marked sensitive.
  - [ ] `dotnet test ... --filter FullyQualifiedName~BuiltInFeatureActivation` passes.
  **QA Scenarios**:
  ```
  Scenario: Known feature returns activation success contract
    Tool: Bash
    Steps: Execute activation-result unit tests for a known page and window descriptor.
    Expected: Success includes canonical ID and correlation ID.
    Evidence: .omo/evidence/task-2-activation.txt

  Scenario: Unknown or shutdown feature returns structured failure
    Tool: Bash
    Steps: Execute tests for unknown ID and host shutdown activation.
    Expected: NotFound or ShutdownInProgress code; no exception escapes.
    Evidence: .omo/evidence/task-2-activation-error.txt
  ```
  **Commit**: YES | Message: `refactor: define native feature activation contracts` | Files: activation contract files and tests

- [ ] T3. Add canonical route parsing and legacy alias normalization

  **What to do**: Add a parser accepting bare canonical IDs and `plugin:<id>` (plus input-only `feature:<id>`), normalizing to `BuiltInFeatureId`. Reject empty, malformed, unknown, and external IDs with `NotFound`. Ensure new internal navigation stores only typed IDs and never emits legacy tags.
  **Must NOT do**: Do not change persisted external plugin IDs or emit `plugin:<id>` from new code.
  **Recommended Agent Profile**: Category `quick` - deterministic parser. Skills: none. Omitted: `playwright` because no UI behavior is required.
  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: T6,T7,T8,T11,T13 | Blocked By: T1
  **References**: `src/Luminalium.App/Views/MainWindow.axaml.cs:199-243` for current route inputs; `src/Luminalium.Plugins/BuiltInPluginRegistry.cs:35-43` for current ID lookup semantics.
  **Acceptance Criteria**:
  - [ ] All eight bare IDs and `plugin:<id>` aliases map to one canonical typed ID.
  - [ ] `plugin:`, `plugin:unknown`, `feature:unknown`, empty, and external IDs return structured NotFound.
  - [ ] Round-trip tests prove typed internal state does not emit `plugin:<id>`.
  **QA Scenarios**:
  ```
  Scenario: Legacy and canonical routes normalize equally
    Tool: Bash
    Steps: Run parser tests for `timer`, `plugin:timer`, and `feature:timer`.
    Expected: All accepted inputs produce the same canonical timer ID.
    Evidence: .omo/evidence/task-3-route.txt

  Scenario: Malformed route is rejected without throwing
    Tool: Bash
    Steps: Run parser tests for empty, unknown, and external IDs.
    Expected: Structured NotFound and no catalog mutation.
    Evidence: .omo/evidence/task-3-route-error.txt
  ```
  **Commit**: YES | Message: `refactor: normalize built-in feature routes` | Files: route parser and tests

- [ ] T4. Establish migration diagnostics and static baseline

  **What to do**: Add a deterministic diagnostic collector/counters for legacy alias use, legacy projection use, placeholder factory use, direct native construction, duplicate registration, and activation failures. Add a script or test that records current occurrences under `artifacts/feature-migration/baseline/` without changing runtime behavior.
  **Must NOT do**: Do not remove compatibility code or claim zero usage from static search alone.
  **Recommended Agent Profile**: Category `unspecified-high` - cross-cutting evidence work. Skills: none. Omitted: `git-master` because no history mutation is needed.
  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: T13,T15 | Blocked By: none
  **References**: `src/Luminalium.Plugins/BuiltInPluginCatalog.cs:68-124` for placeholder/direct registration markers; `tools/ForbiddenReferenceCheck.ps1` for repository scan conventions.
  **Acceptance Criteria**:
  - [ ] Baseline report lists each legacy usage category and exact file/reference count.
  - [ ] Diagnostics include canonical ID and correlation context without sensitive values.
  - [ ] Re-running baseline is deterministic on unchanged source.
  **QA Scenarios**:
  ```
  Scenario: Baseline scan is reproducible
    Tool: Bash
    Steps: Run the migration scan twice and compare normalized reports.
    Expected: Same categories and counts.
    Evidence: .omo/evidence/task-4-baseline.txt

  Scenario: Sensitive diagnostic detail is omitted
    Tool: Bash
    Steps: Run diagnostic serialization with token/password/config exception text.
    Expected: Sensitive values do not appear in output.
    Evidence: .omo/evidence/task-4-baseline-error.txt
  ```
  **Commit**: YES | Message: `test: baseline native feature migration usage` | Files: diagnostic/scan files and tests

- [ ] T5. Implement application-scoped BuiltInFeatureHost

  **What to do**: Add `IBuiltInFeatureHost`/`BuiltInFeatureHost` under `src/Luminalium.App/Services/`. It must own activation, per-feature serialization, UI dispatcher marshalling, instance policy, idempotent close, shutdown state, and disposal. Use typed catalog descriptors and typed factory registrations; do not accept arbitrary plugin commands or view factories. Preserve current fresh-window behavior unless descriptor mode explicitly says singleton/shared page.
  **Must NOT do**: Do not let `MainWindow` or view models construct built-in windows directly; do not cache fresh-window features.
  **Recommended Agent Profile**: Category `deep` - lifecycle and UI-thread concurrency. Skills: `debugging` for race/lifecycle verification. Omitted: `playwright` because deterministic host fakes are sufficient.
  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: T6,T7,T9,T10,T13 | Blocked By: T1,T2
  **References**: `src/Luminalium.App/Views/MainWindow.axaml.cs:213-243` for direct construction to remove; `src/Luminalium.App/Services/AsyncSerialGate.cs` for existing serialization pattern; `src/Luminalium.App/Views/TimerWindow.axaml.cs` and sibling windows for close/termination behavior.
  **Acceptance Criteria**:
  - [ ] Activation from background test dispatcher is marshalled to UI dispatcher.
  - [ ] Concurrent same-feature activation is serialized/coalesced according to descriptor policy.
  - [ ] Shutdown activation returns ShutdownInProgress and creates no instance.
  - [ ] Construction failure returns structured failure and leaves no cached partial instance.
  - [ ] Close/dispose is idempotent and invokes feature termination exactly once.
  **QA Scenarios**:
  ```
  Scenario: Window activation is centralized and recoverable
    Tool: Bash
    Steps: Run host tests with a fake window factory and fake dispatcher.
    Expected: One activation result, correct owner, and exactly one close/dispose.
    Evidence: .omo/evidence/task-5-host.txt

  Scenario: Activation during shutdown is rejected
    Tool: Bash
    Steps: Mark host shutting down, then request timer and page activation.
    Expected: ShutdownInProgress; no factory invocation.
    Evidence: .omo/evidence/task-5-host-error.txt
  ```
  **Commit**: YES | Message: `refactor: centralize native feature activation` | Files: host/contracts/tests

- [ ] T6. Move ShellViewModel to typed built-in feature state

  **What to do**: Replace the authoritative `_pluginPages`/built-in registry construction with immutable catalog descriptors and a typed page map. Keep `Plugins`/`NavigateToPlugin` only as explicitly named compatibility projections that normalize through T3. Preserve onboarding first-run routing and shared page instances.
  **Must NOT do**: Do not delete external plugin consumption or change onboarding/logs behavior.
  **Recommended Agent Profile**: Category `deep` - central shell migration. Skills: none. Omitted: `frontend-ui-ux` because visuals are unchanged.
  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: T7,T9,T11 | Blocked By: T1,T3,T5
  **References**: `src/Luminalium.App/ViewModels/ShellViewModel.cs:101-148` for current registry/page construction; `tests/Luminalium.Tests/ShellViewModelTests.cs` for shared page/back-stack expectations.
  **Acceptance Criteria**:
  - [ ] Shell initializes native pages from typed catalog, not `BuiltInPluginCatalog.CreateDefaultRegistry()`.
  - [ ] Incomplete config starts onboarding; completed config starts overview.
  - [ ] Legacy `NavigateToPlugin` accepts old IDs only through parser and does not become internal state.
  - [ ] Page instances remain shared and back-stack behavior remains unchanged.
  **QA Scenarios**:
  ```
  Scenario: Typed shell navigation preserves first-run behavior
    Tool: Bash
    Steps: Run shell tests with onboarding incomplete and complete configs.
    Expected: Onboarding or overview is selected correctly; legacy registry is not called.
    Evidence: .omo/evidence/task-6-shell.txt

  Scenario: Unknown legacy plugin route is safe
    Tool: Bash
    Steps: Invoke compatibility navigation with `plugin:unknown`.
    Expected: NotFound diagnostic; current page unchanged.
    Evidence: .omo/evidence/task-6-shell-error.txt
  ```
  **Commit**: YES | Message: `refactor: route shell through native features` | Files: ShellViewModel and tests

- [ ] T7. Delegate MainWindow navigation to the feature host

  **What to do**: Remove the built-in ID switch and direct `new BoardWindow/TimerWindow/SpotlightWindow/AppLauncherWindow/StatusBarWindow` calls from `MainWindow.axaml.cs`. Normalize incoming tags through T3, delegate to `IBuiltInFeatureHost`, preserve current tag input aliases for one release, and leave only generic external-plugin routing in the window.
  **Must NOT do**: Do not emit legacy tags internally; do not change window visuals or feature behavior.
  **Recommended Agent Profile**: Category `visual-engineering` - UI routing integration without redesign. Skills: none. Omitted: `playwright` because native desktop behavior is covered by host fakes and smoke tests.
  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: T10,T15 | Blocked By: T5,T6
  **References**: `src/Luminalium.App/Views/MainWindow.axaml.cs:199-243` for current routing; `src/Luminalium.App/Services/BuiltInFeatureHost.cs` from T5.
  **Acceptance Criteria**:
  - [ ] MainWindow has no built-in switch and no direct built-in window construction.
  - [ ] Existing `plugin:<id>` tags still activate the same feature through the parser/host.
  - [ ] Unknown tags do not throw or change current selection.
  **QA Scenarios**:
  ```
  Scenario: Legacy timer tag delegates to native host
    Tool: Bash
    Steps: Run MainWindow routing tests with a fake host and `plugin:timer`.
    Expected: Host receives typed Timer ID exactly once.
    Evidence: .omo/evidence/task-7-main-window.txt

  Scenario: Unknown tag is rejected safely
    Tool: Bash
    Steps: Run routing test with `plugin:unknown`.
    Expected: Structured NotFound diagnostic and no window creation.
    Evidence: .omo/evidence/task-7-main-window-error.txt
  ```
  **Commit**: YES | Message: `refactor: route native windows through feature host` | Files: MainWindow/host integration and tests

- [ ] T8. Add read-only native-to-legacy compatibility projection

  **What to do**: Generate the legacy `PluginMetadata`/`PluginEntryViewModel` projection from the native catalog. Mark the projection adapter internal/compatibility-scoped, prohibit catalog mutation through it, preserve exact IDs/order/metadata for one release, and make placeholder behavior observable rather than user-facing.
  **Must NOT do**: Do not let legacy registry registration become a second source of truth; do not route native activation back through legacy commands/factories.
  **Recommended Agent Profile**: Category `deep` - compatibility boundary. Skills: none. Omitted: `git-master` because history changes are not needed.
  **Parallelization**: Can Parallel: YES | Wave 2 | Blocks: T11,T13 | Blocked By: T1,T3
  **References**: `src/Luminalium.Plugins/BuiltInPluginCatalog.cs:13-81`; `src/Luminalium.App/ViewModels/PluginEntryViewModel.cs`; `tests/Luminalium.Tests/BuiltInPluginRegistryTests.cs`.
  **Acceptance Criteria**:
  - [ ] Projection contains all eight IDs in current order and metadata-equivalent values.
  - [ ] Mutating or registering through projection cannot modify native catalog.
  - [ ] Native path never invokes placeholder command/view factory.
  - [ ] Compatibility usage is counted by T4 diagnostics.
  **QA Scenarios**:
  ```
  Scenario: Legacy overview projection remains readable
    Tool: Bash
    Steps: Run compatibility projection tests and compare IDs/order/metadata.
    Expected: Exact compatibility projection with no native catalog mutation.
    Evidence: .omo/evidence/task-8-compatibility.txt

  Scenario: Placeholder cannot be reached by native activation
    Tool: Bash
    Steps: Activate every native descriptor using fake factories.
    Expected: No placeholder command/view invocation is recorded.
    Evidence: .omo/evidence/task-8-compatibility-error.txt
  ```
  **Commit**: YES | Message: `refactor: add native feature compatibility projection` | Files: adapter and compatibility tests

- [ ] T9. Migrate shell pages to native feature routing

  **What to do**: Replace `PluginPageViewModel` as the built-in page carrier in `ShellPageFactory` and `OverviewViewModel`. Map overview/settings/onboarding/logs explicitly to native page types; keep an independently named external extension page adapter only if external UI still needs it.
  **Must NOT do**: Do not delete external plugin UI support or change page content.
  **Recommended Agent Profile**: Category `visual-engineering` - page routing integration. Skills: `frontend-ui-ux` omitted because no visual redesign is allowed.
  **Parallelization**: Can Parallel: YES | Wave 3 | Blocks: T11,T13,T15 | Blocked By: T1,T5,T6
  **References**: `src/Luminalium.App/Services/ShellPageFactory.cs:12-20`; `src/Luminalium.App/ViewModels/OverviewViewModel.cs:10-35`; native page XAML/code-behind files under `src/Luminalium.App/Views/Pages/`.
  **Acceptance Criteria**:
  - [ ] Native pages resolve through typed feature/page mapping.
  - [ ] `PluginPageViewModel` has no built-in page consumers.
  - [ ] Existing page sharing, onboarding gating, logs refresh, and back-stack tests pass.
  **QA Scenarios**:
  ```
  Scenario: Native page factory resolves all shell pages
    Tool: Bash
    Steps: Run page factory tests for overview/settings/onboarding/logs.
    Expected: Correct native page type and shared DataContext each time.
    Evidence: .omo/evidence/task-9-pages.txt

  Scenario: Unknown page target fails safely
    Tool: Bash
    Steps: Pass an unknown feature/page target to the factory.
    Expected: Null/structured not-found according to the factory contract; no placeholder page.
    Evidence: .omo/evidence/task-9-pages-error.txt
  ```
  **Commit**: YES | Message: `refactor: move shell pages out of plugin routing` | Files: page factory/overview/tests

- [ ] T10. Migrate native window features behind the host

  **What to do**: Register existing Board, Timer, Spotlight, App Launcher, and Status Bar window factories with `BuiltInFeatureHost`. Preserve current fresh-window behavior, owner assignment, close cleanup, ViewModel termination, and existing injected services. Remove direct construction from all app entrypoints, not only MainWindow.
  **Must NOT do**: Do not make windows plugin commands or move them into `Luminalium.Plugins`.
  **Recommended Agent Profile**: Category `deep` - window lifecycle and integration. Skills: `debugging`. Omitted: `playwright` because app smoke and deterministic fake-window tests cover this desktop path.
  **Parallelization**: Can Parallel: YES | Wave 3 | Blocks: T13,T15 | Blocked By: T1,T2,T5,T7
  **References**: `src/Luminalium.App/Views/MainWindow.axaml.cs:218-232`; `src/Luminalium.App/Views/TimerWindow.axaml.cs`, `BoardWindow.axaml.cs`, `SpotlightWindow.axaml.cs`, `AppLauncherWindow.axaml.cs`, `StatusBarWindow.axaml.cs`; `tests/Luminalium.Tests/OperationalPluginsTests.cs`.
  **Acceptance Criteria**:
  - [ ] Each feature opens with current behavior and closes/terminates exactly once.
  - [ ] Fresh-window features do not unexpectedly become singleton; singleton/shared descriptors reuse/focus exactly one instance.
  - [ ] No production direct construction remains outside host factories.
  - [ ] Shutdown and construction-failure paths leave no leaked instance.
  **QA Scenarios**:
  ```
  Scenario: All native windows activate through host
    Tool: Bash
    Steps: Run feature-host tests with fake factories for board/timer/spotlight/app_launcher/status_bar.
    Expected: Correct factory, owner, and close cleanup for each ID.
    Evidence: .omo/evidence/task-10-windows.txt

  Scenario: Duplicate activation follows descriptor policy
    Tool: Bash
    Steps: Activate singleton twice and fresh-window feature twice.
    Expected: Singleton focuses one instance; fresh mode creates two as current behavior requires.
    Evidence: .omo/evidence/task-10-windows-error.txt
  ```
  **Commit**: YES | Message: `refactor: host native window features` | Files: host registrations/window integration/tests

- [ ] T11. Migrate overview and shell-facing feature entries

  **What to do**: Replace `OverviewViewModel.Plugins` as the authoritative built-in collection with `BuiltInFeatures` descriptors/entries. Keep a clearly named one-release `LegacyPlugins` compatibility projection only where existing navigation UI requires it. Update shell tests and accessibility/automation metadata without changing visible ordering.
  **Must NOT do**: Do not rename or remove external plugin APIs in this task.
  **Recommended Agent Profile**: Category `visual-engineering` - shell-facing model migration. Skills: none. Omitted: `playwright` because no visual redesign is included.
  **Parallelization**: Can Parallel: YES | Wave 3 | Blocks: T13,T15 | Blocked By: T1,T6,T8,T9
  **References**: `src/Luminalium.App/ViewModels/OverviewViewModel.cs:27-35`; `src/Luminalium.App/ViewModels/PluginEntryViewModel.cs`; `src/Luminalium.App/Views/Pages/OverviewPage.axaml`.
  **Acceptance Criteria**:
  - [ ] Overview consumes native feature descriptors as source of truth.
  - [ ] Legacy projection is read-only and explicitly named/deprecated.
  - [ ] Existing feature order, labels, icons, and navigation behavior remain unchanged.
  **QA Scenarios**:
  ```
  Scenario: Overview lists native features in stable order
    Tool: Bash
    Steps: Run overview model tests against the native catalog.
    Expected: Eight features appear in the existing order with matching metadata.
    Evidence: .omo/evidence/task-11-overview.txt

  Scenario: Legacy projection cannot diverge
    Tool: Bash
    Steps: Attempt to mutate a projected entry and reload catalog metadata.
    Expected: Native descriptor remains unchanged and diagnostics record compatibility use.
    Evidence: .omo/evidence/task-11-overview-error.txt
  ```
  **Commit**: YES | Message: `refactor: expose native features in overview` | Files: overview/entry models and tests

- [ ] T12. Isolate and preserve the external plugin boundary

  **What to do**: Rename or wrap app-facing compatibility types as `ExtensionEntryViewModel`/`ExtensionPageViewModel` where needed, keep `PluginContracts` and `BuiltInPluginRegistry` tests for genuine external objects, reject external IDs colliding with canonical built-in IDs, and verify `Luminalium.Plugins` has no App references.
  **Must NOT do**: Do not redesign plugin discovery, add dynamic loading, or remove the public plugin contract.
  **Recommended Agent Profile**: Category `unspecified-high` - cross-project boundary verification. Skills: none. Omitted: `frontend-ui-ux` because this is contract isolation.
  **Parallelization**: Can Parallel: YES | Wave 3 | Blocks: T13,T15 | Blocked By: T5,T6,T7
  **References**: `src/Luminalium.Plugins/PluginContracts.cs`; `src/Luminalium.Plugins/BuiltInPluginRegistry.cs`; `tests/Luminalium.Tests/BuiltInPluginRegistryTests.cs`; `tests/Luminalium.Tests/SolutionScaffoldTests.cs`.
  **Acceptance Criteria**:
  - [ ] A test external plugin registers, renders, executes, and terminates without native feature catalog access.
  - [ ] Built-in/external ID collision fails deterministically.
  - [ ] Project/reference scan confirms Plugins does not reference App types.
  - [ ] External plugin registry tests remain green.
  **QA Scenarios**:
  ```
  Scenario: Genuine external plugin remains functional
    Tool: Bash
    Steps: Register `external.test.plugin`, execute its command, render its view, and terminate it.
    Expected: All lifecycle operations succeed independently of built-ins.
    Evidence: .omo/evidence/task-12-external-plugin.txt

  Scenario: External ID collision is rejected
    Tool: Bash
    Steps: Register an external plugin using canonical built-in ID `timer`.
    Expected: Structured collision error; native catalog remains unchanged.
    Evidence: .omo/evidence/task-12-external-plugin-error.txt
  ```
  **Commit**: YES | Message: `refactor: isolate external plugin boundary` | Files: extension compatibility/tests/project references

- [ ] T13. Add compatibility-release usage and removal gate

  **What to do**: Add a versioned compatibility marker and a deterministic gate requiring: repository-wide production reference search, runtime diagnostic counters, alias/parser coverage, no direct native construction, no user-facing placeholder invocation, and zero production consumers of the legacy built-in projection. Keep `plugin:<id>` input aliases for exactly one compatibility release and emit deprecation diagnostics.
  **Must NOT do**: Do not delete legacy code based only on a clean static search of tests; do not remove aliases before the marker’s release boundary.
  **Recommended Agent Profile**: Category `deep` - migration exit criteria and cross-cutting evidence. Skills: `git-master` omitted because no history rewrite is needed.
  **Parallelization**: Can Parallel: NO | Wave 4 | Blocks: T14,T15 | Blocked By: T4,T8,T9,T10,T11,T12
  **References**: T4 diagnostics; `src/Luminalium.App/Views/MainWindow.axaml.cs`; `src/Luminalium.Plugins/BuiltInPluginCatalog.cs`; existing release/version metadata handling in `src/Luminalium.Core/Identity/`.
  **Acceptance Criteria**:
  - [ ] Gate report distinguishes intentional compatibility tests from production usage.
  - [ ] Runtime alias/projection/placeholder counters are zero in native user flows.
  - [ ] All remaining legacy references are listed as explicit compatibility exceptions.
  - [ ] Release marker documents that aliases remain for exactly one release.
  **QA Scenarios**:
  ```
  Scenario: Compatibility gate passes after native migration
    Tool: Bash
    Steps: Run static scan, focused smoke flows, and diagnostics report.
    Expected: Zero production legacy usage; only documented adapter/tests remain.
    Evidence: artifacts/feature-migration/removal-gate.txt

  Scenario: Gate blocks premature removal
    Tool: Bash
    Steps: Run gate with an intentional placeholder/direct-construction fixture.
    Expected: Non-zero failure identifying file, category, and remediation.
    Evidence: artifacts/feature-migration/removal-gate-error.txt
  ```
  **Commit**: YES | Message: `test: enforce native feature migration gate` | Files: gate/diagnostic tests and release marker

- [ ] T14. Remove built-in registrations and placeholder path after gate approval

  **What to do**: Delete built-in entries from `BuiltInPluginCatalog`/legacy registry projection only after T13 passes. Remove `PlaceholderPluginCommand`, `PlaceholderPluginViewFactory`, `NativeSurfaceViewFactory`, and built-in plugin-specific routing. Retain external plugin contracts/registry and compatibility parser for the defined release boundary if required.
  **Must NOT do**: Do not remove external plugin lifecycle or compatibility input parsing before the release marker permits it.
  **Recommended Agent Profile**: Category `deep` - deletion after measured migration. Skills: none. Omitted: `remove-ai-slops` because this is a targeted architectural deletion, not cleanup by pattern.
  **Parallelization**: Can Parallel: NO | Wave 4 | Blocks: T15 | Blocked By: T13
  **References**: `src/Luminalium.Plugins/BuiltInPluginCatalog.cs:68-124`; T13 gate report; all T5-T12 native consumers.
  **Acceptance Criteria**:
  - [ ] No built-in feature is registered as `BuiltInPlugin` in production.
  - [ ] No user-facing placeholder command/view exists for built-ins.
  - [ ] External plugin registry and contract tests remain green.
  - [ ] Static scan finds no built-in plugin switch in MainWindow/ShellViewModel.
  **QA Scenarios**:
  ```
  Scenario: Native built-ins operate without plugin registration
    Tool: Bash
    Steps: Start the app smoke harness and activate all eight built-ins.
    Expected: All native pages/windows work while plugin registry contains no built-in entry.
    Evidence: artifacts/feature-migration/no-built-in-registration.txt

  Scenario: External plugin still works after deletion
    Tool: Bash
    Steps: Run external plugin lifecycle tests after removing built-in registrations.
    Expected: External registration/execute/render/terminate remains green.
    Evidence: artifacts/feature-migration/no-built-in-registration-error.txt
  ```
  **Commit**: YES | Message: `chore: remove built-in plugin compatibility path` | Files: catalog/registry cleanup and tests

- [ ] T15. Run final architecture integration and runtime verification

  **What to do**: Run all tests/builds, static dependency/reference scans, Release publish/startup smoke, and feature-host lifecycle smoke. Verify working tree changes are limited to this migration plan’s implementation files plus pre-existing unrelated user changes.
  **Must NOT do**: Do not change code during verification except targeted fixes that receive their own regression test and commit.
  **Recommended Agent Profile**: Category `unspecified-high` - broad final QA. Skills: `review-work` at final implementation stage. Omitted: `playwright` because this is a native Windows desktop app with no browser surface.
  **Parallelization**: Can Parallel: NO | Wave 4 | Blocks: none | Blocked By: T14
  **References**: `Luminalium.sln`; `tests/Luminalium.Tests`; `tools/ForbiddenReferenceCheck.ps1`; T4/T13 evidence reports.
  **Acceptance Criteria**:
  - [ ] Full Release test suite passes with 0 failures/skips as expected.
  - [ ] Release build has 0 warnings and 0 errors.
  - [ ] Published executable starts and remains responsive for the smoke interval.
  - [ ] Static scan confirms Plugins -> App dependency is absent and no built-in placeholder path remains.
  - [ ] External plugin regression and all eight native feature smoke scenarios pass.
  **QA Scenarios**:
  ```
  Scenario: Full native feature migration passes release gates
    Tool: Bash
    Steps: Run dotnet test, Release build, publish, static scans, and app startup smoke.
    Expected: All commands pass; evidence files are present under artifacts/feature-migration/final/.
    Evidence: artifacts/feature-migration/final/pass.txt

  Scenario: Cross-boundary regression remains visible
    Tool: Bash
    Steps: Run external plugin lifecycle tests and unknown native route tests.
    Expected: External plugin passes and unknown native routes fail structurally without crashing.
    Evidence: artifacts/feature-migration/final/error.txt
  ```
  **Commit**: YES | Message: `test: verify native feature architecture migration` | Files: final tests/evidence configuration

## Final Verification Wave (MANDATORY — after ALL implementation tasks)
- [ ] F1. Plan Compliance Audit — oracle
- [ ] F2. Code Quality Review — unspecified-high
- [ ] F3. Real Manual QA — unspecified-high
- [ ] F4. Scope Fidelity Check — deep

## Commit Strategy
- One atomic commit per migration slice, in dependency order T1 through T15.
- Never stage paused bug-fix files or unrelated existing dirty files.
- Keep the one-way compatibility adapter in its own commit so it can be reverted independently.
- Remove compatibility code only in T14 after T13 evidence passes.

## Success Criteria
- Built-in features are defined and activated by `Luminalium.App`, not registered as plugins.
- External plugin contracts remain functional and independent.
- Existing built-in IDs and one-release legacy route aliases remain compatible.
- Native pages/windows have centralized, typed, testable ownership and lifecycle.
- No placeholder implementation is reachable through user-facing native paths.
- Full Release tests/build/publish/static/runtime verification passes with evidence.
