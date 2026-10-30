# برومت إعادة بناء إضافة فحص الجودة الهندسية والمعمارية الشاملة (باللغة العربية)

> **نوع الوثيقة:** المواصفات الفنية الشاملة ودليل إعادة البناء البرمجي (AI Rebuild Prompt)  
> **بيئة التشغيل المستهدفة:** `.NET 8.0-windows`  
> **البرنامج المضيف:** ArcGIS Pro (الإصدارات 3.0 حتى 3.4 وما بعدها - ArcGIS Pro .NET 8 SDK)  
> **تاريخ آخر تحديث:** 30 أكتوبر 2026 (تطبيق التحديث Update 01)  
> **المؤلف:** محمود أبو المجد بمساعدة الذكاء الاصطناعي المتقدم  

---

## 📌 سجل التعديلات الأخيرة (Update 01 Changelog)

تم في التحديث الأخير تطبيق المعايير والميزات الهندسية التالية:

1. **الاعتماد الكلي على كاش الشاشة (Display Cache) وإلغاء الأنماط التلقائية:**
   - حذف مزودات البيانات القديمة غير المستخدمة: `LiveQueryProvider.cs` و `EntireLayerProvider.cs` و `RealGeometryProvider.cs` وخدمة الاستماع للتحديد `SelectionService.cs`.
   - الفحص يتم يدوياً وبشكل حصري عند الضغط على زر **Run QC**.
   - جلب الأشكال الهندسية مباشرة وبأقصى سرعة من كاش العرض النشط عبر `DisplayCacheProvider`.
2. **تطبيق سقف حماية عدد المعالم (`MaxFeaturesToProcess` = 500):**
   - فحص عدد المعالم المحددة قبل بدء جلب الأشكال الهندسية. إذا تجاوز العدد السقف المحدد (افتراضياً 500 معلم)، يتوقف الفحص فوراً بأمان وتظهر رسالة واضحة للمستخدم:
     `"Selected {N} features; limit is {Cap}. Reduce the selection or raise the limit in Settings."`
3. **حفظ طبقة الفحص المختارة لكل مشروع (`.aprx`):**
   - استدعاء `OnWriteSettingsAsync(ModuleSettingsWriter)` و `OnReadSettingsAsync(ModuleSettingsReader)` داخل `GeometryQCModule.cs`.
   - حفظ المعرف الفريد للطبقة `TargetLayerUri` واسمها `TargetLayerName` في ملف المشروع. وعند فتح المشروع مجدداً يتم استعادة الطبقة المحددة تلقائياً في القائمة المنسدلة.
4. **حالة النجاح التام "No errors found ✓":**
   - في حال خلو المعالم المفحوصة من أية أخطاء أو تحذيرات، يظهر في رأس لوحة النتائج نص `"No errors found ✓"` مع مسح أي رسومات جرافيكية سابقة على الخريطة.
5. **تحديث شامل للقيم الافتراضية للتفاوتات (Cadastral Standards):**
   - **Overlap (`CHK_OVERLAP`):** `0.00001 m²` (100 سم²).
   - **Enclosed Gap (`CHK_GAP`):** `0.00001 m²` (100 سم²).
   - **Short Segment (`CHK_SHORT_SEG`):** `15.0 cm` (0.15 م).
   - **Angle Issue (`CHK_ANGLE`):** `30.0°`.
   - **Snap Issue (`CHK_SNAP`):** `15.0 cm` (0.15 م).
   - **Redundant Vertex (`CHK_REDUNDANT`):** `175.0°`.
   - **Missing Junction (`CHK_JUNCTION`):** `10.0 cm` (0.10 م).
   - **Restore Defaults:** زر استعادة الإعدادات يسترجع فوراً هذه القيم القياسية المحدثة.
6. **فحص التقاء القوس والخط المماس كتحذير (`CHK_REDUNDANT_ARCLINE`):**
   - خيار جديد في الإعدادات: `FlagArcLineJunctions` (افتراضياً: `false`، زاوية التفاوت: `175.0°`، مستوى الخطأ: `Warning`).
   - رصد الرؤوس غير الضرورية عند نقطة التماس المباشرة بين الخط المستقيم والمنحنى الدائري أو منحنى بيزيير، مع حماية نقاط الربط عبر `JunctionGuard`.
   - تمييز الخطأ على الخريطة برمز CIM جديد: مثلث متساوي الأضلاع بلون مرجاني (Coral - `RGB(255, 127, 80)`).
7. **إمكانية إخفاء التحذيرات من الخريطة (`ShowWarningsOnMap`):**
   - خيار في لوحة الإعدادات (افتراضياً: `true`) يتيح للمستخدم إظهار أو إخفاء رسومات التحذيرات على الخريطة والإبقاء على الأخطاء الحرجة فقط لمنع ازدحام العرض.
