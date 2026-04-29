# STLHub — Usage Guide

This guide walks you through the main workflows in STLHub.

---

## Overview

[![STLHub Overview](https://img.youtube.com/vi/JodswTcvPKE/maxresdefault.jpg)](https://youtu.be/JodswTcvPKE)

STLHub is a desktop app for organizing large 3D model libraries. It indexes your `.stl`, `.3mf` and `.obj` files into a searchable, tagged catalogue so you can find any model in seconds.

---

## Importing Models

[![Adding Objects to STLHub](https://img.youtube.com/vi/9cmEnDykYqQ/maxresdefault.jpg)](https://youtu.be/9cmEnDykYqQ)

### Import files

Drag and drop individual `.stl`, `.3mf` or `.obj` files directly onto the main window. STLHub will:

1. Compute a file hash — duplicates are automatically skipped.
2. Copy the file into your library folder.
3. Generate a thumbnail in the background.
4. Register the model in the database, ready to search.

### Import a folder

Drag an entire folder (or folder tree) onto the window. STLHub maps the folder hierarchy to categories automatically:

- Each **subfolder** becomes a **category** (nested).
- Each **3D file** inside becomes an **object** in that category.
- Other files found alongside a 3D model (images, PDFs, G-code, ZIPs) are added as **attachments** to that object.
- Folders with no 3D files are ignored — no empty categories are created.

**Example:** dragging this structure:

```
Mechanics/
  Gears/
    gear_608.stl
    gear_608.pdf
  Bearings/
    bearing_v2.3mf
    instructions.pdf
```

produces:

| Category | Object | Attachments |
|---|---|---|
| Mechanics → Gears | `gear_608.stl` | `gear_608.pdf` |
| Mechanics → Bearings | `bearing_v2.3mf` | `instructions.pdf` |

---

## Browsing Your Library

Use the **category tree** on the left to navigate by category. Click any category to filter the main grid to objects in that branch.

Switch between **Grid view** (thumbnails) and **List view** (compact rows) using the toolbar buttons in the top-right corner.

---

## Searching

Type any term in the search bar to run a **full-text search** across:

- Object name
- Description
- Tags
- Original file name

Results are ranked by relevance and update instantly. Combine search with a category selection to narrow results further.

---

## Editing an Object

Click any object to open its detail panel. From there you can:

- **Rename** the object.
- **Add or edit a description.**
- **Manage tags** — add multiple tags for flexible cross-cutting classification.
- **Change the category.**
- **View attachments** — click any attachment to open it in its default application.
- **View the thumbnail** — click it to open the full-size image viewer.

---

## Sorting & Filtering

Use the **Sort** dropdown in the toolbar to order your library by:

- Name (A → Z / Z → A)
- Date imported (newest / oldest first)
