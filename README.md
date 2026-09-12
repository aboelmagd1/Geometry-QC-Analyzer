# Geometry QC Analyzer for ArcGIS Pro

<p align="center">
  <img src="Images/GenericPlay32.png" alt="Geometry QC Analyzer Logo" width="80" />
</p>

<h3 align="center">High-Performance In-Memory Geometry & Topology Quality Control Add-In for ArcGIS Pro</h3>

<p align="center">
  <a href="#english">English</a> •
  <a href="#arabic">العربية</a> •
  <a href="USER_GUIDE.md">User Guide / دليل المستخدم</a> •
  <a href="GeometryQC_Rebuild_Prompt.md">AI Rebuild Prompt / برومت الذكاء الاصطناعي</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/ArcGIS%20Pro-3.0%2B%20%7C%203.4.x-007AC2?style=flat-square&logo=esri" alt="ArcGIS Pro" />
  <img src="https://img.shields.io/badge/.NET-8.0--windows-512BD4?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp" alt="C# 12" />
  <img src="https://img.shields.io/badge/License-MIT-green.style=flat-square" alt="License" />
  <img src="https://img.shields.io/badge/Built%20with-AI%20Assistance-FF6F00?style=flat-square&logo=openai" alt="AI Assisted" />
</p>

---

<a name="english"></a>
## English Overview

**Geometry QC Analyzer** is a lightning-fast, 100% read-only Quality Control (QC) Add-in for **ArcGIS Pro 3.x**. It allows GIS specialists, cartographers, cadastral surveyors, and urban planners to validate polygon geometries on the fly — **without building complex Geodatabase Topologies, defining topology rules, or exporting Feature Services to local geodatabases!**

Developed with the assistance of **Advanced AI**, it evaluates selected features or active extent polygons against 10 critical spatial and geometric checks in fractions of a second using an in-memory 2D spatial and vertex grid index.

### 🌟 Key Features

* **⚡ Zero Geodatabase Overhead:** Works directly on selected polygon layers and live Web Feature Services — no need to create Feature Datasets or Geodatabase Topologies.
* **🔒 100% Safe & Read-Only:** Never modifies, locks, or edits your source geodatabase records.
* **🎯 10 Instant QC Checks:** Comprehensive detection of overlaps, gaps, sub-millimeter snap issues, redundant collinear vertices, missing T-junctions, spikes, and invalid geometries.
* **🧭 Interactive Results DockPane:** Categorized tree view with one-click **Zoom to Issue**, feature OID tracking, and detailed diagnostic metrics.
* **🎨 Color-Coded Map Overlays:** Temporary graphic overlays on the map highlighting each error type with distinct, vibrant colors.
* **📐 Sub-Millimeter Precision:** Reports distances below 1 cm in millimeters (`mm`) up to 3–4 decimal places (e.g., `0.04 mm`), eliminating misleading `0.00 cm` ghost messages.
* **🌓 Seamless Light & Dark Theme Support:** Dynamic Navy / Cyan / Turquoise design system tailored to match ArcGIS Pro's interface.

---

### 🔍 The 10 QC Checks at a Glance

| # | Check Name | Check ID | Default Tolerance | Description |
| :-: | :--- | :--- | :-: | :--- |
| **1** | **Invalid Geometry** | `CHK_INVALID_GEOM` | — | Detects self-intersections, bow-ties, unclosed loops, NaN coordinates, and OGC/Esri non-simple polygons. |
| **2** | **Overlap** | `CHK_OVERLAP` | `0.0001 m²` | Detects overlapping areas between polygons. Decomposes multipart intersections and reports accurate square meters or cm². |
| **3** | **Duplicate Geometry** | `CHK_DUPLICATE` | — | Detects 100% coincident identical polygons, avoiding redundant overlap reporting. |
| **4** | **Enclosed Gap** | `CHK_GAP` | `0.001 m²` | Detects enclosed, hidden air gaps and sliver holes formed between adjacent polygons. |
| **5** | **Multi-Part Feature** | `CHK_MULTIPART` | — | Identifies multipart polygon features in datasets requiring strictly singlepart geometries. |
| **6** | **Short Segment** | `CHK_SHORT_SEG` | `10.0 cm` | Detects micro-edges and extremely short polygon boundaries shorter than the user tolerance. |
| **7** | **Angle Issue** | `CHK_ANGLE` | `5.0°` | Identifies severe sharp spikes, narrow needles, and digitizing click artifacts. |
| **8** | **Snap Issue** | `CHK_SNAP` | `1.0 cm` | Detects unsnapped near-coincident vertices with sub-millimeter precision (`mm`), excluding true identical points. |
| **9** | **Redundant Vertex** | `CHK_REDUNDANT` | `179.9°` | Detects superfluous collinear vertices along straight edges. If two coincident vertices both approach 180°, both are flagged as redundant errors. |
| **10**| **Missing Junction** | `CHK_JUNCTION` | `1.0 cm` | Flags T-junction contact points where a polygon vertex touches a neighboring polygon edge without a matching snapped node. |

