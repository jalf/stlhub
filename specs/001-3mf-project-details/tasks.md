# Tasks: 3MF Project Details Display

**Input**: Design documents from `specs/001-3mf-project-details/`

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no unresolved dependencies)
- **[US1]** / **[US2]**: User story this task belongs to
- Setup/Foundational/Polish phases have no story label

---

## Phase 1: Setup

**Purpose**: Create new source files that all subsequent phases depend on.

- [X] T001 Create `ThreeMfProjectInfo` sealed record with all nullable string fields and `HasAnyMetadata` / `HasAnyProfileData` computed properties in `src/STLHub/Models/ThreeMfProjectInfo.cs`

**Checkpoint**: `ThreeMfProjectInfo.cs` exists with full field set — all subsequent tasks can now reference this type.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core service skeleton + ViewModel wiring that both user stories depend on. MUST be complete before US1 or US2 UI work begins.

**⚠️ CRITICAL**: T002 and T003 can run in parallel (different files). T004 requires both to be complete.

- [X] T002 [P] Implement `ThreeMfMetadataReader.ReadAsync` with ZIP-open, standard `3D/3dmodel.model` XML metadata parsing (`Title`, `Designer`, `Description`, `CreationDate` via `System.Xml.Linq`), empty-string normalization, and top-level exception catch returning `null` in `src/STLHub/Services/ThreeMfMetadataReader.cs`
- [X] T003 [P] Add `_projectInfoCts` field, `[ObservableProperty] ThreeMfProjectInfo? _projectInfo3mf`, `[ObservableProperty] bool _isProjectInfo3mfLoading`, and computed `IsProjectInfo3mfVisible` property to `src/STLHub/ViewModels/MainWindowViewModel.cs`
- [X] T004 Implement `LoadProjectInfoAsync(string filePath, CancellationToken ct)` method and extend `OnSelectedObjectChanged` to cancel previous CTS, reset properties, and fire `_ = LoadProjectInfoAsync(...)` when `FileType` is `.3mf`, marshalling results back via `Dispatcher.UIThread.Post` in `src/STLHub/ViewModels/MainWindowViewModel.cs`

**Checkpoint**: Foundation ready — `ThreeMfMetadataReader` reads standard 3MF metadata, ViewModel exposes correct observable properties and triggers async loads on selection. US1 and US2 implementation can now proceed.

---

## Phase 3: User Story 1 — View 3MF Project Metadata in Details Panel (Priority: P1) 🎯 MVP

**Goal**: When a `.3mf` object is selected, the Details panel shows a "Informações 3MF" section at the bottom with a loading indicator while fetching, then identity metadata fields (Title, Designer, Description, CreationDate) — each field row only rendered if the value is non-null. Section fully hidden for non-3MF files and when no metadata is found.

**Independent Test**: Select any `.3mf` file with standard metadata. The "Informações 3MF" section must appear below "Arquivos Associados" with at least one field displayed.

- [X] T005 [US1] Add "Informações 3MF" section to the Details panel `StackPanel` in `src/STLHub/Views/MainWindow.axaml`: outer `StackPanel` with `IsVisible="{Binding IsProjectInfo3mfVisible}"`, separator `Border`, section header `TextBlock`, indeterminate `ProgressBar` (`IsVisible="{Binding IsProjectInfo3mfLoading}"`), and inner `StackPanel` (`IsVisible="{Binding !IsProjectInfo3mfLoading}"`) containing sparse field rows for `Title`, `Designer`, `Description`, and `CreationDate` (each row wrapped in `StackPanel IsVisible="{Binding ProjectInfo3mf.FieldName, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"`)  — positioned after the "Anexar Arquivo..." `Button` and before the `ScrollViewer` closes
- [X] T006 [US1] Run `dotnet build` from repo root and resolve any compilation errors; then manually verify US1 by selecting a `.3mf` file in the running app and confirming the section appears with identity fields, and disappears for `.stl` / `.obj` selections

