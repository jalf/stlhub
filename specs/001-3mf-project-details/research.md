# Research: 3MF Project Details Display

**Branch**: `001-3mf-project-details` | **Date**: 2026-08-11

---

## R-001: 3MF Archive Structure & Metadata Location

### Decision
Read metadata from two sources in priority order: (1) slicer-specific files (Bambu/Orca, PrusaSlicer), (2) the standard 3MF core metadata block in `3D/3dmodel.model`.

### Findings

**Standard 3MF Core Specification** (`3D/3dmodel.model`):
```xml
<model xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
  <metadata name="Title">My Model</metadata>
  <metadata name="Designer">Author Name</metadata>
  <metadata name="Description">A useful part</metadata>
  <metadata name="CreationDate">2024-01-15</metadata>
</model>
```
Standard metadata names: `Title`, `Designer`, `Description`, `Copyright`, `LicenseTerms`, `CreationDate`, `ModificationDate`.  
XNamespace: `http://schemas.microsoft.com/3dmanufacturing/core/2015/02`

**Bambu Studio / Orca Slicer**:
- `Metadata/project_settings.config` — **JSON** file with print profile fields:
  ```json
  { "print_settings_id": "0.20mm Quality @BBL X1C", "layer_height": "0.2",
    "wall_loops": "3", "sparse_infill_density": "15%", ... }
  ```
- `Metadata/model_settings.config` — XML with per-object settings; model-level title may appear in standard `3D/3dmodel.model` metadata.
- Profile author is typically not present in Bambu files; omit if absent (consistent with FR-005 sparse rendering).

**PrusaSlicer** (3MF with Slic3r PE config):
- `Metadata/Slic3r_PE.config` — **INI** format with print settings:
  ```ini
  print_settings_id = 0.20mm QUALITY
  layer_height = 0.2
  perimeters = 3
  fill_density = 15%
  ```
- Model metadata (title, designer) is written to the standard `3D/3dmodel.model` metadata block.

### Mapping to FR-002 / FR-005 fields

| Field | Standard 3MF | Bambu JSON key | PrusaSlicer INI key |
|-------|-------------|---------------|-------------------|
| Model Title | `Title` metadata | — | — |
| Author/Designer | `Designer` metadata | — | — |
| Description | `Description` metadata | — | — |
| Creation Date | `CreationDate` metadata | — | — |
| Profile Name | — | `print_settings_id` | `print_settings_id` |
| Layer Height | — | `layer_height` | `layer_height` |
| Wall Count | — | `wall_loops` | `perimeters` |
| Infill % | — | `sparse_infill_density` | `fill_density` |
| Profile Author | — | *(absent)* | *(absent)* |

### Alternatives Considered
- Parsing `Metadata/model_settings.config` for Bambu object names — rejected; that file contains per-mesh settings, not project identity.
- Using a third-party 3MF library — rejected; `System.IO.Compression` + `System.Xml.Linq` already in project, no new dependency needed for this scope.

---

## R-002: Async Loading Pattern in Avalonia MVVM

### Decision
Reuse the existing `CancellationTokenSource` cancellation pattern already used in `LoadItemsAsync`. Fire-and-forget via `_ = LoadProjectInfoAsync(ct)` inside `OnSelectedObjectChanged`.

### Findings
`MainWindowViewModel` already uses:
```csharp
private CancellationTokenSource? _loadCts;
// ...
_loadCts?.Cancel();
_loadCts = new CancellationTokenSource();
_ = LoadItemsAsync(searchTerm, _loadCts.Token);
```
The same pattern is safe for metadata loading. A separate `CancellationTokenSource` (`_projectInfoCts`) is needed so cancelling item loads does not cancel a concurrent metadata load.

Marshalling back to the UI thread: use `Dispatcher.UIThread.Post(...)` after async file I/O completes — same pattern used in `ImportProgressDialog`.

### Alternatives Considered
- `ICommand` with async relay command — rejected; selection change is not user-initiated command, it is a property-changed side-effect.
- Background worker / Task.Run — rejected; `ZipFile.OpenRead` + `XDocument.Load` are fast enough (<10ms for metadata-only reads) that wrapping in `Task.Run` would add overhead with no benefit. The async pattern here is for cancellability, not parallelism.

---

## R-003: ViewModel / UI Architecture for Conditional Section

### Decision
Add a `ThreeMfProjectInfo` record to hold extracted data. Expose it as an observable property `ProjectInfo3mf` on `MainWindowViewModel`. Drive visibility from `IsProjectInfo3mfVisible` (computed) and `IsProjectInfo3mfLoading` (observable bool).

### Findings
- CommunityToolkit.Mvvm `[ObservableProperty]` generates `OnXxxChanged` partial methods, making it straightforward to trigger async loads from property setters.
- Avalonia compiled bindings require `x:DataType` — the Details panel already uses `DataContext` from `MainWindowViewModel`, so all new bindings are first-class.
- Avalonia does not ship a built-in `ProgressRing` / `ActivityIndicator` out of the box in v12. Use an `Ellipse` animated via a `RotateTransform` with `Animation`, or use a `ProgressBar` in indeterminate mode (`IsIndeterminate="True"`) — the latter is simpler and already available.

### Alternatives Considered
- Separate `ThreeMfDetailsViewModel` — rejected; single-object scope, no reuse needed, adding a nested VM adds complexity with no payoff.
- Persisting metadata to SQLite and loading from DB — rejected per spec assumption; metadata is fetched fresh on each selection.

---

## R-004: JSON Parsing for Bambu project_settings.config

### Decision
Use `System.Text.Json.JsonDocument` (built into .NET 10, no new dependency) with `TryGetProperty` for safe optional field access.

### Alternatives Considered
- `Newtonsoft.Json` — not in project; would add a dependency.
- Manual string scanning — fragile; rejected.

---

## R-005: INI Parsing for PrusaSlicer Slic3r_PE.config

### Decision
Parse with a minimal hand-rolled key=value reader (split on `=`, trim whitespace). PrusaSlicer INI has no sections for the fields we need, making a regex or line-split approach sufficient and self-contained.

### Alternatives Considered
- Third-party INI parser — not worth a new dependency for ~5 key lookups.
