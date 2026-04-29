# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Environment

This project is developed on **Windows**. All automation scripts must be written in **PowerShell**. All documentation must be written in **English**.

## Build & Run

```bash
dotnet build
dotnet run --project src/STLHub
dotnet publish -c Release
```

After any code change, always run `dotnet build` to verify there are no compilation errors. If the app fails to start, check `fatal.log` in the app data directory for startup errors.

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10, C# 13 |
| UI | Avalonia UI 12 (MVVM, compiled bindings) |
| Database | SQLite with FTS5 via Dapper + Microsoft.Data.Sqlite |
| MVVM helpers | CommunityToolkit.Mvvm |
| Image processing | SixLabors.ImageSharp |

## Architecture

The app follows a layered MVVM architecture:

**Models** (`src/STLHub/Models/`) — plain data entities: `Object3D`, `Category`, `Tag`, `Attachment`, `ObjectTag`.

**Data** (`src/STLHub/Data/`) — `DatabaseInitializer` creates the SQLite schema on startup (including FTS5 triggers for `Object3D_FTS`); `ObjectRepository` handles all CRUD and FTS-based search queries.

**Services** (`src/STLHub/Services/`) — business logic decoupled from UI:
- `LibraryManager` — recursive folder import, hash-based duplicate detection, file copy to library path, orchestrates thumbnail generation
- `ThumbnailGenerator` — extracts embedded previews from `.3mf` ZIPs, renders `.stl`, falls back to a generated placeholder
- `UserSettings` — JSON-persisted settings in `%APPDATA%/STLHub/settings.json` (window state, theme, view preferences, recent repos)

**ViewModels** (`src/STLHub/ViewModels/`) — `MainWindowViewModel` (~941 lines) is the central orchestrator: search, sort, filter state, `CategoryNode` tree, and all commands. `ViewModelBase` is the abstract MVVM base.

**Views** (`src/STLHub/Views/`) — Avalonia XAML. `MainWindow.axaml` is the primary shell with grid/list modes, search bar, and category tree panel. Dialogs: `AboutDialog`, `ConfirmationDialog`, `WarningDialog`, `ImportProgressDialog`, `ImageViewerWindow`.

**Converters** (`src/STLHub/Converters/`) — `CategoryIdToNameConverter`, `FileTypeIconConverter`, `LocalImagePathConverter`.

**App entry** — `Program.cs` (STAThread, exception → `fatal.log`), `App.axaml.cs` (database init, repository wiring, VM instantiation, theme application).

### Data flow

- **Import:** `LibraryManager` hashes files → copies to library → calls `ThumbnailGenerator` → registers in `ObjectRepository` (SQLite)
- **Display:** `ObjectRepository` queries SQLite → `MainWindowViewModel` builds `ObservableCollection` → compiled bindings update views
- **Search:** FTS5 virtual table `Object3D_FTS` kept in sync by INSERT/UPDATE/DELETE triggers; queries ranked by relevance
- **Categories:** `CategoryNode` builds a recursive tree from the `Category` table (self-referencing `ParentCategoryId`)

## C# & Avalonia Guidelines

- Use idiomatic modern C#: auto-properties, pattern matching, expression-bodied members, nullable reference types.
- MVVM: all logic in ViewModels; avoid code-behind except for UI-only interactions that cannot be data-bound.
- Always use `x:DataType` in AXAML for strongly-typed bindings; prefer `CompiledBinding`.
- Use `partial` classes for code-behind only when unavoidable.
- Use `StyledProperty`/`DirectProperty` for custom Avalonia controls.
- Use `DynamicResource` for theme-aware resources, `StaticResource` for fixed assets.
- Layout: prefer `Grid`, `StackPanel`, `DockPanel`; use `Margin`/`Padding` for spacing.
- All public APIs must have XML doc comments in English.
- Keep XAML clean — remove unused namespaces and attributes.
