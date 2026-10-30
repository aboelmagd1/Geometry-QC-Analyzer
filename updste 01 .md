You are modifying an existing ArcGIS Pro 3.x Add-in (C# / .NET 8-windows / WPF)
called files 

═══ STEP 0 — STUDY FIRST (no edits yet) ═══
Read the whole project before changing anything: Config/GeometryQCSettings.cs,
Core/GeometryValidator.cs, Core/GeometryDataProvider.cs, DisplayCacheProvider.cs,
LiveQueryProvider.cs, Core/Checks/*.cs (especially RedundantVertexCheck.cs),
Models/*, Graphics/*, Services/SelectionService.cs, UI/ResultsDockPane*.cs/xaml,
UI/SettingsDockPane*.cs/xaml, Config.daml, GeometryQCModule.cs.
Then give me a short summary: how settings are stored/loaded, how the run flow works
(button -> ViewModel -> Validator -> Graphics), and which files you will touch.
Do not wait for confirmation; continue after the summary.

═══ GLOBAL RULES ═══
1. DO NOT change the logic of any existing check or the verification/dedup pipeline.
   Only add settings/options around them. The behavior of the 10 checks must remain
   identical except for the new default values requested in Change 5.
2. The add-in stays 100% read-only (no edit operations, no writes to source data).
3. Keep the existing architecture, naming style, Light/Dark theme support and the
   packaging rules (Install/ folder, autoLoad="false", Button Enabled = true,
   appearsOnAddInTab="false").
4. Every new setting needs: a property in GeometryQCSettings with a default, a control
   in SettingsDockPane (bound to the ViewModel), inclusion in "Restore Defaults",
   and persistence.
5. Make minimal, surgical edits. Do not refactor unrelated code.
6. If anything in the existing code conflicts with these instructions, stop and ask me
   that one question instead of guessing.

═══ CHANGE 1 — Remove "Live" completely ═══
- Remove the Live Query data source and any live/auto-run-on-selection behavior:
  LiveQueryProvider usage, the Data Source selector (Display Cache vs Live Query),
  the Execution Mode (Manual vs Auto), the debounced auto-run in SelectionService,
  and all related ribbon/settings UI and text.
- Display Cache becomes the only data source. Running is manual only (Run QC button).
- Delete code/files that become unused (e.g., LiveQueryProvider) only if nothing else
  references them; the project must still build.
- If SelectionService existed only for auto-run, remove that path but keep anything
  else that depends on it.

═══ CHANGE 2 — Max features cap ═══
- New setting MaxFeaturesToProcess (int, default 500, editable in the Settings pane,
  validation: minimum 1, sensible maximum).
- Applies to the number of SELECTED features processed per run.
- If the selection exceeds the cap: do NOT silently truncate. Show a clear message in
  the Results pane (e.g., "Selected 1,240 features; limit is 500. Reduce the selection
  or raise the limit in Settings.") and do not run. Tell me if you chose differently.

═══ CHANGE 3 — Persist target layer per project ═══
- The selected target layer (including "All Polygon Layers") must be remembered inside
  the current ArcGIS Pro PROJECT (not globally) and restored when the project reopens
  or the Results pane reloads, until the user changes it.
- Use the Module's OnReadStateAsync / OnWriteStateAsync (Module project-settings
  pattern with a Dictionary<string,string>) and store a stable layer identifier
  (layer URI / CIM URI) plus the layer name as fallback.
- RefreshLayersAsync must not reset the selection; if the saved layer no longer
  exists in the map, fall back to "All Polygon Layers" without any alarming message.
- Other settings (tolerances, toggles, max features) stay as they are today unless the
  current code already stores them per project.

═══ CHANGE 4 — "No errors found" message ═══
- When a run completes successfully with zero verified issues, show a clear non-error
  message in the Results pane (e.g., "No errors found ✓"), keep the analyzed
  count/time visible, and clear any old map graphics.
- It disappears as soon as a new run starts or results are cleared.
- Distinguish it from "run failed", "cancelled", "nothing selected" and
  "cap exceeded" states (each with its own message).

═══ CHANGE 5 — Update default values ═══
Update the DEFAULT values in GeometryQCSettings (and Restore Defaults, the Settings
pane initial values, README, USER_GUIDE and GeometryQC_Rebuild_Prompt) to exactly:

| Setting                                        | Old default | NEW default        |
|------------------------------------------------|-------------|--------------------|
| Overlap min area                               | 0.0001 m²   | 0.00001 m²         |
| Gap min area (GapMinAreaSqMeters)              | 0.001 m²    | 0.00001 m²         |
| Short Segment (ShortSegmentThresholdCm)        | 10.0 cm     | 15.0 cm            |
| Angle Issue (AngleThresholdDegrees)            | 5.0°        | 30°                |
| Snap (SnapToleranceCm)                         | 1.0 cm      | 15 cm              |
| Redundant Vertex (RedundantVertexAngleDegrees) | 179.9°      | 175°               |
| Missing Junction (MissingJunctionToleranceCm)  | 1.0 cm      | 10 cm              |
| EnableForServiceLayers                         | false       | false (unchanged)  |
| MaxFeaturesToProcess (new)                     | —           | 500                |
| All 10 checks enabled                          | —           | true               |

Rules for this change:
- Change ONLY these default values; do not touch check logic.
- Print a before/after table of every changed default with its file/line.
- Search for hard-coded copies of the old values elsewhere (checks, validators, UI
  text, tooltips, unit helpers, docs) and update or report each one.
- Verify the new values are consistent with internal guards (overlap sliver guard,
  XY tolerance/resolution guards). If a new default is smaller than a guard threshold
  and would make the setting ineffective, REPORT it to me instead of silently
  changing the guard.
- Do NOT overwrite users' already-saved settings; new defaults apply to new installs
  and to "Restore Defaults" only.

═══ CHANGE 6 — Redundant Vertex: arc/line "maybe error" option ═══
Add a NEW OPTIONAL sub-feature under the Redundant Vertex check, OFF by default, that
does not touch the existing redundant-vertex logic:
- New settings in the Settings pane under Redundant Vertex:
  a) FlagArcLineJunctions (bool, default false): when enabled, additionally inspect
     vertices where a true curve segment (EllipticArc / Bezier, detected via segment
     type) meets a straight line segment where the join is (near-)tangent, so the
     vertex looks collinear/180°. Report them as a separate result type:
     "Possible redundant vertex (arc–line junction)".
  b) ArcLineAngleToleranceDegrees (double, default = the redundant-vertex angle
     threshold, so behavior is consistent).
  c) ShowWarningsOnMap (bool, default true): controls whether WARNING-level issues
     are drawn as map graphics. Warnings still appear in the Results tree either way
     (optionally add ShowWarningsInResults, default true).
- Reuse the existing Models/IssueSeverity.cs and use a Warning level. These items are
  Warnings, not Errors:
  * own category in the Results tree,
  * own distinct color/symbol in QCGraphicSymbolProvider (different from the
    Redundant Vertex error color),
  * counted separately in the header badge (e.g., "3 errors · 2 warnings"),
  * not drawn on the map when ShowWarningsOnMap = false.
- The existing real-vertex logic and the rule "if two coincident vertices both approach
  180°, both are flagged" stay unchanged. Make sure arc–line items already flagged by
  the existing check are not double-reported (dedup).
- Add a short tooltip/help text next to the option.

═══ DOCUMENTATION ═══
Update README.md, USER_GUIDE.md and GeometryQC_Rebuild_Prompt.md (English + Arabic
sections) to reflect: live mode removed, max features cap, per-project layer
persistence, "no errors" message, new defaults, and the arc–line warning option.

═══ DELIVERABLES ═══
1. The Step-0 summary.
2. All code changes, file by file, with a brief reason for each.
3. The before/after defaults table (Change 5).
4. Updated documentation sections.
5. Build instructions to produce the .esriAddinX, and a manual test checklist covering:
   live removal, cap exceeded, layer persistence after closing/reopening the project,
   zero-error message, new defaults loading, Restore Defaults, arc–line option on/off,
   warnings drawn/hidden on the map.