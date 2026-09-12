# ArcGIS Pro Geometry QC Analyzer — User Guide
# دليل مستخدم أداة فحص الجودة الهندسية

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
8. [Recommended Workflow](#8-recommended-workflow)
9. [Frequently Asked Questions (FAQ) & Troubleshooting](#9-frequently-asked-questions-faq--troubleshooting)

---

## 1. Overview

The **Geometry QC Analyzer** is an enterprise-grade Quality Control Add-In for **ArcGIS Pro 3.x**, engineered specifically for GIS professionals, cadastral surveyors, and urban planners. It provides instant, in-memory validation of polygon features without requiring enterprise geodatabase schemas, topology datasets, or write permissions.

### Key Architectural Highlights:
* **100% Read-Only Safety:** The Add-in never locks, modifies, or writes to your source feature classes or geodatabases.
* **Sub-Second In-Memory Processing:** Utilizes custom 2D spatial grid indexing and vertex-level spatial hash indexing to evaluate thousands of polygons in fractions of a second.
* **Direct Web Feature Service Support:** Validate live web layers and Feature Services on the fly without having to export them to local geodatabases.
* **Adaptive Light & Dark Theme UI:** Designed to seamlessly integrate with native ArcGIS Pro styling across themes.
* **Interactive Visual Diagnostics:** Highlights each detected issue on the map with color-coded temporary graphics and provides instant zoom navigation.

---

## 2. Installation & Setup

### Prerequisites:
* Operating System: **Windows 10 / 11 (x64)**
* Host Application: **ArcGIS Pro 3.0 or higher** (fully certified up to **ArcGIS Pro 3.4.x**)
* Runtime: **.NET 8.0 Windows Desktop Runtime**

### Installation Steps:
1. Close **ArcGIS Pro** if running.
2. Navigate to your project directory and double-click the installer file:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. In the official **Esri ArcGIS Pro Add-In Utility** window, click **Install Add-In**.
4. A confirmation prompt will appear:
   > *"Installation Succeeded! The add-in has been installed successfully."*
5. Open ArcGIS Pro; a dedicated **Geometry QC** tab will now be available in the top Ribbon.

---

## 3. User Interface Overview

### A. Geometry QC Ribbon Tab

When opening the **Geometry QC** tab, tools are logically organized into two functional groups:

| Tool / Button | Function Description |
| :--- | :--- |
| **Enable QC** | Master toggle to enable or disable Add-in analysis tools. |
| **Run QC** | Executes the quality control validation on the active selection or map extent. |
| **Clear Results** | Clears all recorded issues and removes temporary graphic overlays from the map. |
| **Cancel** | Immediately and safely terminates an ongoing validation task. |
| **Results** | Opens or focuses the side **Results DockPane**. |
| **Settings** | Opens the **Settings DockPane** to adjust thresholds and execution modes. |

---

### B. Results DockPane

The **Results DockPane** is located on the right side of the ArcGIS Pro canvas:

1. **Header & Statistics Counter:**
   - Displays the dockpane title, processed feature count, and processing elapsed time in seconds.
   - Highlights total issue count with a distinct badge (`Issues: N`).
2. **Target Layer Selector:**
   - Dropdown list to filter the QC evaluation to a specific layer (or all polygon layers), with a quick refresh button (`↻`).
3. **Progress Bar & Status:**
   - Visible during processing to indicate progress percentage and current check name with an immediate **Cancel** button.
4. **Categorized Issue Tree:**
   - Collapsible categories for each check type (e.g., *Overlap*, *Snap Issue*) showing individual issue counts.
   - Expanding a category displays affected features with their Feature OID, Layer Name, and precise issue metrics.
   - Dedicated **Zoom** button beside each issue to instantly navigate and frame the error on the map.
5. **Issue Details Panel:**
   - Shows detailed diagnostic notes, exact measured values (distance, area, or angle), and related feature IDs.
6. **Bottom Command Bar:**
   - Quick action buttons for **Run QC** and **Clear Results**.

---

### C. Settings DockPane

Allows complete customization of tolerances, thresholds, and execution modes:

* **Data Source Mode:**
  - `Display Cache` *(Default & Recommended)*: Reads geometries directly from the active map display cache in memory — up to 10x faster.
  - `Live Query`: Queries geometries directly from the underlying geodatabase via spatial queries.
* **Enable for Service Layers:** Toggle whether to inspect web feature layers and service layers (disabled by default to prevent network latency).
* **Execution Mode:**
  - `Manual`: Runs QC strictly when clicking the **Run QC** button.
  - `Auto`: Automatically triggers QC whenever the map feature selection changes.
* **10 QC Check Thresholds:** Enable/disable individual checks and customize thresholds (in cm, m², and degrees).
* **Restore Defaults Button:** Resets all parameters back to official standard geometric tolerances with one click.

---

## 4. Target Layer Selection

The **Target Layer Selection** dropdown prevents false-positive conflicts between different thematic layers:

```text
+-------------------------------------------------------------+
| Layer: [ Parcels                                      ▼ ] [↻] |
+-------------------------------------------------------------+
```

1. **How to Use:**
   - In the **Results DockPane**, select the specific layer to validate (e.g., `Parcels`).
   - Or select **`All Polygon Layers`** to validate all selected polygon layers together.
2. **Refresh Button (`↻`):**
   - Click `↻` whenever you add, rename, or remove layers from the map to update the list immediately.
3. **Key Benefit:**
   - Prevents invalid cross-layer overlap or snap errors between layers that are expected to overlap (e.g., parcels vs. zoning districts or administrative boundaries).

---

## 5. The 10 QC Checks in Detail

| # | Check Name | Internal Code | Default Tolerance | Geometric Nature & Error Description |
| :-: | :--- | :--- | :-: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | Validates topological simplicity per OGC/Esri specs. Catches self-intersections (bow-ties), inverted rings, rings with < 3 vertices, and undefined NaN coordinates. |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.0001 m²` | Detects overlapping areas between polygons. Decomposes multipart intersections and reports accurate square meters or cm², excluding boundary slivers. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | Detects 100% coincident identical polygons digitized on top of each other, suppressing duplicate overlap warnings. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.001 m²` | Identifies invisible enclosed air holes and slivers trapped between adjacent polygon boundaries due to digitizing errors. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | Detects features consisting of multiple disconnected rings (PartCount > 1) in databases requiring strict singlepart polygons. |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `10.0 cm` | Detects micro-edges and polygon segments shorter than allowed tolerance, excluding coincident vertices. |
| **7** | **Angle Issue** | `CHK_ANGLE` | `5.0°` | Identifies severe sharp spikes and needle vertices with deflection angles below the threshold caused by accidental mouse clicks. |
| **8** | **Snap Issue** | `CHK_SNAP` | `1.0 cm` | Detects unsnapped near-coincident vertices. Displays sub-centimeter gaps in millimeters (`mm`) with high precision, excluding exact identical nodes. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `179.9°` | Detects unnecessary collinear vertices along straight edges (180°). **Junction Guard** protects valid T-junctions, but if two coincident vertices both approach 180°, both are flagged as redundant errors. |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `1.0 cm` | Detects missing nodes at T-junctions where a vertex of Polygon A touches the interior boundary of Polygon B without a matching snapped vertex on Polygon B. |

---

## 6. Deduplication & Verification Engine

The Add-in incorporates an intelligent **Verification & Deduplication Engine**:

1. **Sub-Millimeter Precision:**
   - In **Snap Issue**: Gaps below 1 cm are measured and reported in millimeters (`mm`) with up to 3–4 decimal places (e.g., `0.04 mm`). Truly identical nodes (exact matching coordinates) are excluded, preventing misleading `0.00 cm` ghost messages.
   - In **Overlap**: Microscopic slivers caused by boundary floating-point precision are filtered out.
2. **Pairwise & Spatial Deduplication:**
   - Pair-based checks (Overlap, Snap) use canonical sorting `[minOID, maxOID]` so each conflict is reported once.
   - **T-Junction Deduplication:** When two adjacent parcels meet a neighbor at the same spot, the issue is attributed once to the target polygon lacking the node, listing all touching feature OIDs.
3. **Topological Junction Protection in Redundant Vertex:**
   - Vertices with 180° angle that serve as a legitimate T-junction corner for an adjacent parcel are preserved to protect neighbourhood topology.
4. **Closed Ring Normalization:**
   - Ring closure vertices (Vertex 0 and Vertex N-1) are normalized to avoid duplicate checks.

---

## 7. Graphic Symbols Legend on the Map

Upon validation, temporary graphic overlays are drawn on the active map view:

| Check Name | Color & Symbol on Map | Graphic Icon |
| :--- | :--- | :---: |
| **Invalid Geometry** | Thick Purple Cross | ✖ |
| **Overlap** | Transparent Red Fill with Vivid Red Outline | ▨ |
| **Duplicate Geometry** | Dark Crimson Hatch Polygon | ▦ |
| **Enclosed Gap** | Bright Amber / Yellow Fill | ▨ |
| **Multi-Part Feature** | Orange Diamond Marker | ◆ |
| **Short Segment** | Vivid Thick Blue Line | ━ |
| **Angle Issue** | Cyan Triangle Marker | ▲ |
| **Snap Issue** | Blue Circle with White Border | ● |
| **Redundant Vertex** | Gray Square Marker | ■ |
| **Missing Junction** | Magenta Cross / Star Marker | ✱ |

---

## 8. Recommended Workflow

### Step 1: Select Features
- Use the standard ArcGIS Pro **Select Tool** to select the polygons or parcels you want to inspect.

### Step 2: Choose Target Layer
- Open the **Results** dockpane from the Ribbon.
- Select your target layer from the **Layer** dropdown (e.g., `Parcels`).

### Step 3: Run Validation
- Click the green **Run QC** button.
- Monitor the progress bar; hundreds of features will be validated in seconds.

### Step 4: Inspect & Fix Issues
- Browse the categorized tree view.
- Click **Zoom** beside any issue to navigate directly to it on the map.
- The error is highlighted in its distinctive color overlay.
- Use standard ArcGIS Pro editing tools (*Edit Vertices*, *Reshape*, *Merge*) to correct the issue.

### Step 5: Clear & Re-Validate
- Click **Clear Results** to remove graphic overlays.
- Click **Run QC** again to confirm that all issues are resolved (`Issues: 0`).

---

## 9. Frequently Asked Questions (FAQ) & Troubleshooting

#### Q1: Why does the Add-In report "No polygon features selected"?
**A:** The Add-In validates selected features. Ensure at least one polygon feature is selected using the standard Select Tool in ArcGIS Pro before clicking **Run QC**.

#### Q2: Does the Add-In delete or alter any data in my geodatabase?
**A:** No! The tool is strictly **Read-Only**. All calculations and graphics run in ArcGIS Pro runtime memory without issuing any edit operations or modifying records.

#### Q3: How do I remove the colored graphic symbols from the map after finishing?
**A:** Simply click **Clear Results** in the Results DockPane or Ribbon tab, and all temporary overlays will be removed immediately.

#### Q4: What is the difference between Display Cache and Live Query?
**A:**
- **Display Cache (Recommended):** Reads geometry instances already in display memory — up to 10x faster and ideal for day-to-day editing.
- **Live Query:** Queries geometries directly from the database; useful for extremely large datasets or complex server workflows.

#### Q5: Can I customize check tolerances (e.g., change spike angle threshold from 5° to 10°)?
**A:** Yes! Click the **Settings** button on the Ribbon to open the Settings DockPane. You can adjust any tolerance value, and your preferences are automatically saved on your machine.

---

<a name="دليل-المستخدم-باللغة-العربية"></a>
# دليل المستخدم باللغة العربية (Arabic User Guide)

## الفهرس
1. [مقدمة عن الأداة (Overview)](#1-مقدمة-عن-الأداة-overview-ar)
2. [متطلبات التشغيل والتثبيت (Installation & Setup)](#2-متطلبات-التشغيل-والتثبيت-installation--setup-ar)
3. [واجهة المستخدم والأشرطة (User Interface)](#3-واجهة-المستخدم-والأشرطة-user-interface-ar)
   - [شريط الأدوات الرئيسي (Ribbon Tab)](#أ-شريط-الأدوات-الرئيسي-geometry-qc-ribbon-ar)
   - [لوحة عرض النتائج (Results DockPane)](#ب-لوحة-عرض-النتائج-results-dockpane-ar)
   - [لوحة الإعدادات والتفاوتات (Settings DockPane)](#ج-لوحة-الإعدادات-والتفاوتات-settings-dockpane-ar)
4. [ميزة اختيار الطبقة المستهدفة (Target Layer Selection)](#4-ميزة-اختيار-الطبقة-المستهدفة-target-layer-selection-ar)
5. [الفحوصات الهندسية العشرة بالتفصيل (The 10 QC Checks)](#5-الفحوصات-الهندسية-العشرة-بالتفصيل-the-10-qc-checks-ar)
6. [نظام التحقق ومنع تكرار الأخطاء (Deduplication & Verification)](#6-نظام-التحقق-ومنع-تكرار-الأخطاء-deduplication--verification-ar)
7. [دليل ألوان ورموز الأخطاء على الخريطة (Graphic Symbols Legend)](#7-دليل-ألوان-ورموز-الأخطاء-على-الخريطة-graphic-symbols-legend-ar)
8. [خطوات العمل النموذجية (Recommended Workflow)](#8-خطوات-العمل-النموذجية-recommended-workflow-ar)
9. [الأسئلة الشائعة واستكشاف الأخطاء (FAQ & Troubleshooting)](#9-الأسئلة-الشائعة-واستكشاف-الأخطاء-faq--troubleshooting-ar)

---

<a name="1-مقدمة-عن-الأداة-overview-ar"></a>
## 1. مقدمة عن الأداة (Overview)

أداة **Geometry QC Analyzer** هي إضافة متقدمة واحترافية لبرنامج **ArcGIS Pro 3.x**، مصممة خصيصاً لمهندسي نظم المعلومات الجغرافية ومساحي الكاداستر والمخططين للتحقق اللحظي من سلامة طبقات المضلعات (Polygon Layers) واكتشاف العيوب الطوبولوجية وعيوب الرسم الرقمي دون الحاجة إلى إنشاء قواعد بيانات مكانية معقدة (Enterprise Geodatabase Topology).

### المميزات الرئيسية:
- **قراءة فقط (100% Read-Only):** لا تقوم الأداة بأي تعديل مباشر أو قفل للبيانات الأصلية.
- **أداء فائق بالذاكرة (In-Memory Processing):** تعتمد على فهارس مكانية ثنائية الأبعاد (`2D Spatial Index` و `Vertex Index`) لفحص آلاف المضلعات في أجزاء من الثانية.
- **دعم طبقات الويب (Feature Services):** فحص الطبقات المنشورة أونلاين مباشرة دون الحاجة لعمل Export إلى Local GDB.
- **دعم الوضعين الفاتح والداكن (Light & Dark Themes):** تصميم حديث وعصري متوافق تماماً مع إعدادات المظهر في ArcGIS Pro.
- **رسومات توضيحية مؤقتة (Dynamic Overlays):** إظهار مواقع الأخطاء مباشرة فوق الخريطة مع إمكانية التقريب الفوري (`Zoom to Issue`).

---

<a name="2-متطلبات-التشغيل-والتثبيت-installation--setup-ar"></a>
## 2. متطلبات التشغيل والتثبيت (Installation & Setup)

### متطلبات التشغيل:
- نظام تشغيل: **Windows 10 / 11 (x64)**
- برنامج: **ArcGIS Pro 3.0** أو أحدث (تم اختبارها واعتمادها حتى **ArcGIS Pro 3.4.x**)
- حزمة: **.NET 8.0 Windows Desktop Runtime**

### خطوات التثبيت:
1. تأكد من إغلاق برنامج ArcGIS Pro.
2. توجه إلى مجلد المشروع وانقر نقراً مزدوجاً على ملف الحزمة الجاهز:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. ستظهر لك نافذة تثبيت الإضافات الرسمية من Esri (`Esri ArcGIS Pro Add-In Utility`)، اضغط على **Install Add-In**.
4. ستظهر رسالة تأكيد نجاح التثبيت:
   > *"Installation Succeeded! The add-in has been installed successfully."*
5. افتح برنامج ArcGIS Pro وستجد تبويباً جديداً مخصصاً بالكامل باسم **Geometry QC** في الشريط العلوي (Ribbon).

---

<a name="3-واجهة-المستخدم-والأشرطة-user-interface-ar"></a>
## 3. واجهة المستخدم والأشرطة (User Interface)

<a name="أ-شريط-الأدوات-الرئيسي-geometry-qc-ribbon-ar"></a>
### أ. شريط الأدوات الرئيسي (Geometry QC Ribbon)

عند فتح تبويب **Geometry QC**، ستجد الأدوات مرتبة في مجموعتين:

| الأداة / الزر | الوصف الوظيفي |
| :--- | :--- |
| **Enable QC** | تفعيل أو تعطيل عمل الإضافة بالكامل. |
| **Run QC** | بدء تشغيل عملية فحص الجودة على المضلعات المحددة. |
| **Clear Results** | مسح جميع الأخطاء المسجلة وإزالة الرسومات المؤقتة من على الخريطة. |
| **Cancel** | إيقاف عملية الفحص الجارية فوراً وبأمان. |
| **Results** | فتح أو إظهار لوحة النتائج الجانبية (`Results DockPane`). |
| **Settings** | فتح لوحة إعدادات التفاوتات ومعايير الفحص (`Settings DockPane`). |

---

<a name="ب-لوحة-عرض-النتائج-results-dockpane-ar"></a>
### ب. لوحة عرض النتائج (Results DockPane)

تقع اللوحة على يمين شاشة ArcGIS Pro وتتكون من الأقسام التالية:

1. **الترويسة والعداد (Header & Counter):** تعرض عنوان اللوحة مع إحصائية سريعة لعدد المعالم المفحوصة وزمن المعالجة بالثانية، مع شارة رقمية بارزة (`Issues: N`).
2. **محدد الطبقة (Target Layer Selector):** قائمة منسدلة لاختيار الطبقة المراد فحصها، مع زر تحديث سريع (`↻`).
3. **شريط التقدم (Progress Indicator):** يظهر فقط أثناء المعالجة لبيان نسبة الإنجاز واسم الفحص الجاري حالياً مع زر إلغاء فوري (`Cancel`).
4. **شجرة الأخطاء المصنفة (Categorized Issue Tree):** تصنيف رئيسي لكل نوع فحص وبجانبه عدد الأخطاء، مع زر **Zoom** بجانب كل خطأ للانتقال الفوري وتكبير موقعه.
5. **لوحة التفاصيل (Issue Details Panel):** رسالة تشخيصية مفصلة توضح القياس الدقيق (المسافة، المساحة، أو الزاوية).
6. **شريط الأوامر السفلي:** زران سريعان لـ **Run QC** و **Clear Results**.

---

<a name="ج-لوحة-الإعدادات-والتفاوتات-settings-dockpane-ar"></a>
### ج. لوحة الإعدادات والتفاوتات (Settings DockPane)

تتيح للمستخدم ضبط كل تفاوتات وفحوصات الجودة لتلائم متطلبات مشروعه:

* **طريقة جلب البيانات (Data Source Mode):**
  - `Display Cache` *(الافتراضي والموصى به)*: قراءة المعالم المحددة في الذاكرة دون استعلام قواعد البيانات، وهو الخيار الأسرع.
  - `Live Query`: استعلام مباشر من قاعدة البيانات الجغرافية عبر محدد الاستعلام المكاني.
* **طبقات الخدمات (Enable for Service Layers):** خيار تشغيل الفحص على طبقات الويب والـ Feature Services.
* **وضع التنفيذ (Execution Mode):**
  - `Manual`: الفحص عند الضغط على زر Run QC فقط.
  - `Auto`: تشغيل الفحص تلقائياً بمجرد تغيير التحديد على الخريطة (Selection Change).
* **إعدادات وتفاوتات الفحوصات الـ 10:** تفعيل/تعطيل كل فحص وتحديد القيم الحرجة (بالسنتيمتر والمتر المربع والدرجات).
* **زر استعادة الإعدادات الافتراضية (Restore Defaults):** لإعادة ضبط جميع القيم على المعايير القياسية بضغطة واحدة.

---

<a name="4-ميزة-اختيار-الطبقة-المستهدفة-target-layer-selection-ar"></a>
## 4. ميزة اختيار الطبقة المستهدفة (Target Layer Selection)

تمنع تداخل الفحوصات بين الطبقات المختلفة:

```text
+-------------------------------------------------------------+
| Layer: [ Parcels                                      ▼ ] [↻] |
+-------------------------------------------------------------+
```

1. **كيفية الاستخدام:** اختر الطبقة المحددة (مثل `Parcels`) أو اختر **`All Polygon Layers`** لفحص جميع طبقات المضلعات المحددة معاً.
2. **زر التحديث (`↻`):** لتحديث قائمة الطبقات فوراً عند إضافة أو تسمية طبقات جديدة دون إعادة فتح اللوحة.
3. **الفائدة:** منع الأخطاء الزائفة بين طبقات من المفترض أن تتداخل (مثل قطع الأراضي مع الأحياء أو مناطق استخدامات الأراضي).

---

<a name="5-الفحوصات-الهندسية-العشرة-بالتفصيل-the-10-qc-checks-ar"></a>
## 5. الفحوصات الهندسية العشرة بالتفصيل (The 10 QC Checks)

| # | اسم الفحص (Check Name) | الكود الداخلي | التفاوت الافتراضي | الوصف الهندسي وطبيعة الخطأ |
| :-: | :--- | :--- | :-: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | فحص صحة المضلع طوبولوجياً طبقاً لمواصفات OGC/Esri. يكتشف التقاطعات الذاتية (Bow-tie)، الحلقات المعكوسة، الحلقات التي تحتوي على أقل من 3 نقاط، والإحداثيات غير المعرفة (NaN). |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.0001 m²` | يكتشف المساحات المشتركة المتداخلة بين المضلعات. يقوم بتفكيك التداخلات المتعددة وعرض مساحتها بوحدات واضحة (`m²` أو `cm²`) مع استبعاد شوائب تلاصق الحدود. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | يكتشف المضلعات المتطابقة هندسياً بنسبة 100% المرسومة فوق بعضها بالخطأ (Coincident Polygons)، ويمنع تكرار تقريرها كـ Overlap. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.001 m²` | يكتشف الفجوات والثقوب الهوائية غير المرئية المحصورة والمغلقة تماماً بين المضلعات المتجاورة الناتجة عن أخطاء الرسم. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | يكتشف المعالم ذات السجل الواحد التي تتكون من أكثر من مضلع منفصل (PartCount > 1) في قواعد البيانات التي تتطلب مضلعات أحادية (Singlepart). |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `10.0 cm` | يكتشف الأضلاع أو الأجزاء متناهية الصغر التي يقل طولها عن الحد المسموح، مع استبعاد النقاط المتطابقة لحصر الأخطاء في عيوب الرسم الفعلية. |
| **7** | **Angle Issue** | `CHK_ANGLE` | `5.0°` | يكتشف الزوايا الحادة والشاذة جداً (Spikes) التي تقل عن الدرجة المحددة والناتجة عادةً عن نقرات خاطئة أثناء الرسم بالماوس. |
| **8** | **Snap Issue** | `CHK_SNAP` | `1.0 cm` | يكتشف الرؤوس والنقاط المتقاربة غير الملتقطة. تُعرض المسافات تحت السنتيمتر بالملليمتر (`mm`) بدقة فائقة لمنع تقريب المسافات غير الملتقطة إلى `0.00 cm` مع استبعاد النقاط المتطابقة تماماً. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `179.9°` | يكتشف النقاط الزائدة على استقامة الخط (180°). وتعمل ميزة **حماية نقاط الربط (Junction Guard)** على استثناء النقاط المشتركة فقط إذا كان المضلع المجاور ينكسر أو يرتبط عندها برأس حقيقي (زاوية < 180°)، أما إذا وُجد رأسان متطابقان فوق بعض وكلاهما على استقامة الخط (تقترب الزاوية لكل منهما من 180°) فيتم اعتبارهما خطأ رأس زائد (Redundant Vertex) على كلا المضلعين. |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `1.0 cm` | يكتشف العقد المفقودة في الوصلات التبادلية (T-Junctions) عندما تلامس نقطة من مضلع ضلع مضلع مجاور دون وجود رأس مشترك ملتقط عليه. |

---

<a name="6-نظام-التحقق-ومنع-تكرار-الأخطاء-deduplication--verification-ar"></a>
## 6. نظام التحقق ومنع تكرار الأخطاء (Deduplication & Verification)

1. **دقة القياسات والمسافات متناهية الصغر (Sub-Millimeter Precision):**
   - في فحص الـ `Snap`: الرؤوس التي لم تلتقط وتفصل بينها مسافات صغيرة جداً (أقل من سنتيمتر أو تحت الملليمتر) لا يتم تجاهلها أو تقريبها إلى `0.00 cm`، بل تُقاس وتُعرض بوحدة الملليمتر (`mm`) بدقة تصل إلى 3 خانات عشرية (مثل `0.04 mm`). يتم استثناء الرؤوس المتطابقة تماماً فقط (إحداثيات متماثلة هندسياً).
   - في فحص الـ `Overlap`: استبعاد الشرائح المجهرية الناتجة عن التقريب الحسابي للحدود المتلاصقة.
2. **منع التكرار التبادلي والمكاني (Pairwise & Spatial Deduplication):**
   - للأخطاء التي تحدث بين مضلعين (مثل Overlap و Snap)، يتم توحيد المعرفات `[minOID, maxOID]` ليظهر الخطأ مرة واحدة فقط للمستخدم.
   - **دمج نقاط الوصلات المفقودة في نفس الموقع (Missing Junction Deduplication):** دمج الأخطاء في نقطة الالتقاء الواحدة لتظهر مرة واحدة فقط على القطعة المستهدفة موضحة أرقام القطع الملامسة لها.
3. **حماية نقاط الربط الطوبولوجية في (Redundant Vertex):**
   - عندما تكون النقطة بزاوية 180° ولكنها تمثل زاوية لقطعة أرض مجاورة (T-Junction)، يتعرف النظام عليها كنقطة ربط أساسية لطوبولوجيا الحي ويستبعدها تلقائياً من قائمة الأخطاء لمنع حذفها بالخطأ.
4. **معالجة حلقة المضلع المغلقة (Closed Ring Handling):**
   - عدم تكرار فحص نقطة بداية ونهاية المضلع (Vertex 0 و Vertex N-1) كنقطتين منفصلتين.

---

<a name="7-دليل-ألوان-ورموز-الأخطاء-على-الخريطة-graphic-symbols-legend-ar"></a>
## 7. دليل ألوان ورموز الأخطاء على الخريطة (Graphic Symbols Legend)

عند انتهاء الفحص بنجاح، ترسم الإضافة طبقات مؤقتة فوق الخريطة بألوان مميزة لكل نوع خطأ:

| نوع الخطأ (Check Name) | اللون / الشكل على الخريطة | الرمز التوضيحي |
| :--- | :--- | :---: |
| **Invalid Geometry** | صليب بنفسجي عريض (Purple Cross) | ✖ |
| **Overlap** | مساحة حمراء شفافة بحدود حمراء ساطعة (Red Fill & Outline) | ▨ |
| **Duplicate Geometry** | مضلع مهشر باللون النبيتي الغامق (Dark Red Hatch) | ▦ |
| **Enclosed Gap** | مضلع أصفر ساطع بلون الكهرمان (Bright Yellow/Amber) | ▨ |
| **Multi-Part Feature** | معين برتقالي اللون (Orange Diamond) | ◆ |
| **Short Segment** | خط أزرق سماوي ساطع عريض (Vivid Blue Line) | ━ |
| **Angle Issue** | مثلث سماوي مشرق (Cyan Triangle) | ▲ |
| **Snap Issue** | دائرة زرقاء بحواف بيضاء (Blue Circle) | ● |
| **Redundant Vertex** | مربع رمادي اللون (Gray Square) | ■ |
| **Missing Junction** | نجمة أو علامة X فوشيا ماجنتا (Magenta Star/Cross) | ✱ |

---

<a name="8-خطوات-العمل-النموذجية-recommended-workflow-ar"></a>
## 8. خطوات العمل النموذجية (Recommended Workflow)

### الخطوة 1: تحديد المعالم
- استخدم أداة التحديد العادية في ArcGIS Pro (`Select Tool`) لتحديد المضلعات المطلوب فحصها.

### الخطوة 2: اختيار الطبقة
- افتح لوحة النتائج بالضغط على زر **Results** من شريط الأدوات، ومن القائمة المنسدلة **Layer** اختر الطبقة المستهدفة (مثلاً: `Parcels`).

### الخطوة 3: بدء الفحص
- اضغط على زر **Run QC** الأخضر، وراقب شريط التقدم.

### الخطوة 4: مراجعة وتصحيح الأخطاء
- تصفح شجرة الأخطاء بحسب التصنيف، واضغط على زر **Zoom** بجانب أي خطأ للانتقال الفوري وتكبير مكانه على الخريطة.
- قم بتصحيح الرسم باستخدام أدوات التحرير المعتادة في ArcGIS Pro (`Edit Vertices`, `Reshape`, `Merge`).

### الخطوة 5: إعادة الفحص والمسح
- اضغط على **Clear Results** لمسح الرسومات المؤقتة، ثم اضغط على **Run QC** مرة أخرى للتأكد من زوال الأخطاء بالكامل (`Issues: 0`).

---

<a name="9-الأسئلة-الشائعة-واستكشاف-الأخطاء-faq--troubleshooting-ar"></a>
## 9. الأسئلة الشائعة واستكشاف الأخطاء (FAQ & Troubleshooting)

#### س1: لماذا يظهر لي "No polygon features selected"؟
**ج:** الإضافة تفحص المضلعات المحددة على الخريطة. تأكد من تحديد مضلع واحد على الأقل باستخدام أداة التحديد في ArcGIS Pro قبل الضغط على Run QC.

#### س2: هل تقوم الأداة بحذف أو تعديل أي شيء في بياناتي؟
**ج:** نهائياً! الأداة مبنية بمبدأ **Read-Only** الصارم؛ كل العمليات الحسابية والرسومات تجري في الذاكرة المؤقتة دون إجراء أي تعديل على جداول البيانات.

#### س3: كيف أقوم بإخفاء الرسومات الملونة من على الخريطة بعد الانتهاء؟
**ج:** ببساطة اضغط على زر **Clear Results** في لوحة النتائج أو في شريط الأدوات العلوي.

#### س4: ما الفرق بين Display Cache و Live Query في الإعدادات؟
**ج:** 
- **Display Cache (الموصى به):** يقرأ الأشكال المخزنة بالفعل في ذاكرة العرض النشطة، وهو أسرع بنسبة تصل إلى 10 أضعاف.
- **Live Query:** يقوم بعمل استعلام مباشر من قاعدة البيانات، ويُفضل استخدامه إذا كانت المعالم معقدة جداً أو عند فحص أعداد هائلة من السجلات.

#### س5: كيف يمكنني تعديل حساسية الفحوصات (مثلاً تغيير زاوية الخطأ من 5 درجات إلى 10 درجات)؟
**ج:** اضغط على زر **Settings** من شريط الأدوات؛ يمكنك تغيير أي رقم أو تفاوت، وسيتم حفظ التعديلات تلقائياً في جهازك للمرات القادمة.

---
*Developed with ❤️ for the GIS & Geospatial Community.*
