# ArcGIS Pro Geometry QC Analyzer — User Guide
# دليل مستخدم أداة فحص الجودة الهندسية في ArcGIS Pro

<p align="center">
  <a href="#english-user-guide">English User Guide</a> •
  <a href="#دليل-المستخدم-باللغة-العربية">دليل المستخدم باللغة العربية</a> •
  <a href="QC_CHECKS_LOGIC.md">Detailed Mathematical & Cadastral Logic Guide</a>
</p>

---

<a name="english-user-guide"></a>
# English User Guide

## Table of Contents
1. [Overview](#1-overview)
2. [Installation & Setup](#2-installation--setup)
3. [User Interface Overview](#3-user-interface-overview)
   - [Geometry QC Ribbon Tab](#a-geometry-qc-ribbon-tab)
   - [Results DockPane](#b-results-dockpane)
   - [Settings DockPane](#c-settings-dockpane)
4. [Target Layer Selection & Data Source Modes](#4-target-layer-selection--data-source-modes)
5. [The 10 QC Checks in Detail](#5-the-10-qc-checks-in-detail)
6. [Deduplication, Junction Guard & Sub-Millimeter Engine](#6-deduplication-junction-guard--sub-millimeter-engine)
7. [Graphic Symbols Legend on the Map](#7-graphic-symbols-legend-on-the-map)
8. [Exporting Errors to Geodatabase (GDB)](#8-exporting-errors-to-geodatabase-gdb)
9. [Recommended Workflow](#9-recommended-workflow)
10. [Frequently Asked Questions (FAQ)](#10-frequently-asked-questions-faq)

---

## 1. Overview

**Geometry QC Analyzer** is a native, professional ArcGIS Pro add-in developed for GIS professionals, surveyors, and cadastral authorities. It performs comprehensive geometric, topological, and cartographic validation on polygon layers without altering underlying datasets or requiring complex geodatabase topology configurations.

### Key Capabilities:
* **⚡ 100% In-Memory & Read-Only:** Validates selected features on the fly without schema locks, temporary feature classes, or database modifications.
* **🎯 Display Cache Data Source & Manual Execution:** Ultra-fast screen-rendered geometry cache for selected polygon features visible in the current viewport. Validation runs on-demand via the **Run QC** button.
* **🛡️ Max Features Safety Cap:** Configurable protection threshold (default: 500 features) preventing browser freezing or memory exhaustion on accidental massive selections.
* **💾 Per-Project Target Layer Persistence:** Remembers chosen target layers (including "All Polygon Layers") per ArcGIS Pro project, restoring them automatically upon reopening.
* **✅ Clear "No errors found ✓" Status:** Distinguishes verified zero-error runs with clear feedback while keeping analyzed feature counts and execution times visible.
* **🛡️ Curve-Aware Analysis & Junction Guard:** Validates parametric curves (Circular Arcs & Cubic Béziers) and protects boundary nodes where neighbor parcels split or turn. Includes an optional warning sub-check for near-tangent arc–line collinear junctions.
* **📐 Sub-Millimeter Measurement Precision:** Formats distances below 1 cm in millimeters (`mm`) down to sub-millimeter fractions (e.g., `0.35 mm`), preventing misleading `0.00 cm` labels.
* **💾 Direct Geodatabase Export:** Exports detected errors with one click into a dedicated `QC_Errors` Feature Dataset in the project's Default Geodatabase.

For a comprehensive mathematical and geometric treatise on all algorithms, consult [QC_CHECKS_LOGIC.md](QC_CHECKS_LOGIC.md).

---

## 2. Installation & Setup

### System Requirements:
* **Operating System:** Windows 10 / 11 (64-bit)
* **GIS Platform:** ArcGIS Pro 3.3.x, 3.4.x, or later (.NET 8 runtime environment)
* **Runtime:** .NET 8.0 Windows Desktop Runtime

> [!IMPORTANT]
> **Always close ArcGIS Pro before installing or updating add-ins.** ArcGIS Pro registers add-ins into memory strictly during application startup and does not support hot-reloading.

### Method 1: Installing the Pre-built Package
1. Ensure **ArcGIS Pro** is completely closed.
2. Locate the packaged add-in in the project root:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. Double-click the `.esriAddinX` file.
4. In the Esri Add-In Installation Utility dialog, click **Install Add-In**.
5. Launch ArcGIS Pro. The tools will appear in the dedicated **Geometry QC** Ribbon tab and under the **Add-In** tab.

### Method 2: Building from Source
1. Close **ArcGIS Pro**.
2. Run the .NET build command from the project root:
   ```powershell
   dotnet build -c Release
   ```
3. The build automatically compiles and packages `GeometryQCAddIn.esriAddinX` into the project root directory.
4. Double-click the generated package to install, or deploy it directly to:
   `%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\`
5. Launch ArcGIS Pro.

---

## 3. User Interface Overview

### A. Geometry QC Ribbon Tab

Located in the top ArcGIS Pro Ribbon bar:

| Tool / Button | Icon Asset | Description & Function |
| :--- | :---: | :--- |
| **Enable QC** | `QC_Toggle_ON32.png` | Master toggle to enable or disable the QC validation suite. |
| **Run QC** | `QC_Run32.png` | Executes quality validation on the active selection or map extent. |
| **Clear Results** | `QC_Clear16.png` | Clears all recorded issues and clears graphic overlays from the map view. |
| **Cancel** | `QC_Cancel16.png` | Immediately and safely halts an ongoing validation task. |
| **Results** | `QC_Results32.png` | Displays or activates the side **Results DockPane**. |
| **Settings** | `QC_Settings32.png` | Displays the **Settings DockPane** to customize thresholds and execution modes. |

---

### B. Results DockPane

Docked on the right side of the ArcGIS Pro canvas:

1. **Header & Performance Metrics:** Displays processed feature count, elapsed validation time in seconds, and active layer name.
1. **Header & Performance Metrics:** Displays processed feature count, elapsed validation time in seconds, and categorized issue badge (e.g. `3 errors · 2 warnings` or `No errors found ✓`).
2. **Target Layer Selector:** Dropdown to select a specific layer or choose `All Polygon Layers`. The choice is persistently saved inside the current ArcGIS Pro project. Accompanied by a refresh button (`↻`) to update layer lists dynamically.
3. **Issue Categories Tree:** Hierarchical tree grouping errors with color-coded severity badges:
   - 🔴 **Errors:** Overlap, Invalid Geometry, Duplicate Geometry.
   - 🟠 **Warnings:** Enclosed Gap, Snap Issue, Missing Junction, Short Segment, Angle Issue, Arc–Line Junctions.
   - 🔵 **Info:** Redundant Vertex, Multi-Part Feature.
4. **Issue Details Panel:** Selecting any issue displays:
   - Primary Feature OID and source layer.
   - Related Feature OID(s) (e.g., overlapping neighbor polygon).
   - Precise measurement value (area in `m²`/`cm²`, distance in `mm`/`cm`, angle in `°`).
   - Detailed technical diagnosis.
   - **Zoom to Issue Button (`🔍 Zoom`):** Instantly pans and centers the active map view directly on the defect.
5. **Action Buttons:**
   - **Run QC (Cyan):** Run validation.
   - **Export to GDB (Emerald Green):** Export all detected issues into the project's Default Geodatabase.
   - **Clear Results (Navy / Slate):** Clear tree results and graphic overlays.

---

### C. Settings DockPane

Provides complete customization over tolerances and thresholds:

| Parameter | Internal Setting Key | Default Value | Description |
| :--- | :--- | :---: | :--- |
| **Max Features Cap** | `MaxFeaturesToProcess` | `500` | Safety limit on selected features processed per run. |
| **Overlap Tolerance** | `OverlapMinAreaSqMeters` | `0.00001 m²` | Minimum intersection area required to report an overlap. |
| **Gap Tolerance** | `GapMinAreaSqMeters` | `0.00001 m²` | Minimum enclosed void area required to flag a gap. |
| **Short Segment Tolerance** | `ShortSegmentThresholdCm` | `15.0 cm` | Segment length threshold below which edges are flagged. |
| **Angle Tolerance** | `AngleThresholdDegrees` | `30.0°` | Sharp angles narrower than this are flagged. |
| **Snap Tolerance** | `SnapToleranceCm` | `15.0 cm` | Vertex distance threshold for detecting unsnapped nodes. |
| **Redundant Vertex Angle** | `RedundantVertexAngleDegrees` | `175.0°` | Straight angle threshold for collinear vertices & curve tangents. |
| **Flag Arc–Line Junctions** | `FlagArcLineJunctions` | `false` | Optional warning check for near-tangent curve–line joins. |
| **Arc–Line Angle Tolerance**| `ArcLineAngleToleranceDegrees` | `175.0°` | Tangent angle threshold for arc–line collinear junctions. |
| **Show Warnings on Map** | `ShowWarningsOnMap` | `true` | Controls whether warning-level symbols are drawn on the map. |
| **Junction Tolerance** | `MissingJunctionToleranceCm` | `10.0 cm` | Distance threshold for detecting missing T-junction vertices. |
| **Individual Check Toggles** | `Check*` | `true` | Independent check-boxes to enable or disable each of the 10 checks. |
| **Restore Defaults** | — | — | One-click reset to factory standard thresholds. |

---

## 4. Target Layer Selection & Project Persistence

```text
+-------------------------------------------------------------+
| Layer: [ Parcels                                      ▼ ] [↻] |
+-------------------------------------------------------------+
```

### 4.1 Target Layer Selector & Per-Project Persistence
* **Single Layer vs. All Polygon Layers:** Restricts validation to a single layer (e.g., `Parcels`) to avoid cross-layer false positives, or selects `All Polygon Layers` to inspect multiple polygon layers simultaneously.
* **Project Persistence:** The selected target layer is saved directly within the ArcGIS Pro project (`.aprx`). When reopening the project, your layer choice is seamlessly restored. If the layer is no longer present, it gracefully falls back to `All Polygon Layers`.
* **Dynamic Refresh (`↻`):** Refreshes the layer list immediately when layers are added or renamed without resetting your active selection.

### 4.2 Data Acquisition & Manual Execution
* **Display Cache Data Source:** Geometries are extracted directly from the in-memory display cache of selected polygon features in the active map view, ensuring zero database locks and rapid evaluation.
* **Manual Execution:** Runs exclusively on demand via the **Run QC** button.
* **Selection Cap Guard:** If the number of selected features exceeds the `MaxFeaturesToProcess` setting (default 500), execution stops safely with an informative prompt.

---

## 5. The 10 QC Checks in Detail

| # | Check Name | Internal Code | Default Tolerance | Severity | Description & Detection Scope |
| :-: | :--- | :--- | :---: | :---: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | 🔴 Error | Polygon / Point: Detects non-simple geometries according to OGC/Esri specifications (self-intersections, bow-ties, inverted rings, rings with < 3 points, NaN coordinates). |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.00001 m²` | 🔴 Error | Polygon: Detects overlapping regions between polygon pairs. Decomposes multipart intersections into separate polygons, calculates area accurately in `m²` or `cm²`, and excludes boundary sliver noise. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | 🔴 Error | Polygon: Detects 100% coincident identical polygons stacked on top of each other, suppressing duplicate overlap reporting. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.00001 m²` | 🟠 Warning | Polygon: Detects enclosed, hidden air gaps and sliver holes bounded entirely by adjacent polygons. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | 🔵 Info | Polygon: Identifies single records containing multiple disconnected polygon rings (`PartCount > 1`) in singlepart workflows. |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `15.0 cm` | 🟠 Warning | Polyline: Detects micro-edges and tiny segments shorter than tolerance, excluding coincident vertices (`d < 1e-6`). |
| **7** | **Angle Issue** | `CHK_ANGLE` | `30.0°` | 🟠 Warning | Point: Detects acute angles and needle spikes resulting from accidental clicks or digitizing errors. |
| **8** | **Snap Issue** | `CHK_SNAP` | `15.0 cm` | 🟠 Warning | Point: Detects unsnapped near-coincident vertices within tolerance. Distances under 1 cm are formatted in millimeters (`mm`) down to sub-millimeter precision (`0.35 mm`). Truly coincident vertices are excluded. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `175.0°` | 🔵 Info | Point: Detects superfluous collinear vertices along straight lines and redundant intermediate vertices between continuous curves (Curve → Vertex → Curve). Analyzes curve properties (center, radius, curvature, tangent direction); preserves valid curve/line transitions and compound curve changes. Features **Junction Guard**: protected if adjacent feature forms a legitimate boundary corner or junction. Optional sub-feature: flags near-tangent arc–line junctions as warnings (`CHK_REDUNDANT_ARCLINE`). |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `10.0 cm` | 🟠 Warning | Point: Flags T-junction contact points where a polygon vertex touches a neighboring edge without a matching snapped node (cadastral standard). |
| **11**| **Flat / Pseudo-Arc** | `CHK_FLAT_PSEUDO_ARC` | `0.01 cm` / `10.0 cm²` | 🟠 Warning | Polyline: Detects true circular arcs with negligible sagitta ($\le 0.01\text{ cm} / 0.1\text{ mm}$) or enclosed segment area ($\le 10.0\text{ cm}^2$) that functionally represent straight lines (Disabled by default). |

---

## 6. Deduplication, Junction Guard & Sub-Millimeter Engine

* **Spatial Deduplication:** Enforces ordered pairing `(min(OID_A, OID_B), max(OID_A, OID_B))` to guarantee that overlaps between pairs are reported exactly once.
* **Sub-Millimeter Precision:** Measures and displays distances below 1 cm in millimeters (`mm`) with 2 to 4 decimal places (e.g., `0.35 mm` or `1.20 mm`), eliminating the confusing `0.00 cm` rounding artifacts.
* **Junction Guard:** Eliminates false alarms on shared boundaries by checking vertex angles and curve continuity on neighboring polygon boundaries. A vertex is only flagged if redundant on both sides or if no neighbor corner exists.
* **Parametric Curve Continuity:** Validates Circular Arcs and Cubic Béziers using tangent alignment ($C^1$) and curvature consistency ($C^2$).

---

## 7. Graphic Symbols Legend on the Map

Upon running validation, temporary graphics are rendered on the active map view:

| Error Type | Geometry Type | Color Name | Hex Code | Visual Style |
| :--- | :---: | :--- | :---: | :--- |
| **Overlap** | Polygon | Semi-Transparent Red | `#CCFF0000` | Solid red fill with 80% opacity |
| **Invalid Geometry** | Polygon / Point | Magenta / Fuchsia | `#CCFF00FF` | Bright magenta fill / outline |
| **Duplicate Geometry** | Polygon | Amber / Orange | `#CCFF8C00` | Orange hatched fill |
| **Enclosed Gap** | Polygon | Bright Yellow | `#CCFFD700` | Yellow fill with solid outline |
| **Multi-Part Feature** | Polygon | Purple | `#CC8A2BE2` | Purple hatched fill |
| **Short Segment** | Polyline | Deep Sky Blue | `#FF00BFFF` | Bold 3px cyan/blue line |
| **Angle Issue** | Point | Orange Red | `#FFFF4500` | Star marker (size 12) |
| **Snap Issue** | Point | Neon Lime | `#FF32CD32` | Circle marker with inner crosshair |
| **Redundant Vertex** | Point | Slate Gray | `#FF708090` | Square marker (size 8) |
| **Possible Redundant (Arc–Line)** | Point | Coral / Tangerine | `#FFFF7F50` | Triangle marker (size 9) |
| **Missing Junction** | Point | Magenta | `#FFFF00FF` | X / Star marker (size 11) |
| **Flat Pseudo-Arc** | Polyline | Turquoise / Teal | `#FF00CED1` | Dashed turquoise line (size 3) |

---

## 8. Exporting Errors to Geodatabase (GDB)

The Add-in includes a native **Export to GDB** service (`GdbExportService.cs`) that converts in-memory QC issues into persistent Geodatabase feature classes:

### Export Process:
1. Click **Export to GDB** in the Results DockPane.
2. The service connects to the project's **Default Geodatabase** (`Project.Current.DefaultGeodatabasePath`).
3. Creates a dedicated Feature Dataset named **`QC_Errors`** (or increments to `QC_Errors_1`, `QC_Errors_2`, etc., if previously exported).
4. Groups errors by geometry type into separate Feature Classes:
   - `QC_Overlap_Polygons`
   - `QC_InvalidGeom_Polygons`
   - `QC_Gap_Polygons`
   - `QC_ShortSegment_Lines`
   - `QC_SnapIssue_Points`
   - `QC_AngleIssue_Points`
   - `QC_Junction_Points`
   - `QC_RedundantVertex_Points`
5. Populates complete attribute metadata using an optimized `InsertCursor`:

| Attribute Field | Type | Description |
| :--- | :---: | :--- |
| `Issue_Type` | String (100) | Human-readable check name (e.g., `Overlap`, `Snap Issue`). |
| `Check_ID` | String (50) | Internal check code (e.g., `CHK_OVERLAP`). |
| `Source_OID` | Integer | Object ID of the primary feature having the error. |
| `Related_OIDs` | String (255) | Object ID(s) of any related features (e.g., overlapping neighbor). |
| `Layer_Name` | String (100) | Name of the source map layer. |
| `Severity` | String (20) | Issue severity level (`Error`, `Warning`, `Info`). |
| `Metric_Val` | Double | Numerical measurement value (area in m², distance in meters, angle in degrees). |
| `Metric_Unit` | String (20) | Measurement unit (`m²`, `cm²`, `mm`, `cm`, `deg`). |
| `Description` | String (500) | Complete diagnostic description. |
| `Export_Time` | String (50) | Timestamp of the export operation. |

6. Adds all exported feature classes to the active map inside an organized Group Layer with matching color symbology.

---

## 9. Recommended Workflow

```text
[Select Features] ➔ [Select Layer in DockPane] ➔ [Click Run QC]
         │
         ▼
[Review Tree View & Zoom to Errors]
         │
         ├───► [Export to GDB for Official Auditing]
         │
         ├───► [Fix Errors using ArcGIS Pro Edit Tools]
         │
         ▼
[Clear Results & Re-run QC to Verify Fixes]
```

1. **Select Data Source Mode:** Choose among Display Cache, Live Query, Real Geometry, or Entire Layer in Settings.
2. **Select Features:** Select polygons on the map (or select a local layer in Entire Layer mode).
3. **Configure Layer:** Choose the target layer in the Results DockPane dropdown.
4. **Execute:** Click **Run QC** (Cyan button).
5. **Inspect & Zoom:** Expand categories in the tree view and click **Zoom** to navigate directly to each defect.
6. **Audit / Archive:** Click **Export to GDB** to generate official geodatabase layers.
7. **Remediate:** Use ArcGIS Pro editing tools (Reshape, Align Features, Split, Snap) to resolve the issues.
8. **Verify:** Click **Clear Results** and re-run QC to confirm 100% clean geometry.

---

## 10. Frequently Asked Questions (FAQ)

#### Q1: Why doesn't the Add-in require building a Geodatabase Topology?
**A:** Traditional ArcGIS topologies require feature datasets, rule definitions, and schema locks. Geometry QC Analyzer builds an ephemeral, in-memory spatial index and calculates OGC/Esri geometries on the fly, saving hours of geodatabase configuration.

#### Q2: Can I run this on Web Feature Services from ArcGIS Online or Enterprise?
**A:** Yes! In `DisplayCache`, `LiveQuery`, and `RealGeometry` modes, the Add-in works directly on polygon layers in your map, including Feature Services and shapefiles, in 100% read-only mode.

#### Q3: Why did the Add-in not appear after building or installing?
**A:** ArcGIS Pro only discovers and loads add-ins at application startup. If Pro was running during installation, you must **close and restart ArcGIS Pro**.

---
---

<a name="دليل-المستخدم-باللغة-العربية"></a>
# دليل المستخدم باللغة العربية (Arabic User Guide)

## فهرس المحتويات
1. [نظرة عامة على الأداة](#1-نظرة-عامة-على-الأداة-ar)
2. [التثبيت والتشغيل السريع](#2-التثبيت-والتشغيل-السريع-ar)
3. [واجهة المستخدم وعناصر التحكم](#3-واجهة-المستخدم-وعناصر-التحكم-ar)
   - [تبويب شريط الأدوات (Ribbon Tab)](#أ-تبويب-شريط-الأدوات-geometry-qc-ribbon)
   - [لوحة النتائج (Results DockPane)](#ب-لوحة-النتائج-results-dockpane)
   - [لوحة الإعدادات (Settings DockPane)](#ج-لوحة-الإعدادات-settings-dockpane)
4. [ميزة اختيار الطبقة المستهدفة وأنماط جلب البيانات](#4-ميزة-اختيار-الطبقة-المستهدفة-وأنماط-جلب-البيانات-ar)
5. [الفحوصات الهندسية العشرة بالتفصيل](#5-الفحوصات-الهندسية-العشرة-بالتفصيل-ar)
6. [محرك الفحص، حارس الربط ودقة القياس تحت الملليمتر](#6-محرك-الفحص-حارس-الربط-ودقة-القياس-تحت-الملليمتر-ar)
7. [دليل الرموز والألوان التوضيحية على الخريطة](#7-دليل-الرموز-والألوان-التوضيحية-على-الخريطة-ar)
8. [تصدير الأخطاء لقاعدة البيانات الجغرافية (Export to GDB)](#8-تصدير-الأخطاء-لقاعدة-البيانات-الجغرافية-ar)
9. [دورة العمل النموذجية الموصى بها](#9-دورة-العمل-النموذجية-الموصى-بها-ar)
10. [الأسئلة الشائعة وحلول المشاكل (FAQ)](#10-الأسئلة-الشائعة-وحلول-المشاكل-ar)

---

<a name="1-نظرة-عامة-على-الأداة-ar"></a>
## 1. نظرة عامة على الأداة

أداة **Geometry QC Analyzer** هي إضافة برمجية أصلية متكاملة لبرنامج **ArcGIS Pro** مخصصة لمهندسي المساحة ونظم المعلومات الجغرافية والمخططات العقارية. تهدف الأداة إلى فحص الجودة الهندسية والطوبولوجية والكارتوجرافية لطبقات المضلعات بدقة متناهية وسرعة فائقة دون تعديل البيانات الأصلية أو اشتراط إنشاء قواعد طوبولوجيا معقدة في قاعدة البيانات.

### أبرز المزايا:
* **⚡ فحص لحظي في الذاكرة بالكامل (100% In-Memory):** فحص فوري للمعالم المحددة في وضع القراءة فقط، دون إنشاء ملفات مؤقتة على القرص أو حجز أقفال (Locks) على قواعد البيانات.
* **🎯 مصدر بيانات كاش العرض وتشغيل يدوي:** اعتماد كاش العرض (Display Cache) مصدراً وحيداً للبيانات للمعالم المحددة داخل نطاق الشاشة، مع تشغيل يدوي عبر زر **Run QC**.
* **🛡️ سقف أمان لعدد المعالم المحددة (Max Features Cap):** حماية أداء التطبيق بمنع فحص أكثر من 500 معلم محدد كحد افتراضي لحماية الذاكرة ومنع التجميد.
* **💾 حفظ اختيار الطبقة لكل مشروع (Per-Project Layer Persistence):** حفظ الطبقة المستهدفة (بما في ذلك "All Polygon Layers") داخل ملف مشروع ArcGIS Pro واستعادتها تلقائياً عند فتح المشروع.
* **✅ إشعار واضح لعدم وجود أخطاء ("No errors found ✓"):** تمييز حالات خلو المضلعات من الأخطاء مع بقاء عدد المعالم وزمن التحليل معروضاً بوضوح.
* **🛡️ فحص ذكي للمنحنيات وحارس نقاط الربط (Junction Guard):** تحليل متقدم للأقواس الدائرية ومنحنيات بيزيير مع حماية النقاط المشتركة، مع خيار تدقيق تحذيري إضافي لنقاط التماس بين المنحنيات والخطوط (Arc–Line).
* **📐 دقة فائقة تحت الملليمتر:** قياس المسافات الأقل من 1 سم بوحدة الملليمتر (`mm`) حتى 3 و4 خانات عشرية (مثل `0.35 mm`) لمنع الرسائل المضللة `0.00 cm`.
* **💾 تصدير مباشر لقاعدة البيانات:** تصدير كافة الأخطاء بضغطة زر إلى Feature Dataset مخصص باسم `QC_Errors` في الـ Default Geodatabase للمشروع.

للاطلاع على الشرح الرياضي والخوارزميات الهندسية بالتفصيل، راجع ملف: [QC_CHECKS_LOGIC.md](QC_CHECKS_LOGIC.md).

---

<a name="2-التثبيت-والتشغيل-السريع-ar"></a>
## 2. التثبيت والتشغيل السريع

### متطلبات التشغيل:
* نظام التشغيل: **Windows 10 / 11 (64-bit)**
* برنامج نظم المعلومات: **ArcGIS Pro 3.3.x أو 3.4.x أو أحدث** (بيئة .NET 8)
* بيئة التشغيل: **.NET 8.0 Windows Desktop Runtime**

> [!IMPORTANT]
> **يجب إغلاق برنامج ArcGIS Pro بالكامل قبل البدء في التثبيت أو التحديث.** حيث يقرأ ArcGIS Pro مجلد الإضافات البرمجية ويحمّلها في الذاكرة فقط عند بداية إقلاع البرنامج، ولا يدعم التحديث المباشر أثناء عمله.

### الطريقة 1: التثبيت اليدوي عبر الحزمة الجاهزة
1. تأكد من إغلاق **ArcGIS Pro**.
2. توجّه إلى ملف الحزمة في المجلد الرئيسي للمشروع:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. انقر نقراً مزدوجاً (Double-click) على الملف.
4. في نافذة معالج التثبيت من Esri، انقر على **Install Add-In**.
5. شغل ArcGIS Pro، وستجد تبويب **Geometry QC** في الشريط العلوي (Ribbon)، وأيضاً داخل تبويب **Add-In**.

### الطريقة 2: البناء من الكود المصدري
1. تأكد من إغلاق **ArcGIS Pro**.
2. قم بالبناء عبر سطر الأوامر:
   ```powershell
   dotnet build -c Release
   ```
3. يقوم الـ Build تلقائياً بإنشاء حزمة `GeometryQCAddIn.esriAddinX` في المجلد الرئيسي للمشروع.
4. انقر نقراً مزدوجاً على الحزمة لتثبيتها أو انقلها لمجلد الإضافات المفضل لديك.
5. افتح برنامج ArcGIS Pro.

---

<a name="3-واجهة-المستخدم-وعناصر-التحكم-ar"></a>
## 3. واجهة المستخدم وعناصر التحكم

### أ. تبويب شريط الأدوات (Geometry QC Ribbon)

| الأداة / الزر | الأيقونة | الوصف والوظيفة |
| :--- | :---: | :--- |
| **Enable QC** | `QC_Toggle_ON32.png` | المفتاح الرئيسي لتفعيل أو تعطيل أدوات فحص الجودة. |
| **Run QC** | `QC_Run32.png` | بدء تنفيذ الفحص على المعالم المحددة أو المعالم الظاهرة في الخريطة. |
| **Clear Results** | `QC_Clear16.png` | مسح نتائج الفحص وإزالة الرسومات التوضيحية المؤقتة من الخريطة. |
| **Cancel** | `QC_Cancel16.png` | إيقاف عملية الفحص الجارية فوراً وبأمان. |
| **Results** | `QC_Results32.png` | إظهار أو تنشيط **لوحة النتائج الجانبية (Results DockPane)**. |
| **Settings** | `QC_Settings32.png` | إظهار **لوحة الإعدادات (Settings DockPane)** لتعديل التفاوتات والحساسية. |

---

### ب. لوحة النتائج (Results DockPane)

تقع على يمين شاشة ArcGIS Pro وتتضمن:
1. **الترويسة وشارة المشاكل المصنفة:** تعرض عدد المعالم المفحوصة والزمن المستغرق وشارة تصنيف العيوب (مثل `3 errors · 2 warnings` أو `No errors found ✓`).
2. **محدد الطبقة المستهدفة (Target Layer Selector):** لاختيار طبقة محددة للفحص (مثل `Parcels`) أو فحص جميع الطبقات (`All Polygon Layers`). يُحفظ الاختيار تلقائياً داخل المشروع، وبجانبه زر تحديث (`↻`) لتحديث القائمة دون فقدان الاختيار الحالي.
3. **شجرة تصنيف الأخطاء (Issue Categories Tree):** تجميع هرمي للأخطاء حسب نوع الفحص مع شارات ملونة لدرجة الأهمية:
   - 🔴 **أخطاء (Errors):** Overlap, Invalid Geometry, Duplicate.
   - 🟠 **تحذيرات (Warnings):** Enclosed Gap, Snap Issue, Missing Junction, Short Segment, Angle Issue, Arc–Line Junctions.
   - 🔵 **معلومات (Info):** Redundant Vertex, Multi-Part Feature.
4. **لوحة تفاصيل الخطأ المختار:** عند النقر على أي خطأ في الشجرة تظهر بياناته التفصيلية مع زر **Zoom** للتكبير الفوري على موقع الخطأ.
5. **أزرار الإجراءات:**
   - **Run QC (سماوي / تركواز):** تشغيل الفحص فوراً.
   - **Export to GDB (أخضر زمردي):** تصدير كافة الأخطاء المكتشفة إلى Geodatabase المشروع.
   - **Clear Results (كحلي / رمادي):** مسح نتائج الفحص وإزالة الأشكال التوضيحية من الخريطة.

---

### ج. لوحة الإعدادات (Settings DockPane)

تتيح التحكم الكامل في حساسية وتفاوتات كافة الفحوصات:

| معامل الإعداد | الكود البرمجي | القيمة الافتراضية | الوصف التفصيلي |
| :--- | :--- | :---: | :--- |
| **سقف المعالم المحددة** | `MaxFeaturesToProcess` | `500` | أقصى عدد من المعالم المحددة المسموح بمعالجتها في كل تشغيل. |
| **تفاوت التداخل** | `OverlapMinAreaSqMeters` | `0.00001 m²` | أقل مساحة تقاطع يُعتبر عندها التداخل خطأً مساحياً. |
| **تفاوت الفجوات** | `GapMinAreaSqMeters` | `0.00001 m²` | أقل مساحة للفراغ الهوائي المحصور ليُعتبر فجوة غير قانونية. |
| **تفاوت الأضلاع القصيرة** | `ShortSegmentThresholdCm` | `15.0 cm` | الحد الأدنى لطول الضلع الذي يُصنف ما دونه كضلع قصير جداً. |
| **تفاوت الزوايا الحادة** | `AngleThresholdDegrees` | `30.0°` | الزوايا الأضيق من هذه القيمة تُصنف كزوايا شاذة. |
| **تفاوت عدم الالتقاط** | `SnapToleranceCm` | `15.0 cm` | أقصى مسافة فاصلة بين رأسين متقاربين للتحقق من عدم التقاطهما. |
| **زاوية الرأس الزائد** | `RedundantVertexAngleDegrees` | `175.0°` | زاوية الاستقامة للخطوط وتطابق المماسات للمنحنيات المتصلة. |
| **فحص تماس الأقواس والخطوط** | `FlagArcLineJunctions` | `false` | خيار تحذيري إضافي لنقاط التقاء المنحنيات والخطوط على خط مماس شبه مستقيم. |
| **زاوية تماس القوس والخط** | `ArcLineAngleToleranceDegrees` | `175.0°` | زاوية التماس المعتمدة لاعتبار نقطة الالتقاء كرأس زائد محتمل. |
| **إظهار التحذيرات على الخريطة** | `ShowWarningsOnMap` | `true` | التحكم في رسم رموز المشاكل المصنفة كتحذير فوق الخريطة. |
| **تفاوت العقد المفقودة** | `MissingJunctionToleranceCm` | `10.0 cm` | أقصى مسافة فاصلة بين نقطة مضلع وضلع مضلع مجاور للتحقق من وجود عقدة. |
| **مفاتيح تفعيل الفحوصات** | `Check*` | `true` | مربعات اختيار لتشغيل أو إيقاف أي فحص بشكل مستقل. |
| **استعادة الافتراضيات** | — | — | إعادة ضبط كافة القيم للمعايير الافتراضية المعتمدة. |

---

<a name="4-ميزة-اختيار-الطبقة-المستهدفة-وأنماط-جلب-البيانات-ar"></a>
## 4. اختيار الطبقة المستهدفة والحفظ لكل مشروع

```text
+-------------------------------------------------------------+
| Layer: [ Parcels                                      ▼ ] [↻] |
+-------------------------------------------------------------+
```

### 4.1 اختيار الطبقة المستهدفة والحفظ التلقائي لكل مشروع
* تتيح القائمة المنسدلة اختيار طبقة مضلعات محددة (مثل `Parcels`) أو اختيار **`All Polygon Layers`** لفحص جميع طبقات المضلعات المحددة معاً.
* **الحفظ التلقائي في المشروع:** يتم تخزين الطبقة المختارة داخل ملف مشروع ArcGIS Pro (`.aprx`) واستعادتها بسلاسة عند إعادة فتح المشروع دون أي رسائل تحذيرية مزعجة.
* **زر التحديث (`↻`):** لتحديث قائمة الطبقات فوراً دون التأثير على الاختيار الحالي.

### 4.2 آلية جلب البيانات والتشغيل اليدوي
* **مصدر بيانات كاش العرض (Display Cache):** يتم استخراج الأشكال الهندسية مباشرة من كاش العرض للمعالم المحددة في نطاق الخريطة النشط لضمان أقصى سرعة واستجابة.
* **التشغيل اليدوي:** الفحص يتم يدوياً عند الضغط على زر **Run QC**.
* **حماية سقف المعالم:** إذا تجاوز عدد المعالم المحددة سقف الأمان (500 معلم افتراضياً)، يتوقف الفحص بأمان مع توجيه المستخدم لتقليل التحديد أو رفع السقف من الإعدادات.

---

<a name="5-الفحوصات-الهندسية-العشرة-بالتفصيل-ar"></a>
## 5. الفحوصات الهندسية العشرة بالتفصيل

| # | اسم الفحص (Check Name) | الكود الداخلي | التفاوت الافتراضي | مستوى الأهمية | الوصف الهندسي وطبيعة الخطأ |
| :-: | :--- | :--- | :---: | :---: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | 🔴 خطأ حرج | مضلع / نقطة: فحص صحة المضلع طوبولوجياً طبقاً لمواصفات OGC/Esri. يكتشف التقاطعات الذاتية (Bow-tie)، الحلقات المعكوسة، الحلقات التي تحتوي على أقل من 3 نقاط، والإحداثيات غير المعرفة (NaN). |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.00001 m²` (100 cm²) | 🔴 خطأ حرج | مضلع: يكتشف المساحات المشتركة المتداخلة بين المضلعات. يقوم بتفكيك التداخلات المتعددة وعرض مساحتها بوحدات واضحة (`m²` أو `cm²`) مع استبعاد شوائب تلاصق الحدود بفضل حارس السمك الأدنى. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | 🟠 تحذير | مضلع: يكتشف المضلعات المتطابقة هندسياً بنسبة 100% المرسومة فوق بعضها بالخطأ (Coincident Polygons)، ويمنع تكرار تقريرها كـ Overlap. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.00001 m²` (100 cm²) | 🟠 تحذير | مضلع: يكتشف الفجوات والثقوب الهوائية غير المرئية المحصورة والمغلقة تماماً بين المضلعات المتجاورة الناتجة عن أخطاء الرسم. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | 🔵 معلومة | مضلع: يكتشف المعالم ذات السجل الواحد التي تتكون من أكثر من مضلع منفصل (`PartCount > 1`) في قواعد البيانات التي تتطلب مضلعات أحادية (Singlepart). |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `15.0 cm` | 🟠 تحذير | خط: يكتشف الأضلاع أو الأجزاء متناهية الصغر التي يقل طولها عن الحد المسموح، مع استبعاد النقاط المتطابقة لحصر الأخطاء في عيوب الرسم الفعلية. |
| **7** | **Angle Issue** | `CHK_ANGLE` | `30.0°` | 🟠 تحذير | نقطة: يكتشف الزوايا الحادة والشاذة جداً (Spikes) التي تقل عن الدرجة المحددة والناتجة عادةً عن نقرات خاطئة أثناء الرسم بالماوس. |
| **8** | **Snap Issue** | `CHK_SNAP` | `15.0 cm` | 🟠 تحذير | نقطة: يكتشف الرؤوس والنقاط المتقاربة غير الملتقطة. تُعرض المسافات تحت السنتيمتر بالملليمتر (`mm`) بدقة فائقة لمنع تقريب المسافات غير الملتقطة إلى `0.00 cm` مع استبعاد النقاط المتطابقة تماماً. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` / `CHK_REDUNDANT_ARCLINE` | `175.0°` (أقواس: 175.0° اختياري) | 🔵 معلومة / 🟠 تحذير | نقطة: يكتشف النقاط الزائدة على استقامة الخط (≥ 175°) والرؤوس غير الضرورية الواقعة بين منحنيين متصلين، مع فحص اختياري للرؤوس الزائدة عند التقاء القوس والخط المماس (Arc–Line Junction) كمستوى تحذير برمز مثلث مرجاني، مع حماية نقاط الربط (Junction Guard) وخيار لإظهار/إخفاء التحذيرات على الخريطة. |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `10.0 cm` | 🟠 تحذير | نقطة: يكتشف العقد المفقودة في الوصلات التبادلية (T-Junctions) عندما تلامس نقطة من مضلع ضلع مضلع مجاور دون وجود رأس مشترك ملتقط عليه (المعيار المعتمد في المخططات العقارية). |
| **11**| **Flat / Pseudo-Arc** | `CHK_FLAT_PSEUDO_ARC` | `0.01 cm` / `10.0 cm²` | 🟠 تحذير | خط / قوس: كشف الأقواس الدائرية الوهمية والمنبسطة ذات سهم القوس الضئيل جداً ($\le 0.01\text{ cm} / 0.1\text{ mm}$) أو المساحة المحصورة ($\le 10.0\text{ cm}^2$) التي تمثل خطوطاً مستقيمة عملياً (معطل افتراضياً). |

---

<a name="6-محرك-الفحص-حارس-الربط-ودقة-القياس-تحت-الملليمتر-ar"></a>
## 6. محرك الفحص، حارس الربط ودقة القياس تحت الملليمتر

* **منع التكرار (Deduplication):** منع تسجيل نفس عيب التداخل مرتين للزوج (أ، ب) و(ب، أ).
* **دقة القياس تحت الملليمتر (Sub-Millimeter Precision):** قياس وعرض المسافات التي تقل عن 1 سنتيمتر بوحدة الملليمتر (`mm`) مع 2 إلى 4 خانات عشرية (مثل `0.35 mm` أو `1.20 mm`) لمنع الخداع البصري برسائل التقريب `0.00 cm`.
* **حارس نقاط الربط (Junction Guard):** تفادي حذف الرؤوس المشتركة بين المضلعات المتجاورة عبر التحقق من زوايا الحدود واستمرارية المنحنيات للمضلع المجاور قبل تصنيف النقطة كـ Redundant Vertex.
* **فحص المنحنيات البارامترية:** التحقق من اتساق الأقواس الدائرية ومنحنيات بيزيير عبر استمرارية المماس ($C^1$) وتناسق الانحناء ($C^2$).

---

<a name="7-دليل-الرموز-والألوان-التوضيحية-على-الخريطة-ar"></a>
## 7. دليل الرموز والألوان التوضيحية على الخريطة

فور انتهاء الفحص، يتم رسم أشكال جرافيكية مؤقتة ملونة على الخريطة:

| نوع الخطأ (Issue Type) | الشكل الهندسي | اللون المعتمد | كود اللون (Hex) | الوصف والشكل البصري |
| :--- | :---: | :--- | :---: | :--- |
| **Overlap** | مضلع | أحمر نصف شفاف | `#CCFF0000` | تعبئة حمراء بدرجة شفافية 80% |
| **Invalid Geometry** | مضلع / نقطة | فوشيا / ماجنتا | `#CCFF00FF` | حدود وتعبئة فوشيا ساطعة |
| **Duplicate Geometry** | مضلع | برتقالي عنبري | `#CCFF8C00` | مضلع مهشر باللون البرتقالي |
| **Enclosed Gap** | مضلع | أصفر ذهبي | `#CCFFD700` | تعبئة صفراء بحدود مصمتة |
| **Multi-Part Feature** | مضلع | بنفسجي | `#CC8A2BE2` | تعبئة بنفسجية مهشرة |
| **Short Segment** | خط | أزرق سماوي | `#FF00BFFF` | خط عريض بسمك 3 بكسل |
| **Angle Issue** | نقطة | برتقالي ناري | `#FFFF4500` | علامة نجمة بحجم 12 |
| **Snap Issue** | نقطة | أخضر ليموني | `#FF32CD32` | علامة دائرية مع تقاطع داخلي |
| **Redundant Vertex** | نقطة | رمادي رصاصي | `#FF708090` | علامة مربعة بحجم 8 |
| **Redundant Arc–Line Junction** | نقطة | مرجاني (Coral) | `#FFFF7F50` | علامة مثلث متساوي الأضلاع بحجم 9 (تحذير) |
| **Missing Junction** | نقطة | مرجاني (Coral) | `#FFFF7F50` | علامة معينة بحجم 10 |
| **Flat Pseudo-Arc** | خط | فيروزي (Turquoise) | `#FF00CED1` | خط متقطع بلون فيروزي بحجم 3 |

---

<a name="8-تصدير-الأخطاء-لقاعدة-البيانات-الجغرافية-ar"></a>
## 8. تصدير الأخطاء لقاعدة البيانات الجغرافية (Export to GDB)

تتضمن الإضافة خدمة متكاملة لتصدير الأخطاء (`GdbExportService.cs`) لتحويل نتائج الفحص اللحظية إلى طبقات جغرافية دائمة داخل قاعدة بيانات المشروع:

### آلية العمل:
1. انقر على زر **Export to GDB** في لوحة النتائج.
2. تتصل الخدمة تلقائياً بقاعدة البيانات الافتراضية للمشروع (`Default Geodatabase`).
3. تنشئ Feature Dataset مخصصاً باسم **`QC_Errors`** (أو تزيد العداد تلقائياً `QC_Errors_1`, `QC_Errors_2`... إلخ لحفظ التاريخ).
4. تقسم الأخطاء بحسب نوع الخطأ وشكله الهندسي إلى Feature Classes منفصلة:
   - `QC_Overlap_Polygons`
   - `QC_InvalidGeom_Polygons`
   - `QC_Gap_Polygons`
   - `QC_ShortSegment_Lines`
   - `QC_SnapIssue_Points`
   - `QC_AngleIssue_Points`
   - `QC_Junction_Points`
   - `QC_RedundantVertex_Points`
5. تملأ الحقول الوصفية بدقة عبر `InsertCursor`:

| اسم الحقل (Field Name) | النوع | الوصف والبيانات المخزنة |
| :--- | :---: | :--- |
| `Issue_Type` | String (100) | اسم نوع الخطأ (مثل `Overlap`, `Snap Issue`). |
| `Check_ID` | String (50) | الكود البرمجي للفحص (مثل `CHK_OVERLAP`). |
| `Source_OID` | Integer | رقم المعلم الأصلي المتسبب في الخطأ. |
| `Related_OIDs` | String (255) | أرقام المعالم المشتركة في الخطأ (مثل المضلع المتداخل المجاور). |
| `Layer_Name` | String (100) | اسم الطبقة الأصلية في الخريطة. |
| `Severity` | String (20) | مستوى الخطورة (`Error`, `Warning`, `Info`). |
| `Metric_Val` | Double | القيمة الرقمية المقاسة (المساحة بـ م²، المسافة بالأمتار، الزاوية بالدرجات). |
| `Metric_Unit` | String (20) | وحدة القياس (`m²`, `cm²`, `mm`, `cm`, `deg`). |
| `Description` | String (500) | الوصف التفصيلي والتشخيص الفني للخلل. |
| `Export_Time` | String (50) | التوقيت والتاريخ الدقيق لعملية التصدير. |

6. تضيف الطبقات المصدرة تلقائياً إلى الخريطة النشطة ضمن Group Layer منظم يحمل اسم الـ Feature Dataset، مع تطبيق نفس الألوان التوضيحية المعتمدة في الفحص.

---

<a name="9-دورة-العمل-النموذجية-الموصى-بها-ar"></a>
## 9. دورة العمل النموذجية الموصى بها

```text
[تحديد المعالم على الخريطة] ➔ [اختيار الطبقة من اللوحة] ➔ [النقر على Run QC]
         │
         ▼
[استعراض شجرة الأخطاء والتكبير Zoom على كل عيب]
         │
         ├───► [تصدير الأخطاء لقاعدة البيانات Export to GDB للتوثيق والاعتماد]
         │
         ├───► [معالجة الأخطاء بأدوات التعديل القياسية في ArcGIS Pro]
         │
         ▼
[مسح النتائج وإعادة الفحص للتحقق من سلامة التصحيح]
```

1. **تحديد النمط:** اختر **نمط جلب البيانات (Data Source Mode)** المناسب من لوحة الإعدادات (كاش العرض، استعلام مباشر، الأشكال الحقيقية، أو كامل الطبقة).
2. **التحديد:** استخدم أداة التحديد في ArcGIS Pro لاختيار المضلعات المراد فحصها (أو تجاوز هذه الخطوة في حال تفعيل نمط Entire Layer).
3. **التهيئة واختيار الطبقة:** افتح لوحة **Results** واختر الطبقة المستهدفة (إلزامية في نمط Entire Layer).
4. **التشغيل:** انقر على زر **Run QC** (زر التشغيل السماوي).
5. **التصفح والتكبير:** تصفح الأخطاء حسب التصنيف، وانقر على أي خطأ ثم زر **Zoom** للانتقال إلى موقعه.
6. **التوثيق:** انقر على **Export to GDB** لإنشاء طبقات رسمية للأخطاء.
7. **المعالجة:** استخدم أدوات التعديل القياسية في ArcGIS Pro (Reshape, Align, Split, Merge, Snapping) لإصلاح الخلل.
8. **إعادة التحقق:** انقر على **Clear Results** وأعد تشغيل الفحص للتأكد من زوال كافة الأخطاء.

---

<a name="10-الأسئلة-الشائعة-وحلول-المشاكل-ar"></a>
## 10. الأسئلة الشائعة وحلول المشاكل (FAQ)

#### س1: لماذا لا تتطلب الأداة بناء Geodatabase Topology؟
**ج:** بناء الـ Topology التقليدي يتطلب وضع البيانات في Feature Dataset محلي وتحديد قوانين معقدة وقفل الجداول. تعتمد أداة Geometry QC Analyzer على فهرسة مكانية لحظية بالذاكرة وتقوم بحساب العلاقات الهندسية مباشرة، مما يوفر ساعات طويلة من العمل الروتيني.

#### س2: هل يمكن استخدام الأداة مع طبقات الويب (Feature Services) من ArcGIS Online أو Portal؟
**ج:** نعم بكل تأكيد! في أنماط `DisplayCache`, `LiveQuery`, و`RealGeometry`، تقرأ الأداة المعالم مباشرة وتعمل في وضع القراءة فقط 100% دون الحاجة لتصدير الطبقات محلياً.

#### س3: قمت بتثبيت الإضافة أو عمل Build ولكن الشريط لم يظهر في ArcGIS Pro؟
**ج:** برنامج ArcGIS Pro لا يدعم التحديث الحي أثناء تشغيله (No Hot-Reloading). يجب **إغلاق برنامج ArcGIS Pro بالكامل ثم إعادة فتحه**، حيث يقرأ البرنامج مجلد الإضافات فقط لحظة إقلاعه (Startup).

---
*Developed with ❤️ for the GIS & Geospatial Community.*
