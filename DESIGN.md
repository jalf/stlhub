---
name: STLHub
description: Organize, catalogue and find your 3D models in seconds.
colors:
  accent: "#E8922A"
  accent-hover: "#F0A040"
  content-bg: "#141210"
  sidebar-bg: "#171410"
  toolbar-bg: "#1E1B18"
  card-bg: "#252019"
  card-thumbnail-bg: "#302820"
  hover-bg: "#2B2620"
  active-bg: "#38302A"
  toolbar-btn-bg: "#2B2620"
  toolbar-btn-hover-bg: "#38302A"
  separator: "#2B2620"
  tag-bg: "#3A2E1C"
  text-primary: "#EDE8E2"
  text-secondary: "#A09890"
  text-muted: "#605850"
  text-on-accent: "#1C1710"
  badge-bg: "#141210CC"
  badge-text: "#C8C0B8"
  danger: "#C84040"
  danger-hover: "#B53636"
  text-on-danger: "#FFF8F0"
  accent-light: "#C07320"
  accent-hover-light: "#D17D23"
  content-bg-light: "#FAF8F4"
  sidebar-bg-light: "#F5F1EC"
  toolbar-bg-light: "#EFEBE5"
  card-bg-light: "#FEFCF9"
  card-thumbnail-bg-light: "#EAE4DC"
  hover-bg-light: "#E8E2DB"
  active-bg-light: "#DDD5CC"
  toolbar-btn-bg-light: "#E0D8D0"
  toolbar-btn-hover-bg-light: "#D4CAC0"
  separator-light: "#D8D0C8"
  tag-bg-light: "#FAEEDD"
  text-primary-light: "#1C1810"
  text-secondary-light: "#5A5248"
  text-muted-light: "#A09888"
  text-on-accent-light: "#1C1810"
  badge-bg-light: "#1C1810CC"
  badge-text-light: "#F0EBE4"
  danger-light: "#C83030"
  danger-hover-light: "#B02828"
  text-on-danger-light: "#FFFFFF"
typography:
  display:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "28px"
    fontWeight: 700
  headline:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "18px"
    fontWeight: 700
  title:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "14px"
    fontWeight: 600
  body:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "13px"
    fontWeight: 400
  body-sm:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "12px"
    fontWeight: 400
  label:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "11px"
    fontWeight: 600
  section-label:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "11px"
    fontWeight: 700
    letterSpacing: "1.5px"
  caption:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "10px"
    fontWeight: 400
rounded:
  sm: "6px"
  md: "8px"
  lg: "12px"
  search: "16px"
spacing:
  xxs: "2px"
  xs: "4px"
  sm: "6px"
  md: "8px"
  lg: "12px"
  xl: "14px"
  2xl: "16px"
  3xl: "20px"
  4xl: "24px"
components:
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.text-on-accent}"
    rounded: "{rounded.sm}"
    padding: "0 12px"
    height: "32px"
  button-primary-hover:
    backgroundColor: "{colors.accent-hover}"
    textColor: "{colors.text-on-accent}"
  button-save:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.text-on-accent}"
    typography: "{typography.title}"
    rounded: "{rounded.md}"
    padding: "12px"
  button-secondary:
    backgroundColor: "{colors.toolbar-btn-bg}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.sm}"
    padding: "8px 10px"
  button-secondary-hover:
    backgroundColor: "{colors.toolbar-btn-hover-bg}"
  button-icon:
    backgroundColor: "transparent"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.sm}"
    size: "32px"
  button-icon-hover:
    backgroundColor: "{colors.toolbar-btn-bg}"
  input-search:
    rounded: "{rounded.search}"
    padding: "7px 38px 7px 14px"
    height: "38px"
    width: "380px"
  input-field:
    rounded: "{rounded.sm}"
    typography: "{typography.body}"
  model-card:
    backgroundColor: "{colors.card-bg}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.lg}"
    width: "180px"
    height: "198px"
  model-card-thumbnail:
    backgroundColor: "{colors.card-thumbnail-bg}"
    height: "132px"
  file-type-badge:
    backgroundColor: "{colors.badge-bg}"
    textColor: "{colors.badge-text}"
    typography: "{typography.caption}"
    padding: "2px 6px"
  category-badge:
    backgroundColor: "{colors.tag-bg}"
    textColor: "{colors.text-primary}"
    typography: "{typography.caption}"
    rounded: "{rounded.md}"
    padding: "2px 8px"
  tag-chip:
    backgroundColor: "{colors.tag-bg}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.lg}"
    padding: "4px 10px"
  sidebar-item:
    backgroundColor: "transparent"
    textColor: "{colors.text-primary}"
    typography: "{typography.body}"
    padding: "10px 14px"
  sidebar-item-hover:
    backgroundColor: "{colors.hover-bg}"
  sidebar-item-selected:
    backgroundColor: "{colors.active-bg}"
  sidebar-section-header:
    backgroundColor: "{colors.toolbar-bg}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.section-label}"
    padding: "8px 14px"
