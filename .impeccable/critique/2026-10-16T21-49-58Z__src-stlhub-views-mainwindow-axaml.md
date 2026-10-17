---
target: check the UI and suggest improvements
total_score: 20
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 4
target_identity: "file:/Users/jalf/Dev/stlhub/src/STLHub/Views/MainWindow.axaml"
target_fingerprint: "sha256:ac96fbbee199431b9fe86a38f05f52f5e046f1d1cf77723d0b2639dbde2e58a2"
target_path: /Users/jalf/Dev/stlhub/src/STLHub/Views/MainWindow.axaml
timestamp: 2026-10-16T21-49-58Z
slug: src-stlhub-views-mainwindow-axaml
closed: true
---
Method: dual-agent (A: design review w/ live app screenshots · B: detector + computed contrast)

## Design Health Score
| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 2 | Zero-hit search shows "Biblioteca vazia" (VM:265); duplicate skips unreported; status bar stuck on "Pronto." |
| 2 | Match System / Real World | 3 | Folder→category and 3MF print fields fit makers; pasta/categoria/subpastas vocabulary mixed |
| 3 | User Control and Freedom | 1 | No undo; switching cards silently discards edits; Esc dead in search and dialogs |
| 4 | Consistency and Standards | 2 | Fluent blue focus/dropdown in amber system; in-category search is substring vs FTS; three sidebar row heights |
| 5 | Error Prevention | 2 | One-click global tag delete; attachment ✕ deletes the file from disk unconfirmed |
| 6 | Recognition Rather Than Recall | 2 | Open/rename/nest are double-click or drag only; truncated names have no tooltip |
| 7 | Flexibility and Efficiency | 2 | No Cmd+F, no Enter-to-open, no multi-select/batch |
| 8 | Aesthetic and Minimalist Design | 3 | Calm warm chrome; red ✕ column, permanent theme picker, empty state over watermark |
| 9 | Error Recovery | 1 | Raw ex.Message in 11px status bar; open-file failures swallowed |
| 10 | Help and Documentation | 2 | Tooltips mirrored to status bar; otherwise a GitHub link |
| Total | | 20/40 | Acceptable (low) |

## Design Specificity Verdict
LLM: Themed, not authored. Palette, corner-docked type badge, amber ring, 3MF fields and hover carousel are specific; the composition is the stock four-pane DAM shell (Bridge/Explorer). Nothing addresses the hand-off to the slicer. Emoji icons and Fluent blue pull it generic.
Detector: not meaningful for AXAML. Directory scans covered 0 files; per-file scans ran regex rules only; AXAML-as-HTML gave 7 broken-image false positives (Avalonia <Image Source>). Computed contrast: 66/180 theme pairings fail WCAG AA.
Overlays: not applicable (desktop app).

## Priority Issues
- [P1] Core job dead-ends after finding: no open-in-slicer / show-in-folder; context menu only Refazer Thumbnail/Excluir; open is hidden double-click; drag-out carries empty DataTransfer (MainWindow.axaml.cs:545). Fix: inspector amber "Abrir no fatiador" + "Mostrar na pasta", context menu top, Enter opens, file paths in drag payload, demote library Abrir. → shape, clarify
- [P1] Empty state lies: IsLibraryEmpty => Items.Count == 0 so zero-hit search/empty category/unused tag show "Biblioteca vazia". Fix: three distinct states (empty library, no results with clear-search, empty category); don't overlay watermark. → harden, onboard
- [P1] One-click deletes and silent loss: sidebar tag ✕ deletes globally with no confirmation (VM:792); attachment ✕ deletes the file; switching selection discards edits; no undo. Fix: context menu + count confirm or undo toast, hover-only ✕, auto-save on blur or prompt. → harden
- [P1] Keyboard/screen-reader gaps: Todos/Nova Categoria are Borders (Tab skips); cards announced "STLHub.Models.Object3D"; zero AutomationProperties.Name; view size by opacity only; no IsDefault/IsCancel/Esc; square focus box. → audit, harden
- [P2] Contrast + Fluent leak: TextMuted fails in all themes; empty hint 1.7:1; Confirmar on danger fails all 6; Dracula on-accent 2.26:1; Fluent blue focus/dropdown; Todos row keeps dark brush after live switch to Light (MainWindow.axaml.cs:247, ~1.4:1). → polish

## Persona Red Flags
Alex: no Cmd+F/autofocus; Enter does nothing; single-select only; Esc dead; set-once sort/theme in prime toolbar space.
Sam: Todos/Nova Categoria unreachable; cards announced by type name; 6 unnamed icon buttons; opacity-only state; no announcements; hardcoded #FFC107 ⚠.
Marina (hobbyist, ~2,000 downloads): truncated names hide "6x4" and have no tooltip; leaf-only category badge; silent duplicate skips; virtualization defeated + per-keystroke query + modal loading dialog; 3MF profile hidden in inspector.

## Minor Observations
Absolute path as library label; uncategorized card thumbnail offset; Name field overflows panel; "Detalhes" headline instead of model name; two amber fills in details panel (DESIGN.md contradicts itself on "+"); tree rows indent ~22px vs 14px; row heights 20/30/40; English strings in pt-BR UI and watermark; About omits STEP; status hint sticks; save reloads grid; fixed-size confirm dialog may clip; tag and category filters cancel each other.

## Questions to Consider
- Why is the loudest button the library switcher if the job ends in the slicer?
- What if 3MF print-profile data were chips on the card?
- Should the inspector collapse when nothing is selected?
