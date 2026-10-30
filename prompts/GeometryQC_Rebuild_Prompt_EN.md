# Geometry QC Analyzer — Complete System Architecture & AI Rebuild Prompt (English)

> **Document Type:** Production Architecture Specification & Complete Rebuild Prompt  
> **Target Framework:** `.NET 8.0-windows`  
> **Host Application:** ArcGIS Pro 3.0 through 3.4+ (ArcGIS Pro .NET 8 SDK)  
> **Last Modified:** October 30, 2026 (Update 01 Implementation)  
> **Author:** Mahmoud Aboelmagd with Advanced AI  

---

## 📌 Latest Modifications & Changelog (Update 01)

The following changes were implemented in the latest architecture update:

1. **Pure Viewport DisplayCache Architecture (Removed Live/Entire Layer Modes):**
   - Removed `LiveQueryProvider.cs`, `EntireLayerProvider.cs`, `RealGeometryProvider.cs`, and `SelectionService.cs`.
   - Execution is strictly manual: runs when the user clicks **Run QC**.
   - Features are acquired directly from ArcGIS Pro's active MapView display cache via `DisplayCacheProvider`.
2. **Feature Cap Guard (`MaxFeaturesToProcess` = 500):**
   - Added pre-acquisition count verification. If selected features exceed `MaxFeaturesToProcess` (default: 500), execution halts immediately with a clear prompt:
     `"Selected {N} features; limit is {Cap}. Reduce the selection or raise the limit in Settings."`
3. **Per-Project Target Layer Persistence:**
   - Overrode `OnWriteSettingsAsync(ModuleSettingsWriter)` and `OnReadSettingsAsync(ModuleSettingsReader)` in `GeometryQCModule.cs`.
   - Saves `TargetLayerUri` (primary) and `TargetLayerName` (fallback) into the ArcGIS Pro `.aprx` project file. Layer choice restores automatically when reopening the project.
4. **"No errors found ✓" Verification State:**
   - When zero issues are identified, the Results DockPane explicitly shows `"No errors found ✓"` and clears temporary map graphics.
5. **Cadastral Default Tolerances Overhaul:**
   - **Overlap (`CHK_OVERLAP`):** `0.00001 m²` ($100\text{ cm}^2$).
   - **Enclosed Gap (`CHK_GAP`):** `0.00001 m²` ($100\text{ cm}^2$).
   - **Short Segment (`CHK_SHORT_SEG`):** `15.0 cm` ($0.15\text{ m}$).
   - **Angle Issue (`CHK_ANGLE`):** `30.0°`.
   - **Snap Issue (`CHK_SNAP`):** `15.0 cm` ($0.15\text{ m}$).
   - **Redundant Vertex (`CHK_REDUNDANT`):** `175.0°`.
   - **Missing Junction (`CHK_JUNCTION`):** `10.0 cm` ($0.10\text{ m}$).
   - **Restore Defaults:** Automatically restores these new standard defaults.
6. **Redundant Arc–Line Junction Check (`CHK_REDUNDANT_ARCLINE`):**
   - Optional setting: `FlagArcLineJunctions` (default: `false`, tolerance `175.0°`, severity: `Warning`).
   - Flags redundant vertices at tangential transitions between straight segments and circular/Bézier arcs while respecting `JunctionGuard`.
   - Rendered on the map using a distinct Coral equilateral triangle CIM symbol (`RGB(255, 127, 80)`).
7. **Warning Map Overlay Suppression (`ShowWarningsOnMap`):**
   - New toggle in Settings (default: `true`) allowing the user to hide warning graphics from the map while keeping errors visible.
8. **UI & Packaging Enhancements:**
   - Header badge in Results DockPane dynamically updates (e.g. `"3 errors · 2 warnings"`).
   - In `Config.daml`, `insertModule` uses `autoLoad="false"` and buttons explicitly enforce `Enabled = true;`.

---

## 1. Project Profile & Target Environment

* **Product Name:** Geometry QC Analyzer for ArcGIS Pro
* **Add-In ID:** `{8a7f921d-44a3-4b92-95f2-953e5e6080dc}`
* **Target Framework:** `.NET 8.0-windows`
* **Language:** C# 12.0
* **Host Application:** ArcGIS Pro 3.0 through 3.4+ (ArcGIS Pro .NET 8 SDK)
* **Architecture Pattern:** MVVM (Model-View-ViewModel) with ArcGIS Pro Framework Contracts
* **Thread Safety Model:** Esri `QueuedTask.Run(...)` for all ArcGIS Pro SDK geometry and geodatabase interactions; WPF Dispatcher for UI bindings.
* **Safety Mandate:** 100% In-Memory, Read-Only inspection. Zero modifications or locks on source geodatabases or feature services during validation.