---

# Design System: STLHub

## Overview

**Creative North Star: "The Filament Spool"**

STLHub is styled after the printer. Its single accent is amber, like PLA glowing under an enclosure light. Its neutrals are warm browns and off-whites, like a spool core, a wooden bench and raw filament. Its structure works like a print's layers: orderly bands of toolbar, sidebar, gallery and details, stacked without fuss. It is a dense workshop catalogue for people who own hundreds or thousands of models. It is not a showroom and not a dashboard.

The chrome stays warm and quiet so the user's models are the most colorful thing on screen. Amber marks only what the user can act on next or what is currently selected: the open-library button, the save action, the selection ring and the busy indicator. Everything else is built from tonal steps of one warm neutral family, so the interface reads as a single material rather than a collection of panels. Controls are tactile and friendly: softly rounded, warmly filled, with one confident amber action per region.

Motion behaves like a print coming off the bed. Things rise into place and settle on a cubic ease-out; nothing bounces, glows or loops. Motion explains feedback and change, and it has exactly one authored moment: a freshly made thumbnail printing itself in, layer by layer.

The identity rejects three things. It is not a cold-grey IDE with a Microsoft-blue accent, not a corporate dashboard of identical metric cards and accent gradients, and not a neon gamer skin whose personality competes with the models.

**Key Characteristics:**
- One warm neutral family (hue ~30–40°) from near-black bench to off-white filament. No cool greys in the canonical themes.
- A single amber accent, reserved for action and selection.
- Tonal layering for chrome. A soft shadow appears only under the user's models.
- Fixed-size model cards in a wrapping gallery, at three user-chosen sizes.
- Inter throughout, in a compact 10–18 px working range. Larger sizes are reserved for the About dialog.
- Dark is the default theme. Light is its full counterpart.
- Feedback-sized motion (120–180ms) with one authored moment, the print-in reveal; all spatial movement yields to the OS reduce-motion setting.

## Colors

A warm, low-chroma brown-to-cream neutral family carries the whole interface. One saturated amber is the only voice. Values in the frontmatter without a suffix are the default Dark theme; `-light` keys are the Light theme counterpart of the same role. Every role exists as a `DynamicResource` key in `App.axaml` (for example `accent` → `AccentColor`, `hover-bg` → `SidebarHoverBg`).

### Primary
- **Workshop Amber** (`accent`; `accent-light` on light): the one action color. Used for the toolbar **Abrir** button, **Salvar Alterações**, the 2px selection ring around the selected model card, the rename field's border, and the indeterminate progress bar. On Light it deepens to a burnt amber so it still holds as a fill.
- **Hot Amber** (`accent-hover`, `accent-hover-light`): the hover state of amber fills. It is *lighter* than the base in both canonical themes, so the near-black `text-on-accent` gains contrast on hover instead of losing it (Light: 5.6:1).

