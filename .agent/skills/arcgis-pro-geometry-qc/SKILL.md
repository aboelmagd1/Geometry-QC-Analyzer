---
name: arcgis-pro-geometry-qc
description: >-
  Cadastral topology and polygon geometry quality control engine for ArcGIS Pro Add-Ins.
  Use when implementing, tuning, or debugging geometric validation algorithms including
  overlaps, slivers, gaps, spikes, snap issues, parametric true curves (circular arcs & Béziers),
  redundant vertices, and missing T-junctions with sub-millimeter precision and spatial indexing.
---

# ArcGIS Pro Geometry Quality Control & Cadastral Topology

This skill guides the design, implementation, and optimization of high-performance, read-only geometry quality control (QC) and cadastral topology validation engines in ArcGIS Pro (`net8.0-windows` / C# / ArcGIS Pro SDK).

---

## 1. Core Principles & Safety Mandates

1. **Strict Read-Only Execution:**
   - Geometry validation must NEVER modify the source feature class, open an `EditOperation`, or place schema/write locks on geodatabases or feature services.
   - All spatial analysis is performed in-memory on lightweight geometry representations.
2. **Sub-Millimeter Cadastral Precision:**
   - Coordinate discrepancies $< 1\text{ cm}$ must be formatted in millimeters (`mm`) with up to 4 decimal places (e.g., `0.35 mm`) to prevent UI display truncation to `0.00 cm`.
   - Coincident points ($d < 10^{-6}\text{ m}$) must be filtered to prevent false-positive reporting.
3. **Dual Spatial Indexing:**
   - Use an adaptive 2D Bounding-Box Grid Spatial Hash (`SpatialIndex`) to reduce candidate polygon pair comparisons from $O(N^2)$ to $O(N \log N)$.
   - Use a Point Spatial Hash (`VertexIndex`) with cell size equal to snap/junction tolerance to achieve instant $O(1)$ neighborhood vertex queries.

---

## 2. Standard Cadastral QC Checks & Thresholds

| Check ID | Name | Severity | Cadastral Standard Default | Mathematical / Algorithmic Core |
| :--- | :--- | :---: | :---: | :--- |
| `CHK_INVALID_GEOM` | Invalid Geometry | 🔴 Error | OGC Standard | `GeometryEngine.Instance.Simplify()`. Flag non-positive area, ring points $< 3$, self-intersections (bow-ties), or `NaN` coordinates. |
| `CHK_OVERLAP` | Overlap | 🔴 Error | `0.00001 m²` ($100\text{ cm}^2$) | `GeometryEngine.Instance.Intersection(polyA, polyB)`. Filter micro-slivers using thickness guard: `minThickness = Math.Max(sr?.XYTolerance ?? 0.001, 0.0005)`. |
| `CHK_DUPLICATE` | Duplicate Geometry | 🟠 Warning | Exact Match | Vertex spatial hash matching followed by `GeometryEngine.Instance.Equals()`. Suppress duplicate reporting under Overlap. |
| `CHK_GAP` | Enclosed Gap | 🟠 Warning | `0.00001 m²` ($100\text{ cm}^2$) | Union candidate neighbors, identify interior rings that do not intersect external polygon envelopes. |
| `CHK_MULTIPART` | Multi-Part Feature | 🔵 Info | PartCount $> 1$ | Flag features where `polygon.PartCount > 1` in cadastre requiring single-part parcels. |
| `CHK_SHORT_SEG` | Short Segment | 🟠 Warning | `15.0 cm` ($0.15\text{ m}$) | Traverse ring segments: flag $d(p_i, p_{i+1}) < \text{tolerance}$, excluding coincident points ($d < 10^{-6}\text{ m}$). |
| `CHK_ANGLE` | Angle Issue (Spikes) | 🟠 Warning | `30.0°` | Triplet interior angle $\theta = \arccos\left(\frac{\vec{u} \cdot \vec{v}}{\|\vec{u}\| \|\vec{v}\|}\right)$. Flag acute needle angles $< 30.0°$. |
| `CHK_SNAP` | Snap Issue | 🟠 Warning | `15.0 cm` ($0.15\text{ m}$) | Query `VertexIndex` for cross-feature vertices separated by $0 < d \le \text{tolerance}$. Guard: $d \ge \max(\text{XYResolution}, 10^{-7}\text{ m})$. |
| `CHK_REDUNDANT` | Redundant Vertex | 🔵 Info | `175.0°` | 1. Straight: collinear angle $\ge 175.0°$.<br/>2. Parametric Arcs: match center, radius, and tangent $\ge 175.0°$.<br/>3. Béziers: $C^1$ tangent $\ge 175.0°$ and $C^2$ curvature match.<br/>4. Apply `JunctionGuard` to protect adjacent shared parcels. |
| `CHK_REDUNDANT_ARCLINE` | Redundant Arc–Line | 🟠 Warning | `175.0°` *(Optional)* | Inspect transition vertices where a straight line meets a circular arc or Bézier curve. Flag if deflection angle $\ge 175.0°$. |
| `CHK_JUNCTION` | Missing Junction | 🟠 Warning | `10.0 cm` ($0.10\text{ m}$) | Detect T-junctions: vertex of polygon A touches edge of polygon B without an existing vertex on B. Guard: $d \ge 2\text{ cm}$. |

---

## 3. Parametric True Curve Handling & Junction Guard

### True Curves Analysis ($C^1$ and $C^2$ Continuity)
* When decomposing geometries with `SegmentExtension.GetSegments()`, distinguish between straight line segments, `EllipticArcSegment`, and `CubicBezierSegment`.
* **Never flag valid geometric transitions:** Transition between a straight line and a curve is valid by definition unless the optional `FlagArcLineJunctions` mode is explicitly requested.
* For two adjacent circular arcs, verify:
  1. Center coordinates match within XY tolerance: `(c1.X - c2.X)^2 + (c1.Y - c2.Y)^2 < tol^2`.
  2. Radii match within tolerance: `|r1 - r2| < tol`.
  3. Tangent angle continuity at junction vertex $\ge \text{threshold}$ (e.g. 175.0°).

### Cadastral Junction Guard
* In parcel cadastre, a vertex that appears redundant along a straight line in Polygon A must NOT be removed if Polygon B terminates at that vertex (forming a boundary division or corner).
* Before flagging a vertex as redundant:
  ```csharp
  bool isProtected = vertexIndex.FindCoincidentVertices(v, 1e-4)
      .Any(neighborVertex => neighborVertex.FeatureId != currentFeatureId 
                             && neighborVertex.IsCornerOrTransition());
  ```

---

## 4. Deduplication Pipeline

To prevent double-counting pairwise issues (e.g. Polygon 10 vs Polygon 20 and Polygon 20 vs Polygon 10):
1. Normalize pair keys: `(min(OID_A, OID_B), max(OID_A, OID_B))`.
2. Spatial cluster deduplication: For overlapping areas, cluster intersection polygons by centroid within $1\text{ mm}$ tolerance.
3. Group and categorize issues into a tree view hierarchy:
   `Severity (Error / Warning / Info) -> Check Type -> Issues List`.
