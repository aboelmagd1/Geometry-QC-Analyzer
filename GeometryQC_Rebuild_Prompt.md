# Geometry QC Analyzer — Complete System Architecture & AI Rebuild Prompt
# الدليل الشامل للهندسة المعمارية وبرومت إعادة بناء إضافة فحص الجودة الهندسية

<p align="center">
  <a href="#english-architecture">English Specification</a> •
  <a href="#المواصفات-الفنية-باللغة-العربية">المواصفات الفنية باللغة العربية</a>
</p>

---

<a name="english-architecture"></a>
# English Specification & Rebuild Prompt

This document provides the complete, authoritative technical specification to reconstruct the **Geometry QC Analyzer** ArcGIS Pro Add-In from scratch. Any advanced AI coding agent or senior software engineer can faithfully regenerate the entire codebase using this specification.

---

## 1. Project Profile & Target Environment

* **Product Name:** Geometry QC Analyzer for ArcGIS Pro
* **Add-In ID:** `{8a7f921d-44a3-4b92-95f2-953e5e6080dc}`
* **Target Framework:** `.NET 8.0-windows`
* **Language:** C# 12.0
* **Host Application:** ArcGIS Pro 3.0 through 3.4+ (Built on ArcGIS Pro .NET 8 SDK)
* **Architecture Pattern:** MVVM (Model-View-ViewModel) with ArcGIS Pro Framework Contracts
* **Thread Safety Model:** Esri `QueuedTask.Run(...)` for all ArcGIS Pro SDK geometry and geodatabase interactions; WPF Dispatcher for UI bindings.
* **Safety Mandate:** 100% In-Memory, Read-Only inspection. Zero modifications or locks on source geodatabases or feature services during validation.

---

## 2. Directory Hierarchy & Code Structure

```text
QC Preview V01/
├── Config.daml                                # ArcGIS Pro Declarative Add-in Markup
├── GeometryQCAddIn.csproj                     # MSBuild SDK Project with Auto-Deploy
├── GeometryQCAddIn.sln                        # Visual Studio Solution
├── GeometryQCModule.cs                        # Add-In Module Singleton
├── package.ps1                                # PowerShell Build & Cache-Purge Script
├── Config/
│   └── GeometryQCSettings.cs                  # User Settings Model & Persistence
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
│   │   ├── RedundantVertexCheck.cs            # CHK_REDUNDANT (Junction Guard)
│   │   └── JunctionVertexCheck.cs             # CHK_JUNCTION (T-Junctions)
│   ├── DisplayCacheProvider.cs                # Ultra-fast viewport display cache reader
│   ├── GeometryDataProvider.cs                # Base provider abstraction
│   ├── GeometryHelpers.cs                     # Angle calculation & sub-millimeter metrics
│   ├── GeometryQCContext.cs                   # Execution context (Spatial Reference, Index, Features)
│   ├── GeometryValidator.cs                   # Orchestrator & Deduplication engine
│   ├── LiveQueryProvider.cs                   # Fresh layer query provider via SearchCursor
│   ├── SpatialIndex.cs                        # 2D Bounding-Box Grid Spatial Hash
│   ├── UnitConverter.cs                       # Coordinate unit normalization (metric/degrees)
│   └── VertexIndex.cs                         # Sub-millimeter Point Spatial Hash
├── Models/
│   ├── IssueResult.cs                         # Issue data model with metric formatting
│   ├── IssueSeverity.cs                       # Enum: Error, Warning, Info
│   ├── MapLayerItem.cs                        # Layer selector dropdown model
│   ├── QCFeature.cs                           # In-memory polygon geometry container
│   └── QCStatistics.cs                        # Feature count & elapsed time counters
├── Services/
│   ├── GdbExportService.cs                    # Export issues to GDB Feature Dataset
│   ├── LoggingService.cs                      # Diagnostic logging service
│   ├── MapViewService.cs                      # Map panning & graphics overlay engine
│   ├── ProgressService.cs                     # Task cancellation & progress reporting
│   ├── SelectionService.cs                    # Active map polygon selection listener
│   └── ThemeService.cs                        # ArcGIS Pro Light/Dark theme synchronizer
├── UI/
│   ├── Buttons.cs                             # Ribbon Button definitions
│   ├── RelayCommand.cs                        # ICommand implementation for MVVM
│   ├── ResultsDockPane.xaml                   # Results DockPane View
│   ├── ResultsDockPane.xaml.cs                # Code-behind
│   ├── ResultsDockPaneViewModel.cs            # Results DockPane ViewModel
│   ├── SettingsDockPane.xaml                  # Settings DockPane View
│   ├── SettingsDockPane.xaml.cs               # Code-behind
│   ├── SettingsDockPaneViewModel.cs           # Settings DockPane ViewModel
│   └── ThemeResources.xaml                    # Navy/Cyan/Turquoise brushes & styles
└── Images/                                    # 16x16, 32x32, 64x64 Custom Icons
    ├── QC_Toggle_ON*.png
    ├── QC_Toggle_OFF*.png
    ├── QC_Run*.png
    ├── QC_Clear*.png
    ├── QC_Cancel*.png
    ├── QC_Results*.png
    ├── QC_Settings*.png
    └── ...
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
      <Date>2026-09-21</Date>
      <Subject>Geometry Quality Control</Subject>
    </AddInInfo>
  ```