### Neutral
- **Enclosure Dark / Natural PLA** (`content-bg`): the gallery canvas, the deepest surface on Dark and the brightest on Light. Models sit directly on it.
- **Spool Core** (`sidebar-bg`): the category sidebar, one step off the canvas.
- **Bench Top** (`toolbar-bg`): toolbar, status bar, details panel and sidebar section headers. This is the frame that surrounds the gallery.
- **Card Stock** (`card-bg`): model card body. On Dark it sits above the canvas; on Light it is near-white.
- **Print Bed** (`card-thumbnail-bg`): the well behind every thumbnail and attachment tile, so transparent or missing previews still read as an object slot.
- **Worn Wood** (`hover-bg`) and **Pressed Wood** (`active-bg`): sidebar hover and selected rows.
- **Tool Handle** (`toolbar-btn-bg`, `toolbar-btn-hover-bg`): secondary button fill and icon-button hover.
- **Seam** (`separator`): 1px dividers, the 4px sidebar splitter, toolbar-group ticks.
- **Raw Filament Tint** (`tag-bg`): an amber-tinted neutral for tag chips and the category badge on cards. It is warm enough to tie back to the accent without competing with it.
- **Ink** (`text-primary`), **Graphite** (`text-secondary`), **Dust** (`text-muted`): the three text levels. `text-on-accent` is a near-black brown used on amber fills.
- **Overlay Badge** (`badge-bg`, `badge-text`): an 80%-opaque scrim used for file-type tags and filenames laid over thumbnails.
- **Failed Print** (`danger`, `danger-hover`, `text-on-danger`): destructive actions only (the ✕ on tags, the confirm button of delete dialogs). Filled danger buttons carry `text-on-danger` (light in most themes, dark in Dracula), never `text-on-accent`.

### Theme skins
Nord, Dracula, Solarized Dark and Solarized Light are **user-selectable skins**, not the identity. They map the same 24 resource keys to their own published palettes. They are not documented here as design references and must not inspire new tokens. They do have to define every key the warm themes define and meet the same contrast floor.

### Named Rules
**The Amber Is Earned Rule.** Amber marks the next action or the current selection, nothing else. One filled amber button per region (toolbar, details panel, dialog footer). Never use amber for panel fills, decorative strokes, headings or icons at rest.

**The Models Wear the Color Rule.** The user's thumbnails are the most saturated things on screen. If a new element out-shouts a thumbnail, it is wrong.

