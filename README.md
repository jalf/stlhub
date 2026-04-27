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

**STLHub** is a desktop application for managing large 3D object libraries. It lets makers, designers and engineers import, tag, search and organize `.stl`, `.3mf` and `.obj` files — so you never lose track of a model again.

## Features

- **Import files & folders** — drag and drop files or entire folder trees; folder structure is automatically mapped to hierarchical categories.
- **Automatic thumbnails** — preview images are generated in background for every imported model.
- **Full-text search** — find models instantly by name, description, tags or file name (powered by SQLite FTS5).
- **Hierarchical categories** — organize objects in a tree of categories and subcategories.
- **Tags** — assign multiple tags to any object for flexible cross-cutting classification.
- **Attachments** — associate images, G-code, PDFs, instructions and other files to each 3D object.
- **Duplicate detection** — file hashes prevent the same model from being imported twice.
- **Grid / List view** — browse your library with thumbnails, names, tags and dates.

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | [Avalonia UI](https://avaloniaui.net/) 12 |
| Runtime | .NET 10 |
| Database | SQLite + FTS5 |
| ORM / Data | Dapper + Microsoft.Data.Sqlite |
| MVVM | CommunityToolkit.Mvvm |
| Image Processing | SixLabors.ImageSharp |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run

```bash
git clone https://github.com/your-user/stlhub.git
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

## Roadmap

- [ ] Interactive 3D viewer
- [ ] AI-powered auto-tagging
- [ ] Cloud sync
- [ ] Thingiverse / Printables integration
- [ ] Model versioning

## License

This project is licensed under the MIT License.