---

## 2. Directory Hierarchy & Code Structure

```text
QC Preview V01/
├── Config.daml                                # ArcGIS Pro Declarative Add-in Markup (autoLoad="false")
├── GeometryQCAddIn.csproj                     # MSBuild SDK Project with Auto-Deploy & Cache Purge
├── GeometryQCAddIn.sln                        # Visual Studio Solution
├── GeometryQCModule.cs                        # Add-In Module Singleton & Project Settings Persistence
├── package.ps1                                # PowerShell Build & Cache-Purge Script
├── README.md                                  # Project Overview (Bilingual)
├── USER_GUIDE.md                              # User Guide (Bilingual)
├── QC_CHECKS_LOGIC.md                         # Mathematical & Cadastral Logic Guide (Bilingual)
├── prompts/                                   # AI Rebuild Prompts & Architecture Specifications
│   ├── README.md                              # Prompts Index & Changelog Summary
│   ├── GeometryQC_Rebuild_Prompt_EN.md        # Complete Rebuild Prompt (English)
│   ├── GeometryQC_Rebuild_Prompt_AR.md        # Complete Rebuild Prompt (Arabic)
│   ├── Update_01_Specification_EN.md          # Update 01 Requirements & Implementation Notes (English)
│   └── Update_01_Specification_AR.md          # Update 01 Requirements & Implementation Notes (Arabic)
├── Config/
│   └── GeometryQCSettings.cs                  # User Settings Model & Persistence (JSON in %APPDATA%)
├── Core/
│   ├── Checks/
│   │   ├── IGeometryCheck.cs                  # Common Interface for all Checks
│   │   ├── InvalidGeometryCheck.cs            # CHK_INVALID_GEOM
│   │   ├── OverlapCheck.cs                    # CHK_OVERLAP
│   │   ├── DuplicateCheck.cs                  # CHK_DUPLICATE
│   │   ├── GapCheck.cs                        # CHK_GAP
│   │   ├── MultiPartCheck.cs                  # CHK_MULTIPART
│   │   ├── ShortSegmentCheck.cs               # CHK_SHORT_SEG
│   │   ├── AngleIssueCheck.cs                 # CHK_ANGLE
│   │   ├── SnapIssueCheck.cs                  # CHK_SNAP
│   │   ├── RedundantVertexCheck.cs            # CHK_REDUNDANT (Curves, Arc–Line, & Junction Guard)
│   │   └── JunctionVertexCheck.cs             # CHK_JUNCTION (T-Junctions)
│   ├── DisplayCacheProvider.cs                # Viewport display cache reader with feature cap protection
│   ├── GeometryDataProvider.cs                # Base provider abstraction
│   ├── GeometryHelpers.cs                     # Angle calculation & sub-millimeter metrics
│   ├── GeometryQCContext.cs                   # Execution context (Spatial Reference, Index, Features)
│   ├── GeometryValidator.cs                   # Orchestrator & Deduplication engine
│   ├── SpatialIndex.cs                        # 2D Bounding-Box Grid Spatial Hash
│   ├── UnitConverter.cs                       # Coordinate unit normalization (metric/degrees)
│   └── VertexIndex.cs                         # Sub-millimeter Point Spatial Hash
├── Graphics/
│   ├── QCGraphicManager.cs                    # Temporary map overlay manager (supports warning suppression)
│   └── QCGraphicSymbolProvider.cs             # CIM symbols for issue rendering (including Arc-Line triangle)
├── Models/
│   ├── IssueResult.cs                         # Issue data model with metric formatting
│   ├── IssueSeverity.cs                       # Enum: Error, Warning, Info
│   ├── MapLayerItem.cs                        # Layer selector dropdown model
│   ├── QCFeature.cs                           # In-memory polygon geometry container
│   └── QCStatistics.cs                        # Feature count & elapsed time counters
├── Services/
│   ├── GdbExportService.cs                    # Export issues to GDB Feature Dataset
│   ├── LoggingService.cs                      # Diagnostic logging service
│   ├── MapViewService.cs                      # Map panning & navigation engine
│   ├── ProgressService.cs                     # Task cancellation & progress reporting
│   └── ThemeService.cs                        # ArcGIS Pro Light/Dark theme synchronizer
├── UI/
│   ├── Buttons.cs                             # Ribbon Button definitions
│   ├── RelayCommand.cs                        # ICommand implementation for MVVM
│   ├── ResultsDockPane.xaml                   # Results DockPane View
│   ├── ResultsDockPane.xaml.cs                # Code-behind
│   ├── ResultsDockPaneViewModel.cs            # Results DockPane ViewModel
│   ├── SettingsDockPane.xaml                  # Settings DockPane View
│   ├── SettingsDockPane.xaml.cs               # Code-behind
│   ├── SettingsDockPaneViewModel.cs            # Settings DockPane ViewModel
│   └── ThemeResources.xaml                    # Navy/Cyan/Turquoise brushes & styles
└── Images/                                    # 16x16, 32x32 Custom Icons
```

