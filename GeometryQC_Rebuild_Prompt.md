# AI Rebuild Prompt: ArcGIS Pro Geometry QC Analyzer
# برومت إعادة بناء أداة فحص الجودة الهندسية باستخدام الذكاء الاصطناعي

<p align="center">
  <a href="#english-rebuild-prompt">English Rebuild Prompt</a> •
  <a href="#برومت-إعادة-البناء-باللغة-العربية">برومت إعادة البناء باللغة العربية</a>
</p>

---

<a name="english-rebuild-prompt"></a>
# English Rebuild Prompt

> **Prompt Instructions for AI**: Use the specifications, architectural details, and step-by-step requirements below to generate the complete C# / WPF source code, project files, and DAML configuration for an enterprise-grade ArcGIS Pro Add-In from scratch.

---

## A. Project Overview

The **Geometry QC Analyzer** is a high-performance, read-only polygon Quality Control (QC) Add-in for **ArcGIS Pro 3.x**. 

### 1. Purpose
The Add-in inspects selected polygon features (or features in the active map extent) to detect geometric topology violations, digitizing artifacts, and spatial inconsistencies without modifying underlying geodatabase records.

### 2. Problem It Solves
Manual topological inspection in GIS cadastral, parcel, and zoning datasets is slow and error-prone. Standard topology tools often require enterprise geodatabase schemas and write permissions. This Add-in provides instant, in-memory validation of 10 discrete geometric checks directly from the ribbon with visual map overlays and interactive zoom navigation. It also works seamlessly on live Feature Services without exporting to local geodatabases.

### 3. Target User
GIS Analysts, Cadastral Surveyors, Cartographers, and Spatial Data Quality Engineers working within ArcGIS Pro (supporting both Light and Dark themes).

---

## B. Technical Requirements

* **Programming Language:** C# (version 12.0 / `latest`, nullable reference types enabled).
* **Target Framework:** `.NET 8.0-windows` (`net8.0-windows`), targeting Windows x64 (`win-x64`).
* **UI Framework:** Windows Presentation Foundation (WPF) with ArcGIS Pro native styling (`pack://application:,,,/ArcGIS.Desktop.Framework;component/Themes/Default.xaml`).
* **Runtime Environment:** ArcGIS Pro 3.3.x, 3.4.x and later (.NET 8 runtime, x64).
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
│   ├── MapLayerItem.cs
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
5. **DockPane ViewModel (`ResultsDockPaneViewModel.cs`)**:
   - Manages map layer selection through `AvailableLayers` and `SelectedLayer` (`MapLayerItem`), dynamically populated via `RefreshLayersAsync()`.
   - Orchestrates execution on `QueuedTask`, updates progress via `ProgressService`, feeds `GeometryValidator` with the selected target layer URI, updates `IssueGroups` observable collection, and pushes graphics to `QCGraphicManager`.
6. **Geometry Validator (`GeometryValidator.cs`)**:
   - Fetches polygon geometries using `GeometryDataProvider` (`DisplayCacheProvider` or `LiveQueryProvider`), filtered by `targetLayerUri`.
   - Builds `GeometryQCContext` (with `SpatialIndex`, `VertexIndex`, and `UnitConverter`).
   - Executes the 10 `IGeometryCheck` instances with per-check and per-pair error isolation.
   - Runs a central **Verification and Deduplication Pipeline** (`DeduplicateAndVerifyIssues`) before delivering results.
7. **Results & Settings UI**: Bind to ViewModels with full Light/Dark Theme compatibility using `ThemeResources.xaml`.

---

## D. Functional Requirements Detail

### 1. Data Ingestion, Extent Filtering & Target Layer Selection
* **Target Layer Selection (`MapLayerItem`):**
  - The user can select a specific polygon layer from the active map or choose `"All Polygon Layers"` via a ComboBox on the Results DockPane.
  - Layer list refreshes dynamically via `RefreshLayersCommand` (`↻` button) querying `MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()`.
  - When a target layer is selected, `GeometryDataProvider` strictly limits feature acquisition to that layer, preventing irrelevant cross-layer overlaps, snapping, or missing junction artifacts.
