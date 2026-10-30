# ArcGIS Pro Geometry QC Analyzer Add-In
# إضافة فحص الجودة الهندسية والمكانية لبرنامج ArcGIS Pro

<p align="center">
  <img src="Images/QC_Ribbon_Preview.png" alt="Geometry QC Ribbon & UI Preview" width="850" />
</p>

<p align="center">
  <strong>High-Performance, In-Memory Geospatial Topology & Cadastral Quality Control Suite for ArcGIS Pro</strong><br>
  <em>حزمة فحص ومراقبة الجودة الهندسية والطوبولوجية فائقة السرعة للمخططات العقارية والخرائط الرقمية في ArcGIS Pro</em>
</p>

<p align="center">
  <a href="#english-overview">English Documentation</a> •
  <a href="#نظرة-عامة-باللغة-العربية">الوصف باللغة العربية</a> •
  <a href="USER_GUIDE.md">User Guide / دليل المستخدم</a> •
  <a href="QC_CHECKS_LOGIC.md">Checks Logic & Math / المنطق الرياضي</a> •
  <a href="GeometryQC_Rebuild_Prompt.md">Rebuild Prompt / دليل البناء</a>
</p>

---

<a name="english-overview"></a>
## English Overview

**Geometry QC Analyzer** is a native, professional .NET 8 ArcGIS Pro add-in engineered for GIS professionals, cadastral cartographers, and land administration surveyors. Developed with the assistance of **Advanced AI**, it evaluates polygon and parcel datasets against rigorous geometric, topological, and cartographic rules in real time.

### 🌟 Key Highlights
* **⚡ 100% In-Memory & Read-Only:** Validates selected features on the fly without schema locks, temporary feature classes, or database modifications.
* **🎯 Display Cache Data Source & Manual Execution:** Ultra-fast screen-rendered geometry cache for selected features within the current viewport extent. Running is manual and controlled via the **Run QC** button.
* **🛡️ Max Features Safety Cap:** Configurable protection threshold (default: 500 features) preventing browser freezing or memory exhaustion on accidental massive selections.
* **💾 Per-Project Target Layer Persistence:** Remembers chosen target layers (including "All Polygon Layers") per ArcGIS Pro project, restoring them automatically upon reopening.
* **✅ Clear "No errors found ✓" Status:** Distinguishes verified zero-error runs with clear feedback while keeping analyzed feature counts and execution times visible.
* **🧭 Interactive Results DockPane:** Categorized issue tree with categorized counts ("3 errors · 2 warnings") and instant **Zoom** to any issue.
* **🎨 Dual Map Display Modes:** Choose between fast dynamic screen cache overlays or a persistent Graphics Layer inside a `QC_Geometry_Errors` Group Layer in the Map Contents pane, with an option to toggle warning markers on/off.
* **💾 Geodatabase Error Export (Export to GDB):** One-click export of all detected issues into a dedicated `QC_Errors` Feature Dataset inside the project's Default Geodatabase, categorized by error geometry type with complete audit metadata.
* **📐 Sub-Millimeter Measurement Precision:** Formats distances below 1 cm in millimeters (`mm`) down to sub-millimeter fractions (e.g., `0.35 mm`), preventing misleading `0.00 cm` labels.
* **🛡️ Curve-Aware Analysis & Arc–Line Warning Option:** Validates parametric curves (Circular Arcs & Cubic Béziers) and protects boundary nodes via **Junction Guard**. Includes an optional sub-check for near-tangent arc–line collinear junctions.
* **🌓 Full Light & Dark Theme Support:** Modern WPF UI styled to match ArcGIS Pro's native theme.

---

### 🔍 The 10 QC Checks at a Glance

