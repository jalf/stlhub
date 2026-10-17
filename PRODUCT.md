# Product

<!-- impeccable:product-schema 1 -->

> Distilled product context for design work. The full feature specification (Portuguese) lives in [docs/PRD.md](docs/PRD.md); this file holds the durable product truth that design decisions depend on.

## Platform

web

`web` is Impeccable's value for a custom, non-native design language; STLHub is not a website. It is a cross-platform desktop app (Avalonia UI 12, custom-drawn). Shipped for Windows (primary development platform; installer and portable zip), macOS (signed `.app`/`.dmg`, arm64 and x64) and Linux (tarball).

Design stance: **one STLHub look on every OS.** The app does not adapt its design language per platform. OS conventions are respected only at integration points: the native macOS application menu, platform keyboard shortcuts, window chrome, and system file/folder dialogs.

## Users

Primary: makers and 3D-printing hobbyists who keep large personal libraries of model files gathered over years from their own work and community sites (Printables, Thingiverse, MakerWorld). Secondary: 3D designers, mechanical/product engineers, prototyping studios, and small businesses with CAD/STL archives.

They open STLHub at the workbench or at their work computer, just before or after a print session. The job: find the right model fast, or get a fresh batch of downloads organized so it never gets lost.

They know the maker community well and have strong, rich references for how a model library should look.

## Product Purpose

Organize, catalogue, and find 3D models in seconds. The library is the maker's creative collection; STLHub exists so no model is ever lost.

Problems it removes: files lost across messy folders, useless file names (`final_v2_ok.stl`), old models that can't be found, no quick visual preview, models disconnected from their photos and instructions, and re-downloading or re-modelling something the user already has.

Success: open the app, find the right model, and send it to the printer in under 30 seconds. Target metrics from the PRD: average time to find a file under 10 seconds, share of the library tagged, weekly retention, and imports per user.

## Positioning

A local-first catalogue for the files already sitting on the user's disk:

- No account, no cloud, no runtime to install. Free and open source (MIT).
- Dragging in a folder tree turns it into a category tree. Sibling files (photos, PDFs, G-code, ZIPs) become attachments of the model next to them.
- Hash-based duplicate detection stops the same model from being imported twice under different names.
- 3MF projects show the metadata already embedded in them (title, designer, description, print profile, layer height, walls, infill), so the user doesn't have to open a slicer.

## Operating Context

- **Library (repository):** a folder on local disk that STLHub owns. Imported files are copied into it, and paths are stored relative to it. Users can switch between libraries (up to 10 recent) and open external ones. An external library that stalls falls back to the default.
- **Import:** drag and drop files or whole folder trees onto the main window. Folders with no 3D files are ignored, and no empty categories are created. Thumbnails generate in the background.
- **Retrieve:** type a query, narrow by category and file type, open the item, then open its folder or use the file directly in a slicer.
- **Surrounding tools:** slicers (Bambu Studio, PrusaSlicer, OrcaSlicer), the user's file manager, and community model sites.

## Capabilities and Constraints

- **Formats:** `.stl`, `.3mf`, `.obj`, `.step`/`.stp`. Thumbnails come from the preview embedded in a 3MF, from rendered STL meshes, or from isometric wireframes for STEP; anything else gets a generated placeholder.
- **Metadata:** name, description, multiple tags, one hierarchical category (optional), import date, attachments, thumbnail.
- **Categories:** nested tree; expand, rename, and move by drag and drop.
- **Search:** full-text (SQLite FTS5) over name, description, tags, and file name, ranked by relevance, combinable with category and file-type filters. Target: under 300 ms with 10k files. Libraries over 100k files are a known risk.
- **Views:** grid (thumbnails, with size options) and list. A details panel shows a larger preview, metadata, attachments, and 3MF info.
- **Themes:** multiple themes, including both dark and light.
- **Language:** the UI currently ships in Brazilian Portuguese only, with no localization layer. English/multi-language support is planned, so new UI copy should be written so it can be extracted later (no strings baked into logic or assembled from fragments). Code, docs, and comments are in English.
- **Undecided / future (do not present as shipped):** AI auto-tagging, cloud sync, Thingiverse/Printables integration, model versioning, a personal marketplace, edit history, and catalogue backup/export.

## Brand Commitments

- **Name:** STLHub. Tagline: "Organize, catalogue and find your 3D models in seconds." Logo assets: `docs/logo.png`, `src/STLHub/Assets/logo.png`.
- **Personality:** creative, expressive, vibrant. It should feel like an organized, living workshop, with the energy of making rather than administering.
- **Anti-references:** generic dev tools such as VS Code (cold grey palette, Microsoft-blue accent, IDE look); corporate SaaS dashboards (identical cards, giant-number metrics, accent gradients); gamer/RGB apps (neon, so much personality that it competes with the content).
- **Voice:** plain, friendly, maker-to-maker ("Made with ❤ for makers"). Honest about limitations; for example, the README says plainly that the builds are unsigned or not notarized.

## Evidence on Hand

- Screenshot: `docs/screenshot.png`. Video walkthroughs (YouTube) are linked from `docs/USAGE.md`.
- Public GitHub repository and releases: github.com/jalf/stlhub.
- No testimonials, user counts, download figures, press, or benchmarks exist. Do not fabricate them.

## Product Principles

1. **The model is the protagonist.** Users' thumbnails and visual browsing come first; chrome and controls step back.
2. **Structure that frees.** Categories and tags support creative exploration and should never feel like bureaucracy.
3. **Maker language.** Authentic to the maker community, never corporate.
4. **Fast clarity.** Scannable in seconds, with no friction between intent and action, measured against the 30-second find-and-print goal.
5. **Alive, not loud.** Vibrance goes in the right details. The user's own models are the most colorful thing on screen.

## Accessibility & Inclusion

- WCAG AA contrast at minimum, in every theme.
- Dark and light modes are both supported.
- Critical information is never conveyed by color alone.
