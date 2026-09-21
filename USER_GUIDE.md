# ArcGIS Pro Geometry QC Analyzer — User Guide
# دليل مستخدم أداة فحص الجودة الهندسية في ArcGIS Pro

<p align="center">
  <a href="#english-user-guide">English User Guide</a> •
  <a href="#دليل-المستخدم-باللغة-العربية">دليل المستخدم باللغة العربية</a>
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
4. [Target Layer Selection](#4-target-layer-selection)
5. [The 10 QC Checks in Detail](#5-the-10-qc-checks-in-detail)
6. [Deduplication & Verification Engine](#6-deduplication--verification-engine)
7. [Graphic Symbols Legend on the Map](#7-graphic-symbols-legend-on-the-map)
8. [Exporting Errors to Geodatabase (GDB)](#8-exporting-errors-to-geodatabase-gdb)
9. [Recommended Workflow](#9-recommended-workflow)
10. [Frequently Asked Questions (FAQ) & Troubleshooting](#10-frequently-asked-questions-faq--troubleshooting)

---

## 1. Overview

The **Geometry QC Analyzer** is an enterprise-grade Quality Control Add-In for **ArcGIS Pro 3.x**, engineered specifically for GIS professionals, cadastral surveyors, and urban planners. It provides instant, in-memory validation of polygon features without requiring enterprise geodatabase schemas, topology datasets, or write permissions.

### Key Architectural Highlights:
* **100% Read-Only Safety:** The Add-in never locks, modifies, or writes to your source feature classes or geodatabases during validation.
* **Sub-Second In-Memory Processing:** Utilizes custom 2D spatial grid indexing and vertex-level spatial hash indexing to evaluate thousands of polygons in fractions of a second.
* **Direct Web Feature Service Support:** Validate live web layers and Feature Services on the fly without having to export them to local geodatabases.
* **One-Click GDB Export:** Export all verified error geometries with rich attribution into the project's Default File Geodatabase.
* **Adaptive Light & Dark Theme UI:** Designed to seamlessly integrate with native ArcGIS Pro styling across themes (Navy / Cyan / Turquoise palette).
* **Interactive Visual Diagnostics:** Highlights each detected issue on the map with color-coded temporary graphics and provides instant zoom navigation.

---

## 2. Installation & Setup

### Prerequisites:
* Operating System: **Windows 10 / 11 (x64)**
* Host Application: **ArcGIS Pro 3.3.x, 3.4.x, or later** (compatible with all .NET 8 ArcGIS Pro releases)
* Runtime: **.NET 8.0 Windows Desktop Runtime**

> [!IMPORTANT]
> **ArcGIS Pro must be closed before installing or updating Add-Ins.** ArcGIS Pro scans the Add-Ins directory and loads assemblies into memory only at application startup. Hot-reloading is not supported by ArcGIS Pro.

### Installation Option A: Pre-built Package (Manual Installation)
1. Close **ArcGIS Pro** if running.
2. Locate the pre-built Add-in package in the project root:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. Double-click `GeometryQCAddIn.esriAddinX`.
4. In the official **Esri ArcGIS Pro Add-In Utility** window, click **Install Add-In**.
5. A confirmation prompt will appear:
   > *"Installation Succeeded! The add-in has been installed successfully."*
6. Open ArcGIS Pro; a dedicated **Geometry QC** tab will appear in the Ribbon, and controls will also be available on the **Add-In** tab.

### Installation Option B: Source Build & Auto-Deployment
1. Close **ArcGIS Pro**.
2. Run `package.ps1` in PowerShell or build using .NET CLI:
   ```powershell
   .\package.ps1
   # OR: dotnet build -c Release
   ```
3. The build target automatically creates the package, deploys it to:
   `%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\`
   and purges stale assembly caches in:
   `%LOCALAPPDATA%\ESRI\ArcGISPro\AssemblyCache\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\`
4. Launch ArcGIS Pro.

---

## 3. User Interface Overview

### A. Geometry QC Ribbon Tab

When opening the **Geometry QC** tab (or the **Add-In** tab), tools are organized into two functional groups:

| Tool / Button | Icon | Function Description |
| :--- | :---: | :--- |
| **Enable QC** | `QC_Toggle_ON32.png` | Master toggle to enable or disable Add-in analysis tools. |
| **Run QC** | `QC_Run32.png` | Executes the quality control validation on the active selection or map extent. |
| **Clear Results** | `QC_Clear16.png` | Clears all recorded issues and removes temporary graphic overlays from the map. |
| **Cancel** | `QC_Cancel16.png` | Immediately and safely terminates an ongoing validation task. |
| **Results** | `QC_Results32.png` | Opens or focuses the side **Results DockPane**. |
| **Settings** | `QC_Settings32.png` | Opens the **Settings DockPane** to adjust thresholds and execution modes. |

---

### B. Results DockPane

The **Results DockPane** is located on the right side of the ArcGIS Pro canvas:

1. **Header & Statistics Counter:**
   - Displays the processed feature count and processing elapsed time in seconds.
2. **Target Layer Selector:**
   - Dropdown list allowing you to restrict validation to a single layer (e.g., `Parcels`) or select `All Polygon Layers`.
   - Refresh button (`↻`) to update layer list dynamically.
3. **Execution Mode Selector:**
   - Toggle between **Display Cache** (ultra-fast screen-rendered geometry cache) and **Live Query** (fetches fresh geometries directly from the layer source).
4. **Issue Categories Tree:**
   - Issues are grouped by check type with color-coded severity badges and issue counts:
     - 🔴 **Errors:** Overlap, Invalid Geometry.
     - 🟠 **Warnings:** Gap, Snap Issue, Missing Junction, Short Segment, Angle Issue, Duplicate.
     - 🔵 **Info:** Redundant Vertex, Multi-Part Feature.
5. **Issue Details Panel:**
   - Selecting any issue in the tree displays:
     - Primary Feature OID and Layer Name.
     - Related Feature OID(s) (e.g., overlapping neighbor polygon).
     - Precise measurement metric (e.g., area in `m²` / `cm²`, distance in `mm` / `cm`, angle in `°`).
     - Technical description of the defect.
     - **Zoom to Issue Button (`🔍 Zoom`):** Automatically pans and zooms the active map view directly to the error location at an optimal scale.
6. **Action Buttons:**
   - **Run QC (Cyan / Turquoise):** Execute validation immediately.
   - **Export to GDB (Emerald Green):** Export all detected issues into the project's Default Geodatabase.
   - **Clear Results (Navy / Slate):** Clear issue tree and remove map overlays.

---

### C. Settings DockPane

Allows complete customization of validation sensitivity and thresholds:

| Setting Parameter | Internal Key | Default Value | Description |
| :--- | :--- | :---: | :--- |
| **Overlap Tolerance** | `OverlapToleranceSqMeters` | `0.0001 m²` (1 cm²) | Minimum intersection area required to report an overlap. |
| **Gap Tolerance** | `GapToleranceSqMeters` | `0.001 m²` (10 cm²) | Minimum enclosed void area required to flag a gap. |
| **Short Segment Tolerance** | `ShortSegmentToleranceMeters` | `0.10 m` (10 cm) | Threshold below which polygon segments are flagged. |
| **Angle Tolerance** | `AngleToleranceDegrees` | `5.0°` | Spikes and acute angles narrower than this are flagged. |
| **Snap Tolerance** | `SnapToleranceMeters` | `0.01 m` (1.0 cm) | Vertex separation below which an unsnapped node is flagged. |
| **Redundant Vertex Angle** | `RedundantVertexAngleTolerance` | `179.9°` | Angles approaching 180° flagged as redundant collinear points. |
| **Junction Tolerance** | `JunctionDistanceToleranceMeters` | `0.10 m` (10 cm) | Distance threshold for detecting missing T-junction vertices. |
| **Individual Check Toggles** | `IsCheckEnabled_*` | `true` | Individual check-boxes to enable or disable each of the 10 checks. |
| **Restore Defaults** | — | — | One-click reset to factory standard tolerances. |

---

## 4. Target Layer Selection

The Target Layer feature prevents false positive error detection across unrelated thematic layers:

```text
+-------------------------------------------------------------+
| Layer: [ Parcels                                      ▼ ] [↻] |
+-------------------------------------------------------------+
```

1. **How to use:** Select a specific polygon layer (e.g., `Parcels`) or select **`All Polygon Layers`**.
2. **Refresh Button (`↻`):** Immediately queries the active map to refresh the dropdown without reopening the pane.
3. **Benefit:** Prevents reporting false overlaps between layers that are expected to overlap (e.g., parcels overlapping with zoning or administrative boundary layers).

---

## 5. The 10 QC Checks in Detail

| # | Check Name | Check ID | Default Tolerance | Severity | Geometric Output & Description |
| :-: | :--- | :--- | :-: | :---: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | 🔴 Error | Polygon / Point: Detects non-simple geometries according to OGC/Esri specifications (self-intersections, bow-ties, inverted rings, rings with < 3 points, NaN coordinates). |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.0001 m²` | 🔴 Error | Polygon: Detects overlapping regions between polygon pairs. Decomposes multipart intersections into separate polygons, calculates area accurately in `m²` or `cm²`, and excludes boundary sliver noise. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | 🟠 Warning | Polygon: Detects 100% coincident identical polygons stacked on top of each other, suppressing duplicate overlap reporting. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.001 m²` | 🟠 Warning | Polygon: Detects enclosed, hidden air gaps and sliver holes bounded entirely by adjacent polygons. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | 🔵 Info | Polygon: Identifies single records containing multiple disconnected polygon rings (`PartCount > 1`) in singlepart workflows. |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `10.0 cm` | 🟠 Warning | Polyline: Detects micro-edges and tiny segments shorter than tolerance, excluding coincident vertices (`d < 1e-6`). |
| **7** | **Angle Issue** | `CHK_ANGLE` | `5.0°` | 🟠 Warning | Point: Detects extreme acute angles and needle spikes resulting from accidental mouse clicks. |
| **8** | **Snap Issue** | `CHK_SNAP` | `1.0 cm` | 🟠 Warning | Point: Detects unsnapped near-coincident vertices within tolerance. Distances under 1 cm are formatted in millimeters (`mm`) down to sub-millimeter precision (`0.04 mm`). Truly coincident vertices are excluded. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `179.9°` | 🔵 Info | Point: Detects superfluous collinear vertices along straight lines. Features **Junction Guard**: if a vertex touches another polygon where the neighbor bends (< 180°), it is protected. If two coincident vertices both approach 180°, both are flagged. |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `10.0 cm` | 🟠 Warning | Point: Flags T-junction contact points where a polygon vertex touches a neighboring edge without a matching snapped node (cadastral standard). |

---

## 6. Deduplication & Verification Engine

Raw issues detected during validation pass through a rigorous verification pipeline:
* **Spatial Deduplication:** Prevents reporting the same overlap twice for feature pair (A, B) and (B, A).
* **Sub-Millimeter Reporting:** Distances below 1.0 cm are formatted in millimeters (`mm`) with 3–4 decimal places, preventing misleading `0.00 cm` reports.
* **Junction Guard:** Eliminates false alarms on boundary shared vertices by checking vertex angles on neighboring polygon boundaries.

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
| **Missing Junction** | Point | Coral / Salmon | `#FFFF7F50` | Diamond marker (size 10) |

---

## 8. Exporting Errors to Geodatabase (GDB)

The Add-in includes a native **Export to GDB** service (`GdbExportService.cs`) that converts in-memory QC issues into persistent Geodatabase feature classes:

### How it Works:
1. Click **Export to GDB** in the Results DockPane.
2. The service automatically connects to the project's **Default Geodatabase** (`Project.Current.DefaultGeodatabasePath`).
3. Creates a dedicated Feature Dataset named **`QC_Errors`**.
   - If `QC_Errors` already exists from a prior export, the service automatically increments the name (`QC_Errors_1`, `QC_Errors_2`, etc.) to prevent overwriting historical QC audits.
4. Groups errors by issue type and geometry type into individual Feature Classes:
   - `QC_Overlap_Polygons`
   - `QC_InvalidGeom_Polygons`
   - `QC_Gap_Polygons`
   - `QC_ShortSegment_Lines`
   - `QC_SnapIssue_Points`
   - `QC_AngleIssue_Points`
   - `QC_Junction_Points`
   - `QC_RedundantVertex_Points`
   - etc.
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

6. Automatically adds all exported feature classes to the active map inside a organized Group Layer named after the Feature Dataset, and applies color-matched symbology.

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

1. **Select:** In ArcGIS Pro, select the polygon features you wish to inspect (or leave unselected to validate visible extent).
2. **Configure:** Open **Results DockPane**, pick the target layer from the dropdown.
3. **Execute:** Click **Run QC** (Green Play button).
4. **Navigate:** Expand the categories, click any issue, and use **Zoom** to center on the defect.
5. **Archive / Report:** Click **Export to GDB** to generate official GIS layers of the errors.
6. **Remediate:** Use standard ArcGIS Pro editing tools (Reshape, Align, Split, Merge, Snap) to fix the defects.
7. **Verify:** Click **Clear Results** and re-run QC to verify all issues have been resolved.

---

## 10. Frequently Asked Questions (FAQ) & Troubleshooting

#### Q1: Why doesn't the Add-in require building a Geodatabase Topology?
**A:** Traditional ArcGIS topologies require feature datasets, rule definitions, and schema locks. Geometry QC Analyzer builds an ephemeral, in-memory spatial index and calculates OGC/Esri geometries on the fly, saving hours of geodatabase configuration.

#### Q2: Can I run this on Web Feature Services from ArcGIS Online or Enterprise?
**A:** Yes! The Add-in works directly on polygon layers in your map, including Feature Services and shapefiles, in 100% read-only mode.

#### Q3: Why did the Add-in not appear after building or installing?
**A:** ArcGIS Pro only discovers and loads add-ins at application startup. If Pro was running during installation, you must **close and restart ArcGIS Pro**.

#### Q4: Where are Add-in files deployed on my machine?
**A:**
- Package destination: `%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\GeometryQCAddIn.esriAddinX`
- Assembly runtime cache: `%LOCALAPPDATA%\ESRI\ArcGISPro\AssemblyCache\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\`

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
4. [ميزة اختيار الطبقة المستهدفة (Target Layer Selection)](#4-ميزة-اختيار-الطبقة-المستهدفة-ar)
5. [الفحوصات الهندسية العشرة بالتفصيل](#5-الفحوصات-الهندسية-العشرة-بالتفصيل-ar)
6. [محرك الفحص ودقة القياس تحت الملليمتر](#6-محرك-الفحص-ودقة-القياس-تحت-الملليمتر-ar)
7. [دليل الرموز والألوان التوضيحية على الخريطة](#7-دليل-الرموز-والألوان-التوضيحية-على-الخريطة-ar)
8. [تصدير الأخطاء لقاعدة البيانات الجغرافية (Export to GDB)](#8-تصدير-الأخطاء-لقاعدة-البيانات-الجغرافية-ar)
9. [دورة العمل النموذجية الموصى بها](#9-دورة-العمل-النموذجية-الموصى-بها-ar)
10. [الأسئلة الشائعة وحلول المشاكل (FAQ)](#10-الأسئلة-الشائعة-وحلول-المشاكل-ar)

---

<a name="1-نظرة-عامة-على-الأداة-ar"></a>
## 1. نظرة عامة على الأداة

أداة **Geometry QC Analyzer** هي إضافة برمجية مؤسسية لبرنامج **ArcGIS Pro 3.x**، طُوّرت خصيصاً لخدمة أخصائيي نظم المعلومات الجغرافية، والمساحين، ومخططي المدن. تتيح الأداة فحص وتدقيق الجودة الهندسية والطوبولوجية لطبقات المضلعات (Polygons) بصورة لحظية في الذاكرة دون الحاجة لأي صلاحيات تعديل أو إنشاء قواعد طوبولوجيا معقدة.

### أبرز المزايا المعمارية:
* **أمان تام وقراءة فقط (100% Read-Only):** لا تعدل الأداة أي معالم ولا تضع أقفالاً (Locks) على قواعد البيانات أثناء التحليل.
* **فحص لحظي فائق السرعة:** تعتمد على فهرسة مكانية ثنائية الأبعاد وفهرسة بالذاكرة لرؤوس المضلعات لمعالجة آلاف المعالم في ثوانٍ معدودة.
* **دعم مباشر لطبقات الويب (Feature Services):** فحص طبقات الويب المنشورة مباشرة دون الحاجة لتصديرها محلياً.
* **تصدير الأخطاء بضغطة زر (Export to GDB):** حفظ كافة الأخطاء المكتشفة كطبقات جغرافية كاملة البيانات في قاعدة بيانات المشروع الافتراضية.
* **تكيف تلقائي مع المظهر الفاتح والداكن:** واجهة مستخدم احترافية بتدرجات الكحلي والتركواز تتماشى مع إعدادات ArcGIS Pro.
* **تشخيص بصري تفاعلي:** رسم أشكال هندسية مؤقتة ملونة على الخريطة لتوضيح أماكن العيوب بدقة مع زر تكبير فوري.

---

<a name="2-التثبيت-والتشغيل-السريع-ar"></a>
## 2. التثبيت والتشغيل السريع

### متطلبات التشغيل:
* نظام التشغيل: **Windows 10 / 11 (x64)**
* برنامج نظم المعلومات: **ArcGIS Pro 3.3.x أو 3.4.x أو أحدث** (بيئة Pro 3.x المبنية على .NET 8)
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
5. ستظهر رسالة نجاح التثبيت:
   > *"Installation Succeeded! The add-in has been installed successfully."*
6. شغّل ArcGIS Pro، وستجد تبويب **Geometry QC** في الشريط العلوي (Ribbon)، وأيضاً داخل تبويب **Add-In**.

### الطريقة 2: التثبيت التلقائي مع البناء من الكود المصدري
1. تأكد من إغلاق **ArcGIS Pro**.
2. شغّل سكريبت التجميع في PowerShell:
   ```powershell
   .\package.ps1
   # أو عبر سطر الأوامر: dotnet build -c Release
   ```
3. يقوم الـ Build تلقائياً بإنشاء حزمة `.esriAddinX`، ونسخها إلى مجلد إضافات ArcGIS Pro:
   `%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\`
   مع مسح الكاش القديم في:
   `%LOCALAPPDATA%\ESRI\ArcGISPro\AssemblyCache\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\`
4. افتح برنامج ArcGIS Pro.

---

<a name="3-واجهة-المستخدم-وعناصر-التحكم-ar"></a>
## 3. واجهة المستخدم وعناصر التحكم

### أ. تبويب شريط الأدوات (Geometry QC Ribbon)

تنتظم الأدوات في مجموعتين وظيفيتين:

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
1. **الترويسة والعدادات الإحصائية:** تعرض عدد المعالم المفحوصة والزمن المستغرق بالثواني.
2. **محدد الطبقة المستهدفة (Target Layer Selector):** لاختيار طبقة محددة للفحص (مثل `Parcels`) أو فحص جميع الطبقات (`All Polygon Layers`)، مع زر التحديث السريع (`↻`).
3. **محدد طريقة جلب البيانات (Execution Mode):** التبديل بين **Display Cache** (كاش العرض اللحظي فائق السرعة) و **Live Query** (الاستعلام المباشر من مصدر البيانات).
4. **شجرة تصنيف الأخطاء (Issues Tree View):** تصنيف الأخطاء في مجموعات مع شارات لونية وعدد الأخطاء:
   - 🔴 **الأخطاء الحرجة (Errors):** Overlap, Invalid Geometry.
   - 🟠 **التحذيرات (Warnings):** Gap, Snap Issue, Missing Junction, Short Segment, Angle Issue, Duplicate.
   - 🔵 **المعلومات والملاحظات (Info):** Redundant Vertex, Multi-Part Feature.
5. **لوحة تفاصيل العيب وزر التكبير:**
   - عرض رقم المعلم (Feature OID) واسم الطبقة.
   - أرقام المعالم المرتبطة (مثل المضلع المتداخل المجاور).
   - القياس الدقيق للخطأ (المساحة بوحدة `m²` أو `cm²`، المسافة بوحدة `mm`، الزاوية بوحدة `°`).
   - الوصف التشخيصي الشامل للخلل.
   - **زر التكبير الفوري (`🔍 Zoom`):** ينقل الخريطة فوراً ويكبّر موقع العيب بمقياس رسم مثالي للمعالجة.
6. **شريط الأوامر السفلي (Action Buttons):**
   - **Run QC (أخضر تركواز):** لبدء فحص المعالم المحددة فوراً.
   - **Export to GDB (أخضر زمردي):** لتصدير الأخطاء المكتشفة إلى قاعدة البيانات الجغرافية الافتراضية داخل Feature Dataset مخصص باسم `QC_Errors` وإضافتها للخريطة.
   - **Clear Results (أزرق داكن):** لمسح النتائج وإزالة الرسومات المؤقتة من الخريطة.

---

### ج. لوحة الإعدادات (Settings DockPane)

تتيح التحكم الكامل في حساسية الفحوصات العشرة:

| معامل الإعداد | الاسم البرمجي | القيمة الافتراضية | الوصف الهندسي |
| :--- | :--- | :---: | :--- |
| **تفاوت التداخل** | `OverlapToleranceSqMeters` | `0.0001 m²` (1 سم²) | الحد الأدنى لمساحة التقاطع لاحتسابه كتداخل هندسي. |
| **تفاوت الفجوات الهوائية** | `GapToleranceSqMeters` | `0.001 m²` (10 سم²) | الحد الأدنى لمساحة الفراغ المحصور بين المضلعات لاعتباره فجوة. |
| **تفاوت الأضلاع القصيرة** | `ShortSegmentToleranceMeters` | `0.10 m` (10 سم) | طول الضلع الذي يعتبر ما دونه ضلعاً متناهي الصغر. |
| **تفاوت الزوايا الحادة** | `AngleToleranceDegrees` | `5.0°` | الزوايا الأقل من هذه الدرجة تعتبر إبر شاذة ناتجة عن الرسم الخاطئ. |
| **تفاوت عدم الالتقاط** | `SnapToleranceMeters` | `0.01 m` (1.0 سم) | المسافة الفاصلة بين الرؤوس المتقاربة لاحتسابها خطأ عدم التقاط. |
| **زاوية الرأس الزائد** | `RedundantVertexAngleTolerance` | `179.9°` | الزوايا التي تقترب من 180° وتعتبر رؤوساً غير ضرورية على استقامة الخط. |
| **تفاوت العقد المفقودة** | `JunctionDistanceToleranceMeters` | `0.10 m` (10 سم) | أقصى مسافة فاصلة بين نقطة مضلع وضلع مضلع مجاور للتحقق من وجود عقدة. |
| **مفاتيح تفعيل الفحوصات** | `IsCheckEnabled_*` | `true` | مربعات اختيار لتشغيل أو إيقاف أي فحص بشكل مستقل. |
| **استعادة الافتراضيات** | — | — | إعادة ضبط كافة القيم للمعايير المصنعية الموصى بها. |

---

<a name="4-ميزة-اختيار-الطبقة-المستهدفة-ar"></a>
## 4. ميزة اختيار الطبقة المستهدفة (Target Layer Selection)

تمنع تداخل الفحوصات بين الطبقات المختلفة غير المتجانسة:

```text
+-------------------------------------------------------------+
| Layer: [ Parcels                                      ▼ ] [↻] |
+-------------------------------------------------------------+
```

1. **كيفية الاستخدام:** اختر الطبقة المحددة (مثل `Parcels`) أو اختر **`All Polygon Layers`** لفحص جميع طبقات المضلعات المحددة معاً.
2. **زر التحديث (`↻`):** لتحديث قائمة الطبقات فوراً عند إضافة أو تسمية طبقات جديدة دون إعادة فتح اللوحة.
3. **الفائدة العملية:** منع الأخطاء الزائفة الناتجة عن تداخل طبقات متباينة بطبيعتها (مثل تداخل قطع الأراضي مع الأحياء السكنية أو نطاقات استخدامات الأراضي).

---

<a name="5-الفحوصات-الهندسية-العشرة-بالتفصيل-ar"></a>
## 5. الفحوصات الهندسية العشرة بالتفصيل

| # | اسم الفحص (Check Name) | الكود الداخلي | التفاوت الافتراضي | مستوى الأهمية | الوصف الهندسي وطبيعة الخطأ |
| :-: | :--- | :--- | :---: | :---: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | 🔴 خطأ حرج | مضلع / نقطة: فحص صحة المضلع طوبولوجياً طبقاً لمواصفات OGC/Esri. يكتشف التقاطعات الذاتية (Bow-tie)، الحلقات المعكوسة، الحلقات التي تحتوي على أقل من 3 نقاط، والإحداثيات غير المعرفة (NaN). |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.0001 m²` | 🔴 خطأ حرج | مضلع: يكتشف المساحات المشتركة المتداخلة بين المضلعات. يقوم بتفكيك التداخلات المتعددة وعرض مساحتها بوحدات واضحة (`m²` أو `cm²`) مع استبعاد شوائب تلاصق الحدود. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | 🟠 تحذير | مضلع: يكتشف المضلعات المتطابقة هندسياً بنسبة 100% المرسومة فوق بعضها بالخطأ (Coincident Polygons)، ويمنع تكرار تقريرها كـ Overlap. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.001 m²` | 🟠 تحذير | مضلع: يكتشف الفجوات والثقوب الهوائية غير المرئية المحصورة والمغلقة تماماً بين المضلعات المتجاورة الناتجة عن أخطاء الرسم. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | 🔵 معلومة | مضلع: يكتشف المعالم ذات السجل الواحد التي تتكون من أكثر من مضلع منفصل (`PartCount > 1`) في قواعد البيانات التي تتطلب مضلعات أحادية (Singlepart). |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `10.0 cm` | 🟠 تحذير | خط: يكتشف الأضلاع أو الأجزاء متناهية الصغر التي يقل طولها عن الحد المسموح، مع استبعاد النقاط المتطابقة لحصر الأخطاء في عيوب الرسم الفعلية. |
| **7** | **Angle Issue** | `CHK_ANGLE` | `5.0°` | 🟠 تحذير | نقطة: يكتشف الزوايا الحادة والشاذة جداً (Spikes) التي تقل عن الدرجة المحددة والناتجة عادةً عن نقرات خاطئة أثناء الرسم بالماوس. |
| **8** | **Snap Issue** | `CHK_SNAP` | `1.0 cm` | 🟠 تحذير | نقطة: يكتشف الرؤوس والنقاط المتقاربة غير الملتقطة. تُعرض المسافات تحت السنتيمتر بالملليمتر (`mm`) بدقة فائقة لمنع تقريب المسافات غير الملتقطة إلى `0.00 cm` مع استبعاد النقاط المتطابقة تماماً. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `179.9°` | 🔵 معلومة | نقطة: يكتشف النقاط الزائدة على استقامة الخط (180°). وتعمل ميزة **حماية نقاط الربط (Junction Guard)** على استثناء النقاط المشتركة فقط إذا كان المضلع المجاور ينكسر أو يرتبط عندها برأس حقيقي (زاوية < 180°)، أما إذا وُجد رأسان متطابقان فوق بعض وكلاهما على استقامة الخط (تقترب الزاوية لكل منهما من 180°) فيتم اعتبارهما خطأ رأس زائد على كلا المضلعين. |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `10.0 cm` | 🟠 تحذير | نقطة: يكتشف العقد المفقودة في الوصلات التبادلية (T-Junctions) عندما تلامس نقطة من مضلع ضلع مضلع مجاور دون وجود رأس مشترك ملتقط عليه (المعيار المعتمد في المخططات العقارية). |

---

<a name="6-محرك-الفحص-ودقة-القياس-تحت-الملليمتر-ar"></a>
## 6. محرك الفحص ودقة القياس تحت الملليمتر

تخضع جميع النتائج المكتشفة لخط معالجة وتدقيق متقدم:
* **منع التكرار (Deduplication):** منع تسجيل نفس عيب التداخل مرتين للزوج (أ، ب) و(ب، أ).
* **دقة القياس تحت الملليمتر (Sub-Millimeter Precision):** قياس وعرض المسافات التي تقل عن 1 سنتيمتر بوحدة الملليمتر (`mm`) مع 3 إلى 4 خانات عشرية (مثل `0.04 mm` أو `0.35 mm`) لمنع الخداع البصري برسائل التقريب `0.00 cm`.
* **حارس نقاط الربط (Junction Guard):** تفادي حذف الرؤوس المشتركة بين المضلعات المتجاورة عبر التحقق من زوايا الحدود المشتركة قبل تصنيف النقطة كـ Redundant Vertex.

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
| **Missing Junction** | نقطة | مرجاني (Coral) | `#FFFF7F50` | علامة معينة بحجم 10 |

---

<a name="8-تصدير-الأخطاء-لقاعدة-البيانات-الجغرافية-ar"></a>
## 8. تصدير الأخطاء لقاعدة البيانات الجغرافية (Export to GDB)

تتضمن الإضافة خدمة متكاملة لتصدير الأخطاء (`GdbExportService.cs`) لتحويل نتائج الفحص اللحظية إلى طبقات جغرافية دائمة داخل قاعدة بيانات المشروع:

### آلية العمل:
1. انقر على زر **Export to GDB** في لوحة النتائج.
2. تتصل الخدمة تلقائياً بقاعدة البيانات الافتراضية للمشروع (`Default Geodatabase`).
3. تنشئ Feature Dataset مخصصاً باسم **`QC_Errors`**.
   - إذا كان `QC_Errors` موجوداً مسبقاً، تقوم الأداة تلقائياً بإضافة رقم تسلسلي (`QC_Errors_1`, `QC_Errors_2`... إلخ) للحفاظ على سجلات الفحوصات التاريخية دون استبدالها.
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

1. **التحديد:** استخدم أداة التحديد في ArcGIS Pro لاختيار المضلعات المراد فحصها (أو اتركها دون تحديد لفحص كامل النطاق المعروض).
2. **التهيئة:** افتح لوحة **Results** واختر الطبقة المستهدفة.
3. **التشغيل:** انقر على زر **Run QC** (زر التشغيل الأخضر).
4. **التصفح والتكبير:** تصفح الأخطاء حسب التصنيف، وانقر على أي خطأ ثم زر **Zoom** للانتقال إلى موقعه.
5. **التوثيق:** انقر على **Export to GDB** لإنشاء طبقات رسمية للأخطاء.
6. **المعالجة:** استخدم أدوات التعديل القياسية في ArcGIS Pro (Reshape, Align, Split, Merge, Snapping) لإصلاح الخلل.
7. **إعادة التحقق:** انقر على **Clear Results** وأعد تشغيل الفحص للتأكد من زوال كافة الأخطاء.

---

<a name="10-الأسئلة-الشائعة-وحلول-المشاكل-ar"></a>
## 10. الأسئلة الشائعة وحلول المشاكل (FAQ)

#### س1: لماذا لا تتطلب الأداة بناء Geodatabase Topology؟
**ج:** بناء الـ Topology التقليدي يتطلب وضع البيانات في Feature Dataset محلي وتحديد قوانين معقدة وقفل الجداول. تعتمد أداة Geometry QC Analyzer على فهرسة مكانية لحظية بالذاكرة وتقوم بحساب العلاقات الهندسية مباشرة، مما يوفر ساعات طويلة من العمل الروتيني.

#### س2: هل يمكن استخدام الأداة مع طبقات الويب (Feature Services) من ArcGIS Online أو Portal؟
**ج:** نعم بكل تأكيد! الأداة تقرأ المعالم مباشرة من شاشة العرض أو من خلال استعلامات الطبقة، وتعمل في وضع القراءة فقط 100% دون الحاجة لتصدير الطبقات محلياً.

#### س3: قمت بتثبيت الإضافة أو عمل Build ولكن الشريط لم يظهر في ArcGIS Pro؟
**ج:** برنامج ArcGIS Pro لا يدعم التحديث الحي أثناء تشغيله (No Hot-Reloading). يجب **إغلاق برنامج ArcGIS Pro بالكامل ثم إعادة فتحه**، حيث يقرأ البرنامج مجلد الإضافات فقط لحظة إقلاعه (Startup).

#### س4: أين تُخزن ملفات الإضافة وكاش التجميع على جهازي؟
**ج:**
- **مسار حزمة الإضافة:** `%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\GeometryQCAddIn.esriAddinX`
- **مسار الكاش المؤقت (Assembly Cache):** `%LOCALAPPDATA%\ESRI\ArcGISPro\AssemblyCache\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}\`

#### س5: كيف يمكنني تعديل حساسية الفحوصات (مثلاً تغيير زاوية الخطأ أو تفاوت الالتقاط)؟
**ج:** اضغط على زر **Settings** في الشريط العلوي لتفتح لوحة الإعدادات؛ حيث يمكنك تعديل أي رقم أو تفاوت، وسيتم حفظ التعديلات تلقائياً للمرات القادمة.

---
*Developed with ❤️ for the GIS & Geospatial Community.*
