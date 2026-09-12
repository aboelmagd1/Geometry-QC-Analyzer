# Prompt: Build the "ArcGIS Pro Geometry QC Analyzer" Add-In from Scratch

> **Prompt Instructions for AI**: Use the specifications, architectural details, and step-by-step requirements below to generate the complete C# / WPF source code, project files, and DAML configuration for an enterprise-grade ArcGIS Pro Add-In from scratch.

---

## A. Project Overview

The **Geometry QC Analyzer** is a high-performance, read-only polygon Quality Control (QC) Add-in for **ArcGIS Pro 3.x**. 

### 1. Purpose
The Add-in inspects selected polygon features (or features in the active map extent) to detect geometric topology violations, digitizing artifacts, and spatial inconsistencies without modifying underlying geodatabase records.

### 2. Problem It Solves
Manual topological inspection in GIS cadastral, parcel, and zoning datasets is slow and error-prone. Standard topology tools often require enterprise geodatabase schemas and write permissions. This Add-in provides instant, in-memory validation of 10 discrete geometric checks directly from the ribbon with visual map overlays and interactive zoom navigation.

### 3. Target User
GIS Analysts, Cadastral Surveyors, Cartographers, and Spatial Data Quality Engineers working within ArcGIS Pro (supporting both Light and Dark themes).

---

## B. Technical Requirements

* **Programming Language:** C# (version 12.0 / `latest`, nullable reference types enabled).
* **Target Framework:** `.NET 8.0-windows` (`net8.0-windows`), targeting Windows x64 (`win-x64`).
* **UI Framework:** Windows Presentation Foundation (WPF) with ArcGIS Pro native styling (`pack://application:,,,/ArcGIS.Desktop.Framework;component/Themes/Default.xaml`).
* **Runtime Environment:** ArcGIS Pro 3.0+ (tested on ArcGIS Pro 3.4.x, x64).
* **Target Output Package:** ArcGIS Pro Add-in Package (`.esriAddinX`), where all managed assemblies (`.dll`, `.pdb`, `.deps.json`) **must** reside inside an internal `Install/` folder within the zip archive.
* **Dependencies & References:**
  * `ArcGIS.Core.dll`
  * `ArcGIS.Desktop.Framework.dll`
  * `Extensions\Core\ArcGIS.Desktop.Core.dll`
  * `Extensions\Mapping\ArcGIS.Desktop.Mapping.dll`
  * `Extensions\DesktopExtensions\ArcGIS.Desktop.Extensions.dll`
  * `ArcGIS.Desktop.Shared.Wpf.dll`
  * `ArcGIS.Desktop.Resources.dll`

---

## C. Structure and Files

Generate the project with the following directory structure:

```text
GeometryQCAddIn/
├── Config.daml
├── GeometryQCAddIn.csproj
├── GeometryQCAddIn.sln
├── GeometryQCModule.cs
├── package.ps1
├── Config/
│   └── GeometryQCSettings.cs
├── Models/
│   ├── IssueSeverity.cs
│   ├── IssueResult.cs
│   ├── QCFeature.cs
│   └── QCStatistics.cs
├── Core/
│   ├── GeometryQCContext.cs
│   ├── GeometryValidator.cs
│   ├── GeometryHelpers.cs
│   ├── UnitConverter.cs
│   ├── SpatialIndex.cs
│   ├── VertexIndex.cs
│   ├── GeometryDataProvider.cs
│   ├── DisplayCacheProvider.cs
│   ├── LiveQueryProvider.cs
│   └── Checks/
│       ├── IGeometryCheck.cs
│       ├── InvalidGeometryCheck.cs
│       ├── OverlapCheck.cs
│       ├── DuplicateCheck.cs
│       ├── GapCheck.cs
│       ├── MultiPartCheck.cs
│       ├── ShortSegmentCheck.cs
│       ├── AngleIssueCheck.cs
│       ├── SnapIssueCheck.cs
│       ├── RedundantVertexCheck.cs
│       └── JunctionVertexCheck.cs
├── Graphics/
│   ├── QCGraphicManager.cs
│   └── QCGraphicSymbolProvider.cs
├── Services/
│   ├── LoggingService.cs
│   ├── MapViewService.cs
│   ├── ProgressService.cs
│   ├── SelectionService.cs
│   └── ThemeService.cs
├── UI/
│   ├── Buttons.cs
│   ├── RelayCommand.cs
│   ├── ThemeResources.xaml
│   ├── ResultsDockPane.xaml
│   ├── ResultsDockPane.xaml.cs
│   ├── ResultsDockPaneViewModel.cs
│   ├── SettingsDockPane.xaml
│   ├── SettingsDockPane.xaml.cs
│   └── SettingsDockPaneViewModel.cs
└── Tests/
    └── GeometryQCTests.cs
```

