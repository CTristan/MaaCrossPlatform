# AGENTS.md — MaaAssistantArknights

> This document captures institutional knowledge for AI agents working on this codebase.
> Last updated: 2026-03-05

## Project Overview

**MaaAssistantArknights (MAA)** is an automation assistant for the mobile game *Arknights*. It uses image recognition (OpenCV, PaddleOCR, ONNX Runtime) to automate daily in-game tasks like combat, recruitment, infrastructure management, and more.

The core engine (`MaaCore`) is written in **C++20** and exposes a C API (`include/AsstCaller.h`). Multiple GUI frontends and language bindings exist on top of this core.

**License:** AGPL-3.0-only (with additional terms of service).

## Repository Structure

```
MaaAssistantArknights/
├── src/
│   ├── MaaCore/           # C++ core engine (image recognition, task automation)
│   ├── MaaWpfGui/         # WPF GUI (Windows-only, C#, production-grade)
│   ├── MaaGui/            # Avalonia GUI (cross-platform, C#, in development)
│   ├── MaaGui.Tests/      # xUnit tests for MaaGui
│   ├── MaaMacGui/         # Native macOS GUI (Swift/SwiftUI)
│   ├── maa-cli/           # CLI interface (Rust)
│   ├── Python/            # Python bindings
│   ├── Rust/              # Rust bindings + HTTP interface
│   ├── Java/              # Java bindings + HTTP interface
│   ├── Golang/            # Go bindings
│   ├── Dart/              # Dart bindings
│   ├── Woolang/           # Woolang bindings
│   ├── Cpp/               # C++ integration example
│   ├── MaaUtils/          # Shared utilities
│   └── MaaWineBridge/     # Wine compatibility layer
├── resource/              # Game recognition resources (images, JSON task definitions)
├── include/               # C API headers (AsstCaller.h)
├── cmake/                 # CMake build scripts
├── tools/                 # Development tools
├── docs/                  # VuePress documentation site
├── interface.json         # Controller/resource definitions for MaaCore
└── CMakeLists.txt         # Root CMake build (for MaaCore)
```

## GUI Frontends — Comparison

### MaaWpfGui (Windows-only, production)

- **Location:** `src/MaaWpfGui/`
- **Tech:** WPF, .NET, HandyControl, Stylet MVVM
- **Size:** ~74K lines across 289 C# files and 69 XAML files
- **Status:** Mature, full-featured, production-grade
- **Layout:** Top tab navigation (一键长草, 自动战斗, 小工具, 设置); 3-column task queue (task list | settings panel | log feed)
- **Key libraries:** HandyControl (UI controls), Stylet (MVVM), CalcBinding, Serilog, Newtonsoft.Json + System.Text.Json, GongSolutions DragDrop

**Features (comprehensive):**
- Task queue with drag-and-drop reordering, add/delete tasks, tri-state checkboxes
- Per-task status indicators (in-progress, completed, error, skipped) with color coding
- Rich task settings for all types (Fight, Recruit, Infrast, Mall, Award, Roguelike, Reclamation, Custom, StartUp)
- Both plain-text and card-based log views
- Post-action settings (shutdown, hibernate, suspend, close game, etc.)
- Settings panels: Connection, Game, GUI, Timer, Hot Keys, Performance, External Notifications, Remote Control, Background Image, Version Updates, Issue Reporting, Achievements, About
- System tray icon with minimize-to-tray
- Growl-style notifications
- Background image with blur effect and opacity control
- GIF mascot overlay
- Window always-on-top toggle
- Rainbow animated version update text
- Guided onboarding flow
- Multiple configuration profiles
- Localization (many languages)
- Download progress cards in log

### MaaMacGui (macOS-only, production)

- **Location:** `src/MaaMacGui/`
- **Tech:** Swift, SwiftUI, macOS native
- **Size:** ~65 Swift files
- **Status:** Production, actively maintained
- **Layout:** `NavigationSplitView` with 3 panes (sidebar | content list | detail); native macOS Settings window

**Navigation structure:**
- **Sidebar:** Daily Tasks (`一键长草`), Copilot (`自动战斗`), Utilities (`实用工具`), plus resource update / log / settings links
- **Daily Tasks detail** switches between: task config, log view, timer config (via toolbar buttons)