**Checkpoint**: US1 fully functional — identity metadata displayed from standard 3MF files, loading indicator visible during fetch, section hidden when not applicable.

---

## Phase 4: User Story 2 — View Print Profile Details from 3MF (Priority: P2)

**Goal**: For `.3mf` files from Bambu Studio/Orca Slicer or PrusaSlicer, the "Informações 3MF" section also shows a "Perfil de Impressão" sub-section with profile name, layer height, wall count, infill %, and profile author — each field row only rendered if the value is present.

**Independent Test**: Select a `.3mf` exported from Bambu Studio or PrusaSlicer. The profile sub-section must appear with at least `ProfileName` and `LayerHeight` populated.

**⚠️ NOTE**: T007 (JSON parser) and T009 (AXAML profile rows) can run in parallel after T005/T006 since they touch different files.

- [X] T007 [US2] Implement Bambu Studio / Orca Slicer JSON profile parser: probe for `Metadata/project_settings.config` entry in the ZIP; if present, read stream, parse with `System.Text.Json.JsonDocument`, extract `print_settings_id` → `ProfileName`, `layer_height` → `LayerHeight`, `wall_loops` → `WallCount`, `sparse_infill_density` → `InfillPercent`, merge into `ThreeMfProjectInfo` in `src/STLHub/Services/ThreeMfMetadataReader.cs`
- [X] T008 [US2] Implement PrusaSlicer INI profile parser: probe for `Metadata/Slic3r_PE.config` entry in the ZIP; if present, read stream, parse line-by-line `key = value` format, extract `print_settings_id` → `ProfileName`, `layer_height` → `LayerHeight`, `perimeters` → `WallCount`, `fill_density` → `InfillPercent`, merge into `ThreeMfProjectInfo` in `src/STLHub/Services/ThreeMfMetadataReader.cs` (after T007, same file)
- [X] T009 [US2] Add "Perfil de Impressão" sub-section inside the existing inner `StackPanel` (after `CreationDate` row) in `src/STLHub/Views/MainWindow.axaml`: outer `StackPanel IsVisible="{Binding ProjectInfo3mf.HasAnyProfileData}"` containing sparse field rows for `ProfileName`, `LayerHeight`, `WallCount`, `InfillPercent`, `ProfileAuthor` — same `StringConverters.IsNotNullOrEmpty` pattern as identity fields (parallel with T007/T008 only; requires T005 complete)
- [X] T010 [US2] Run `dotnet build` from repo root and resolve any compilation errors; then manually verify US2 by selecting a Bambu Studio `.3mf` file (profile fields appear) and a PrusaSlicer `.3mf` file (profile fields appear) in the running app

**Checkpoint**: US2 fully functional — print profile data displayed for Bambu/Orca and PrusaSlicer files; absent fields produce no empty rows.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validate edge cases and error handling across both stories.

- [X] T011 [P] Verify error handling (FR-007): create a corrupt test fixture by copying any `.3mf` file and truncating it (e.g., open in a hex editor and zero out the last 2KB, or simply rename a `.txt` file to `.3mf`); import the corrupt file into the library; select it and confirm the Details panel loads without an unhandled exception and the "Informações 3MF" section is absent
- [X] T012 [P] Verify empty-state behaviour (FR-009): use a `.3mf` file exported from a CAD tool that writes no slicer metadata (e.g., FreeCAD or Fusion 360 generic 3MF export), or strip the `<metadata>` entries from `3D/3dmodel.model` manually; import it; select it and confirm the entire "Informações 3MF" section — including its header — is fully invisible; also confirm the same for any `.stl` selection

**Checkpoint**: All FR-007, FR-009, SC-003, SC-004 acceptance criteria met.

---

## Phase 6: Description Rendering (follow-on, FR-010 / FR-011)