### Component Relationships & Data Flow
1. **Ribbon (`Config.daml` / `UI/Buttons.cs`)**: Triggers `RunQCButton`, `ToggleQCButton`, `ResultsButton`, `SettingsButton`, `ClearResultsButton`, or `CancelQCButton`.
2. **Module (`GeometryQCModule.cs`)**: Initializes `ThemeService` and `SelectionService` on startup (protected by try/catch).
3. **Theme Service (`ThemeService.cs`)**: Detects Light/Dark ArcGIS Pro mode and updates custom Navy/Cyan theme palette brushes dynamically.
4. **Selection Service (`SelectionService.cs`)**: Listens to `MapSelectionChangedEvent`. In `Auto` mode, debounces selection changes (minimum 200ms) and calls `ResultsDockPaneViewModel.RunValidationAsync()`.
5. **DockPane ViewModel (`ResultsDockPaneViewModel.cs`)**: Orchestrates execution on `QueuedTask`, updates progress via `ProgressService`, feeds `GeometryValidator`, updates `IssueGroups` observable collection, and pushes graphics to `QCGraphicManager`.
6. **Geometry Validator (`GeometryValidator.cs`)**: Fetches polygon geometries using `GeometryDataProvider` (`DisplayCacheProvider` or `LiveQueryProvider`), builds `GeometryQCContext` (with `SpatialIndex` and `UnitConverter`), and executes the 10 `IGeometryCheck` instances concurrently or sequentially.
7. **Results & Settings UI**: Bind to ViewModels with full Light/Dark Theme compatibility using `ThemeResources.xaml`.

---

## D. Functional Requirements Detail

### 1. Data Ingestion & Extent Filtering
* **Display Cache Mode (Default):** Read selected polygon features from active `MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()`. Query only layers whose shape type is `esriGeometryPolygon`. For in-memory speed, query features selected in the current map view extent.
* **Live Query Mode:** Query the underlying geodatabase table directly via `FeatureLayer.Search()` with a spatial filter bounding the active map extent.
* **Service Layer Guard:** Provide an explicit setting `EnableForServiceLayers` (default `false`). Skip ArcGIS Server / Feature Service layers unless explicitly enabled to avoid network bottlenecks.
* **Multi-selection Support:** If features are selected across multiple layers, inspect all layers and preserve the source layer name and ObjectID (`OID`) on each reported issue.

### 2. The 10 QC Geometry Checks
Implement 10 distinct, configurable checks inheriting from `IGeometryCheck`:

1. **Check 1: Invalid Geometry (`InvalidGeometryCheck`)**
   * Detects non-simple geometries according to OGC/Esri specifications using `GeometryEngine.Instance.IsSimple()`.
   * Flags self-intersections, interior ring self-touch, inversion of inner/outer rings, and unclosed polygon rings.
2. **Check 2: Overlaps (`OverlapCheck`)**
   * Uses `SpatialIndex` bounding boxes to find candidate intersecting pairs `(Polygon A, Polygon B)`.
   * Computes spatial intersection: `GeometryEngine.Instance.Intersection(polyA, polyB, GeometryDimension.esriGeometry2Dimension)`.
   * Flags overlap if the intersection polygon area >= `OverlapMinAreaSqMeters` (default `0.0001` sq m).
3. **Check 3: Duplicate Geometries (`DuplicateCheck`)**
   * Checks if two features share the exact same boundary vertices (either forward or reversed), or if their overlap area ratio against both features exceeds `0.9999`.
4. **Check 4: Enclosed Gaps (`GapCheck`)**
   * Computes the union or merged boundary of adjacent selected polygons within their mutual convex hull.
   * Identifies unassigned slivers/voids enclosed by polygons whose area is between `GapMinAreaSqMeters` (default `0.001` sq m) and a maximum sliver threshold, excluding external outer voids.
5. **Check 5: Multi-Part Features (`MultiPartCheck`)**
   * Checks `polygon.PartCount > 1`. Flags multipart polygons and reports total part count.
6. **Check 6: Short Segments (`ShortSegmentCheck`)**
   * Iterates through every segment of every ring in the polygon.
   * Computes geodesic or planar segment length in map units, converts to centimeters via `UnitConverter`.
   * Flags segments where `length < ShortSegmentThresholdCm` (default `10.0` cm).
