# Feature Specification: 3MF Project Details Display

**Feature Branch**: `001-3mf-project-details`

**Created**: 2026-08-11

**Status**: Draft

**Input**: User description: "Vamos adicionar o suporte para exibir os detalhes do projeto que existe dentro dos arquivos 3MF. Eles devem ser exibidor dentro do painel de Detalhes que aparece quando se clica em um objeto."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View 3MF Project Metadata in Details Panel (Priority: P1)

A user selects a `.3mf` object from the library and opens the Details panel. The panel shows metadata embedded in the 3MF file — such as the model name, author/creator, and model description — so the user can quickly identify the original project details without opening an external slicer application.

**Why this priority**: This is the core deliverable of the feature. Without it, users must open the 3MF file in a slicer to access project-level information that is already present in the file.

**Independent Test**: Select any `.3mf` object that contains project metadata. The Details panel must display the extracted metadata fields alongside the existing object information.

**Acceptance Scenarios**:

1. **Given** a `.3mf` object with embedded project metadata is selected, **When** the Details panel is displayed, **Then** the panel shows the model name, author, and model description read from the 3MF file.
2. **Given** a `.3mf` object with no embedded metadata is selected, **When** the Details panel is displayed, **Then** the 3MF metadata section is fully hidden — no section header or placeholder is visible.
3. **Given** a non-3MF object (e.g., `.stl`, `.obj`) is selected, **When** the Details panel is displayed, **Then** no 3MF metadata section is shown.

---

### User Story 2 - View Print Profile Details from 3MF (Priority: P2)

A user selects a `.3mf` project file that contains slicer profile information (layer height, wall count, infill percentage, profile author). The Details panel shows this print profile data so the user can see the print settings used when the file was saved, without opening the slicer.

**Why this priority**: Print profile data is highly valuable for users managing a library of 3MF project files from slicers like Bambu Studio or PrusaSlicer, but it is secondary to basic project identity metadata.

**Independent Test**: Select a `.3mf` file exported from Bambu Studio or PrusaSlicer that includes profile settings. The Details panel must display the profile name/settings.

**Acceptance Scenarios**:

1. **Given** a `.3mf` file with embedded slicer profile data is selected, **When** the Details panel is displayed, **Then** the panel shows the profile name and key print settings (e.g., layer height, infill).
2. **Given** a `.3mf` file with no profile data is selected, **When** the Details panel is displayed, **Then** the print profile sub-section is fully hidden — no header or placeholder is shown.

---

### Edge Cases

- What happens when the 3MF file is corrupt or the internal XML is malformed? The Details panel must still load; the 3MF metadata section is fully hidden — the same as the no-metadata case. No error indicator is shown; no crash occurs.
- What happens when the 3MF file is very large and metadata extraction takes time? The panel must not block the UI; metadata loads asynchronously.
- What happens when metadata fields contain very long strings? The UI must truncate or scroll gracefully.
- In the current design, identity fields (Title, Designer, Description) are sourced exclusively from the standard `3D/3dmodel.model` metadata block, and profile fields (ProfileName, LayerHeight, etc.) are sourced exclusively from slicer-specific config files. There is no field overlap between the two sources; conflict resolution is not required for v1.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST extract project metadata from `.3mf` files when displaying the Details panel for a selected object.
- **FR-002**: Extracted metadata MUST include, at minimum: model name, author/creator, and model description (read from the standard 3MF metadata block or slicer-specific equivalents).
- **FR-003**: The system MUST display a dedicated "3MF Project Info" section within the existing Details panel, visible only when a `.3mf` file is selected and metadata is available. This section MUST be positioned at the bottom of the Details panel — after the Attachments area and before the Save button — so all existing editable fields are undisturbed.
- **FR-004**: The system MUST support reading metadata from at least two slicer formats: Bambu Studio/Orca Slicer and PrusaSlicer, in addition to the base 3MF specification.
- **FR-005**: The system MUST extract print profile information (profile name, layer height, wall count, infill percentage, profile author) when present in the 3MF file. Each field MUST be rendered only if a value was successfully extracted; fields with no available value MUST be omitted entirely — no empty rows or dash placeholders.
- **FR-006**: Metadata extraction MUST be performed asynchronously so the Details panel remains responsive during loading. While metadata is being fetched, the "3MF Project Info" section MUST display a loading indicator (spinner or shimmer); it MUST be replaced by the extracted data (or hidden) once the operation completes.
- **FR-007**: The system MUST handle malformed or missing metadata gracefully — the Details panel must still render without errors.
- **FR-008**: Extracted metadata fields MUST be read-only; the user cannot edit 3MF project info from within the Details panel. The 3MF Project Info section MUST NOT influence or auto-populate the editable Name or Description fields at any time, including on import.
- **FR-009**: The "3MF Project Info" section MUST be fully hidden (collapsed) when: (a) the selected object is not a `.3mf` file, or (b) metadata extraction completes and yields zero fields. No section header or placeholder message is shown in these cases.
- **FR-010**: Metadata values MUST be HTML-entity-decoded before display. Producers escape metadata to varying depths (Bambu Studio double-escapes), so raw values otherwise reach the user as literal entity text such as `Mother&apos;s Day Statue`.
- **FR-011**: The project description MUST be presented according to its content. A description containing no HTML markup MUST be shown inline in the Details panel. A description containing HTML markup MUST NOT be rendered inline — the panel MUST instead show a button opening a modal that renders it with its formatting (paragraphs, headings, lists, bold/italic), clickable links, and embedded images. Markup detection MUST require a recognized HTML tag name, so that bracketed plain text (such as an e-mail address in angle brackets) is treated as plain and shown verbatim. Unsupported or malformed markup MUST degrade to readable text rather than failing, and an image that cannot be fetched MUST NOT prevent the rest of the description from rendering.