---

## 3. Detailed Component Architecture

### A. Declarative Configuration (`Config.daml`)
* **Schema Compliance:** Strict adherence to ArcGIS Pro Registry:
  ```xml
  <ArcGIS defaultAssembly="GeometryQCAddIn.dll" defaultNamespace="GeometryQCAddIn" 
          xmlns="http://schemas.esri.com/DADF/Registry" 
          xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
          xsi:schemaLocation="http://schemas.esri.com/DADF/Registry http://Config.xsd">
    <AddInInfo id="{8a7f921d-44a3-4b92-95f2-953e5e6080dc}" version="1.0.5" desktopVersion="3.0">
      <Name>Geometry QC Analyzer</Name>
      <Description>High-performance, read-only Polygon Geometry Quality Control Analyzer for ArcGIS Pro.</Description>
      <Image>Images\AddinDesktop32.png</Image>
      <Author>Mahmoud Aboelmagd with Advanced AI</Author>
      <Company>ArcGIS Pro Solutions</Company>
      <Date>2026-10-30</Date>
      <Subject>Geometry Quality Control</Subject>
    </AddInInfo>
  ```
* **Packaging Rules:** Set `<insertModule autoLoad="false">`.
* **Ribbon Tab & Controls:**
  - Tab: `GeometryQC_Tab` ("Geometry QC")
  - Group 1: `GeometryQC_MainGroup` ("Validation", `appearsOnAddInTab="false"`)
    - `GeometryQC_ToggleBtn`
    - `GeometryQC_RunBtn`
    - `GeometryQC_ClearBtn`
    - `GeometryQC_CancelBtn`
  - Group 2: `GeometryQC_PanelsGroup` ("Panels", `appearsOnAddInTab="false"`)
    - `GeometryQC_ResultsBtn`
    - `GeometryQC_SettingsBtn`

### B. In-Memory 2D Spatial Index & Vertex Index

1. **`SpatialIndex.cs` (Bounding-Box Hash Grid):**
   - Calculates grid cell size adaptively based on the average bounding box dimension of candidate features.
   - Maps cell coordinates `(int X, int Y)` to buckets of candidate feature IDs.
   - Reduces polygon intersection candidate pairs from $O(N^2)$ to $O(N \log N)$ or near-linear complexity.
2. **`VertexIndex.cs` (Point Spatial Hash):**
   - Indexes all vertices across polygon rings using spatial hashing with cell size equal to tolerance (e.g., 1 cm).
   - Allows instant $O(1)$ neighborhood vertex queries for sub-millimeter snap checks and junction detection.
3. **Data Acquisition (`DisplayCacheProvider.cs`):**
   - Reads rendered screen graphics directly from ArcGIS Pro MapView display cache for instant preview of selected features within viewport.
   - **Feature Cap Protection (`MaxFeaturesToProcess`, default 500):** Pre-checks the selected feature count before acquisition. If exceeded, safely cancels the run and alerts the user to reduce selection or raise the limit in Settings.

---

### C. The 10 Concrete QC Checks

Each check implements `IGeometryCheck` and executes asynchronously with a `CancellationToken`:

1. **Invalid Geometry (`CHK_INVALID_GEOM`, Error):**
   - Executes `GeometryEngine.Instance.Simplify()`. If geometry changes or is null, flags error.
   - Checks for non-positive area, ring point count $< 3$, and `double.IsNaN` in coordinates.
2. **Overlap (`CHK_OVERLAP`, Error, default 0.00001 m² / 100 cm²):**
   - Uses `SpatialIndex` to find candidate overlapping pairs.
   - Computes `GeometryEngine.Instance.Intersection(polyA, polyB)`.
   - Decomposes multipart intersections into singlepart polygons; calculates area in m² or cm²; eliminates boundary touch slivers via minimum thickness guard (`Math.Max(sr?.XYTolerance ?? 0.001, 0.0005)`).
3. **Duplicate Geometry (`CHK_DUPLICATE`, Warning):**
   - Fast spatial vertex hash comparison followed by `GeometryEngine.Instance.Equals()`.
   - Suppresses redundant overlap reporting for identical features.