7. **Check 7: Angle Issues (`AngleIssueCheck`)**
   * Analyzes the interior/exterior vertex angle between consecutive segments:
     $$\vec{v}_1 = P_{i-1} - P_i, \quad \vec{v}_2 = P_{i+1} - P_i$$
     $$\theta = \arccos\left(\frac{\vec{v}_1 \cdot \vec{v}_2}{\|\vec{v}_1\| \|\vec{v}_2\|}\right) \times \frac{180}{\pi}$$
   * Flags vertices where $\theta < \text{AngleThresholdDegrees}$ (default `5.0^\circ`).
8. **Check 8: Snap Issues (`SnapIssueCheck`)**
   * Compares each vertex against all other vertices in nearby features or non-adjacent segments of the same feature.
   * Flags pairs where $0 < \text{distance} \le \text{SnapToleranceCm}$ (default `1.0` cm).
9. **Check 9: Redundant / Collinear Vertices (`RedundantVertexCheck`)**
   * Identifies unnecessary vertices situated on an almost straight line between their adjacent neighbors.
   * Flags vertex if the angle between incoming and outgoing segments $\ge \text{RedundantVertexAngleDegrees}$ (default `179.9^\circ`).
10. **Check 10: Missing Junction Vertices (`JunctionVertexCheck`)**
    * Detects T-junctions: where a vertex $V$ of Polygon A lies within $\le \text{MissingJunctionToleranceCm}$ (default `1.0` cm) of an edge segment $(P_1, P_2)$ of Polygon B, but Polygon B lacks a coincident vertex at that coordinate.
    * Uses metric perpendicular projection distance:
      $$t = \frac{(V - P_1) \cdot (P_2 - P_1)}{\|P_2 - P_1\|^2}$$
      Ensures $V$ projects strictly inside the segment interior (avoiding false flags on endpoints).

### 3. Execution Control & Threading
* All spatial queries and geometry operations must execute inside `QueuedTask.Run()` to respect the ArcGIS Pro SDK apartment model.
* Support cooperative cancellation via `CancellationTokenSource`.
* Emit real-time percentage and status updates through `ProgressService`.

### 4. Interactive Results & Map Graphics
* Selecting an issue in the Results tree triggers `ZoomToIssueAsync`: centers the `MapView` on the issue geometry with a 20% bounding buffer and flashes/highlights the geometry.
* Map graphic overlays must use temporary graphics via `MapView.Active.AddOverlay` or temporary CIM graphic containers, removable with a single "Clear Results" command.

---

## E. User Interface (WPF & ArcGIS Pro Native Theme)

### 1. Ribbon Tab (`Config.daml`)
Create a custom tab without duplicating buttons on the default "Add-Ins" tab:
* **Tab:** `GeometryQC_Tab` ("Geometry QC")
* **Group 1:** `GeometryQC_MainGroup` ("Validation", `appearsOnAddInTab="false"`)
  * Button `GeometryQC_ToggleBtn` (Large, Caption "Enable QC", icon `GenericCheckMark32.png`)
  * Button `GeometryQC_RunBtn` (Large, Caption "Run QC", icon `GenericPlay32.png`)
  * Button `GeometryQC_ClearBtn` (Medium, Caption "Clear Results", icon `GenericEraser16.png`)
  * Button `GeometryQC_CancelBtn` (Medium, Caption "Cancel", icon `GenericStop16.png`)
* **Group 2:** `GeometryQC_PanelsGroup` ("Panels", `appearsOnAddInTab="false"`)
  * Button `GeometryQC_ResultsBtn` (Large, Caption "Results", icon `Table32.png`)
  * Button `GeometryQC_SettingsBtn` (Large, Caption "Settings", icon `GenericOptions32.png`)
*(Note: Groups must explicitly specify `appearsOnAddInTab="false"` so they do not appear redundantly in the default ArcGIS Pro `Add-Ins` tab, remaining exclusively under `Geometry QC`.)*

### 2. Results DockPane (`ResultsDockPane.xaml`)
Docked on the right side:
* **Header:**
  * Title: "Geometry QC Results" (`DockPaneHeaderStyle`).
  * Subtitle: Dynamic performance metrics (`MetricsText`), e.g., "Analyzed 150 features in 184ms".
  * Issue Counter Badge: Pill-shaped border (`CornerRadius="12"`, background `{DynamicResource Esri_Blue50a_Brush}`, foreground white) displaying "Issues: N".
* **Progress Bar:** Thin progress indicator visible only when `IsBusy == true`, with a textual status label and inline "Cancel" button.
* **Issue TreeView:**
  * Parent Node: Issue Category Name (e.g. "Overlaps", "Angle Issue") with count badge.
  * Child Node: Feature OID, Layer Name in parentheses, short description, and an inline "Zoom" button.