### Key Entities *(include if feature involves data)*

- **3MF Project Metadata**: Represents project-level information embedded in a `.3mf` file. Key attributes: model name, author, description, creation date (if present), thumbnail (already handled), slicer software name/version.
- **Print Profile**: Represents slicer print settings stored in a `.3mf` project. Key attributes: profile name, layer height, wall count, infill percentage, profile author. Optional — not all 3MF files include this.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When a `.3mf` file with embedded metadata is selected, the "3MF Project Info" section appears in the Details panel within 1 second on typical hardware.
- **SC-002**: Metadata is correctly read from 3MF files produced by Bambu Studio, Orca Slicer, and PrusaSlicer (the three most common sources in the target user base).
- **SC-003**: Selecting a `.3mf` file with no metadata or selecting a non-3MF file results in no visible 3MF metadata section — 0 spurious data shown.
- **SC-004**: The Details panel loads without errors for any valid or malformed `.3mf` file — 0 unhandled exceptions from metadata extraction.

---

## Clarifications

### Session 2026-08-11

- Q: Should the 3MF metadata (model name, author) be used to auto-populate the editable Name/Description fields when a file is first imported? → A: No — strictly separate. The 3MF Project Info section is always read-only and never touches the editable Name/Description fields, at import or any other time.
- Q: What should the user see while 3MF metadata is loading asynchronously? → A: Show a loading indicator (spinner or shimmer) inside the "3MF Project Info" section while metadata is being fetched.
- Q: Where in the Details panel should the "3MF Project Info" section appear? → A: At the bottom — after Attachments and before the Save button, so all existing editable fields remain undisturbed.
- Q: When metadata extraction completes but yields zero fields, should the "3MF Project Info" section header remain visible? → A: Fully hidden — if extraction yields no metadata the entire section collapses and is invisible.
- Q: When only some print profile fields are available (e.g., layer height present but wall count absent), how should the layout behave? → A: Show only present fields — each profile field is rendered individually only if a value was extracted; absent fields are omitted entirely with no placeholder row.

## Assumptions

- The 3MF files in the user's library are produced by popular slicers (Bambu Studio, Orca Slicer, PrusaSlicer) or comply with the base 3MF specification; exotic or proprietary 3MF variants are out of scope for v1.
- Metadata is read on-demand (when the user selects the object) and is NOT persisted to the SQLite database; it is fetched fresh from the file each time.
- The "Details panel" refers to the existing side panel in `MainWindow.axaml` (Grid.Column="3") that is displayed when a library object is selected.
- Mobile and web versions are out of scope — this is a desktop-only feature.
- The 3MF metadata section is informational only (read-only); editing project metadata inside the 3MF file is out of scope for this feature.
- Print profile extraction covers only the most common slicer-specific fields; deep slicer configuration (hundreds of individual settings) is out of scope.
- UI labels are in Portuguese, consistent with all existing application strings (e.g., "Detalhes", "Arquivos Associados").
