# Quickstart: 3MF Project Details Display

**Branch**: `001-3mf-project-details` | **Date**: 2026-08-11

---

## Prerequisites

- .NET 10 SDK
- Windows 10+, Visual Studio 2022 or VS Code with C# Dev Kit

No new NuGet packages required. All dependencies (`System.IO.Compression`, `System.Xml.Linq`, `System.Text.Json`) are part of .NET 10.

---

## Build & Run

```powershell
cd c:\dev\stlhub
dotnet build
dotnet run --project src/STLHub
```

---

## How to Test the Feature Manually

1. **Import a `.3mf` file** from Bambu Studio or PrusaSlicer into the library (File → Import Folder, or drag-drop).
2. **Click on the imported object** in the library grid or list.
3. The **Details panel** (right side) opens. Scroll to the bottom.
4. The **"Informações 3MF"** section appears below "Arquivos Associados" with the extracted metadata fields.
5. Select a non-3MF object (`.stl`, `.obj`) — the section must be invisible.
6. Select a `.3mf` file with no embedded metadata — the section must be invisible.

---

## Key Files

| File | Role |
|------|------|
| [src/STLHub/Models/ThreeMfProjectInfo.cs](../../src/STLHub/Models/ThreeMfProjectInfo.cs) | Transient data record for extracted 3MF metadata |
| [src/STLHub/Services/ThreeMfMetadataReader.cs](../../src/STLHub/Services/ThreeMfMetadataReader.cs) | Reads and parses metadata from a `.3mf` ZIP archive |
| [src/STLHub/ViewModels/MainWindowViewModel.cs](../../src/STLHub/ViewModels/MainWindowViewModel.cs) | Exposes `ProjectInfo3mf`, `IsProjectInfo3mfLoading`, triggers async load on selection change |
| [src/STLHub/Views/MainWindow.axaml](../../src/STLHub/Views/MainWindow.axaml) | "Informações 3MF" section at the bottom of the Details panel |

---

## Slicer Test Files

| Slicer | Expected fields present |
|--------|------------------------|
| Bambu Studio 1.x | `ProfileName`, `LayerHeight`, `WallCount`, `InfillPercent` (from `Metadata/project_settings.config` JSON) |
| Orca Slicer | Same as Bambu Studio |
| PrusaSlicer 2.x | `Title`, `Designer`, `ProfileName`, `LayerHeight`, `WallCount`, `InfillPercent` (standard 3MF + `Metadata/Slic3r_PE.config` INI) |
| Generic 3MF | `Title`, `Designer`, `Description` only (standard metadata block in `3D/3dmodel.model`) |