**Features:**
- Task queue with enable/disable, drag-and-drop reordering, add/delete tasks
- Per-task status (running, success, failure, cancel)
- 8 task configuration views: Startup, Fight, Recruit, Infrast, Mall, Award, Roguelike, Reclamation (+ Closedown which has no config)
- Log view with time + colored messages table, auto-scroll toggle
- Task timer system (multiple timers with hour/minute pickers, prevent-sleep alert)
- Copilot: browse bundled/external JSON files, download by `maa://` code, import local files, drag-and-drop, view copilot details (operators, strategy, doc), configure + execute (regular + SSS)
- Utilities: recruit recognition (with config), depot recognition (export to Penguin Stats / Arkntools), operator box recognition, video recognition, gacha polling (with screenshot), mini-games
- Settings: Connection (touch mode, address, gzip, adb-lite), Game (client channel), Updater (beta channel, auto-check/download, resource update channel/CDK), System (prevent sleep)
- Resource update system with download/extract/install progress UI
- Prevent-sleep via IOKit assertions during task execution

### MaaGui (cross-platform, in development) ← **Active development target**

- **Location:** `src/MaaGui/`
- **Tech:** Avalonia UI 11.3, .NET 10, FluentAvalonia, CommunityToolkit.Mvvm
- **Size:** ~5.4K lines across 56 C# files and 17 AXAML files
- **Status:** Early development — infrastructure is solid, UI layer is skeletal
- **Tests:** `src/MaaGui.Tests/` — xUnit with coverlet; currently has ConfigFactory tests and MaaService library load exception tests

**What works:**
- Navigation: `FluentAvalonia.NavigationView` with left sidebar (Daily Tasks, Copilot, Utilities, Settings)
- Task queue: vertical card list with checkboxes, expand/collapse per-task settings, select all / deselect all
- All 9 task type configuration views exist as files but most are **stubs** with minimal fields
- MaaCore interop: full P/Invoke bindings (`MaaService`), lifecycle management (`AsstProxy`), callback processing
- Configuration: JSON-based (`gui.new.json`), with polymorphic task serialization, debounced saves, backup files
- Platform services: `IPlatformServices` with Windows/macOS/Linux implementations (prevent sleep, native lib path, default ADB path)
- DI: `Microsoft.Extensions.DependencyInjection` wired up in `App.axaml.cs`
- Logging: Serilog to file + debug output; in-memory `LogEntryCollection` in TaskQueueViewModel (not rendered in UI)
- Stage tips: `StageManager` with local + web stage loading, activity parsing, mini-game entries
- Notifications: `INotificationPoster` with platform-specific implementations (Windows, macOS, Linux, fallback)
- HTTP: `IHttpService` / `IMaaApiService` for API calls
- Localization: `LocalizationHelper` with `en-us` and `zh-cn` AXAML string resources
- Timer: basic 30-second check loop for scheduled starts

**What's missing (compared to Mac GUI):**
1. Log panel UI (data exists in ViewModel, no view renders it)
2. Task timer view (add/remove/configure multiple timers with hour/minute pickers)
3. Drag-and-drop task reordering
4. Add/delete tasks from UI
5. Copilot section (browse, download, import, display, configure, execute)
6. Utilities section (recruit recognition + config, depot with export, operator box, video recognition, gacha, mini-games)
7. Settings depth (touch mode, gzip, adb-lite, updater settings, system settings)
8. Resource update system with progress UI
9. Rich task configuration views (current views are stubs; Mac GUI has full Fight settings with stage picker/medicine/stone/drops/series/Penguin ID, full Roguelike settings with theme/difficulty/squad/roles/strategy/conditional UI, etc.)
10. Per-task status color indicators wired to UI

## Architecture Details — MaaGui

### Dependency Injection (App.axaml.cs)

```
Singletons: Root (config), IMaaService, AsstProxy, IPlatformServices,
            INotificationPoster, IHttpService, IMaaApiService, StageManager,
            LocalizationHelper
Transient:  MainViewModel, MainWindow, TaskQueueViewModel,
            CopilotViewModel, UtilitiesViewModel, SettingsViewModel
```

