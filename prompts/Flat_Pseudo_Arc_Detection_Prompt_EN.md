# Cadastral Quality Control: Flat / Pseudo-Arc Detection Check (English)

> **Document Type:** AI Rebuild Prompt & Engineering Specification  
> **Topic:** Flat / Pseudo-Arc Detection (Sagitta & Segment Area Criteria)  
> **Target Framework:** ArcGIS Pro SDK (.NET 8 / C#) & CAD/GIS Topology Engines  
> **Author:** Mahmoud Aboelmagd with Advanced AI  
> **Date:** October 30, 2026  

---

## 1. Objective & Context

In parcel cadastres and civil infrastructure datasets, digitizing artifacts or accidental curve commands often create circular arcs with huge radii (e.g., $R > 5,000\text{ m}$) where the lateral deviation from a straight line is sub-millimeter. These "Pseudo-Arcs" or "Flat Arcs" are stored geometrically as true curves (`EllipticArcSegment`) but functionally represent straight boundary lines.

This prompt provides the complete algorithmic and mathematical specification to build an automated check that identifies and flags these pseudo-arcs.

---

## 2. Geometric Concept & Terminology

```text
                     Arc Midpoint (منتصف القوس)
                                ●
                            . ´ │ ` .
                        . ´     │ $h$  ` .    Sagitta / Mid-Ordinate Height
                    . ´         │         ` . 
        (Start) ●───────────────┼───────────────● (End)
                     Chord / الوتر ($C$)
                └───────────────────────────────┘
                     Circular Segment Area ($A$)
```

1. **Chord ($C$):** The imaginary straight line segment connecting the `StartPoint` and `EndPoint` of the arc.
2. **Sagitta / Mid-Ordinate Height ($h$):** The maximum perpendicular distance from the midpoint of the chord to the midpoint of the arc.
3. **Circular Segment Area ($A$):** The closed planar area bounded between the chord and the circular arc.

---

## 3. Mathematical Equations

For any circular arc with radius $R$ and central subtended angle $\Delta\theta$ (in radians):

### 1) Chord Length ($C$):
$$C = 2R \sin\left(\frac{\Delta\theta}{2}\right) = \sqrt{(x_2 - x_1)^2 + (y_2 - y_1)^2}$$

### 2) Sagitta / Height of Arc ($h$):
$$h = R \left(1 - \cos\left(\frac{\Delta\theta}{2}\right)\right)$$
*Alternatively, in terms of chord $C$ and radius $R$:*
$$h = R - \sqrt{R^2 - \left(\frac{C}{2}\right)^2}$$

### 3) Circular Segment Area ($A$):
$$A = \frac{1}{2} R^2 (\Delta\theta - \sin\Delta\theta)$$
*Simpson's parabolic approximation for flat arcs:*
$$A \approx \frac{2}{3} \cdot C \cdot h$$

---

## 4. Detection Criteria & Standard Thresholds

An `EllipticArcSegment` (where `IsCircular == true`) is classified as a **Flat Pseudo-Arc** if it meets either or both of the following cadastral conditions:

| Parameter | Symbol | Cadastral Default Threshold | Practical Meaning |
| :--- | :---: | :---: | :--- |
| **Sagitta (Arc Height)** | $h$ | **$\le 0.01\text{ cm}$** ($0.1\text{ mm}$) | The arc midpoint deviates by less than $0.1\text{ mm}$ from the chord line. |
| **Segment Area** | $A$ | **$\le 10.0\text{ cm}^2$** ($0.001\text{ m}^2$) | The area enclosed between the arc and chord is negligible. |
| **Sanity Radius** | $R$ | **$\ge 5,000\text{ m}$** | (Optional) Radius is enormous with central angle $\Delta\theta \le 0.5^\circ$. |

---

## 5. ArcGIS Pro SDK C# Implementation Pattern

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

    double r = arc.SemiMajorAxis; // Radius in map units (meters)
    double deltaTheta = Math.Abs(arc.CentralAngle); // In radians
    
    // 1. Calculate Sagitta (h)
    double hMeters = r * (1.0 - Math.Cos(deltaTheta / 2.0));
    double hCm = hMeters * 100.0;
    double hMm = hMeters * 1000.0;

    // 2. Calculate Circular Segment Area (A)
    double areaM2 = 0.5 * r * r * (deltaTheta - Math.Sin(deltaTheta));
    double areaCm2 = areaM2 * 10000.0;

    // 3. Chord Length (C)
    double chordM = arc.Length; // or distance between StartPoint and EndPoint

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

## 6. Recommended Issue Reporting & Remediation

* **Issue Code:** `CHK_FLAT_PSEUDO_ARC`
* **Severity:** `IssueSeverity.Warning`
* **Message Format:**  
  `"Flat pseudo-arc detected: Sagitta is {hMm:F3} mm, enclosed area is {areaCm2:F2} cm² (Radius: {r:N0} m, Chord: {chordM:F2} m). Replace with a straight line segment."`
* **Remediation Action:**  
  Replace the `EllipticArcSegment` in the polygon ring with a `LineSegment.Create(arc.StartPoint, arc.EndPoint)`.