* **Display Cache Mode (Default):** Read selected polygon features from in-memory selection cursor without issuing database queries. If map extent is active, filters features intersecting the extent.
* **Live Query Mode:** Query the underlying geodatabase table directly via `FeatureLayer.Search()` with a spatial filter bounding the active map extent.
* **Service Layer Guard:** Provide an explicit setting `EnableForServiceLayers` (default `false`). Skip ArcGIS Server / Feature Service layers unless explicitly enabled to avoid network bottlenecks.
* **Multi-selection Support:** If features are selected across multiple layers, inspect all layers and preserve the source layer name and ObjectID (`OID`) on each reported issue.

### 2. The 10 QC Geometry Checks
Implement 10 distinct, configurable checks inheriting from `IGeometryCheck`:

1. **Check 1: Invalid Geometry (`InvalidGeometryCheck`)**
   - Detects non-simple geometries according to OGC/Esri specifications using `GeometryEngine.Instance.IsSimpleAsFeature()`.
   - Flags self-intersections, interior ring self-touch, inversion of inner/outer rings, NaN/infinite coordinates, and degenerate rings (< 3 vertices).

2. **Check 2: Overlaps (`OverlapCheck`)**
   - Uses `SpatialIndex` bounding boxes to find candidate intersecting pairs `(Polygon A, Polygon B)`.
   - Per-Pair Exception Isolation: Wraps each candidate pair in `try/catch` to prevent a single corrupt geometry from aborting the entire overlap check.
   - Computes spatial intersection: `GeometryEngine.Instance.Intersection(polyA, polyB)`.
   - Multi-Part Decomposition: If two polygons overlap in multiple distinct areas, decomposes them via `GeometryEngine.Instance.MultipartToSinglePart(overlapPoly)` so each overlap patch is independently reported, measured, and zoomable.
   - Shared Boundary Sliver Guard: Filters out microscopic slivers caused by floating-point rounding along shared edges.
   - Duplicate Prioritization: Skips identical duplicate geometries when `CheckDuplicates` is enabled, avoiding double-reporting.
   - Human-Readable Area: Formats overlap area using `UnitConverter.FormatArea` (`m²`, `cm²`, `ha`).

3. **Check 3: Duplicate Geometries (`DuplicateCheck`)**
   - Uses multi-tier signature bucketing (PartCount, VertexCount, rounded Area, rounded Length) before exact `GeometryEngine.Instance.Equals()`.
   - Symmetrically deduplicates pairs `(fA, fB)` and `(fB, fA)`.

4. **Check 4: Enclosed Gaps (`GapCheck`)**
   - Groups touching/adjacent polygons into localized clusters.
   - Computes cluster union and convex hull to identify internal enclosed voids/holes, strictly distinguishing them from outer empty space.
   - Flags voids whose area $\ge \text{GapMinAreaSqMeters}$ (default `0.001` sq m).

5. **Check 5: Multi-Part Features (`MultiPartCheck`)**
   - Checks `polygon.PartCount > 1`. Flags multipart polygons and reports total part count.

6. **Check 6: Short Segments (`ShortSegmentCheck`)**
   - Iterates through all polygon segments across all parts.
   - Degenerate Segment Guard: Ignores sub-tolerance degenerate segments ($< \text{sr.XYTolerance}$) to prevent misreporting 0.00 cm artifacts.
   - Converts segment length to real-world units via `UnitConverter.FormatLinearDistance`.
   - Flags segments where $\text{length} < \text{ShortSegmentThresholdCm}$ (default `10.0` cm).

7. **Check 7: Angle Issues (`AngleIssueCheck`)**
   - Analyzes the interior/exterior vertex angle between consecutive segments:
     $$\vec{v}_1 = P_{i-1} - P_i, \quad \vec{v}_2 = P_{i+1} - P_i$$
     $$\theta = \arccos\left(\text{clamp}\left(\frac{\vec{v}_1 \cdot \vec{v}_2}{\|\vec{v}_1\| \|\vec{v}_2\|}, -1, 1\right)\right) \times \frac{180}{\pi}$$
   - Properly accounts for closed ring connectivity (skipping duplicate closing vertex).
   - Flags vertices where $\theta < \text{AngleThresholdDegrees}$ (default `5.0^\circ`).

