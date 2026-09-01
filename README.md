<p align="center">
  <img src="docs/logo.png" alt="STLHub Logo" width="400">
</p>

<h1 align="center">STLHub</h1>

<p align="center">
  <strong>Organize, catalogue and find your 3D models in seconds.</strong>
</p>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet">
  <img alt="Avalonia UI" src="https://img.shields.io/badge/Avalonia_UI-12-blueviolet">
  <img alt="SQLite" src="https://img.shields.io/badge/SQLite-FTS5-003B57?logo=sqlite">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green">
</p>

---

<p align="center">
  <img src="docs/screenshot.png" alt="STLHub Screenshot" width="900">
</p>

## About

**STLHub** is an desktop application for managing large 3D object libraries. It lets makers, designers and engineers import, tag, search and organize `.stl`, `.3mf` and `.obj` files — so you never lose track of a model again.

## Features

- **Import files & folders** — drag and drop files or entire folder trees; folder structure is automatically mapped to hierarchical categories.
- **Automatic thumbnails** — preview images are generated in background for every imported model.
- **Full-text search** — find models instantly by name, description, tags or file name (powered by SQLite FTS5).
- **Hierarchical categories** — organize objects in a tree of categories and subcategories.
- **Tags** — assign multiple tags to any object for flexible cross-cutting classification.
- **Attachments** — associate images, G-code, PDFs, instructions and other files to each 3D object.
- **Duplicate detection** — file hashes prevent the same model from being imported twice.

## Download

<p align="center">
  <a href="https://github.com/jalf/stlhub/releases/latest">
    <img src="https://img.shields.io/badge/⬇%20Download%20STLHub-2D7FF9?style=for-the-badge" alt="Download STLHub">
  </a>
</p>

<p align="center">
  <em>Free and open source. Windows, Linux and macOS.</em><br>
  <em>Nothing else to install — no .NET, no dependencies, no account.</em>
</p>

### Windows — 3 steps

1. **[Download the installer](https://github.com/jalf/stlhub/releases/latest)** — on the release page, click the file named `STLHub-Setup-x.x.exe`.
2. **Run the file** you just downloaded and follow the wizard.
3. **Open STLHub** from the Start menu. Click **Abrir** to pick a folder where your library will live, then drag your 3D model folders onto the window.

> [!NOTE]
> Windows may show a blue **"Windows protected your PC"** screen when you run the installer. This happens because the installer is not code-signed (a paid certificate we don't have), **not** because anything is wrong with it. Click **More info** → **Run anyway** to continue.

Prefer not to install anything? Download `STLHub-win-x64.zip` instead, unzip it anywhere, and double-click `STLHub.exe` — it runs as-is and can be removed by deleting the folder.

<details>
<summary><strong>macOS</strong></summary>

<br>

1. **[Download the disk image](https://github.com/jalf/stlhub/releases/latest)** for your Mac:

   | Mac | File |
   |---|---|
   | Apple Silicon (M1/M2/M3/M4) | `STLHub-osx-arm64.dmg` |
   | Intel | `STLHub-osx-x64.dmg` |

2. **Open the `.dmg`** and drag **STLHub** onto the **Applications** folder.
3. **First launch:** right-click STLHub in Applications → **Open** → **Open**. macOS asks only once, because the app is signed but not notarized (an Apple Developer subscription we don't have) — **not** because anything is wrong with it.

If macOS still says the app "is damaged" or "can't be opened", clear the quarantine flag once:

```bash
xattr -dr com.apple.quarantine /Applications/STLHub.app
```

Prefer the command line? Download `STLHub-osx-<arch>.tar.gz` instead — it contains the same `STLHub.app`; extract it anywhere and run `open STLHub.app`.

</details>

<details>
<summary><strong>Linux</strong></summary>

<br>

Download `STLHub-linux-x64.tar.gz` from the [latest release](https://github.com/jalf/stlhub/releases/latest), then:

```bash
tar -xzf STLHub-linux-x64.tar.gz
chmod +x STLHub
./STLHub
```

</details>

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | [Avalonia UI](https://avaloniaui.net/) 12 |
| Runtime | .NET 10 |
| Database | SQLite + FTS5 |
| ORM / Data | Dapper + Microsoft.Data.Sqlite |
| MVVM | CommunityToolkit.Mvvm |
| Image Processing | SixLabors.ImageSharp |

## Building from Source

> Only needed if you want to modify STLHub. To simply use it, see [Download](#download) above.

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/jalf/stlhub.git
cd stlhub
dotnet build
dotnet run --project src/STLHub
```

## Project Structure

```
stlhub/
├── docs/               # Documentation & PRD
├── src/
│   └── STLHub/
│       ├── Converters/  # Value converters
│       ├── Data/        # Database access & initialization
│       ├── Models/      # Domain models (Object3D, Category, Tag…)
│       ├── Services/    # Business logic (LibraryManager, ThumbnailGenerator…)
│       ├── ViewModels/  # MVVM view models
│       └── Views/       # Avalonia XAML views
└── Scratch/             # Experimental / prototype code
```

## Data Model

```
Category  (Id, Name, ParentCategoryId, Path, SortOrder)
Object3D  (Id, Name, Description, MainFilePath, FileType, ThumbnailPath, Hash, CategoryId, CreatedAt)
Tag       (Id, Name)
ObjectTag (ObjectId, TagId)
Attachment(Id, ObjectId, FilePath, Type)
```

## Documentation

- [Usage Guide](docs/USAGE.md) — step-by-step instructions with video walkthroughs for importing, searching and managing your library.

## Roadmap

- [ ] AI-powered auto-tagging
- [ ] Cloud sync
- [ ] Thingiverse / Printables integration
- [ ] Model versioning

## License

This project is licensed under the MIT License.