**The Warm Neutral Rule.** Every neutral in the canonical themes carries the brown-amber undertone. A pure or cool grey (#808080, slate, zinc) in the warm themes is a bug.

**The Every-Key-Every-Theme Rule.** A new color resource is added to all six theme dictionaries in `App.axaml` at once. No hardcoded hex in views (the confirmation dialog's `#FFC107` warning glyph and the attachment remove button's `White` are existing debt).

### Contrast status (warm themes)
Measured against the surface each role actually sits on. PRODUCT.md commits to WCAG AA in every theme.
- Passes: `text-primary` (15–17:1), `text-secondary` (6:1+), amber with `text-on-accent` (Dark 7.3:1, Light 4.8:1), Dark amber as text (7.6:1), tag text on `tag-bg` (10:1+).
- **`text-muted` is not a text color for information.** At ~2.7:1 it is reserved for disabled labels (exempt from contrast), input borders at rest and decoration. Everything a user needs to read uses `text-secondary` or stronger.
- **Fails AA for small text:** `accent-light` used as text on `content-bg-light` (3.5:1). It is fine as a fill under `text-on-accent-light`.
- `danger` as small glyph text on Dark cards is 3.5:1, acceptable only for the ✕ icon (non-text UI, 3:1 minimum).

## Typography

**Display Font:** Inter (bundled via `Avalonia.Fonts.Inter`, with the platform default as fallback)
**Body Font:** Inter
**Label/Mono Font:** none distinct; labels use Inter at small sizes, weight and tracking.

**Character:** One neutral grotesk set tight and small, the way a well-labelled parts bin works: the hierarchy comes from weight and color steps, not big size jumps. Personality is left to the models.

### Hierarchy
- **Display** (700, 28px): the app name in the About dialog only.
- **Headline** (700, 18px): panel titles ("Detalhes").
- **Title** (600, 14px): model names on cards at medium size (scales 12 / 14 / 17px with the Small / Medium / Large view), the Save button, the empty-library title (16px).
- **Body** (400, 13px): category tree rows, "Todos os Objetos", search text, detail values, dialog messages.
- **Body Small** (400, 12px): tag names, sidebar tag rows, 3MF field values, toolbar checkbox label, secondary buttons.
- **Label** (600, 11px): form field labels in the details panel ("Nome", "Descrição", "Caminho"), in `text-secondary`, pulled tight (−8px) to the field below.
- **Section Label** (700, 11px, 1.5px tracking, UPPERCASE): sidebar section headers ("TAGS", "CATEGORIAS") only.
- **Caption** (400–700, 9–10px): file-type badges (bold), category badge on cards (500), 3MF field labels, attachment filenames.

### Named Rules
**The Weight Before Size Rule.** Step hierarchy with weight (400 → 600 → 700) and color (Ink → Graphite → Dust) before raising the size. Nothing in the main window exceeds 18px.

**The Uppercase Is a Shelf Label Rule.** Tracked uppercase is reserved for sidebar section headers. Don't uppercase buttons, field labels or titles.

## Layout

A fixed four-region desktop shell (design size 1100×700):

- **Toolbar** (full width, `toolbar-bg`, 8×6px padding, 1px bottom seam). Left: the amber **Abrir** button with the current library name. Center: the 380px search field. Right: control groups (sort, view size, include-subfolders, theme, about) separated by 1×18px seam ticks with 6px margins. All toolbar controls are 32px tall.
- **Sidebar** (250px default, resizable 180–500px by a 4px seam-colored splitter). From top to bottom: "Todos os Objetos", the TAGS section (max 200px, scrolls), then the CATEGORIAS section with a "Nova Categoria" row and the category tree filling the rest.
- **Gallery** (fills the remaining width, `content-bg`, 16px padding). A wrapping flow of fixed-size cards with 4px margin each (8px effective gutter). Card sizes: Small 120×138 (90 thumb), Medium 180×198 (132 thumb), Large 264×288 (204 thumb). The library watermark sits centered underneath at 8% opacity.
- **Details** (300px fixed, `toolbar-bg`, 20px padding, 1px left seam). A scrolling stack with 14px spacing, with the Save button pinned at the bottom. When nothing is selected it shows only the watermark at 8%.
- **Status bar** (26px, `toolbar-bg`, 12×4px padding): progress, status text, object count.

Spacing works in a loose 2px-based rhythm: 2 / 4 / 6 / 8 / 12 / 14 / 16 / 20 / 24px. Sidebar rows use 14px horizontal padding everywhere so labels share one left edge. Dialogs sit on the bench-top surface (`toolbar-bg`, never Fluent's default window grey), with titles in `text-primary`, supporting text in `text-secondary`, 24px margins and right-aligned footer buttons with 10px spacing.

**The One Left Edge Rule.** Everything in the sidebar starts 14px in: section headers, rows, the "Nova Categoria" affordance. New sidebar content aligns to it.

## Elevation & Depth

Hybrid: **chrome is flat and tonal, models are lifted.** Panels separate through stepped warm neutrals (canvas → sidebar → bench → card) and 1px seams, never through shadows. The only shadows belong to the user's objects: the model card and the large preview in the details panel. They read as physical prints resting on the bench. Selection is shown with the amber ring, not with extra elevation.

### Shadow Vocabulary
- **Model Rest** (`box-shadow: 0 2px 12px 0 rgba(0,0,0,0.16)`; Avalonia `0 2 12 0 #28000000`): model cards in the gallery.
- **Model Lift** (`box-shadow: 0 6px 18px 0 rgba(0,0,0,0.22)`; Avalonia `0 6 18 0 #38000000`): the card under the pointer, which also rises 2px.
- **Preview Rest** (`box-shadow: 0 2px 10px 0 rgba(0,0,0,0.16)`; Avalonia `0 2 10 0 #28000000`): the details-panel thumbnail.

### Named Rules
**The Lifted Model Rule.** Only containers that hold a user's model or its preview may cast a shadow. Toolbars, sidebars, panels, buttons and chips stay flat. Popovers and dialogs use the platform/Fluent default.

## Shapes

Soft, friendly corners on a small set of steps: 6px (`sm`) for controls (buttons, inputs, combo boxes, icon buttons), 8px (`md`) for the Save button, attachment tiles and the category badge, 12px (`lg`) for model cards, the details preview and tag chips (fully pill-shaped at their height), and 16px for the search field. Overlay badges are cut into a thumbnail's corner (`6px 0 0 0`, sitting flush bottom-right) instead of floating, like a label stuck on a print. Small circular buttons (16–20px, ✕ removes) are full circles. Thumbnails clip to their container's top radius (`12px 12px 0 0`).

**The Corner Tag Rule.** Metadata laid over an image (file type, filename) is docked to an edge with only its inner corner rounded. It never floats as a free pill over the model.

## Components

Tactile and friendly: warm fills, soft corners, one confident amber action, with everything else stepping back into the bench.

### Buttons
One system, defined once in `App.axaml` (`Application.Styles`). A button never sets its own Background/Foreground/CornerRadius; it picks a variant class. The styles target Fluent's `PART_ContentPresenter`, so hover, pressed and disabled keep the variant instead of falling back to Fluent's cool grey.
- **Shape:** gently rounded (6px), label centered, 13px. Two heights only: 32px (`MinHeight`) for every text button, 36px for "Salvar Alterações" (14px). Padding 12×4 inside a 2px border that is transparent at rest. Dialog footer buttons use `MinWidth`, never a fixed `Width`, so labels stay centered and can grow.
- **Secondary (no class):** `toolbar-btn-bg` fill, `text-primary` label; hover `toolbar-btn-hover-bg`; disabled `text-muted`. "Fechar", "Cancelar", "Anexar Arquivo...", "Limpar busca", the add-tag **+**.
- **Primary (`primary`; not `accent`, which Fluent reserves for its system-blue style):** `accent` fill, `text-on-accent` semibold label; hover `accent-hover`; disabled falls back to the secondary fill with `text-muted`. One per region: "Abrir", "Salvar Alterações", "OK" in warnings, the confirm button of non-destructive confirmations.
- **Danger (`danger`):** `danger` fill, `text-on-danger` semibold label; hover `danger-hover`. Only the confirm button of destructive dialogs, which also makes Cancel the default.
- **Ghost (`ghost`):** transparent, no padding, `toolbar-btn-bg` on hover. Icon-only buttons: ⓘ, clear search, chip and tag ✕. The About dialog's GitHub link is a ghost button too, labelled in `text-primary` and underlined, so it reads as a link in every theme (amber text fails AA on Light).
- **Overlay (`overlay`):** `badge-bg` scrim with `badge-text`, turning `danger` on hover. Buttons laid over a thumbnail (attachment remove).
- **Row (`row`, with the `SidebarRowButton` template):** full-width sidebar entries; transparent, `hover-bg` on hover, `active-bg` + semibold when `.active`.
- **Focus:** keyboard focus colors the button's own 2px border, so the ring follows the corner radius and never shifts layout (`FocusAdorner` is off; Fluent's white square is gone). `accent` on secondary and ghost buttons; `text-primary` on filled primary / danger buttons, where an amber or red ring would vanish into the fill. The fill runs under the transparent border (`BackgroundSizing=OuterBorderEdge`), so a 32px button shows 32px of fill. Sidebar rows draw the same inset 2px `accent` border.
- **Fluent's accent** is set per theme through `FluentTheme.Palettes` (one `ColorPaletteResources` per variant, `Accent` = the theme's `accent`), so checkboxes, focused fields and list selection never show the operating system's blue.
- **Icons inside buttons** (`PathIcon`) don't inherit the label color; bind `Foreground="{Binding $parent[Button].Foreground}"`.
- **Icon toggle group (view size):** radio buttons sharing the icon-button shape. Unchecked icons in `text-secondary`; the checked one gets a `toolbar-btn-hover-bg` fill and `text-primary`, so state never rests on opacity alone. 2px `accent` border on keyboard focus.
- **Row-reveal remove:** destructive ✕ buttons in lists (sidebar tags, attachment tiles) are invisible at rest and appear on row hover, selection or keyboard focus. They always open a confirmation that states the consequence.
- **Inline removes:** vector ✕ (`CloseGlyph`, stroked) in a ghost button; `danger` stroke for global deletes (sidebar tag), `text-secondary` for removing from one object.