* **Module Auto-Loading:** Set `<insertModule autoLoad="true">` to guarantee background services initialize at application startup.
* **Ribbon Tab & Groups:**
  - Tab: `GeometryQC_Tab` ("Geometry QC")
  - Group 1: `GeometryQC_MainGroup` ("Validation", `appearsOnAddInTab="true"`)
    - `GeometryQC_ToggleBtn` (Large, `QC_Toggle_ON32.png`)
    - `GeometryQC_RunBtn` (Large, `QC_Run32.png`)
    - `GeometryQC_ClearBtn` (Middle, `QC_Clear16.png`)
    - `GeometryQC_CancelBtn` (Middle, `QC_Cancel16.png`)
  - Group 2: `GeometryQC_PanelsGroup` ("Panels", `appearsOnAddInTab="true"`)
    - `GeometryQC_ResultsBtn` (Large, `QC_Results32.png`)
    - `GeometryQC_SettingsBtn` (Large, `QC_Settings32.png`)
* **DockPanes:**
  - `GeometryQC_ResultsDockPane`: Docked on right, class `ResultsDockPaneViewModel`.
  - `GeometryQC_SettingsDockPane`: Docked on right, class `SettingsDockPaneViewModel`.

---

### B. In-Memory 2D Spatial Index & Vertex Index

1. **`SpatialIndex.cs` (Bounding-Box Hash Grid):**
   - Calculates grid cell size adaptively based on the average bounding box dimension of candidate features.
   - Maps cell coordinates `(int X, int Y)` to buckets of candidate feature IDs.
   - Reduces polygon intersection candidate pairs from $O(N^2)$ to $O(N \log N)$ or near-linear complexity.
2. **`VertexIndex.cs` (Point Spatial Hash):**
   - Indexes all vertices across polygon rings using spatial hashing with cell size equal to tolerance (e.g., 1 cm).
   - Allows instant $O(1)$ neighborhood vertex queries for sub-millimeter snap checks and junction detection.
3. **Data Providers (`GeometryDataProvider.cs`):**
   - **`DisplayCacheProvider`:** Reads rendered screen graphics directly from ArcGIS Pro MapView display cache for instant preview.
   - **`LiveQueryProvider`:** Uses `FeatureClass.Search(...)` within `QueuedTask` for rigorous validation of attributes and complete geometries.

---

### C. The 10 Concrete QC Checks

Each check implements `IGeometryCheck` and executes asynchronously with a `CancellationToken`:

1. **Invalid Geometry (`CHK_INVALID_GEOM`, Error):**
   - Executes `GeometryEngine.Instance.Simplify()`. If geometry changes or is null, flags error.
   - Checks for non-positive area, ring point count $< 3$, and `double.IsNaN` in coordinates.