8. **تحسينات واجهة المستخدم والتجميع:**
   - شارة رأس لوحة النتائج أصبحت ديناميكية توضح عدد الأخطاء والتحذيرات (مثل: `"3 errors · 2 warnings"`).
   - ضبط `autoLoad="false"` في `Config.daml` وتأكيد `Enabled = true;` في كافة مشيدات الأزرار.

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

```text
QC Preview V01/
├── Config.daml                                # ملف التكوين الإعلاني (autoLoad="false")
├── GeometryQCAddIn.csproj                     # مشروع MSBuild مع النشر التلقائي ومسح الكاش
├── GeometryQCAddIn.sln                        # حل Visual Studio
├── GeometryQCModule.cs                        # الوحدة الرئيسية وحفظ إعدادات المشروع
├── package.ps1                                # سكريبت البناء السريع ونشر الإضافة
├── README.md                                  # نظرة عامة ثنائية اللغة
├── USER_GUIDE.md                              # دليل المستخدم ثنائي اللغة
├── QC_CHECKS_LOGIC.md                         # الدليل الرياضي والمساحي للفحوصات
├── prompts/                                   # مجلد برومتات البناء والمواصفات المعمارية
│   ├── README.md                              # فهرس البرومتات وسجل التعديلات
│   ├── GeometryQC_Rebuild_Prompt_EN.md        # النسخة الإنجليزية الشاملة
│   ├── GeometryQC_Rebuild_Prompt_AR.md        # النسخة العربية الشاملة
│   ├── Update_01_Specification_EN.md          # متطلبات وملاحظات التحديث 01 بالإنجليزية
│   └── Update_01_Specification_AR.md          # متطلبات وملاحظات التحديث 01 بالعربية
├── Config/
│   └── GeometryQCSettings.cs                  # إعدادات المستخدم وحفظها في JSON
├── Core/
│   ├── Checks/
│   │   ├── IGeometryCheck.cs                  # الواجهة الموحدة لجميع الفحوصات
│   │   ├── InvalidGeometryCheck.cs            # CHK_INVALID_GEOM
│   │   ├── OverlapCheck.cs                    # CHK_OVERLAP
│   │   ├── DuplicateCheck.cs                  # CHK_DUPLICATE
│   │   ├── GapCheck.cs                        # CHK_GAP
│   │   ├── MultiPartCheck.cs                  # CHK_MULTIPART
│   │   ├── ShortSegmentCheck.cs               # CHK_SHORT_SEG
│   │   ├── AngleIssueCheck.cs                 # CHK_ANGLE
│   │   ├── SnapIssueCheck.cs                  # CHK_SNAP
│   │   ├── RedundantVertexCheck.cs            # CHK_REDUNDANT (المنحنيات وحارس الربط والقوس والخط)
│   │   └── JunctionVertexCheck.cs             # CHK_JUNCTION (T-Junctions)
│   ├── DisplayCacheProvider.cs                # قارئ كاش العرض مع حماية سقف المعالم
│   ├── GeometryDataProvider.cs                # التجريد الأساسي لمزود البيانات
│   ├── GeometryHelpers.cs                     # الحسابات الهندسية وقياسات تحت الملليمتر
│   ├── GeometryQCContext.cs                   # سياق تنفيذ الفحص
│   ├── GeometryValidator.cs                   # محرك التنسيق وإزالة التكرار
│   ├── SpatialIndex.cs                        # الفهرس المكاني لشبكة المضلعات
│   ├── UnitConverter.cs                       # تحويل الوحدات القياسية
│   └── VertexIndex.cs                         # الفهرس المكاني لرؤوس المضلعات
├── Graphics/
│   ├── QCGraphicManager.cs                    # مدير الرسومات المؤقتة وخيار إخفاء التحذيرات
│   └── QCGraphicSymbolProvider.cs             # رموز CIM لتمثيل الأخطاء (بما في ذلك رمز القوس والخط)
├── Models/
│   ├── IssueResult.cs                         # نموذج نتيجة الخطأ وتنسيق القياسات
│   ├── IssueSeverity.cs                       # نوع الخطأ: Error, Warning, Info
│   ├── MapLayerItem.cs                        # نموذج عنصر قائمة الطبقات
│   ├── QCFeature.cs                           # حاوية المضلع بالذاكرة
│   └── QCStatistics.cs                        # عدادات المعالم والوقت المستغرق
├── Services/
│   ├── GdbExportService.cs                    # خدمة تصدير الأخطاء إلى قاعدة البيانات
│   ├── LoggingService.cs                      # خدمة تسجيل السجلات التشخيصية
│   ├── MapViewService.cs                      # خدمة التحريك والتنقل في الخريطة
│   ├── ProgressService.cs                     # إدارة الإلغاء والتقدم
│   └── ThemeService.cs                        # مزامنة مظهر ArcGIS Pro الفاتح والداكن
├── UI/
│   ├── Buttons.cs                             # أزرار شريط Ribbon
│   ├── RelayCommand.cs                        # تنفيذ ICommand لنمط MVVM
│   ├── ResultsDockPane.xaml                   # واجهة لوحة النتائج
│   ├── ResultsDockPane.xaml.cs                # الكود الخلفي للنتائج
│   ├── ResultsDockPaneViewModel.cs            # نموذج عرض لوحة النتائج
│   ├── SettingsDockPane.xaml                  # واجهة لوحة الإعدادات
│   ├── SettingsDockPane.xaml.cs               # الكود الخلفي للإعدادات
│   ├── SettingsDockPaneViewModel.cs            # نموذج عرض لوحة الإعدادات
│   └── ThemeResources.xaml                    # موارد وألوان المظهر الاحترافي
└── Images/                                    # الأيقونات المخصصة
```