### Chips
- **Tag chip:** `tag-bg` pill (12px radius, 4×10px padding), 12px `text-primary` name with a trailing 16px ✕ remove, wrapping with 5px gaps.
- **Filter chip (gallery top):** `tag-bg` pill (12px radius, 4×10px padding) with a 12px `text-secondary` folder or tag icon, the category path ("Oficina › Suportes") or tag name, and a trailing 20px remove button. Shown only while a category and/or tag filter is active; the two combine.
- **Category badge (on cards):** `tag-bg`, 8px radius, 2×8px padding, 10px medium text, right-aligned above the model name.
- **File-type badge:** `badge-bg` scrim with `badge-text` 10–11px bold extension (".3mf", ".stl"), docked bottom-right of the thumbnail.

### Cards / Containers
- **Model card (signature):** `card-bg`, 12px radius, Model Rest shadow (Model Lift and a 2px rise on hover), fixed size per view mode. The top is a `card-thumbnail-bg` well with the thumbnail at uniform-to-fill and the file-type badge. The bottom has 10px side padding: the category badge, then the model name (600, single line, ellipsis). Selection draws a 2px `accent` ring inset 4px with 12px radius. Right-click opens Refazer Thumbnail / Excluir Objeto.
- **Attachment tile:** 90×100, 8px radius, `card-thumbnail-bg` with the file-type glyph and extension (or the image itself), a filename strip on `badge-bg` at the bottom, and a 20px `danger` circle remove at top-right.