2. **Overlap (`CHK_OVERLAP`, Error, default 0.0001 m²):**
   - Uses `SpatialIndex` to find candidate overlapping pairs.
   - Computes `GeometryEngine.Instance.Intersection(polyA, polyB)`.
   - Decomposes multipart intersections into singlepart polygons; calculates area in m² or cm²; eliminates boundary touch slivers.
3. **Duplicate Geometry (`CHK_DUPLICATE`, Warning):**
   - Fast spatial vertex hash comparison followed by `GeometryEngine.Instance.Equals()`.
   - Suppresses redundant overlap reporting for identical features.
4. **Enclosed Gap (`CHK_GAP`, Warning, default 0.001 m²):**
   - Computes geometric union of adjacent polygons, identifies interior holes/rings that do not touch the exterior boundary, and flags areas above tolerance.
5. **Multi-Part Feature (`CHK_MULTIPART`, Info):**
   - Inspects `poly.PartCount > 1` in datasets requiring singlepart polygons.
6. **Short Segment (`CHK_SHORT_SEG`, Warning, default 0.10 m = 10 cm):**
   - Traverses polygon boundary segments. Flags segment length $< \text{tolerance}$, excluding coincident points ($d < 10^{-6}$).
7. **Angle Issue (`CHK_ANGLE`, Warning, default 5.0°):**
   - Traverses consecutive vertex triplets on each ring. Computes interior angle; flags extreme spikes and needles $< 5.0°$.
8. **Snap Issue (`CHK_SNAP`, Warning, default 0.01 m = 1.0 cm):**
   - Uses `VertexIndex` to find vertices from different features separated by $0 < d \le \text{tolerance}$.
   - Sub-millimeter reporting: If $d < 0.01\text{ m}$, outputs metric in millimeters (`mm`) with up to 4 decimal places (e.g., `0.04 mm`). Excludes identical vertices ($d < 10^{-5}\text{ m}$).
9. **Redundant Vertex (`CHK_REDUNDANT`, Info, default 179.9°):**
   - Flags collinear vertices along straight lines where angle $\ge 179.9°$.
   - **Junction Guard:** Protects vertices that coincide with vertices of adjacent polygons where the neighbor bends ($< 180°$). If both coincident vertices are collinear ($\approx 180°$), flags both as redundant.
10. **Missing Junction (`CHK_JUNCTION`, Warning, default 0.10 m = 10 cm):**
    - Identifies T-junctions where a vertex on polygon A touches an edge of polygon B, but polygon B lacks a matching snapped vertex.

---

### D. Geodatabase Error Export Service (`GdbExportService.cs`)

* **Target Geodatabase:** Resolves active project default geodatabase: `Project.Current.DefaultGeodatabasePath`.
* **Dataset Creation:** Creates Feature Dataset `QC_Errors`. If already existing, automatically increments name (`QC_Errors_1`, `QC_Errors_2`, etc.) via `FeatureDatasetDefinition` checks.
* **Schema Definition via `SchemaBuilder`:**
  - Coordinate system inherited from active MapView.
  - Generates Feature Classes segregated by geometry type (`Points`, `Lines`, `Polygons`).
  - Standard Attribute Schema:
    - `Issue_Type` (String 100)
    - `Check_ID` (String 50)
    - `Source_OID` (Integer)
    - `Related_OIDs` (String 255)
    - `Layer_Name` (String 100)
    - `Severity` (String 20)
    - `Metric_Val` (Double)
    - `Metric_Unit` (String 20)
    - `Description` (String 500)
    - `Export_Time` (String 50)
* **Bulk Insertion:** Writes features via high-performance `InsertCursor` inside `QueuedTask.Run`.
* **Map Integration:** Automatically groups exported feature classes into a GroupLayer in the active map and styles them with error-matched colors.

---

### E. Packaging, Deployment & Clean Cache (`GeometryQCAddIn.csproj`)

