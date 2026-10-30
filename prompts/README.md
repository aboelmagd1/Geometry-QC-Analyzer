# Geometry QC Analyzer — Prompts Directory & Engineering Specifications
# دليل مجلد البرومتات والمواصفات الهندسية

This folder contains the complete, up-to-date AI rebuild prompts and technical specifications for the **Geometry QC Analyzer** ArcGIS Pro Add-In (`net8.0-windows` / C# / WPF / DAML).

يحتوي هذا المجلد على برومتات البناء والمواصفات الهندسية الكاملة والمحدثة لإضافة **Geometry QC Analyzer** لبرنامج ArcGIS Pro.

---

## 📂 Prompts Inventory / قائمة الملفات

| Document / اسم الملف | Language / اللغة | Purpose / الهدف والوصف |
| :--- | :---: | :--- |
| [`GeometryQC_Rebuild_Prompt_EN.md`](GeometryQC_Rebuild_Prompt_EN.md) | English | Complete, authoritative architectural specification and AI rebuild prompt to recreate the entire add-in codebase from scratch. |
| [`GeometryQC_Rebuild_Prompt_AR.md`](GeometryQC_Rebuild_Prompt_AR.md) | العربية | الدليل الهندسي الشامل وبرومت الذكاء الاصطناعي لإعادة بناء الإضافة بالكامل باللغة العربية. |
| [`Update_01_Specification_EN.md`](Update_01_Specification_EN.md) | English | Detailed breakdown of the Update 01 enhancements, migration from Live to DisplayCache, feature cap, and arc–line check. |
| [`Update_01_Specification_AR.md`](Update_01_Specification_AR.md) | العربية | تفاصيل متطلبات التحديث 01، معايير التفاوتات المساحية، سقف المعالم، وحفظ الطبقة لكل مشروع. |
| [`Flat_Pseudo_Arc_Detection_Prompt_EN.md`](Flat_Pseudo_Arc_Detection_Prompt_EN.md) | English | Algorithmic specification and prompt for Flat / Pseudo-Arc detection using Sagitta ($h \le 0.01\text{ cm}$) and Segment Area ($A \le 10\text{ cm}^2$). |
| [`Flat_Pseudo_Arc_Detection_Prompt_AR.md`](Flat_Pseudo_Arc_Detection_Prompt_AR.md) | العربية | المواصفات الرياضية والبرمجية لكشف الأقواس الوهمية والمنبسطة باستخدام سهم القوس ومساحة القطعة الدائرية. |

---

## 📌 Summary of Latest Modifications (Update 01 - October 30, 2026)
## 📌 ملخص آخر التعديلات المنفذة (التحديث 01 - 30 أكتوبر 2026)

1. **Display Cache Only (Live Removed):** Deleted `LiveQueryProvider`, `EntireLayerProvider`, `RealGeometryProvider`, and `SelectionService`. Manual run only via **Run QC** button.
   *(الاعتماد الكلي على كاش الشاشة وحذف النمط المباشر والتلقائي وتحويل التشغيل ليدوي فقط).*
2. **Selected Features Cap (`MaxFeaturesToProcess` = 500):** Halts gracefully with an informative message if selected features exceed the configured threshold.
   *(سقف حماية لعدد المعالم المحددة بحد افتراضي 500 معلم لتفادي تجمد البرنامج).*
3. **Per-Project Target Layer Persistence:** Saves and restores the target layer selection (`TargetLayerUri` / `TargetLayerName`) inside the `.aprx` project file using `OnWriteSettingsAsync` / `OnReadSettingsAsync`.
   *(حفظ الطبقة المحددة داخل ملف المشروع واستعادتها تلقائياً عند إعادة الفتح).*
4. **"No errors found ✓" State:** Header in Results DockPane displays `"No errors found ✓"` and clears previous map graphics when validation finds 0 issues.
   *(رسالة تأكيد خلو المعالم من الأخطاء والتحذيرات ومسح الرسومات المؤقتة).*
5. **Cadastral Default Tolerances Overhaul:**
   - Overlap: `0.00001 m²` ($100\text{ cm}^2$)
   - Enclosed Gap: `0.00001 m²` ($100\text{ cm}^2$)
   - Short Segment: `15.0 cm`
   - Angle Issue: `30.0°`
   - Snap Issue: `15.0 cm`
   - Redundant Vertex: `175.0°`
   - Missing Junction: `10.0 cm`
   - *(تحديث شامل للقيم الافتراضية للتفاوتات طبقاً للمواصفات المساحية والعقارية المعتمدة).*
6. **Redundant Arc–Line Junction Check:** Optional check (`FlagArcLineJunctions = false` by default, `175.0°` angle) identifying redundant vertices at straight-to-curve transitions as Warnings, represented with a Coral triangle CIM symbol.
   *(فحص اختياري للرؤوس الزائدة عند التقاء القوس والخط المماس كتحذير برمز مثلث مرجاني).*
7. **Warning Map Overlay Toggle:** `ShowWarningsOnMap` allows users to suppress warning graphics from the map view.
   *(خيار لإخفاء أو إظهار رسومات التحذيرات على الخريطة لتخفيف الازدحام البصري).*
8. **Dynamic Header Badge:** Badge in Results DockPane dynamically displays `"X errors · Y warnings"`.
   *(شارة ديناميكية في رأس النتائج توضح عدد الأخطاء والتحذيرات بالتفصيل).*