* **Selected Issue Details Panel:** Appears at the bottom of the tree, displaying full diagnostic text of the selected error.
* **Bottom Toolbar:** Full-width "Run QC" and "Clear Results" action buttons (`Style="{DynamicResource Esri_Button}"`).

### 3. Settings DockPane (`SettingsDockPane.xaml`)
* **General Execution Group:**
  * CheckBox: "Enable Geometry QC".
  * RadioButtons: "Display Cache (In-Memory Selection, Default)" vs "Live Query (Feature Layer Filter)".
  * CheckBox: "Enable for Service Layers".
  * RadioButtons: "Manual Run (Run button)" vs "Auto-run on Selection Change".
* **Checks & Tolerances Group (with numeric TextBoxes styled via `ThemeAwareTextBoxStyle`):**
  * CheckBox + Input: Invalid Geometry.
  * CheckBox + Input: Overlaps (Min Overlap Area in sq m).
  * CheckBox + Input: Duplicate Geometries.
  * CheckBox + Input: Enclosed Gaps (Min Gap Area in sq m).
  * CheckBox + Input: Multi-Part Features.
  * CheckBox + Input: Short Segments (Threshold in cm).
  * CheckBox + Input: Angle Issues (Min Angle in degrees).
  * CheckBox + Input: Snap Issues (Tolerance in cm).
  * CheckBox + Input: Redundant / Collinear Vertices (Angle Threshold in degrees).
  * CheckBox + Input: Missing Junction Vertices (Tolerance in cm).
* **Footer:** "Restore Defaults" button.

### 4. Native Theme Styling (`ThemeResources.xaml` & `ThemeService.cs`)
* Must merge `pack://application:,,,/ArcGIS.Desktop.Framework;component/Themes/Default.xaml`.
* Must **NOT** alias DynamicResources using `<DynamicResourceExtension>` inside the dictionary (which throws `InvalidOperationException` on `Foreground`).
* Must directly apply official Esri dynamic resource keys:
  * Primary Text: `{DynamicResource Esri_TextStyleDefaultBrush}`
  * Subdued Text: `{DynamicResource Esri_TextStyleSubduedBrush}`
  * Header/Emphasis: `{DynamicResource Esri_TextStyleEmphasisBrush}`
  * Disabled Text: `{DynamicResource Esri_TextStyleDisabledBrush}`
  * Window/DockPane Background: `{DynamicResource Esri_DockPaneClientAreaBackgroundBrush}`
  * Input Background: `{DynamicResource Esri_ControlBackgroundBrush}`
  * Hover Background: `{DynamicResource Esri_BackgroundHoverBrush}`
  * Selection: `{DynamicResource Esri_BackgroundSelectedBrush}`
  * Borders: `{DynamicResource Esri_BorderBrush}`
* **Dynamic Palette Management (`ThemeService.cs`):** Manages a sleek Navy/Cyan/Turquoise color palette across both Light and Dark modes. Detects ArcGIS Pro theme changes dynamically and injects harmonious styling brushes into application resources.

---

## F. Internal Processing Logic & Mathematical Formulas

### 1. Coordinate & Unit Conversion (`UnitConverter.cs`)
All spatial tolerances are configured in real-world metric units (centimeters or square meters), while features may be in Projected or Geographic Coordinate Systems:
* If Projected (`SpatialReference.Unit` is linear): Convert tolerance in cm to map units using `unit.ConversionFactor`:
  $$\text{tol}_{\text{map}} = \frac{\text{tol}_{\text{cm}}}{100.0 \times \text{conversionFactorToMeters}}$$
* If Geographic (degrees): Project coordinates dynamically or approximate via local latitude WGS84 meter conversion factor.

### 2. Point-to-Segment Metric Projection (`GeometryHelpers.cs`)
To find the distance from point $P$ to segment $(A, B)$ in 2D plane:
$$\vec{AB} = B - A, \quad \vec{AP} = P - A$$
$$L^2 = \vec{AB}_x^2 + \vec{AB}_y^2$$
If $L^2 < 10^{-14}$, distance is $\|P - A\|$. Otherwise:
$$t = \frac{\vec{AP}_x \vec{AB}_x + \vec{AP}_y \vec{AB}_y}{L^2}$$
Projection point $Q$:
* If $t \le 0: Q = A$
* If $t \ge 1: Q = B$
* Else: $Q = A + t \cdot \vec{AB}$
$$\text{Distance} = \|P - Q\|$$