---

### 🚀 Quick Start & Installation

1. Close **ArcGIS Pro** if running.
2. Download the pre-built Add-in package from releases or the root folder:
   ```text
   GeometryQCAddIn.esriAddinX
   ```
3. Double-click `GeometryQCAddIn.esriAddinX` and click **Install Add-In** in the Esri utility.
4. Launch ArcGIS Pro. A new **Geometry QC** tab will appear in the top Ribbon!

### 💻 Typical Workflow

1. **Select:** Use the standard ArcGIS Pro *Select Tool* to select the polygons you want to validate.
2. **Choose Layer:** Open the **Results** dockpane and choose your target layer (or *All Polygon Layers*).
3. **Run:** Click **Run QC** (Green Play button).
4. **Inspect:** Browse detected issues by category, click **Zoom** to navigate directly to each error, and fix using standard Pro edit tools.
5. **Clear:** Click **Clear Results** to remove graphic overlays once done.

---

<a name="arabic"></a>
## ملخص باللغة العربية (Arabic Overview)

أداة **Geometry QC Analyzer** هي إضافة برمجية (Add-in) فائقة السرعة لبرنامج **ArcGIS Pro 3.x**، صُممت خصيصاً لمراجعة الجودة الهندسية والطوبولوجية لطبقات المضلعات (Polygons) في لحظات — **دون الحاجة لبناء Geodatabase Topology، أو كتابة قواعد وقوانين معقدة، ودون الحاجة لتصدير بيانات الـ Feature Services إلى Local GDB!**

تم تطوير الأداة بمساعدة **الذكاء الاصطناعي المتقدم (AI)** لتمكين المستخدم من فحص المعالم المحددة أو المعالم الظاهرة على الشاشة وفقاً لـ 10 فحوصات هندسية دقيقة في أجزاء من الثانية بالاعتماد على الفهرسة المكانية بالذاكرة (`In-Memory Spatial & Vertex Grid`).

### 🌟 أهم المميزات:

* **⚡ بدون روتين الـ Topology:** تعمل مباشرة على الطبقات المحلية وعلى طبقات الويب والـ Feature Services دون الحاجة لأي Export.
* **🔒 قراءة فقط 100% (Read-Only):** أداة آمنة تماماً لا تعدل ولا تقفل قواعد البيانات الأصلية.
* **🎯 10 فحوصات هندسية شاملة:** كشف التداخلات، الفجوات الهوائية، عدم الالتقاط حتى أجزاء الملليمتر، الرؤوس الزائدة على الاستقامة، العقد التبادلية المفقودة، والزوايا الحادة الشاذة.
* **🧭 لوحة نتائج تفاعلية:** شجرة تصنيفية للأخطاء مع زر **Zoom** للانتقال الفوري وتكبير موقع كل عيب على الخريطة.
* **🎨 رسومات توضيحية ملونة:** تمييز بصري فوق الخريطة بألوان مميزة لكل نوع خطأ (تداخل بالأحمر، عدم التقاط بالأزرق، فجوات بالأصفر... إلخ).
* **📐 دقة فائقة تحت الملليمتر:** قياس المسافات الأقل من 1 سنتيمتر بوحدة الملليمتر (`mm`) بدقة حتى 3 و4 خانات عشرية (مثل `0.04 mm`) لمنع الرسائل الوهمية `0.00 cm`.
* **🌓 دعم كامل للمظهرين الفاتح والداكن (Light & Dark Themes):** تصميم عصري يتكيف تلقائياً مع مظهر ArcGIS Pro.

---

### 📦 متطلبات التشغيل:
* نظام التشغيل: **Windows 10 / 11 (x64)**
* برنامج نظم المعلومات: **ArcGIS Pro 3.0** أو أحدث (تم الاختبار على **ArcGIS Pro 3.4.x**)
* بيئة التشغيل: **.NET 8.0-windows Desktop Runtime**

---

### 🤖 الذكاء الاصطناعي والتطوير (AI Attribution)
تم تطوير وتصميم هذه الأداة بمساعدة تقنيات **الذكاء الاصطناعي (AI)** لتقديم حلول هندسية عملية ومبتكرة تخدم مجتمع نظم المعلومات الجغرافية والمساحة والتخطيط العمراني.

---

### 📚 الوثائق وروابط المشروع (Documentation & Links)
* 📖 [دليل المستخدم المفصل (User Guide - English & Arabic)](USER_GUIDE.md)
* 🧠 [برومت إعادة البناء بالكامل (Rebuild Prompt - English & Arabic)](GeometryQC_Rebuild_Prompt.md)
* 🔗 مستودع المشروع على GitHub: [https://github.com/aboelmagd1/Geometry-QC-Analyzer](https://github.com/aboelmagd1/Geometry-QC-Analyzer)

---
*Developed with ❤️ for the GIS & Geospatial Community.*