8. **Check 8: Snap Issues (`SnapIssueCheck`)**
   - Uses `VertexIndex` spatial radius search around each vertex.
   - Sub-Millimeter Detection: Any separation greater than machine resolution ($> \text{sr.XYResolution}$) within the tolerance is recognized as an unsnapped gap.
   - Unit Precision: Formats distances under 1 cm in millimeters (`mm`) with up to 3–4 decimals (e.g. `0.04 mm`), eliminating misleading `0.00 cm` reports.
   - Deduplicates symmetric pairs and normalizes closed ring endpoints.
   - Flags non-coincident pairs where $\text{minDistance} < \text{distance} \le \text{SnapToleranceCm}$ (default `1.0` cm).

9. **Check 9: Redundant / Collinear Vertices (`RedundantVertexCheck`)**
   - Identifies unnecessary vertices situated on an almost straight line between their adjacent neighbors:
     $$\text{StraightAngle} = 180^\circ - \text{DeflectionAngle} \ge \text{RedundantVertexAngleDegrees} \quad (\text{default } 179.9^\circ)$$
   - **Topological Junction Vertex Guard (`IsJunctionVertex`):**
     Inspects touching features. If an adjacent feature meets at this point with a true corner/bend ($< \text{thresholdDeg}$), the vertex is protected as a valid junction.
     **Critical Rule:** If two coincident vertices on adjacent features both approach 180°, neither is a junction — **both are flagged as redundant collinear vertex errors!**

10. **Check 10: Missing Junction Vertices (`JunctionVertexCheck`)**
    - Detects T-junctions: where a vertex $V$ of Polygon A lies within $\le \text{MissingJunctionToleranceCm}$ (default `1.0` cm) of an edge segment $(P_1, P_2)$ of Polygon B, but Polygon B lacks a coincident vertex.
    - Endpoint Coincidence Guard: Skips vertices that are within tolerance of segment endpoints $P_1$ or $P_2$ (already snapped).
    - Closing Vertex Guard: Skips polygon closing vertex duplicate index.
    - Multi-touch Deduplication: Consolidates multiple adjacent features touching the same T-junction location into a single issue report on the target feature lacking the node.

---

### 3. Results Verification & Deduplication Pipeline (`GeometryValidator.cs`)
All raw issues emitted by checks pass through a centralized pipeline (`DeduplicateAndVerifyIssues`):
1. **Existence Verification:** Validates that issue locations have finite, non-NaN coordinates and that `IssueGeometry` is not empty.
2. **Duplicate/Overlap Disambiguation:** When two features are 100% coincident duplicates, `Duplicate Geometry` takes precedence and redundant `Overlap` issues for that pair are suppressed.
3. **Pairwise Symmetric Normalization:** For pairwise issues (Overlap, Duplicate, Snap, Junction), feature IDs are normalized (`min(OidA, OidB), max(OidA, OidB)`), eliminating duplicate reciprocal reports (A→B and B→A).
4. **Millimeter Spatial Deduplication:** Coordinates are rounded to millimeter precision, discarding repeated issues emitted at the exact same physical location.

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

### 2. Results DockPane (`ResultsDockPane.xaml`)
Docked on the right side:
* **Header Panel:**
  * Title: "Geometry QC Results" (`DockPaneHeaderStyle`).
  * Subtitle: Dynamic performance metrics (`MetricsText`), e.g., "Analyzed 150 features in 184ms".
  * Issue Counter Badge: Pill-shaped badge displaying total verified issue count.
* **Target Layer Selector Card:**
  * Label: "Layer:".
  * `ComboBox`: Bound to `AvailableLayers` and `SelectedLayer`, showing layer names or "All Polygon Layers".
  * Refresh Button: `↻` button bound to `RefreshLayersCommand` to dynamically refresh map layers.
* **Progress Indicator Card:** Thin progress bar visible only when `IsBusy == true`, with status text and inline "Cancel" button.
* **Issue TreeView:**
  * Category Header: Issue category name with count badge.
  * Child Node: Feature OID, Layer Name in parentheses, short description, and an inline "Zoom" button.