### 3. Collinear Vertex Angle Formula (`RedundantVertexCheck.cs`)
For vertex $P_i$ between $P_{i-1}$ and $P_{i+1}$:
$$\vec{u} = P_i - P_{i-1}, \quad \vec{v} = P_{i+1} - P_i$$
$$\cos \phi = \frac{\vec{u} \cdot \vec{v}}{\|\vec{u}\| \|\vec{v}\|}$$
$$\text{Angle} = \arccos(\text{clamp}(\cos \phi, -1, 1)) \times \frac{180}{\pi}$$
If $\text{Angle} \ge \text{Threshold}$ (e.g. $179.9^\circ$), vertex $P_i$ is collinear and redundant.

---

## G. Expected Outputs

1. **In-Memory Models:**
   * An `IssueResult` object for each detected issue containing:
     * `Id` (GUID)
     * `Oid` (long)
     * `LayerName` (string)
     * `CheckName` (string)
     * `Description` (string with exact measurements, e.g. "Acute angle of 3.2° detected at vertex 14")
     * `Severity` (`Error` or `Warning`)
     * `Geometry` (Point, Line, or Polygon highlighting the defect)
     * `MetricValue` (double)
     * `MetricUnit` (string, e.g., "cm", "deg", "sq m")
2. **Settings Persistence:**
   * Saved as JSON at `%LOCALAPPDATA%\GeometryQCAddIn\settings.json`.
3. **Map Visual Feedback:**
   * Highlight overlays drawn on map using specific CIM symbols:
     * Overlaps: Semi-transparent red fill with dark red crosshatch outline.
     * Angle Issues: Violet/Magenta diamond marker.
     * Short Segments: Bright orange highlighted line.
     * Missing Junctions: Cyan circle marker.
     * Unclosed / Invalid: Crimson cross marker.
4. **User Notifications:**
   * Progress updates emitted to DockPane status bar.
   * Safe dialogs via `ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show` on unexpected exceptions.

---

## H. Additional Implementation Constraints & Rules

1. **ArcGIS Pro Packaging Target:**
   In `GeometryQCAddIn.csproj`, the packaging post-build target **must** place assemblies in `Install/` and copy the finished package to the project root directory for immediate user deployment:
   ```xml
   <Target Name="PackageArcGISProAddIn" AfterTargets="Build">
     <PropertyGroup>
       <AddInDir>$(TargetDir)AddInStaging</AddInDir>
       <AddInPackage>$(TargetDir)GeometryQCAddIn.esriAddinX</AddInPackage>
     </PropertyGroup>
     <MakeDir Directories="$(AddInDir)\Install" />
     <Copy SourceFiles="$(TargetDir)GeometryQCAddIn.dll" DestinationFolder="$(AddInDir)\Install" />
     <Copy SourceFiles="$(TargetDir)GeometryQCAddIn.pdb" DestinationFolder="$(AddInDir)\Install" Condition="Exists('$(TargetDir)GeometryQCAddIn.pdb')" />
     <Copy SourceFiles="$(TargetDir)GeometryQCAddIn.deps.json" DestinationFolder="$(AddInDir)\Install" Condition="Exists('$(TargetDir)GeometryQCAddIn.deps.json')" />
     <Copy SourceFiles="$(ProjectDir)Config.daml" DestinationFolder="$(AddInDir)" />
     <ItemGroup>
       <AddinImages Include="$(ProjectDir)Images\**\*.*" />
     </ItemGroup>
     <Copy SourceFiles="@(AddinImages)" DestinationFolder="$(AddInDir)\Images\%(RecursiveDir)" Condition="'@(AddinImages)' != ''" />
     <ZipDirectory SourceDirectory="$(AddInDir)" DestinationFile="$(AddInPackage)" Overwrite="true" />
     <RemoveDir Directories="$(AddInDir)" />
     <Copy SourceFiles="$(AddInPackage)" DestinationFolder="$(ProjectDir)" />
   </Target>
   ```
2. **Button Constructor Rule:**
   Every button class inheriting from `ArcGIS.Desktop.Framework.Contracts.Button` must set `Enabled = true;` in its parameterless constructor.
3. **Module AutoLoad Rule:**
   In `Config.daml`, specify `autoLoad="false"` on `<insertModule ...>` to prevent startup race conditions before project/map initialization.
4. **Read-Only Safety:**
   The Add-in must **never** start an edit operation or call `Row.Store()` / `Table.CreateRow()`. All geometry evaluation and graphic display must remain purely read-only and in-memory.
5. **Ribbon Tab Isolation Rule (`appearsOnAddInTab="false"`):**
   In `Config.daml`, ribbon `<group>` elements intended exclusively for the custom tab must set `appearsOnAddInTab="false"`. If set to `true` or omitted, ArcGIS Pro will duplicate the groups inside the generic "Add-Ins" tab.