### Inputs / Fields
- **Search:** 380×38, 16px radius, 13px text, right inset for a 28px clear (✕) button. Fluent's default text box styling otherwise.
- **Detail fields:** Fluent text boxes at 6px radius. The description is multi-line at 80px.
- **Inline rename (category tree):** `card-thumbnail-bg`/`card-bg` fill with a 1px `accent` border, 13px text, 4×2px padding.
- **Focus / Error / Disabled:** Fluent defaults; no custom treatment yet.

### Navigation (sidebar)
- **Rows:** transparent at rest, `hover-bg` on hover, `active-bg` when selected. 13px `text-primary` for categories and "Todos os Objetos", 12px for tags. "Todos os Objetos" and "Nova Categoria" are real focusable buttons (`SidebarRowButton` theme) with a 2px `accent` inset border on keyboard focus.
- **Category tree:** Fluent chevron, tree-indent per level, folder glyph + name with ellipsis. Double-click to rename; drag a category onto another to nest it, or onto "Todos os Objetos" to move it to the root.
- **Section headers:** `toolbar-bg` band with the Section Label style in `text-secondary`.

### Empty and idle states
The gallery has three distinct placeholders; never reuse one for another's situation.
- **Empty library** (the library has no objects): centered folder icon (40px, `text-secondary`), "Biblioteca vazia" (16px semibold, `text-primary`), a one-line hint in `text-secondary` pointing at drag-and-drop and **Abrir**.
- **No results** (objects exist, search/filters match none): 32px search icon, a title naming the query or the filter, one line of guidance, and secondary buttons "Limpar busca" / "Mostrar todos os objetos" as applicable.
- **Slow load:** appears only after 400ms: "Carregando objetos..." over a 3px amber indeterminate bar. Quick queries show nothing.
- **Watermark:** the STLHub mark at 8% opacity behind the gallery (220px) and in the empty details panel (140px). Hidden whenever a placeholder occupies the center.

### Iconography
Vector `PathIcon`s are the system: solid 16×16 shapes with even-odd cut-outs for detail, rendered at 12–14px (24px in attachment tiles), colored by the surrounding text brush (`text-secondary` beside labels). They live as `StreamGeometry` resources in `App.axaml` (`IconModel`, `IconTag`, `IconFolder`, `IconFolderOpen`, `IconFolderPlus`, `IconPencil`, `IconTrash`, `IconRefresh`, `IconWarning`, and the file types `IconFile`, `IconDocument`, `IconImage`, `IconArchive`, `IconVideo`, `IconAudio`, `IconCode`, `IconPrinter`). `FileTypeIconConverter` maps an attachment extension to one of them. Context menus carry their icon in `MenuItem.Icon`, never as an emoji prefix in the label. The only emoji left in the UI is the ❤ in the About footer, which is copy, not an icon.

**The One Icon Set Rule.** New UI uses the vector geometry in `App.axaml`, never emoji. A new icon joins the same family: solid fill, 16×16 bounds, similar ink weight.