| # | Check Name | Check ID | Default Tolerance | Description |
| :-: | :--- | :--- | :-: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | Detects self-intersections (bow-ties), inverted rings, NaN coordinates, and OGC/Esri non-simple polygons. |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.00001 m²` | Detects overlapping regions between polygon pairs with area measurement (`m²` or `cm²`) and pair deduplication. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | Identifies 100% coincident identical polygons stacked on top of each other, suppressing duplicate overlap reporting. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.00001 m²` | Discovers hidden, enclosed void gaps completely bounded by neighboring polygons. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | Flags single-record features consisting of multiple disconnected polygon rings (`PartCount > 1`). |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `15.0 cm` | Detects micro-edges and extremely short polygon boundaries shorter than the user tolerance. |
| **7** | **Angle Issue** | `CHK_ANGLE` | `30.0°` | Identifies severe sharp spikes, narrow needles, and digitizing click artifacts. |
| **8** | **Snap Issue** | `CHK_SNAP` | `15.0 cm` | Detects unsnapped near-coincident vertices with sub-millimeter precision (`mm`), excluding true identical points. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `175.0°` | Detects superfluous collinear vertices along straight edges and redundant intermediate vertices between continuous curves (Curve → Vertex → Curve). Analyzes segment geometry (type, center, radius, tangents) while preserving valid transitions and protecting topological junctions via **Junction Guard**. Optional sub-feature: flags near-tangent arc–line junctions as warnings. |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `10.0 cm` | Flags T-junction contact points where a polygon vertex touches a neighboring polygon edge without a matching snapped node (cadastral standard). |
| **11**| **Flat / Pseudo-Arc** | `CHK_FLAT_PSEUDO_ARC` | `0.01 cm` / `10.0 cm²` | *(Optional, Disabled)* Detects true circular arcs with negligible sagitta ($\le 0.01\text{ cm} / 0.1\text{ mm}$) or enclosed segment area ($\le 10.0\text{ cm}^2$) that functionally represent straight lines. |

For full mathematical equations and cadastral rationale, see [QC_CHECKS_LOGIC.md](QC_CHECKS_LOGIC.md).

---

### 🚀 Quick Start & Installation

> [!IMPORTANT]
> **Always close ArcGIS Pro before installing or updating add-ins.** ArcGIS Pro registers add-ins into memory strictly during application startup and does not support hot-reloading.

#### Method 1: Pre-built Add-in Package (Recommended)
1. Close **ArcGIS Pro**.
2. Double-click the ready-to-use package in the repository root:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. Click **Install Add-In** in the Esri installation prompt.
4. Launch ArcGIS Pro. The tool will appear in the dedicated **Geometry QC** Ribbon tab and on the **Add-In** tab.

#### Method 2: Build from Source
1. Close **ArcGIS Pro**.
2. Run the .NET build command:
   ```powershell
   dotnet build -c Release
   ```
3. The build automatically packages `GeometryQCAddIn.esriAddinX` in the project root directory.
4. Double-click `GeometryQCAddIn.esriAddinX` to install, or deploy it to your preferred ArcGIS Pro AddIns path.
5. Launch ArcGIS Pro.

### 📦 System Requirements:
* OS: **Windows 10 / 11 (x64)**
* GIS: **ArcGIS Pro 3.3.x, 3.4.x, or later** (.NET 8 SDK environment)
* Runtime: **.NET 8.0-windows Desktop Runtime**

---

<a name="نظرة-عامة-باللغة-العربية"></a>
## نظرة عامة باللغة العربية

**أداة فحص الجودة الهندسية والمكانية (Geometry QC Analyzer)** هي إضافة برمجية أصلية لبيئة **ArcGIS Pro** مبنية على منصة **.NET 8**. تم تصميمها وتطويرها بمساعدة تقنيات **الذكاء الاصطناعي المتقدم (Advanced AI)** لخدمة مهندسي المساحة، إدارات الأراضي، ومختصي نظم المعلومات الجغرافية لتدقيق واكتشاف العيوب الهندسية والطوبولوجية في المخططات العقارية والخرائط الرقمية لحظياً وبأعلى معايير الدقة.