* **Selected Issue Details Panel:** Displays complete diagnostic text of the selected error.
* **Bottom Toolbar:** Full-width "Run QC" and "Clear Results" action buttons.

### 3. Settings DockPane (`SettingsDockPane.xaml`)
* **General Execution Group:**
  * Enable/Disable QC toggle.
  * Data source selection: Display Cache vs. Live Query.
  * Enable for Service Layers toggle.
  * Execution mode: Manual Run vs. Auto-run on Selection Change.
* **Checks & Tolerances Group (with numeric inputs):**
  * Toggles and tolerance values for each of the 10 checks.
* **Footer:** "Restore Defaults" button.

### 4. Native Theme Styling (`ThemeResources.xaml` & `ThemeService.cs`)
* Merges `pack://application:,,,/ArcGIS.Desktop.Framework;component/Themes/Default.xaml`.
* Manages a sleek Navy/Cyan/Turquoise color palette across both Light and Dark modes.
* Detects ArcGIS Pro theme changes dynamically and injects harmonious styling brushes into application resources.

---

## F. Implementation Constraints & Rules

1. **ArcGIS Pro Packaging Target:**
   In `GeometryQCAddIn.csproj`, the packaging post-build target **must** place assemblies in `Install/` and copy the finished package to the project root directory for immediate user deployment.
2. **Button Constructor Rule:**
   Every button class inheriting from `ArcGIS.Desktop.Framework.Contracts.Button` must set `Enabled = true;` in its parameterless constructor.
3. **Module AutoLoad Rule:**
   In `Config.daml`, specify `autoLoad="false"` on `<insertModule ...>` to prevent startup race conditions.
4. **Read-Only Safety:**
   The Add-in must **never** start an edit operation or modify geodatabase tables. All geometry evaluations and overlays are strictly read-only and in-memory.
5. **Ribbon Tab Isolation Rule (`appearsOnAddInTab="false"`):**
   In `Config.daml`, ribbon `<group>` elements intended exclusively for the custom tab must set `appearsOnAddInTab="false"` to prevent duplication in the generic "Add-Ins" tab.

---

<a name="برومت-إعادة-البناء-باللغة-العربية"></a>
# برومت إعادة البناء باللغة العربية (Arabic Rebuild Prompt)

> **تعليمات للذكاء الاصطناعي (AI Prompt)**: استخدم المواصفات والمعايير المعمارية والبرمجية المذكورة أدناه لتوليد الكود المصدري الكامل بلغة C# وواجهات WPF وملفات المشروع وإعدادات DAML لإنشاء إضافة ArcGIS Pro Add-In احترافية من الصفر.

---

## أ. نظرة عامة على المشروع

إضافة **Geometry QC Analyzer** هي أداة تدقيق وفحص جودة مكانية وطوبولوجية لطبقات المضلعات في برنامج **ArcGIS Pro 3.x**، تعتمد كلياً على القراءة فقط (`Read-Only`) وتعمل في الذاكرة بأداء فائق.

### 1. الغرض من الأداة
فحص مضلعات المعالم المحددة (أو في نطاق الخريطة الحالي) لاكتشاف العيوب الطوبولوجية وعيوب الرسم الرقمي دون إجراء أي تعديل على قاعدة البيانات الأصلية ودون الحاجة لبناء Geodatabase Topology.

### 2. المشكلة التي تعالجها
الفحص الطوبولوجي التقليدي في قواعد البيانات الجغرافية يتطلب إعدادات معقدة وبناء طوبولوجيا وقوانين تفصيلية، وإذا كانت البيانات على Feature Service فيلزم تصديرها. توفر هذه الإضافة فحصاً فورياً لـ 10 عيوب هندسية أساسية من شريط الأدوات مباشرة مع توجيه بصري وتكبير فوري.

---

## ب. المتطلبات التقنية الأساسية