---

## 3. الفحوصات الهندسية العشرة بالتفصيل

1. **فحص سلامة المضلع (Invalid Geometry - `CHK_INVALID_GEOM`):** استدعاء `Simplify()` والتحقق من التقاطعات الذاتية والحلقات العكسية والإحداثيات غير المعرفة.
2. **فحص التداخل (Overlap - `CHK_OVERLAP`):** اقتطاع التقاطعات بدقة وحساب المساحة بالمتر المربع أو السنتيمتر المربع (افتراضياً: `0.00001 m²` / `100 cm²`) مع حارس السمك الأدنى لاستبعاد خطوط التماس الفاصلة.
3. **فحص التطابق المكرر (Duplicate Geometry - `CHK_DUPLICATE`):** كشف المضلعات المتطابقة 100% ومنع تكرار تقريرها كتداخل.
4. **فحص الفجوات الهوائية (Enclosed Gap - `CHK_GAP`):** رصد الفراغات المغلقة المحصورة بين المضلعات المتجاورة الأكبر من `0.00001 m²`.
5. **فحص تعدد الأجزاء (Multi-Part Feature - `CHK_MULTIPART`):** رصد المعالم متعددة الأجزاء في قواعد البيانات التي تشترط مضلعات أحادية.
6. **فحص الأضلاع القصيرة (Short Segment - `CHK_SHORT_SEG`):** كشف الأضلاع التي يقل طولها عن التفاوت المحدد (افتراضياً: `15.0 cm`).
7. **فحص الزوايا الحادة (Angle Issue - `CHK_ANGLE`):** رصد الإبر والنقرات الشاذة بزوايا حادة أقل من `30.0°`.
8. **فحص عدم الالتقاط (Snap Issue - `CHK_SNAP`):** رصد الرؤوس غير الملتقطة الأقل من `15.0 cm` وعرض المسافات تحت السنتيمتر بالملليمتر (`mm`) بدقة حتى 4 خانات عشرية.
9. **فحص الرؤوس الزائدة (Redundant Vertex - `CHK_REDUNDANT`):**
   - الرؤوس الواقعة على استقامة الخط المستقيم ($\ge 175.0°$).
   - المنحنيات الحقيقية (الأقواس وبيزيير) مع فحص اتساق المركز ونصف القطر واستمرارية المماس والانحناء ($C^1/C^2$).
   - **فحص اختياري للتقاء القوس والخط المماس (`CHK_REDUNDANT_ARCLINE`):** يكتشف الرؤوس الزائدة عند التماس بين الخط والمنحنى عند تفعيله، ويظهر كتحذير برمز مثلث مرجاني.
   - **حارس نقاط الربط (Junction Guard):** يحمي الرؤوس المشتركة إذا كان المضلع المجاور ينكسر أو يرتبط برأس حقيقي عندها.
10. **فحص العقد المفقودة (Missing Junction - `CHK_JUNCTION`):** كشف العقد المفقودة في الوصلات التبادلية (T-Junctions) عندما تلامس نقطة ضلع مضلع مجاور دون التقاط مشترك (افتراضياً: `10.0 cm`).

---

## 4. القواعد المعمارية الإلزامية

1. **تفعيل الأزرار برمجياً:** كتابة `Enabled = true;` في كافة مشيدات الأزرار.
2. **ضبط التعبئة:** ضبط `autoLoad="false"` في `Config.daml`.
3. **عدم دعم التحديث الساخن (No Hot-Reload):** تنبيه المستخدم دائماً بضرورة إغلاق وتشغيل ArcGIS Pro لقراءة أي ملفات DLL محدثة.
4. **أمان القراءة فقط:** عدم تشغيل أي `EditOperation` على طبقات المصدر.
5. **سقف عدد المعالم:** التحقق الصارم من سقف المعالم المحددة لمنع إجهاد الذاكرة في ArcGIS Pro.

