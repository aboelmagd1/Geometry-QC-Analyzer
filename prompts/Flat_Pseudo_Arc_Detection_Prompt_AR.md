# مواصفات وبرومت فحص الأقواس الوهمية والمنبسطة مساحياً (باللغة العربية)

> **نوع الوثيقة:** برومت ومواصفات هندسية لإعادة الاستخدام في مشاريع فحص الجودة  
> **موضوع الفحص:** كشف الأقواس الوهمية / المنبسطة (Flat / Pseudo-Arcs) باستخدام سهم القوس ومساحة القطعة  
> **البيئة المستهدفة:** ArcGIS Pro SDK (.NET 8 / C#) ومحركات الطوبولوجيا في نظم المعلومات الجغرافية  
> **المؤلف:** محمود أبو المجد بمساعدة الذكاء الاصطناعي المتقدم  
> **التاريخ:** 30 أكتوبر 2026  

---

## 1. الهدف والمشكلة الهندسية

في المخططات المساحية العقارية ومشاريع البنية التحتية، كثيراً ما يضغط الرسام بالماوس بالخطأ أثناء إدخال المعالم، مما يحول الضلع المستقيم إلى قوس دائري بنصف قطر هائل (مثل $R > 5,000\text{ m}$). في هذه الحالة، يكون الانحراف عن الخط المستقيم أقل من الملليمتر (غير مرئي بالعين المجردة)، لكنه يخزن كمنحنى حقيقي (`EllipticArcSegment`) مما يسبب مشاكل في التحليل الطوبولوجي وزيادة غير مبررة في حجم البيانات.

يقدم هذا البرومت المواصفات الرياضية والبرمجية الكاملة لبناء فحص آلي يكتشف هذه الأقواس الوهمية.

---

## 2. المفاهيم الهندسية والمصطلحات

```text
                     منتصف القوس (Arc Midpoint)
                                ●
                            . ´ │ ` .
                        . ´     │ $h$  ` .    سهم القوس (Sagitta)
                    . ´         │         ` . 
        (Start) ●───────────────┼───────────────● (End)
                     الوتر الوهمي (Chord $C$)
                └───────────────────────────────┘
                     مساحة القطعة الدائرية ($A$)
```

1. **الوتر (Chord $C$):** الخط المستقيم الوهمي الواصل بين نقطة بداية القوس ونهايته.
2. **سهم القوس (Sagitta $h$):** المسافة العمودية القصوى بين منتصف الوتر ومنتصف القوس.
3. **مساحة القطعة الدائرية (Circular Segment Area $A$):** المساحة السطحية المغلقة المحصورة بين الوتر ومنحنى القوس.

---

## 3. المعادلات الرياضية

لأي قوس دائري نصف قطره $R$ وزاويته المركزية $\Delta\theta$ (بالراديان):

### 1) طول الوتر ($C$):
$$C = 2R \sin\left(\frac{\Delta\theta}{2}\right) = \sqrt{(x_2 - x_1)^2 + (y_2 - y_1)^2}$$

### 2) سهم القوس / ارتفاع المنحنى ($h$):
$$h = R \left(1 - \cos\left(\frac{\Delta\theta}{2}\right)\right)$$
*أو بدلالة طول الوتر ونصف القطر:*
$$h = R - \sqrt{R^2 - \left(\frac{C}{2}\right)^2}$$

### 3) مساحة القطعة الدائرية المحصورة ($A$):
$$A = \frac{1}{2} R^2 (\Delta\theta - \sin\Delta\theta)$$
*أو باستخدام معادلة التقريب المساحي الشهيرة للأقواس المنبسطة:*
$$A \approx \frac{2}{3} \cdot C \cdot h$$

---

## 4. معايير الكشف والقيم المساحية المعتمدة

يصنف القوس الدائري كـ **قوس وهمي / منبسط (Flat Pseudo-Arc)** إذا تحقق أي من الشرطين التاليين:

| المعيار الهندسي | الرمز | الحد المسموح الافتراضي | المعنى العملي والمساحي |
| :--- | :---: | :---: | :--- |
| **سهم القوس (Sagitta)** | $h$ | **$\le 0.01\text{ cm}$** ($0.1\text{ mm}$) | أقصى انحراف للقوس عن الوتر أقل من عُشر الملليمتر (مستقيم عملياً). |
| **مساحة القطعة المحصورة** | $A$ | **$\le 10.0\text{ cm}^2$** ($0.001\text{ m}^2$) | المساحة المحصورة بين القوس والوتر متناهية الصغر وشبه معدومة. |
| **نصف القطر الشاذ** | $R$ | **$\ge 5,000\text{ m}$** | (شرط إضافي) نصف قطر عملاق مع زاوية مركزية متناهية الصغر $\Delta\theta \le 0.5^\circ$. |

---

## 5. نموذج الكود الموصى به (C# / ArcGIS Pro SDK)

```csharp
public class FlatArcCheckResult
{
    public bool IsFlatArc { get; set; }
    public double ChordLengthMeters { get; set; }
    public double SagittaMilliMeters { get; set; }
    public double AreaSqCentimeters { get; set; }
    public double RadiusMeters { get; set; }
}

public static FlatArcCheckResult EvaluateArc(
    EllipticArcSegment arc, 
    double sagittaThresholdCm = 0.01, 
    double areaThresholdCm2 = 10.0)
{
    if (!arc.IsCircular) return new FlatArcCheckResult { IsFlatArc = false };

    double r = arc.SemiMajorAxis; // نصف القطر بوحدات الخريطة (أمتار)
    double deltaTheta = Math.Abs(arc.CentralAngle); // الزاوية المركزية بالراديان
    
    // 1. حساب سهم القوس (h)
    double hMeters = r * (1.0 - Math.Cos(deltaTheta / 2.0));
    double hCm = hMeters * 100.0;
    double hMm = hMeters * 1000.0;

    // 2. حساب مساحة القطعة الدائرية (A)
    double areaM2 = 0.5 * r * r * (deltaTheta - Math.Sin(deltaTheta));
    double areaCm2 = areaM2 * 10000.0;

    // 3. طول الوتر (C)
    double chordM = arc.Length;

    bool isFlat = (hCm <= sagittaThresholdCm) || (areaCm2 <= areaThresholdCm2);

    return new FlatArcCheckResult
    {
        IsFlatArc = isFlat,
        ChordLengthMeters = chordM,
        SagittaMilliMeters = hMm,
        AreaSqCentimeters = areaCm2,
        RadiusMeters = r
    };
}
```

---

## 6. تقرير الخطأ والمعالجة المقترحة

* **كود الخطأ:** `CHK_FLAT_PSEUDO_ARC`
* **مستوى الأهمية:** تحذير (`IssueSeverity.Warning`).
* **نص الرسالة للمستخدم:**  
  `"تم اكتشاف قوس وهمي منبسط: سهم القوس {hMm:F3} ملم والمساحة {areaCm2:F2} سم² (نصف القطر: {r:N0} م، الوتر: {chordM:F2} م). يُنصح بتحويله إلى خط مستقيم."`
* **إجراء التصحيح التلقائي:**  
  استبدال القوس `EllipticArcSegment` داخل حلقة المضلع بضلع مستقيم عادي `LineSegment.Create(arc.StartPoint, arc.EndPoint)`.