```xml
<Target Name="PackageAndDeployAddIn" AfterTargets="Build">
  <PropertyGroup>
    <AddInPackage>$(OutDir)GeometryQCAddIn.esriAddinX</AddInPackage>
    <ProAddInFolder>$(USERPROFILE)\Documents\ArcGIS\AddIns\ArcGISPro\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}</ProAddInFolder>
    <ProAssemblyCache>$(LOCALAPPDATA)\ESRI\ArcGISPro\AssemblyCache\{8a7f921d-44a3-4b92-95f2-953e5e6080dc}</ProAssemblyCache>
  </PropertyGroup>
  <Copy SourceFiles="$(AddInPackage)" DestinationFolder="$(ProjectDir)" />
  <MakeDir Directories="$(ProAddInFolder)" Condition="!Exists('$(ProAddInFolder)')" />
  <Copy SourceFiles="$(AddInPackage)" DestinationFolder="$(ProAddInFolder)" />
  <RemoveDir Directories="$(ProAssemblyCache)" Condition="Exists('$(ProAssemblyCache)')" />
</Target>
```

---

## 4. Mandatory Architectural Rules & Constraints

1. **Button Constructor Rule:** Every button class inheriting from `ArcGIS.Desktop.Framework.Contracts.Button` must explicitly call `Enabled = true;` in its parameterless constructor.
2. **AutoLoad Rule:** In `Config.daml`, `<insertModule>` must have `autoLoad="true"` to ensure `ThemeService` and background listeners activate immediately.
3. **No Hot-Reloading:** ArcGIS Pro only reads add-in assemblies at startup. Always alert the user to close and restart ArcGIS Pro after any installation or build.
4. **Read-Only Safety:** The Add-In must never start an editing transaction (`EditOperation`) on the source layers.

---
---

<a name="المواصفات-الفنية-باللغة-العربية"></a>
# المواصفات الفنية باللغة العربية (Arabic Technical Specification)

هذا القسم يقدم الدليل الهندسي الشامل باللغة العربية لإعادة بناء وبرمجة إضافة **Geometry QC Analyzer** بالكامل من الصفر بواسطة أي مهندس برمجيات أو أداة ذكاء اصطناعي.

---

## 1. بطاقة تعريف المشروع وبيئة العمل

* **اسم الإضافة:** Geometry QC Analyzer for ArcGIS Pro
* **المعرف الفريد (Add-In GUID):** `{8a7f921d-44a3-4b92-95f2-953e5e6080dc}`
* **بيئة التشغيل والإطار المستهدف:** `.NET 8.0-windows`
* **لغة البرمجة:** C# 12.0
* **البرنامج المضيف:** ArcGIS Pro (الإصدارات من 3.0 حتى 3.4 وما بعدها)
* **النمط المعماري:** MVVM مدعوماً بعقود Esri Framework Contracts و `QueuedTask.Run` لضمان أمان المسارات المتعددة (Thread Safety).
* **مبدأ الأمان:** قراءة فقط 100% (Read-Only) مع معالجة بالذاكرة اللحظية دون إجراء أي تعديل أو قفل لقواعد البيانات الأصلية.

---

## 2. الهيكلية المعمارية للمجلدات والكود

1. **`Config.daml`:** ملف التكوين الإعلاني لـ ArcGIS Pro متوافق تماماً مع مخطط `http://Config.xsd` وضبط `desktopVersion="3.0"` وتفعيل `autoLoad="true"`.
2. **`Core/Checks/`:** مجلد الفحوصات الهندسية العشرة المستقلة المشتقة من واجهة `IGeometryCheck`.
3. **`Core/SpatialIndex.cs` & `VertexIndex.cs`:** محركات الفهرسة المكانية بالذاكرة لتسريع المقارنات من $O(N^2)$ إلى سرعات لحظية.
4. **`Services/GdbExportService.cs`:** خدمة تصدير الأخطاء إلى قاعدة البيانات الافتراضية داخل Feature Dataset مخصص باسم `QC_Errors`.
5. **`UI/ResultsDockPane` & `SettingsDockPane`:** لوحات التحكم الجانبية المبنية بتقنية WPF مع دعم المظهر الفاتح والداكن.
6. **`package.ps1` & `csproj`:** سكريبتات التجميع والنشر التلقائي لمجلد إضافات المستخدم مع مسح كاش الـ `AssemblyCache`.