### 🌟 أبرز المميزات الفنية
* **⚡ فحص فوري فائق السرعة بالذاكرة (100% In-Memory):** فحص فوري للمعالم المحددة في وضع القراءة فقط، دون إنشاء ملفات مؤقتة على القرص أو حجز أقفال على قواعد البيانات.
* **🎯 مصدر بيانات كاش العرض وتشغيل يدوي:** اعتماد كاش العرض (Display Cache) مصدراً وحيداً للبيانات للمعالم المحددة داخل نطاق الشاشة، مع تشغيل يدوي عبر زر **Run QC**.
* **🛡️ حد أقصى لعدد المعالم المحددة (Safety Cap):** حماية أداء البرنامج بمنع معالجة أكثر من 500 معلم محدد كحد افتراضي (مع إمكانية تعديل الحد في الإعدادات) لتفادي التجميد أو استهلاك الذاكرة.
* **💾 حفظ اختيار الطبقة لكل مشروع (Per-Project Persistence):** حفظ الطبقة المختارة (بما في ذلك "All Polygon Layers") داخل ملف مشروع ArcGIS Pro واستعادتها تلقائياً عند إعادة فتح المشروع دون رسائل تنبيهية مزعجة.
* **✅ إشعار واضح عند خلو البيانات من الأخطاء ("No errors found ✓"):** إظهار رسالة واضحة تؤكد نجاح الفحص وعدم وجود أخطاء مع بقاء عدد المعالم التي تم فحصها وزمن التحليل ظاهراً.
* **🧭 لوحة نتائج تفاعلية:** شجرة تصنيفية للأخطاء مع تصنيف إجمالي المشاكل ("3 errors · 2 warnings") وزر **Zoom** للانتقال الفوري وتكبير موقع كل عيب على الخريطة.
* **🎨 خياران لعرض الرسومات على الخريطة:** إمكانية الاختيار بين كاش العرض المؤقت على الشاشة (Screen Cache Overlay) أو إنشاء طبقة رسومات (Graphics Layer) داخل Group Layer باسم `QC_Geometry_Errors` في محتويات الخريطة، مع خيار إظهار أو إخفاء رسومات التحذيرات (Warnings).
* **💾 تصدير الأخطاء لقاعدة بيانات جغرافية (Export to GDB):** إمكانية تصدير كافة الأخطاء المكتشفة بضغطة زر إلى Feature Dataset مستقل باسم `QC_Errors` داخل الـ Default Geodatabase للمشروع، مقسمة إلى Feature Classes بحسب نوع وشكل الخطأ مع كامل البيانات الوصفية وإضافتها للخريطة تلقائياً.
* **📐 دقة فائقة تحت الملليمتر:** قياس المسافات الأقل من 1 سنتيمتر بوحدة الملليمتر (`mm`) بدقة حتى 3 و4 خانات عشرية (مثل `0.35 mm`) لمنع الرسائل الوهمية `0.00 cm`.
* **🛡️ حماية نقاط الربط (Junction Guard) وتدقيق نقاط التماس بين الأقواس والخطوط:** تمييز ذكي بين النقاط الزائدة على الاستقامة ونقاط التقاء المضلعات المجاورة، مع خيار تدقيق إضافي تحذيري لنقاط التماس بين المنحنيات والخطوط المستقيمة (Arc–Line Junctions).
* **🌓 دعم كامل للمظهرين الفاتح والداكن (Light & Dark Themes):** تصميم عصري يتكيف تلقائياً مع مظهر ArcGIS Pro.

---

### 🔍 جدول الفحوصات العشرة

| # | اسم الفحص (Check Name) | الكود الداخلي | التفاوت الافتراضي | الوصف الهندسي |
| :-: | :--- | :--- | :-: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | كشف التقاطعات الذاتية (Bow-tie)، الحلقات غير المغلقة، الإحداثيات الشاذة (NaN)، والمضلعات غير القياسية. |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.00001 m²` | كشف المساحات المتداخلة بين المضلعات وتفكيك التداخلات المتعددة مع حساب المساحة بدقة بوحدة `m²` أو `cm²`. |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | كشف المضلعات المتطابقة هندسياً بنسبة 100% المرسومة فوق بعضها ومنع احتسابها كتداخلات مكررة. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.00001 m²` | كشف الفجوات والثقوب الهوائية المغلقة والمحصورة تماماً بين المضلعات المتجاورة. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | كشف المعالم متعددة الأجزاء (Multipart) في الطبقات التي تشترط مضلعات أحادية مفردة (Singlepart). |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `15.0 cm` | كشف الأضلاع والحدود متناهية الصغر التي يقل طولها عن الحد المسموح به. |
| **7** | **Angle Issue** | `CHK_ANGLE` | `30.0°` | كشف الزوايا الحادة والشاذة (Spikes) الناتجة عن أخطاء الرسم أو نقرات الماوس. |
| **8** | **Snap Issue** | `CHK_SNAP` | `15.0 cm` | كشف الرؤوس المتقاربة غير الملتقطة بدقة أجزاء الملليمتر (`mm`) مع استبعاد النقاط المتطابقة تماماً. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `175.0°` | كشف الرؤوس الزائدة على خط الاستقامة (180°) والرؤوس غير الضرورية الواقعة على المنحنيات المتصلة، مع حماية نقاط الربط (Junction Guard) وإمكانية تفعيل خيار التحذير لنقاط التماس بين المنحنيات والخطوط (Arc–Line). |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `10.0 cm` | كشف العقد المفقودة في الوصلات التبادلية (T-Junctions) عندما تلامس نقطة ضلع مضلع مجاور دون التقاط رأس مشترك. |
| **11**| **Flat / Pseudo-Arc** | `CHK_FLAT_PSEUDO_ARC` | `0.01 cm` / `10.0 cm²` | *(اختياري - معطل افتراضياً)* كشف الأقواس الدائرية الوهمية والمنبسطة ذات سهم القوس الضئيل جداً ($\le 0.01\text{ cm} / 0.1\text{ mm}$) أو المساحة المحصورة ($\le 10.0\text{ cm}^2$) التي تمثل خطوطاً مستقيمة عملياً. |