**Purpose**: Real 3MF files store the description as HTML escaped to varying depths, so the panel showed entity soup. Added after a tag survey of all 608 `.3mf` files in the reference library.

- [X] T013 Add `HtmlText.Decode` (decode until stable) in `src/STLHub/Services/HtmlText.cs` and apply it from `ThreeMfMetadataReader.Normalize` so every metadata field is decoded (fixes `Mother&apos;s Day Statue`)
- [X] T014 Add the `DescriptionDocument` block/inline model in `src/STLHub/Models/DescriptionDocument.cs`
- [X] T015 Add `HtmlDescriptionParser` in `src/STLHub/Services/HtmlDescriptionParser.cs` covering the tag subset found in the library (p, br, strong/b, em/i, a, ul/li, h1–h6, figure, div, span, pre, img, oembed, MakerWorld `boost*`), dropping `&nbsp;` spacer paragraphs and degrading unknown markup to text
- [X] T016 Add `DescriptionViewerWindow` (`.axaml` + `.axaml.cs`) rendering the document with themed text, clickable links and async-downloaded remote images; minimize/maximize boxes stripped via `SetWindowLongPtr` so it reads as a dialog while staying resizable
- [X] T017 Replace the inline description text in `MainWindow.axaml` with a "Ver descrição do projeto" button wired to `ViewDescription_Click`
- [X] T019 Move the metadata fields into a `ContentControl` + `DataTemplate` (`x:DataType="models:ThreeMfProjectInfo"`) in `MainWindow.axaml`, so the field bindings are never resolved through a null `ProjectInfo3mf` — this was logging ~19 `[Binding]` path errors on every non-`.3mf` selection and building controls that were never shown
- [X] T018 Add `HtmlDescriptionParser.HasMarkup` (matching only recognized tag names, so bracketed plain text stays plain) plus `HasPlainDescription3mf` / `HasRichDescription3mf` on `MainWindowViewModel`, and split the panel so markup-free descriptions render inline while HTML ones keep the button

**Checkpoint**: Descriptions render with formatting, links and images in a modal; no raw markup reaches the Details panel.

---

## Dependencies

```
T001
 ├── T002 (parallel)
 └── T003 (parallel)
      T002 + T003 → T004
                    T004 → T005 → T006  (US1 complete)
                    T004 → T007 → T008  (US2 service)
                    T005 → T009         (US2 AXAML, different file from T007 — can overlap)
                    T008 + T009 → T010  (US2 complete)
                    T010 → T011 (parallel)
                           T012 (parallel)
```

## Parallel Execution Examples

| When | Can run in parallel |
|------|---------------------|
| After T001 | T002 (service) + T003 (ViewModel) |
| After T004 + T005 | T007 (JSON parser, C#) + T009 (AXAML profile rows) |
| After T010 | T011 (error handling check) + T012 (empty-state check) |

---

## Implementation Strategy

**MVP**: Complete Phases 1–3 (T001–T006) for a deployable increment. Users can view standard 3MF identity metadata immediately. US2 (print profiles) is a self-contained follow-on.

**Incremental delivery**:
1. T001–T004 (foundational) — no visible UI change yet, builds cleanly
2. T005–T006 (US1) — "Informações 3MF" section live for standard 3MF files
3. T007–T010 (US2) — print profile data added for Bambu/PrusaSlicer files
4. T011–T012 (polish) — edge cases verified

---

## Summary

| Phase | Tasks | Story | Parallel opportunities |
|-------|-------|-------|----------------------|
| Setup | T001 | — | — |
| Foundational | T002, T003, T004 | — | T002 ‖ T003 |
| US1 (P1, MVP) | T005, T006 | US1 | — |
| US2 (P2) | T007, T008, T009, T010 | US2 | T007 ‖ T009 (after T005) |
| Polish | T011, T012 | — | T011 ‖ T012 |
| **Total** | **12** | | **3 parallel pairs** |

