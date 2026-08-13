# Data Model: 3MF Project Details Display

**Branch**: `001-3mf-project-details` | **Date**: 2026-08-11

---

## Entity: `ThreeMfProjectInfo`

A transient (non-persisted) record that holds metadata extracted from a single `.3mf` archive. Created by `ThreeMfMetadataReader` and exposed on the ViewModel. Never written to the database.

```
ThreeMfProjectInfo
├── Title          : string?   — model name from <metadata name="Title">
├── Designer       : string?   — author/creator from <metadata name="Designer">
├── Description    : string?   — project description from <metadata name="Description">
├── CreationDate   : string?   — raw date string from <metadata name="CreationDate"> (display as-is)
│
├── ProfileName    : string?   — print profile name (Bambu: print_settings_id, PS: print_settings_id)
├── LayerHeight    : string?   — layer height with unit (e.g. "0.2 mm")
├── WallCount      : string?   — wall/perimeter count
├── InfillPercent  : string?   — infill density (e.g. "15%")
└── ProfileAuthor  : string?   — profile creator name (absent in most slicers; omit if null)
```

**Notes:**
- All fields are nullable strings. `null` means the field was absent in the file. `string.Empty` is treated as absent (normalized to `null` during extraction).
- No validation rules; all values are read-only metadata from the file. The only transformation is HTML entity decoding, applied repeatedly until stable — producers escape to varying depths (Bambu Studio double-escapes), so without it titles show as `Mother&apos;s Day Statue` and descriptions as entity soup.
- `Description` may hold plain text or HTML markup. `HtmlDescriptionParser.HasMarkup` decides which: plain descriptions render inline in the Details panel, HTML ones behind a button that opens `DescriptionViewerWindow`.
- `HasAnyMetadata` : computed bool — `true` if at least one field is non-null. Used to decide whether the section is shown (FR-009).
- `HasAnyProfileData` : computed bool — `true` if at least one profile field is non-null. Used to show/hide the profile sub-section.

---

## ViewModel State: `MainWindowViewModel` additions

Three new observable properties on the existing `MainWindowViewModel`:

| Property | Type | Description |
|---|---|---|
| `ProjectInfo3mf` | `ThreeMfProjectInfo?` | Extracted info for the current selection; `null` when not a `.3mf` or not yet loaded |
| `IsProjectInfo3mfLoading` | `bool` | `true` while async extraction is in progress |
| `IsProjectInfo3mfVisible` | `bool` | Computed: `SelectedObject?.FileType == ".3mf" && (IsProjectInfo3mfLoading || ProjectInfo3mf?.HasAnyMetadata == true)` |

**State transitions on `OnSelectedObjectChanged`:**

```
Object selected (non-3MF)  → ProjectInfo3mf = null, IsProjectInfo3mfLoading = false
Object selected (is .3mf)  → ProjectInfo3mf = null, IsProjectInfo3mfLoading = true → [async] → ProjectInfo3mf = result (or null), IsProjectInfo3mfLoading = false
Object deselected (null)   → ProjectInfo3mf = null, IsProjectInfo3mfLoading = false
```

**Cancellation:** a dedicated `_projectInfoCts : CancellationTokenSource?` field. Cancelled and re-created on each selection change (same pattern as `_loadCts`).

---

## Service: `ThreeMfMetadataReader`

Static class in `Services/`. No dependencies other than `System.IO.Compression`, `System.Xml.Linq`, `System.Text.Json`.

```
ThreeMfMetadataReader
└── ReadAsync(filePath: string, ct: CancellationToken) → Task<ThreeMfProjectInfo?>
      ├── Opens ZipArchive (read-only)
      ├── Reads standard metadata from 3D/3dmodel.model → fills Title, Designer, Description, CreationDate
      ├── Detects slicer type by probing entry names:
      │     Bambu/Orca:    Metadata/project_settings.config (JSON)
      │     PrusaSlicer:   Metadata/Slic3r_PE.config (INI)
      ├── Parses profile fields → fills ProfileName, LayerHeight, WallCount, InfillPercent, ProfileAuthor
      └── Returns ThreeMfProjectInfo (with any non-null fields), or null on any exception
```

Extraction priority (FR-004): slicer-specific profile fields always supplement (never override) standard metadata identity fields; there is no conflict between the two sources since they cover different field groups.

---

## File Structure

```
src/STLHub/
├── Models/
│   └── ThreeMfProjectInfo.cs       ← NEW: transient record
├── Services/
│   └── ThreeMfMetadataReader.cs    ← NEW: static reader service
├── ViewModels/
│   └── MainWindowViewModel.cs      ← MODIFIED: 3 new properties + async load logic
└── Views/
    └── MainWindow.axaml            ← MODIFIED: "Informações 3MF" section added at bottom of Details panel
```

No database schema changes. No new NuGet packages.

---

## Relationships

```
MainWindowViewModel
  SelectedObject : Object3D?
  ProjectInfo3mf : ThreeMfProjectInfo?   ← loaded via ThreeMfMetadataReader.ReadAsync()
                                            when SelectedObject.FileType == ".3mf"
```

`ThreeMfProjectInfo` has no foreign keys or relationships; it is ephemeral per selection.