للاطلاع على الشرح الرياضي والمساحي المفصل، راجع: [QC_CHECKS_LOGIC.md](QC_CHECKS_LOGIC.md).

---

### 🚀 التثبيت والتشغيل السريع (Installation)

> [!IMPORTANT]
> **يجب إغلاق برنامج ArcGIS Pro بالكامل قبل التثبيت أو التحديث.** برنامج ArcGIS Pro يقرأ مجلد الإضافات ويقوم بتحميلها فقط لحظة إقلاع البرنامج (Startup)، ولا يدعم التحديث المباشر أثناء عمله (No Hot-Reloading).

#### الطريقة 1: التثبيت اليدوي من الحزمة الجاهزة
1. تأكد من إغلاق **ArcGIS Pro**.
2. انقر نقراً مزدوجاً (Double-click) على ملف الحزمة في المجلد الرئيسي:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. في نافذة معالج التثبيت من Esri، اضغط على **Install Add-In**.
4. افتح برنامج ArcGIS Pro وستجد شريط الأداة ظهر في تبويب **Geometry QC** وأيضاً داخل تبويب **Add-In**.

#### الطريقة 2: البناء من الكود المصدري
1. تأكد من إغلاق **ArcGIS Pro**.
2. قم بالبناء عبر سطر الأوامر:
   ```powershell
   dotnet build -c Release
   ```
3. يقوم الـ Build تلقائياً بإنشاء حزمة `GeometryQCAddIn.esriAddinX` وحفظها في المجلد الرئيسي للمشروع.
4. انقر نقراً مزدوجاً على الحزمة لتثبيتها، أو انسخها لمجلد الإضافات المفضل لديك.
5. افتح برنامج ArcGIS Pro.

### 📦 متطلبات التشغيل:
* نظام التشغيل: **Windows 10 / 11 (x64)**
* برنامج نظم المعلومات: **ArcGIS Pro 3.3.x أو 3.4.x أو أحدث** (بيئة Pro 3.x المبنية على .NET 8)
* بيئة التشغيل: **.NET 8.0-windows Desktop Runtime**

---

### 🤖 الذكاء الاصطناعي والتطوير (AI Attribution)
تم تطوير وتصميم هذه الأداة بمساعدة تقنيات **الذكاء الاصطناعي (AI)** لتقديم حلول هندسية عملية ومبتكرة تخدم مجتمع نظم المعلومات الجغرافية والمساحة والتخطيط العمراني.

---

### 📚 الوثائق وروابط المشروع (Documentation & Links)
* 📖 [دليل المستخدم المفصل (User Guide - English & Arabic)](USER_GUIDE.md)
* 📐 [المنطق الرياضي والهندسي للفحوصات (Checks Logic & Mathematics)](QC_CHECKS_LOGIC.md)
* 🧠 [برومت إعادة البناء بالكامل (Rebuild Prompt - English & Arabic)](GeometryQC_Rebuild_Prompt.md)
* 🔗 مستودع المشروع على GitHub: [https://github.com/aboelmagd1/Geometry-QC-Analyzer](https://github.com/aboelmagd1/Geometry-QC-Analyzer)

---
*Developed with ❤️ for the GIS & Geospatial Community.*