---

## 3. الفحوصات الهندسية العشرة بالتفصيل

1. **فحص سلامة المضلع (Invalid Geometry):** استدعاء `Simplify()` والتحقق من الحلقات العكسية والتقاطعات الذاتية والإحداثيات غير المعرفة.
2. **فحص التداخل (Overlap):** استخدام الفهرس المكاني لاقتطاع التقاطعات وحساب المساحة بدقة بالمتر المربع أو السنتيمتر المربع مع تفكيك الأجزاء المتعددة.
3. **فحص التطابق المكرر (Duplicate Geometry):** فحص المضلعات المتطابقة بنسبة 100% لمنع تكرار تقريرها كتداخل.
4. **فحص الفجوات الهوائية (Enclosed Gap):** كشف الفراغات المغلقة المحصورة بالكامل بين المضلعات المتجاورة.
5. **فحص تعدد الأجزاء (Multi-Part Feature):** كشف المعالم التي تتكون من أكثر من جزء منفصل (`PartCount > 1`).
6. **فحص الأضلاع القصيرة (Short Segment):** رصد الحدود متناهية الصغر الأقل من التفاوت (افتراضياً 10 سم).
7. **فحص الزوايا الحادة (Angle Issue):** كشف الإبر والزوايا الحادة الشاذة الأقل من 5 درجات.
8. **فحص عدم الالتقاط (Snap Issue):** رصد الرؤوس المتقاربة غير الملتقطة مع قياس المسافة بدقة الملليمتر (`mm`) حتى 4 خانات عشرية للمسافات الأقل من 1 سم.
9. **فحص الرؤوس الزائدة (Redundant Vertex):** كشف الرؤوس التي تقع على استقامة الخط (180°)، مع ميزة **Junction Guard** الذكية لحماية نقاط الربط بين المضلعات المتجاورة.
10. **فحص العقد المفقودة (Missing Junction):** رصد نقاط التماس (T-Junctions) التي تلامس ضلع مضلع مجاور دون وجود رأس ملتقط عليها.

---

## 4. خدمة التصدير لقاعدة البيانات (GDB Export Service)

* الاتصال التلقائي بقاعدة البيانات الافتراضية: `Project.Current.DefaultGeodatabasePath`.
* إنشاء Feature Dataset باسم `QC_Errors` (أو `QC_Errors_1`, `QC_Errors_2`... في حال التكرار).
* بناء المخطط الهندسي باستخدام `SchemaBuilder` وفصل الطبقات بحسب الشكل الهندسي (نقاط، خطوط، مضلعات).
* إدراج السجلات والبيانات الوصفية (نوع الخطأ، الكود، رقم المعلم OID، المعالم المرتبطة، القياس، الوحدة، التاريخ) عبر `InsertCursor`.
* إضافة الطبقات المصدرة مباشرة إلى الخريطة النشطة تحت Group Layer مع تلوينها بألوان الفحص المعتمدة.

---

## 5. قواعد التنفيذ الإلزامية

1. **تفعيل الأزرار برمجياً:** يجب على جميع فئات الأزرار المشتقة من `ArcGIS.Desktop.Framework.Contracts.Button` كتابة `Enabled = true;` في المشيد (Constructor).
2. **التشغيل التلقائي:** ضبط `autoLoad="true"` في `Config.daml`.
3. **عدم دعم التحديث المباشر (No Hot-Reload):** تنبيه المستخدم دائماً بضرورة إغلاق وتشغيل ArcGIS Pro لقراءة أي تحديثات برمجية.
4. **القراءة فقط:** الامتناع التام عن بدء أي عمليات تعديل (`EditOperation`) على طبقات المصدر الأصلية.

---
*تم إعداد هذه الوثيقة كمرجع تقني وهندسي معتمد لبناء وتطوير إضافة Geometry QC Analyzer.*