### Key File Paths (PathsHelper)

| Path | Location |
|------|----------|
| BaseDir | `AppDomain.CurrentDomain.BaseDirectory` |
| UserDataDir | `LocalApplicationData/MaaAssistantArknights` |
| ResourceDir | `BaseDir/resource` |
| ConfigDir | `UserDataDir/config` |
| CacheDir | `UserDataDir/cache` |
| DebugDir | `UserDataDir/debug` |
| DataDir | `UserDataDir/data` |

Config file: `UserDataDir/config/gui.new.json`

### Configuration System (ConfigFactory)

- JSON serialization with `System.Text.Json`, polymorphic task types via `[JsonDerivedType]`
- Debounced saves (200ms timer) with atomic file replacement
- Backup file (`gui.new.json.bak`) with fallback loading
- `Root` → `SpecificConfig[]` (named profiles) → `BaseTask[]` (polymorphic)
- Task types: `StartUpTask`, `FightTask`, `InfrastTask`, `MallTask`, `RecruitTask`, `AwardTask`, `RoguelikeTask`, `ReclamationTask`, `CopilotTask`, `CustomTask`

### MaaCore Interop

- `IMaaService` / `MaaService`: P/Invoke bindings using `[LibraryImport]` source generation; cross-platform library resolution via `NativeLibrary.SetDllImportResolver`
- `AsstProxy`: High-level wrapper managing handle lifecycle, `AsstApiCallback` marshalling, task status tracking (`ConcurrentDictionary`), connection state, async connect with timeout
- Callback flow: `MaaCore` → native callback → `AsstProxy.CallbackFunction` → `CallbackReceived` event → `TaskQueueViewModel.OnMaaCallback` (dispatched to UI thread)

### MVVM Pattern

- Uses `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`)
- Views are Avalonia `UserControl`s with compiled bindings (`x:DataType`)
- Navigation via `FluentAvalonia.NavigationView` with `IsVisible` bindings and `ObjectEqualConverter`

## Long-Term Goals

1. **Phase 1 (current):** Achieve **functional parity with MaaMacGui** — the Avalonia GUI should support every feature the macOS Swift GUI supports
2. **Phase 2+:** Achieve **full parity with MaaWpfGui** — the Avalonia GUI should eventually support every feature the WPF GUI supports, making it the single cross-platform replacement
3. **Design principles:**
   - Behavioral equivalence is mandatory at each phase
   - Visual familiarity with the WPF GUI is preferred but layout can diverge where it genuinely improves functionality
   - The Avalonia GUI should feel native on all platforms (Windows, macOS, Linux)

## Build & Run

### MaaCore (C++ engine)

```bash
# Configure and build
cmake --preset release
cmake --build build --config Release
cmake --install build  # Populates install/ directory
```

### MaaGui (Avalonia)

```bash
cd src/MaaGui
dotnet build
dotnet run
```

**Important:** MaaGui requires MaaCore native libraries to be present. Build MaaCore first, then the `MaaCoreRuntimeArtifacts.targets` MSBuild file copies the libraries to the output directory.

### Tests

```bash
cd src/MaaGui.Tests
dotnet test
```

## Conventions

- **Linting/Formatting:** The project uses Biome (not ESLint) for JS/TS contexts. For C#, StyleCop rules are configured in `src/MaaWpfGui/stylecop.json` and XamlStyler settings in `src/MaaWpfGui/Settings.XamlStyler`.
- **Localization:** String resources in AXAML resource dictionaries (`Localization/Strings/`). Access via `LocalizationHelper.GetString(key)`.
- **Task types:** Always use the `TaskType` enum and the polymorphic `BaseTask` hierarchy. New tasks must add a `[JsonDerivedType]` attribute to `BaseTask`.
- **Platform-specific code:** Use `IPlatformServices` interface + platform implementations. Use `#if WINDOWS` for compile-time platform conditionals in interop code.
- **Testing:** xUnit + coverlet. Test project references MaaGui directly. Target ≥80% coverage for new code.