* **لغة البرمجة:** C# (الإصدار 12.0 / `latest` مع تفعيل `nullable reference types`).
* **إطار العمل المستهدف:** `.NET 8.0-windows` لمعمارية Windows x64.
* **واجهة المستخدم:** Windows Presentation Foundation (WPF) بنمط ArcGIS Pro الأصلي.
* **بيئة التشغيل:** ArcGIS Pro 3.3.x أو 3.4.x أو أحدث (بيئة .NET 8).
* **حزمة المخرجات:** ملف `.esriAddinX`، وتوضع جميع المكتبات داخل مجلد داخلي باسم `Install/`.

---

## ج. الفحوصات الهندسية العشرة المطلوبة (The 10 Checks)

1. **Invalid Geometry (`CHK_INVALID_GEOM`):** كشف المضلعات غير الصحيحة ذاتياً والتقاطعات الذاتية والحلقات المعكوسة والإحداثيات غير المعرفة.
2. **Overlap (`CHK_OVERLAP`):** كشف المساحات المتداخلة بين المضلعات وتقسيم التداخلات المتعددة وعرض مساحتها بدقة (`m²` أو `cm²`).
3. **Duplicate Geometry (`CHK_DUPLICATE`):** كشف المضلعات المتطابقة هندسياً بنسبة 100% ومنع تكرارها كـ Overlap.
4. **Enclosed Gap (`CHK_GAP`):** كشف الفجوات والثقوب الهوائية المغلقة والمحصورة تماماً بين المضلعات المتجاورة.
5. **Multi-Part Feature (`CHK_MULTIPART`):** اكتشاف المعالم التي تتكون من أكثر من مضلع منفصل (PartCount > 1).
6. **Short Segment (`CHK_SHORT_SEG`):** اكتشاف الأضلاع متناهية الصغر الأقل من التفاوت المسموح (افتراضياً 10 سم).
7. **Angle Issue (`CHK_ANGLE`):** اكتشاف الزوايا الحادة والشاذة جداً (Spikes) الناتجة عن أخطاء النقر بالماوس.
8. **Snap Issue (`CHK_SNAP`):** كشف الرؤوس المتقاربة غير الملتقطة. تُقاس المسافات الأقل من سنتيمتر بوحدة الملليمتر (`mm`) بدقة حتى 3 و4 خانات عشرية (مثل `0.04 mm`) لمنع رسائل `0.00 cm`.
9. **Redundant Vertex (`CHK_REDUNDANT`):** كشف الرؤوس الزائدة على خط مستقيم (180°). إذا وُجد رأسان متطابقان على حد مضلعين وكلاهما زاويته تقترب من 180°، يُعتبران خطأ رأس زائد على كلا المضلعين. أما إذا كان للمضلع الآخر زاوية انكسار حقيقية (< 180°) فتُحمى النقطة كنقطة ربط طوبولوجية.
10. **Missing Junction (`CHK_JUNCTION`):** كشف العقد التبادلية المفقودة في الوصلات على شكل T-Junction عندما يلامس رأس مضلع ضلع مضلع مجاور دون وجود رأس مشترك.

---

## د. واجهة المستخدم والتصميم

1. **شريط الأدوات (Ribbon Tab):**
   - تبويب مستقل مخصص باسم `Geometry QC`.
   - أزرار التحكم: `Enable QC`, `Run QC`, `Clear Results`, `Cancel`, `Results`, `Settings`.
2. **لوحة النتائج (Results DockPane):**
   - محدد الطبقة المستهدفة (Target Layer Selector) مع زر التحديث السريع `↻`.
   - شجرة مصنفة للأخطاء مع زر `Zoom` للانتقال الفوري وتكبير الخطأ على الخريطة.
   - لوحة تفاصيل تعرض القياس الدقيق للمشكلة.
3. **لوحة الإعدادات (Settings DockPane):**
   - التحكم في حساسية وتفاوتات الفحوصات الـ 10 واختيار طريقة جلب البيانات (Display Cache أو Live Query).
4. **دعم المظهر الفاتح والداكن (Light & Dark Themes):**
   - ألوان مخصصة بتدرجات الكحلي والتركواز تتكيف تلقائياً مع إعدادات ArcGIS Pro.

---
*تم إعداد هذا البرومت لبناء وتطوير إضافة Geometry QC Analyzer بالكامل.*
