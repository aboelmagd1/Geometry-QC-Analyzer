# Geometry QC Analyzer — Update 01 Requirements & Implementation Specification (English)

> **Document Type:** Update 01 Specification & Implementation Guide  
> **Status:** Fully Implemented and Verified  
> **Target Framework:** `.NET 8.0-windows`  
> **Host Application:** ArcGIS Pro 3.0 through 3.4+  
> **Date:** October 30, 2026  

---

## 📌 Executive Summary of Latest Modifications

Update 01 transformed the Geometry QC Analyzer from a mixed live/entire-layer background add-in into a streamlined, ultra-fast, on-demand polygon quality control tool operating exclusively through the ArcGIS Pro viewport display cache with user-configurable safety caps, project-level state persistence, cadastral-grade default tolerances, and tangent arc–line junction diagnostics.

---

## 1. Global Mandates & Architectural Guardrails

1. **Check Mathematical Invariance:** The core geometric and cadastral verification logic across the 10 checks remained unchanged. Only configuration parameters and default values were updated.
2. **Strict Read-Only Guarantee:** The Add-In executes zero edit operations, creates no schema locks, and performs in-memory inspection only.
3. **Packaging Standard:** `<insertModule autoLoad="false">`, parameterless button constructors explicitly call `Enabled = true;`, and ribbons are configured with `appearsOnAddInTab="false"`.
4. **Configuration Lifecycle:** Every setting adheres to the MVVM pipeline:
   - Backed in `GeometryQCSettings.cs`.
   - Bound two-way in `SettingsDockPane.xaml` / `SettingsDockPaneViewModel.cs`.
   - Reset via `ResetDefaults()` ("Restore Defaults").
   - Persisted to `%APPDATA%\ArcGISPro_GeometryQC\settings.json`.

---

## 2. Detailed Changes Specification

### Change 1 — Complete Elimination of "Live" Query & Auto-Run
* **Removed Components:**
  - `Core/LiveQueryProvider.cs`, `Core/EntireLayerProvider.cs`, `Core/RealGeometryProvider.cs`.
  - `Services/SelectionService.cs` (debounced selection change listeners).
  - Data Source selector (Display Cache vs Live Query) and Execution Mode (Manual vs Auto).
* **New Flow:** `DisplayCacheProvider` is the sole data source provider. Validation executes purely on user demand via the **Run QC** ribbon button.

### Change 2 — Selected Features Cap Protection
* **Setting:** `MaxFeaturesToProcess` (`int`, default: `500`, range: `1` to `100,000`).
* **Enforcement:** `DisplayCacheProvider.AcquireAsync` pre-counts selected features. If `count > MaxFeaturesToProcess`, it triggers `CapExceeded = true`.
* **User Feedback:** DockPane alerts the user with:
  `"Selected {N:N0} features; limit is {Cap:N0}. Reduce the selection or raise the limit in Settings."`

### Change 3 — Per-Project Target Layer Persistence
* **Mechanism:** Overrode ArcGIS Pro SDK Module hooks in `GeometryQCModule.cs`:
  - `protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)`
  - `protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)`
* **Stored Keys:** `TargetLayerUri` (CIM URI) and `TargetLayerName` (human-readable fallback).
* **Behavior:** When reopening an `.aprx` project or reloading the dockpane, `RefreshLayersAsync` restores the previously active layer without alarming messages if the layer was deleted or renamed.

### Change 4 — Clear "No errors found ✓" Verification State
* **Trigger:** When validation completes with `Issues.Count == 0`.
* **Feedback:** Header status displays `"No errors found ✓"`, execution time and feature counters remain visible, and any previous map graphics are cleared.

### Change 5 — Cadastral Standard Defaults Overhaul
* **Overlap (`CHK_OVERLAP`):** `0.00001 m²` ($100\text{ cm}^2$).
* **Gap (`CHK_GAP`):** `0.00001 m²` ($100\text{ cm}^2$).
* **Short Segment (`CHK_SHORT_SEG`):** `15.0 cm` ($0.15\text{ m}$).
* **Angle Issue (`CHK_ANGLE`):** `30.0°`.
* **Snap Issue (`CHK_SNAP`):** `15.0 cm` ($0.15\text{ m}$).
* **Redundant Vertex (`CHK_REDUNDANT`):** `175.0°`.
* **Missing Junction (`CHK_JUNCTION`):** `10.0 cm` ($0.10\text{ m}$).
* **MaxFeaturesToProcess:** `500`.
* **Non-destructive:** User settings in `%APPDATA%` are preserved; new defaults apply to clean installs and the "Restore Defaults" button.

### Change 6 — Redundant Arc–Line Junction Warning Check
* **Optional Diagnostics:** `FlagArcLineJunctions` (default: `false`), `ArcLineAngleToleranceDegrees` (default: `175.0°`).
* **Target:** Detects redundant vertices where a straight line meets a circular arc or Bézier curve tangentially ($seg_1.IsCurve \neq seg_2.IsCurve$).
* **Classification:** Classified as `IssueSeverity.Warning` with code `CHK_REDUNDANT_ARCLINE`.
* **Graphics Symbol:** Rendered with Coral equilateral triangle CIM point marker (`RGB(255, 127, 80)`).
* **Overlay Suppression:** Controlled by `ShowWarningsOnMap` (default: `true`), allowing users to declutter map overlays while retaining warnings in the Results DockPane tree.