### About dialog
The one place the Display size appears. The bench-top (`toolbar-bg`) window holds, centered: the emblem only (`Assets/logo-mark.png`, cropped from the full logo, whose navy "STL" wordmark disappears on dark themes and repeats the name below), the name in Display with the version as a small `tag-bg` tag on the same line, the tagline in `text-secondary`, one line of purpose in `text-primary`, the supported formats as file-type tags (`card-thumbnail-bg`, 11px bold `text-secondary`, 6px radius) and the repository link. A footer band behind a 1px seam carries the license line on the left and **Fechar** on the right, following the dialog-footer convention. The window sizes to its content.

### Motion and feedback
Quick and physical: state changes acknowledge themselves in 120–180ms on a cubic ease-out, and one moment, the print-in reveal, is allowed to take its time.

- **Print-in reveal (the authored moment):** a thumbnail that was just made (import, or Refazer miniatura) is uncovered bottom-up over 650ms (80ms delay) by `PrintReveal` (`src/STLHub/Controls/`), with a 2px amber nozzle line riding the edge and fading out over the last stretch. It plays once per object (`Object3D.IsFreshPrint`), never on scroll, search or re-sort. The About dialog's emblem prints in the same way each time the dialog opens; the cover is removed after 1.5s even if the animation clock stalls.
- **Feedback (120–180ms):** the card lifts 2px with Model Lift on hover; the selection ring fades in from 97% scale; filter chips and a just-added tag chip pop from 92%; the details panel fades up from 35% opacity when the selection changes; "Salvar Alterações" turns into "✓ Salvo" for 1.4s after a save.
- **Drag feedback:** files over the window turn the gallery into an opaque drop zone (2px dashed amber outline, title naming the action, one line naming where the files land). An internal drag lights the target category row (or "Todos os Objetos") with `hover-bg` and the 2px amber inset border.
- **Exit:** a deleted card fades and shrinks to 95% in 150ms (ease-in) before the list reloads. Exits are faster than entrances.
- **Reduced motion:** `MotionPreference` reads the OS setting (Windows animation effects, macOS Reduce motion, GNOME enable-animations) and puts `reduce-motion` on the window. Every spatial style is gated on `:is(Window):not(.reduce-motion)`; with it, the reveal is skipped and lifts, scales and rises are dropped, while fades and state colors stay.

**The One Moment Rule.** The print-in reveal is the only animation longer than 200ms. New motion is feedback-sized, or it replaces the reveal as the authored moment rather than joining it.

**The Gate Rule.** Any new transform, translate or scale animation goes under `:is(Window):not(.reduce-motion)`. Opacity and color may stay ungated.

## Do's and Don'ts

### Do:
- **Do** reference colors only through `{DynamicResource ...}` keys, and add any new key to all six theme dictionaries in the same change.
- **Do** keep amber to one filled action per region, plus the selection ring and progress indicator (The Amber Is Earned Rule).
- **Do** separate chrome with tonal steps and 1px `separator` seams. Reserve shadows for model cards and previews (`0 2 12 0 #28000000`).
- **Do** use 32px-tall toolbar controls with 6px radius, and 14px horizontal padding for every sidebar row.
- **Do** step text hierarchy with Inter weight and the Ink / Graphite / Dust colors before raising size. Stay at or under 18px in the main window.
- **Do** dock overlay metadata to a thumbnail edge with a single rounded inner corner.
- **Do** use vector `StreamGeometry` icons for anything new.
- **Do** check new text against WCAG AA (4.5:1 body, 3:1 for ≥18px or icons) on the surface it actually sits on, in Dark and Light.

### Don't:
- **Don't** introduce cool or neutral greys, a blue accent, or gradients in the warm themes. That is the IDE look PRODUCT.md rejects.
- **Don't** build dashboard furniture: no big-number metric tiles, no identical icon-heading-text card grids.
- **Don't** add neon, glow shadows or saturated chrome that competes with the models.
- **Don't** put shadows on toolbars, sidebars, panels, chips or buttons.
- **Don't** use `text-muted` for information the user needs to read; it is for disabled labels and resting borders only.
- **Don't** hardcode hex values or `White` in views.
- **Don't** add emoji as icons.
- **Don't** take design cues from the Nord / Dracula / Solarized skins; they follow the warm system's roles, not the other way round.