4. **Enclosed Gap (`CHK_GAP`, Warning, default 0.00001 m² / 100 cm²):**
   - Computes geometric union of adjacent polygons, identifies interior holes/rings that do not touch the exterior boundary, and flags areas above tolerance.
5. **Multi-Part Feature (`CHK_MULTIPART`, Info):**
   - Inspects `poly.PartCount > 1` in datasets requiring singlepart polygons.
6. **Short Segment (`CHK_SHORT_SEG`, Warning, default 0.15 m = 15.0 cm):**
   - Traverses polygon boundary segments. Flags segment length $< \text{tolerance}$, excluding coincident points ($d < 10^{-6}$).
7. **Angle Issue (`CHK_ANGLE`, Warning, default 30.0°):**
   - Traverses consecutive vertex triplets on each ring. Computes interior angle; flags extreme spikes and needles $< 30.0°$.
8. **Snap Issue (`CHK_SNAP`, Warning, default 0.15 m = 15.0 cm):**
   - Uses `VertexIndex` to find vertices from different features separated by $0 < d \le \text{tolerance}$.
   - Sub-millimeter reporting: If $d < 0.01\text{ m}$, outputs metric in millimeters (`mm`) with up to 4 decimal places (e.g., `0.04 mm`). Excludes identical vertices ($d < 10^{-5}\text{ m}$).
9. **Redundant Vertex (`CHK_REDUNDANT`, Info, default 175.0°):**
   - **Straight Edges:** Flags collinear vertices along straight lines where angle $\ge 175.0°$.
   - **Parametric True Curves (Circular Arcs & Cubic Béziers):**
     - Preserves valid transitions: Line $\leftrightarrow$ Curve and Arc $\leftrightarrow$ Bézier transitions are strictly protected.
     - **Circular Arcs:** Tests center coincidence, radius match, orientation consistency, and tangent angle $\ge 175.0°$.
     - **Cubic Béziers:** Tests $C^1$ tangent angle continuity $\ge 175.0°$ and $C^2$ second derivative curvature consistency.
   - **Optional Arc–Line Junction Check (`CHK_REDUNDANT_ARCLINE`, Warning, default 175.0°, disabled by default):**
     - When `FlagArcLineJunctions = true`, tests tangent deflection angle where a straight segment meets a circular arc or Bézier curve. If deflection angle $\ge \text{ArcLineAngleToleranceDegrees}$ (default 175.0°), flags vertex as possible redundant arc–line transition.
     - Rendered with Coral triangle CIM symbol (`RGB(255, 127, 80)`). Suppressed from map when `ShowWarningsOnMap = false`.
   - **Junction Guard:** Protects vertices that coincide with vertices of adjacent polygons where the neighbor bends or forms a legitimate transition. If both coincident vertices along a shared boundary are redundant, flags both.
10. **Missing Junction (`CHK_JUNCTION`, Warning, default 0.10 m = 10 cm):**
    - Identifies T-junctions where a vertex on polygon A touches an edge of polygon B, but polygon B lacks a matching snapped vertex.

---

### D. Geodatabase Error Export Service (`GdbExportService.cs`)

* **Target Geodatabase:** Resolves active project default geodatabase: `Project.Current.DefaultGeodatabasePath`.
* **Dataset Creation:** Creates Feature Dataset `QC_Errors`. If already existing, automatically increments name (`QC_Errors_1`, `QC_Errors_2`, etc.).
* **Schema Definition via `SchemaBuilder`:** Segregates issues into Points, Lines, and Polygons with metadata fields (`Issue_Type`, `Check_ID`, `Source_OID`, `Related_OIDs`, `Layer_Name`, `Severity`, `Metric_Val`, `Metric_Unit`, `Description`, `Export_Time`).
* **Bulk Insertion:** Writes features via high-performance `InsertCursor` inside `QueuedTask.Run`.

---

## 4. Mandatory Architectural Rules & Constraints

1. **Button Constructor Rule:** Every button class inheriting from `ArcGIS.Desktop.Framework.Contracts.Button` must explicitly call `Enabled = true;` in its parameterless constructor.
2. **AutoLoad Rule:** In `Config.daml`, `<insertModule>` must have `autoLoad="false"` per ArcGIS Pro add-in best practices.
3. **No Hot-Reloading:** ArcGIS Pro only reads add-in assemblies at startup. Always alert the user to close and restart ArcGIS Pro after any installation or build.
4. **Read-Only Safety:** The Add-In must never start an editing transaction (`EditOperation`) on the source layers.
5. **Feature Cap Protection:** The `DisplayCacheProvider` enforces `MaxFeaturesToProcess` (default: 500) to safeguard application responsiveness during selection processing.

