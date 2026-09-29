# Geometry QC Analyzer — Checks Logic, Mathematics & Cadastral Rationale
# المنطق الرياضي والهندسي وقواعد التحقق لفحوصات الجودة المكانية في ArcGIS Pro

<p align="center">
  <a href="#english-logic">English Specification & Mathematical Rationale</a> •
  <a href="#المنطق-الهندسي-وقواعد-التدقيق-باللغة-العربية">المنطق الهندسي والرياضي باللغة العربية</a>
</p>

---

<a name="english-logic"></a>
# Part 1: English Specification & Mathematical Rationale

## 1. Architectural Philosophy & Engine Foundations

The **Geometry QC Analyzer** is designed to validate polygon and parcel datasets against stringent cadastral, cartographic, and geodatabase topological integrity standards. 

Traditional GIS validation tools frequently suffer from four critical deficiencies:
1. **False Positives on Legal Boundaries:** Flagging valid boundary nodes where neighbor parcels split or intersect.
2. **Curve Ignorance:** Treating true parametric curves (Circular Arcs, Elliptic Arcs, Cubic Béziers) as segmented polylines, which falsely flags every intermediate vertex or fails to detect real redundancy.
3. **Misleading Precision Artifacts:** Reporting sub-centimeter errors as `0.00 cm` due to blunt rounding, which confuses cartographers.
4. **Destructive or Slow Execution:** Locking source geodatabases or requiring slow disk-based intermediate layers.

The Geometry QC Analyzer resolves these challenges through an **in-memory, non-destructive, sub-millimeter pipeline** powered by custom 2D spatial indexing, vector geometry mathematics, parametric curve continuity verification, and the multi-feature **Junction Guard**.

---

## 2. Mathematical Framework & Common Primitives

### 2.1 Coordinate Space & Spatial Reference Normalization
All geometric operations are conducted in the native spatial reference system of the input layer. When metrics (areas, lengths, distances) are evaluated against user thresholds:
- If the spatial reference is projected and its linear unit is not meters (e.g., US Survey Foot, International Foot), conversion factors are derived directly from `SpatialReference.Unit.ConversionFactorToMeters`:
  $$\text{Length}_{\text{meters}} = \text{Length}_{\text{map\_units}} \times \text{ConversionFactorToMeters}$$
  $$\text{Area}_{\text{sq\_meters}} = \text{Area}_{\text{map\_units}} \times (\text{ConversionFactorToMeters})^2$$
- If the layer is geographic (WGS84, NAD83 in degrees), Euclidean planar metrics are normalized using local geodesic scale approximations.

### 2.2 2D Spatial & Vertex Indexing
To avoid the $O(N^2)$ pairwise bottleneck across large parcel layers:
- **Envelope R-Tree / Spatial Grid:** Features are indexed by their minimal 2D bounding box $(X_{\min}, Y_{\min}, X_{\max}, Y_{\max})$. Only candidate pairs whose bounding envelopes intersect plus the tolerance margin $\tau$ are subjected to fine-grained geometry checks.
- **Vertex Bucket Index:** Polygon vertices are indexed in a uniform spatial grid with cell size $\Delta \approx \max(\tau_{\text{snap}}, \tau_{\text{junction}})$. Vertex neighborhood queries run in expected $O(1)$ time per vertex.

---

## 3. The 10 QC Checks in Detail

---

### Check 1: Invalid Geometry (`CHK_INVALID_GEOM`)

#### A. Cadastral & GIS Significance
In cadastral registers and enterprise geodatabases, an invalid polygon is an existential threat to spatial integrity:
- Breaks spatial relationship operators (`ST_Intersects`, `ST_Contains`, `ST_Union`).
- Causes geoprocessing tools to crash or return silent inaccuracies.
- Generates undefined parcel areas, invalidating property valuations and land deed records.

#### B. Defect Taxonomy & Algorithmic Detection
The engine evaluates polygon validity across four fundamental failure modes:
1. **Self-Intersection (Bow-Tie Polygons):**
   - Non-adjacent boundary segments $S_i = \overline{P_i P_{i+1}}$ and $S_j = \overline{P_j P_{j+1}}$ ($|i - j| > 1$) intersect at an interior point $Q$:
     $$Q = S_i \cap S_j \neq \emptyset$$
   - This divides the polygon into opposing winding-order lobes, creating indeterminate topological interiors.
2. **Inverted Exterior or Interior Rings:**
   - Exterior rings must follow a clockwise (CW) orientation (positive signed area in right-handed coordinate systems), while interior hole rings must be counter-clockwise (CCW).
   - Evaluated via the Shoelace Formula (Green's Theorem):
     $$A = \frac{1}{2} \sum_{k=0}^{n-1} (X_k Y_{k+1} - X_{k+1} Y_k)$$
     If $A_{\text{exterior}} \le 0$, the ring is inverted.
3. **Degenerate Rings ($< 3$ Unique Vertices):**
   - A closed polygon ring must contain at least three distinct non-collinear vertices plus the closure vertex ($N \ge 4$ points where $P_0 = P_n$). Rings with $\le 2$ unique points collapse into lines or single coordinates.
4. **NaN or Infinite Coordinates:**
   - Evaluated by scanning all vertex components: $\text{IsNaN}(X) \lor \text{IsNaN}(Y) \lor \text{IsInfinity}(X) \lor \text{IsInfinity}(Y)$.

```text
       Self-Intersection (Bow-Tie)               Inverted Ring
           P1 +---------+ P2                       P0 <------- P3
               \       /                            |           ^
                \  Q  /                             |    CCW    |
                 \   /                              V           |
                  \ /                              P1 -------> P2
                   X                              (Should be CW for
                  / \                              Exterior Ring)
                 /   \
           P4 +-------+ P3
```

#### C. Edge Cases & Remediation
- **Edge Case:** Touching rings at a single tangent vertex (pinch-point). The engine flags this as invalid because it violates OGC Simple Feature standards for single polygons.
- **Recommended Remediation:** Run **Simplify Geometry** or **Repair Geometry** in ArcGIS Pro; split bow-ties into two discrete polygons.

---

### Check 2: Overlap (`CHK_OVERLAP`)

#### A. Cadastral & GIS Significance
An overlap between two parcels signifies **dual legal ownership** or conflicting claims over the same physical land. It inflates total tax assessments, violates property boundaries, and causes legal liability in boundary demarcation.

#### B. Algorithmic Logic & Equations
For every candidate pair $(F_A, F_B)$ identified via the 2D spatial index:
1. The 2D intersection is computed at areal dimension:
   $$\Omega_{AB} = \text{GeometryEngine.Intersection}(F_A, F_B, \text{Dimension} = 2)$$
2. If $\Omega_{AB} \neq \emptyset$, the intersection is exploded into constituent polygon parts:
   $$\Omega_{AB} = \bigcup_{k=1}^{m} \omega_k$$
3. For each part $\omega_k$, the planar area in square meters is computed:
   $$\text{Area}(\omega_k) = \text{CalculateArea}(\omega_k)$$
4. If $\text{Area}(\omega_k) \ge \tau_{\text{overlap}}$ (default $0.0001\text{ m}^2 = 1\text{ cm}^2$):
   - The region is flagged as an illegal overlap.
   - Dynamic unit formatting: if $\text{Area} < 1.0\text{ m}^2$, output is formatted in $\text{cm}^2$ (e.g., `42.50 cm²`); otherwise in $\text{m}^2$ (e.g., `1.84 m²`).

```text
       +-------------------+ (Polygon A)
       |                   |
       |             +-----+-------------+ (Polygon B)
       |             |/////|             |
       |             |/////|             |  Overlap Area = Ω_AB
       |             +-----+-------------+  Flagged if Area >= Tolerance
       |                   |
       +-------------------+
```

#### C. False Positive Prevention & Deduplication
- **Boundary Tangency (Sliver Suppression):** Two polygons sharing a common boundary may generate line or point intersections ($\text{Dimension} < 2$). These are filtered out by requesting area dimension only.
- **Pair Deduplication:** Validation enforces an ordered key check:
  $$\text{Key}(A, B) = \min(\text{OID}_A, \text{OID}_B) \mathbin{\Vert} \max(\text{OID}_A, \text{OID}_B)$$
  This guarantees that overlap $(A, B)$ is processed once, preventing mirror duplication $(B, A)$.
- **Duplicate Suppression:** If $F_A$ and $F_B$ are $100\%$ identical geometries, the error is routed exclusively to **Duplicate Geometry** (`CHK_DUPLICATE`) and suppressed here to prevent double-counting.
- **Recommended Remediation:** **Clip**, **Erase**, or **Align Features** with parcel priority rules.

---

### Check 3: Duplicate Geometry (`CHK_DUPLICATE`)

#### A. Cadastral & GIS Significance
Duplicate features occur when operators double-paste features, accidentally merge duplicate layers, or repeat append routines. Duplicates distort parcel counts, skew attribute aggregations (e.g., census, land value), and cause invisible rendering overhead.

#### B. Algorithmic Logic
For candidate pairs $(F_A, F_B)$ with identical bounding envelopes:
1. Centroid coincidence is verified:
   $$\|\text{Centroid}(F_A) - \text{Centroid}(F_B)\| < \epsilon_{\text{xy}}$$
2. Area equality is verified:
   $$|\text{Area}(F_A) - \text{Area}(F_B)| < \tau_{\text{area\_epsilon}}$$
3. Symmetric difference is evaluated:
   $$\Delta(F_A, F_B) = (F_A \setminus F_B) \cup (F_B \setminus F_A)$$
   If $\text{Area}(\Delta(F_A, F_B)) \le \epsilon_{\text{identical}}$, then $F_A \equiv F_B$.
4. Both features are registered in a `HashSet<long>` of duplicate OIDs to ensure one record is reported as the warning and the other as the duplicate counterpart.

#### C. Recommended Remediation
Delete the redundant feature record after transferring any non-duplicate attribute values.

---

### Check 4: Enclosed Gap (`CHK_GAP`)

#### A. Cadastral & GIS Significance
An enclosed gap (often called a "donut hole" or "sliver gap") between adjacent parcels represents unallocated land where no owner or tax parcel is registered. In cadastral fabrics, continuous parcel mosaics must form a seamless 2D partition of the territory without gaps.

#### B. Algorithmic Logic
1. The spatial engine extracts a contiguous cluster of neighboring polygons $\mathcal{C} = \{F_1, F_2, \dots, F_k\}$.
2. The geometric union of the cluster is generated:
   $$\mathcal{U} = \bigcup_{i=1}^{k} F_i$$
3. The interior boundary rings (holes) of $\mathcal{U}$ are extracted:
   $$\mathcal{H} = \text{InteriorRings}(\mathcal{U})$$
4. For each ring $H \in \mathcal{H}$:
   - Convert ring $H$ into a closed polygon $\mathcal{P}_H$.
   - Calculate area $\text{Area}(\mathcal{P}_H)$.
   - If $\text{Area}(\mathcal{P}_H) \ge \tau_{\text{gap}}$ (default $0.001\text{ m}^2 = 10\text{ cm}^2$):
     - Check if $\mathcal{P}_H$ is completely bounded by the outer perimeter of the cluster (not touching the exterior universe boundary).
     - Flag $\mathcal{P}_H$ as an illegal enclosed gap.

```text
       +---------------+---------------+
       |               |               |
       |   Polygon A   |   Polygon B   |
       |               |               |
       +-------+       +-------+-------+
       |       |  GAP  |       |
       |       | [///] |       |  <--- Enclosed Void
       +-------+       +-------+
       |               |               |
       |   Polygon C   |   Polygon D   |
       |               |               |
       +---------------+---------------+
```

#### C. False Positive Prevention
- **Open Margins:** Void areas open to the exterior map extent or right-of-way roads are not enclosed and are therefore excluded.
- **Micro Gaps:** Gaps smaller than $\tau_{\text{gap}}$ caused by floating-point coordinate precision are filtered out.
- **Recommended Remediation:** Use **Align Features** or create a new parcel geometry in the gap.

---

### Check 5: Multi-Part Feature (`CHK_MULTIPART`)

#### A. Cadastral & GIS Significance
A multi-part polygon contains multiple disconnected physical boundaries (or disjoint outer rings) linked to a single database row and unique PIN (Property Identification Number).
- In land administration, each independent parcel plot must possess an unambiguous spatial identifier, area, and centroid for deed registration.
- Multi-part parcels create severe ambiguity in spatial queries, centroid labeling, and building permit allocations.

#### B. Algorithmic Logic
1. The engine interrogates the geometry part collection:
   $$n_{\text{parts}} = \text{polygon.PartCount}$$
2. If $n_{\text{parts}} > 1$, the feature is flagged with the exact count of constituent disjoint parts.

#### C. Remediation
Run the **Multipart To Singlepart** geoprocessing tool in ArcGIS Pro to split disjoint parts into individual records with unique identifiers.

---

### Check 6: Short Segment (`CHK_SHORT_SEG`)

#### A. Cadastral & GIS Significance
A short segment (micro-edge) is an edge shorter than practical measurement limits (e.g., $< 10\text{ cm}$). They originate from:
- Inadvertent double-clicks during manual digitizing.
- Slivers generated during automated snapping or intersections.
- Microscopic vertex jitter from scanning/vectorization.
These micro-edges bloat database size, slow down rendering, and create topological cracking.

#### B. Algorithmic Logic
1. Every polygon ring is decomposed into its individual boundary segments $S_i = \overline{P_i P_{i+1}}$.
2. For each segment $S_i$:
   - If $S_i$ is a straight line:
     $$L(S_i) = \sqrt{(X_{i+1} - X_i)^2 + (Y_{i+1} - Y_i)^2}$$
   - If $S_i$ is a true curve (Circular Arc or Bézier):
     $$L(S_i) = \text{Segment.Length}$$
   - Convert length to meters: $L_{\text{meters}} = L(S_i) \times \text{ConversionFactorToMeters}$.
3. Evaluation:
   $$\text{If } \epsilon_{\text{coincident}} < L_{\text{meters}} < \tau_{\text{short\_segment}} \implies \text{Flag Short Segment}$$

#### C. False Positive Prevention
- **Coincident Zero-Length Points:** If $L_{\text{meters}} < 10^{-6}\text{ m}$, the points are identical coordinates. These are excluded from the short segment check and handled by the snap/redundant check to prevent redundant warning noise.
- **Recommended Remediation:** Delete the extraneous vertex or run **Simplify Polygon**.

---

### Check 7: Angle Issue / Needle Spike (`CHK_ANGLE`)

#### A. Cadastral & GIS Significance
Severe acute angles ($< 5^\circ$) almost never reflect genuine property boundaries. They represent digitizing spikes caused by accidental mouse clicks where the operator clicked a point, pulled the cursor away, and then clicked back along the same path.
- Spikes distort parcel perimeter measurements.
- They generate cartographic rendering artifacts (sharp visual needles).

#### B. Algorithmic Logic & Formulas
At each vertex $V_i$ of a polygon ring between incoming vector $\vec{v}_{\text{in}} = \vec{P}_{i-1} P_i$ and outgoing vector $\vec{v}_{\text{out}} = \vec{P}_i P_{i+1}$:
1. Vectors are constructed:
   $$\vec{v}_1 = (X_{i-1} - X_i, Y_{i-1} - Y_i), \quad \vec{v}_2 = (X_{i+1} - X_i, Y_{i+1} - Y_i)$$
2. Vector magnitudes are computed:
   $$l_1 = \|\vec{v}_1\|, \quad l_2 = \|\vec{v}_2\|$$
   If $l_1 < 10^{-12}$ or $l_2 < 10^{-12}$, skip degenerate duplicate.
3. The interior angle $\alpha$ at vertex $V_i$ is computed via normalized dot product:
   $$\cos(\alpha) = \frac{\vec{v}_1 \cdot \vec{v}_2}{l_1 \cdot l_2} = \frac{v_{1x} v_{2x} + v_{1y} v_{2y}}{l_1 \cdot l_2}$$
   $$\alpha = \arccos\left(\text{clamp}(\cos(\alpha), -1.0, 1.0)\right) \times \frac{180^\circ}{\pi}$$
4. If $\alpha < \tau_{\text{angle}}$ (default $5.0^\circ$), vertex $V_i$ is flagged as a needle spike.

```text
                  P_i (Spike Vertex, Angle < 5°)
                     /\
                    /  \
                   /    \   Angle alpha < 5.0 deg
                  /      \
                 /        \
          P_{i-1}          P_{i+1}
```

#### C. False Positive Prevention
- **Ring Wrap-Around:** Vertices $P_0$ and $P_{N-1}$ are correctly treated as connected through modular indexing: $P_{-1} \equiv P_{N-2}$.
- **Duplicate Vertex Skipping:** If an adjacent vertex is coincident, the algorithm steps backward or forward until a distinct geometric coordinate is reached before computing vectors.

---

### Check 8: Snap Issue / Unsnapped Node (`CHK_SNAP`)

#### A. Cadastral & GIS Significance
A snap issue occurs when two vertices from neighboring parcels (or non-adjacent parts of the same parcel) are located within close proximity ($\le \tau_{\text{snap}}$, e.g., $1.0\text{ cm}$) but fail to snap together into a single, shared coordinate.
- Unsnapped nodes create micro-gaps and micro-overlaps.
- They lead to topological cracking and parcel fabric disintegration.

#### B. Algorithmic Logic & Sub-Millimeter Metrics
1. The spatial vertex bucket index is queried for all vertices $V_j$ within search radius $R = \tau_{\text{snap}}$ around vertex $V_i$.
2. For each neighbor vertex $V_j$:
   - Euclidean distance in map units:
     $$d(V_i, V_j) = \sqrt{(X_i - X_j)^2 + (Y_i - Y_j)^2} \times \text{ConversionFactorToMeters}$$
3. Condition for Snap Issue:
   $$\epsilon_{\text{xy\_tolerance}} < d(V_i, V_j) \le \tau_{\text{snap}}$$
4. **Sub-Millimeter Formatting Pipeline:**
   - If $d < 0.01\text{ m}$ ($1\text{ cm}$): format in millimeters with up to 4 decimal places:
     $$\text{Formatted} = \text{string.Format}("{0:0.##} mm", d \times 1000.0)$$
     Example: $0.00035\text{ m} \to \mathbf{0.35\text{ mm}}$ (instead of the misleading `0.00 cm`).
   - If $d \ge 0.01\text{ m}$: format in centimeters:
     $$\text{Formatted} = \text{string.Format}("{0:0.##} cm", d \times 100.0)$$

#### C. False Positive Prevention
- **Exclusion of Truly Coincident Vertices:** Vertices sharing identical coordinates ($d < \text{XYTolerance}$) are legitimate shared boundary nodes; flagging them would be catastrophic. They are strictly excluded.
- **OID Deduplication:** Unsnapped vertex pairs $(V_{A}, V_{B})$ are reported with both feature OIDs for bilateral inspection.

---

### Check 9: Redundant Vertex (`CHK_REDUNDANT`)

#### A. Cadastral & GIS Significance
A redundant vertex is an intermediate point that adds **zero geometric or shape information** to a boundary.
- **On Straight Lines:** A vertex sitting directly on a $180^\circ$ straight line.
- **On Continuous Curves:** An arbitrary split vertex placed along a smooth, uniform circular arc or cubic Bézier spline (Curve $\to$ Vertex $\to$ Curve).
- **The Problem:** Redundant vertices inflate dataset storage, complicate parcel drafting, and cause editing errors.
- **The Danger (False Positives):** Blindly deleting collinear vertices can destroy shared cadastral junctions where neighboring parcel lot lines terminate (T-junctions)!

#### B. Algorithmic Logic: Straight Edges
At vertex $V_i$ between $V_{i-1}$ and $V_{i+1}$:
1. Direction vectors: $\vec{u} = V_i - V_{i-1}$, $\vec{w} = V_{i+1} - V_i$.
2. Deflection angle $\theta = \arccos\left(\frac{\vec{u} \cdot \vec{w}}{\|\vec{u}\| \|\vec{w}\|}\right)$.
3. Straight angle:
   $$\Phi = 180^\circ - \theta$$
4. If $\Phi \ge \tau_{\text{redundant\_angle}}$ (default $179.9^\circ$), the vertex is mathematically collinear.

#### C. Algorithmic Logic: Parametric Curves (True Curves)
When vertex $V_i$ connects an incoming segment $S_{\text{in}}$ and outgoing segment $S_{\text{out}}$:
1. **Heterogeneous Transitions are Preserved:**
   - If $S_{\text{in}}$ is a Line and $S_{\text{out}}$ is a Curve (or vice versa): **NOT Redundant** (Valid transition point).
   - If $S_{\text{in}}$ is an Arc and $S_{\text{out}}$ is a Bézier: **NOT Redundant** (Valid spline transition).
2. **Circular Arc Continuity ($C^1$ & Curvature):**
   - Both segments are circular arcs ($S_1, S_2$).
   - **Center Coincidence:** $\|\text{Center}_1 - \text{Center}_2\| \le \tau_{\text{pos}}$.
   - **Radius Coincidence:** $|R_1 - R_2| \le \tau_{\text{pos}}$.
   - **Rotational Consistency:** $\text{Orientation}_1 == \text{Orientation}_2$ (both CW or both CCW; an inflection point where curvature flips is NOT redundant).
   - **Tangent Continuity:** Tangent vectors at $V_i$ must be collinear: $\Phi(\vec{T}_{\text{in}}, \vec{T}_{\text{out}}) \ge \tau_{\text{angle}}$.
   - If all match $\implies$ Redundant intermediate vertex on a single circular arc!
3. **Cubic Bézier Continuity ($C^1$ & $C^2$):**
   - Both segments are cubic Béziers: $B_{\text{in}}(t)$ and $B_{\text{out}}(t)$ ($t \in [0, 1]$).
   - **$C^1$ Tangent Alignment:** The incoming end tangent $\vec{B}'_{\text{in}}(1) = 3(P_3 - P_2)$ and outgoing start tangent $\vec{B}'_{\text{out}}(0) = 3(Q_1 - Q_0)$ must have straight angle $\ge \tau_{\text{angle}}$.
   - **$C^2$ Curvature Continuity:** Second derivative vectors must match across parameter speed ratio $k = \frac{\|\vec{B}'_{\text{out}}(0)\|}{\|\vec{B}'_{\text{in}}(1)\|}$:
     $$\vec{B}''_{\text{out}}(0) \cdot \frac{1}{k^2} \approx \vec{B}''_{\text{in}}(1)$$
   - Kinks, sharp corners, or intentional inflection points are preserved.

```text
       Straight Line Collinear Vertex:
       P_{i-1} -------------------- P_i -------------------- P_{i+1}
                             Angle = 180° (Redundant)

       Continuous Circular Arc Split:
               ..---'''''---..
            .-'               `-.
          .'         P_i         `.   Same Center, Same Radius, Same Direction
         /            *            \  Tangents match at 180°
        |   Arc 1    / \   Arc 2    | (Redundant Split Vertex)
```

#### D. The Junction Guard (Multi-Feature Protection)
Before flagging any collinear or curve-redundant vertex $V_i$ as an error:
1. The engine checks if $V_i$ is coincident with any vertex $W$ of an **adjacent parcel** $F_{\text{neighbor}}$.
2. If coincident with $W$:
   - The engine checks the boundary geometry of $F_{\text{neighbor}}$ at $W$.
   - Does $F_{\text{neighbor}}$ form a corner, turn ($\text{Angle} < \tau_{\text{angle}}$), or transition at $W$?
     - **YES:** Vertex $V_i$ is a **REQUIRED TOPOLOGICAL JUNCTION**. Removing it would break parcel topology. Vertex $V_i$ is **PROTECTED and NOT flagged**.
     - **NO (Both are collinear/redundant):** Both $V_i$ on $F_{\text{current}}$ and $W$ on $F_{\text{neighbor}}$ sit along a straight, shared boundary. **BOTH vertices are flagged as redundant errors** so the cartographer can delete both simultaneously!

```text
       JUNCTION GUARD SCENARIO A: Legitimate Junction (PROTECTED)
       +-----------------------+
       |       Parcel A        |
       +-----------*-----------+ <--- Vertex V is collinear on Parcel A (180°)
       |           |           |      BUT Parcel B & C meet here with a corner!
       | Parcel B  | Parcel C  |      --> V is a MANDATORY JUNCTION (PROTECTED)
       +-----------+-----------+

       JUNCTION GUARD SCENARIO B: Redundant on Shared Edge (FLAGGED ON BOTH)
       +-----------------------+
       |       Parcel A        |
       +-----------*-----------+ <--- Collinear on Parcel A (180°)
       |           *           | <--- Collinear on Parcel B (180°)
       |       Parcel B        |      --> FLAGGED ON BOTH PARCELS!
       +-----------------------+
```

---

### Check 10: Missing Junction / T-Junction (`CHK_JUNCTION`)

#### A. Cadastral & GIS Significance
In cadastral fabrics, a T-junction occurs when the corner vertex of a lot touches the continuous edge of a neighboring lot or right-of-way.
- **The Defect:** If Polygon A has a vertex $V$ touching the edge of Polygon B, but Polygon B **does not have a matching vertex** at that contact point, a **Missing Junction** error exists.
- **Topological Consequence:** Violates planar graph rules ("Must Be Covered By Boundary Of"). During coordinate reprojection or geometric buffer operations, the edge of Polygon B will bow or crack, creating artificial micro-slivers.

#### B. Algorithmic Logic
For each vertex $V_A = (X_A, Y_A)$ of Polygon A:
1. Spatial query identifies nearby segments $S_B = \overline{P_1 P_2}$ of neighboring Polygon B.
2. Orthogonal projection parameter $t$ of $V_A$ onto line $P_1 P_2$:
   $$\vec{u} = P_2 - P_1, \quad \vec{w} = V_A - P_1$$
   $$t = \frac{\vec{w} \cdot \vec{u}}{\|\vec{u}\|^2} = \frac{(X_A - X_1)(X_2 - X_1) + (Y_A - Y_1)(Y_2 - Y_1)}{(X_2 - X_1)^2 + (Y_2 - Y_1)^2}$$
3. Interior Segment Test:
   $$0 < t < 1$$
   (If $t \le 0$ or $t \ge 1$, the projection falls outside the segment interior; handled by snap check).
4. Closest projected point on the segment:
   $$Q = P_1 + t \cdot \vec{u}$$
5. Perpendicular distance:
   $$d_{\perp} = \|V_A - Q\| \times \text{ConversionFactorToMeters}$$
6. Evaluation:
   $$\text{If } d_{\perp} \le \tau_{\text{junction}} \quad (\text{default } 0.10\text{ m} = 10\text{ cm}) \implies \mathbf{Missing\ Junction\ Detected!}$$
   - The engine confirms that Polygon B does not have an existing vertex within $\epsilon_{\text{xy}}$ of $Q$.

```text
       Polygon B Boundary:   P1 ----------------------- Q ----------------------- P2
                                                        |
                                                        | d_perp <= 10 cm
                                                        | (Missing Node on B!)
       Polygon A Boundary:                             V_A
                                                       / \
                                                      /   \  Polygon A
```

#### C. Recommended Remediation
Insert a split vertex into Polygon B at projected coordinate $Q$, creating an explicit, snapped node on both boundaries.

---

## 4. Verification & Deduplication Pipeline Summary

| Check ID | Defect Output | Metric Reported | Tolerance Default | Deduplication / Guard Mechanism |
| :--- | :--- | :--- | :---: | :--- |
| `CHK_INVALID_GEOM` | Polygon / Point | Topology Rule Broken | N/A | OGC Simple Feature validation |
| `CHK_OVERLAP` | Polygon | Area (`m²` or `cm²`) | $0.0001\text{ m}^2$ | Keyed pair hash `(min, max)`; duplicate suppression |
| `CHK_DUPLICATE` | Polygon | Match Ratio ($100\%$) | $\epsilon$ | Symmetric difference area $< \epsilon$ |
| `CHK_GAP` | Polygon | Void Area (`m²` or `cm²`) | $0.001\text{ m}^2$ | Bounded cluster hole extraction |
| `CHK_MULTIPART` | Polygon | Part Count ($N > 1$) | $N > 1$ | Per-feature inspection |
| `CHK_SHORT_SEG` | Polyline | Length (`m` or `cm`) | $0.10\text{ m}$ | Excludes zero-length coincident vertices |
| `CHK_ANGLE` | Point | Angle ($^\circ$) | $5.0^\circ$ | Vector dot product with duplicate vertex stepping |
| `CHK_SNAP` | Point | Distance (`mm` or `cm`) | $0.01\text{ m}$ | Sub-millimeter formatting; excludes identical coords |
| `CHK_REDUNDANT` | Point | Straight Angle ($^\circ$) | $179.9^\circ$ | **Junction Guard**; Curve ($C^1/C^2$) parametric continuity |
| `CHK_JUNCTION` | Point | Distance to Edge | $0.10\text{ m}$ | Segment interior projection; excludes endpoints |

---

<a name="المنطق-الهندسي-وقواعد-التدقيق-باللغة-العربية"></a>
# الجزء الثاني: المنطق الهندسي والرياضي وقواعد التحقق التفصيلية

## 1. الفلسفة المعمارية وأسس محرك التحقق

صُممت إضافة **Geometry QC Analyzer** في ArcGIS Pro لتوفير أعلى معايير الجودة المكانية والطبولوجية للمخططات المساحية، طبقات الأراضي، وقواعد البيانات الجغرافية المؤسسية.

تعتمد معظم أدوات الفحص التقليدية على خوارزميات عامة تفتقر للذكاء الطوبولوجي، مما ينتج عنه أربعة عيوب جوهرية:
1. **الإنذارات الخاطئة عند نقاط الربط القانونية:** اعتبار النقاط المشتركة التي تلتقي عندها قطع أراضي مجاورة كأنها رؤوس زائدة يجب حذفها، مما يؤدي إلى تدمير ترابط المخططات.
2. **تجاهل هندسة المنحنيات الحقيقية (True Curves):** تفكيك الأقواس الدائرية ومنحنيات بيزيير إلى مضلعات خطية متكسرة، مما يؤدي إلى رصد كل نقطة وسيطة كخطأ أو العجز عن كشف التقسيم الزائد للمنحنى.
3. **أخطاء التقريب البصري:** تقريب المسافات غير الملتقطة الأقل من سنتيمتر إلى `0.00 cm`، مما يربك مهندس المساحة.
4. **المعالجة البطيئة وغير الآمنة:** فرض أقفال على قواعد البيانات أو إنشاء ملفات وسيطة على القرص.

تم التغلب على هذه المشكلات جذرياً عبر محرك فحص **داخلي في الذاكرة بالكامل (In-Memory)، فائق السرعة، يعمل بدقة تحت الملليمتر** ومدعوم بفهرسة مكانية ثنائية الأبعاد، وتحليل متقدم لاستمرارية المنحنيات الرياضية، ونظام **حارس نقاط الربط (Junction Guard)**.

---

## 2. الإطار الرياضي والتحويلات الهندسية

### 2.1 توحيد المعايير ونظام الإحداثيات (Spatial Reference)
تُنفذ كافة الحسابات ضمن نظام الإحداثيات الخاص بالطبقة. وعند مقارنة النتائج مع تفاوتات المستخدم:
- يتم تحويل الوحدات الطولية والمساحية بدقة إلى المتر والمتر المربع عبر معامل التحويل الصريح للنظام:
  $$\text{Length}_{\text{meters}} = \text{Length}_{\text{map\_units}} \times \text{ConversionFactorToMeters}$$
  $$\text{Area}_{\text{sq\_meters}} = \text{Area}_{\text{map\_units}} \times (\text{ConversionFactorToMeters})^2$$

### 2.2 الفهرسة المكانية وفهرسة الرؤوس في الذاكرة
لتفادي مشكلة البطء $O(N^2)$ عند فحص آلاف المضلعات:
- **فهرس النطاقات المكانية (2D Envelope R-Tree):** تُفهرس حدود المضلعات مستطيلينياً، ولا يتم فحص أي زوج مضلعات إلا إذا تداخلت نطاقاتهما ضمن مسافة التفاوت.
- **فهرس شبكة الرؤوس (Vertex Grid Bucket Index):** تُقسم الرؤوس جغرافياً إلى خلايا شبكية متساوية، مما يتيح استعلام النقاط المجاورة في زمن قياسي $O(1)$.

---

## 3. الشرح الهندسي والتفصيلي للفحوصات العشرة

---

### الفحص الأول: الهندسة غير الصالحة (`CHK_INVALID_GEOM`)

#### أ. الأهمية المساحية والجغرافية
المضلع غير الصالح هندسياً يمثل خطراً جسيماً على قاعدة البيانات الجغرافية:
- يعطل العمليات المكانية مثل الدمج، القص، وإنشاء الحرم (Buffers).
- يتسبب في انهيار أدوات التحليل الجغرافي أو إرجاع نتائج مضللة.
- يفقد المضلع مساحته الحقيقية، مما يفسد السجلات العقارية ومطالبات الملكية.

#### ب. منطق الكشف الرياضي وحالات الفحص
يفحص المحرك صحة المضلع طوبولوجياً طبقاً لمواصفات OGC و Esri عبر أربعة محاور:
1. **التقاطع الذاتي (Self-Intersection / Bow-Tie):**
   - يحدث عندما يتقاطع ضلعان غير متجاورين في المضلع $S_i \cap S_j \neq \emptyset$.
   - ينتج عن ذلك فصوص متعاكسة في اتجاه الدوران، مما يجعل تعريف "داخل المضلع" و"خارجه" مستحيلاً رياضياً.
2. **الحلقات المعكوسة (Inverted Rings):**
   - تشترط المواصفات القياسية أن تدور حدود المضلع الخارجية في اتجاه عقارب الساعة (CW)، بينما تدور الحلقات الداخلية (الثقوب/الجزر) عكس عقارب الساعة (CCW).
   - يتم التحقق بحساب المساحة الموجهة عبر صيغة جاوس (Shoelace Formula):
     $$A = \frac{1}{2} \sum_{k=0}^{n-1} (X_k Y_{k+1} - X_{k+1} Y_k)$$
     إذا كانت مساحة الحلقة الخارجية سالبة ($A \le 0$)، يتم تسجيل الحلقة كمعكوسة.
3. **الحلقات التالفة (أقل من 3 رؤوس فريدة):**
   - المضلع المغلق يجب أن يتكون من 3 نقاط مختلفة على الأقل بالإضافة لنقطة الإغلاق ($N \ge 4$). وأي حلقة تتكون من نقطتين تنهار إلى خط، وتسجل كخطأ فوري.
4. **الإحداثيات الشاذة (NaN أو Infinity):**
   - فحص وجود أي قيم حسابية غير معرفة داخل إحداثيات الرؤوس.

#### ج. المعالجة الموصى بها
استخدام أداة **Repair Geometry** أو إعادة رسم المضلع وتفكيك العقد المتصالبة.

---

### الفحص الثاني: تداخل المضلعات (`CHK_OVERLAP`)

#### أ. الأهمية المساحية
التداخل بين قطعتي أرض يعني **ازدواجية الملكية العقارية** لنفس المساحة على الطبيعة، وهو من أخطر الأخطاء المساحية التي ينتج عنها نزاعات قضائية ومشاكل في إصدار صكوك الملكية.

#### ب. خوارزمية الفحص والمعادلات
لكل زوج مضلعات مرشح $(F_A, F_B)$:
1. يُحسب التقاطع المساحي:
   $$\Omega_{AB} = \text{Intersection}(F_A, F_B, \text{Dimension} = 2)$$
2. إذا نتجت مساحة مشتركة، يتم تفكيك الأجزاء المتعددة للمقاطعة: $\Omega_{AB} = \bigcup \omega_k$.
3. تُحسب المساحة الدقيقة لكل جزء بالمتر المربع.
4. إذا كانت المساحة $\ge \tau_{\text{overlap}}$ (الافتراضي $0.0001\text{ m}^2 = 1\text{ cm}^2$):
   - يُسجل التداخل كخطأ حرج مع عرض مساحته بدقة بالغة (بالـ $\text{cm}^2$ إذا كانت أقل من متر، أو بالـ $\text{m}^2$).

#### ج. منع النتائج الزائفة
- استبعاد التماس السطحي على الحدود (الذي تكون أبعاده نقطية أو خطية $\text{Dimension} < 2$).
- منع تكرار تسجيل الزوج $(A, B)$ و$(B, A)$ عبر مفتاح فريد مرتب عددياً.
- استبعاد المضلعات المتطابقة بنسبة 100% وتوجيهها لفحص الـ Duplicate Geometry.
- **المعالجة:** استخدام أدوات **Clip** أو **Erase** أو **Align Features**.

---

### الفحص الثالث: المضلعات المكررة (`CHK_DUPLICATE`)

#### أ. الأهمية المساحية
ينشأ التكرار الهندسي عن أخطاء النسخ واللصق المزدوج أو التصدير المتكرر. يؤدي وجود مضلعين متطابقين تماماً مرسومين فوق بعضهما إلى مضاعفة المساحات المحسوبة في التقارير الإحصائية وثقل العرض.

#### ب. الخوارزمية
1. مطابقة النطاق الإحداثي (Bounding Envelope) والمركز الهندسي (Centroid).
2. مطابقة المساحة الإجمالية.
3. حساب الفارق المتماثل (Symmetric Difference):
   $$\Delta(F_A, F_B) = (F_A \setminus F_B) \cup (F_B \setminus F_A)$$
   إذا كانت مساحة الفارق تقترب من الصفر المطلق ($\le \epsilon$)، يُعتبر المضلعان متطابقين بنسبة 100%.
- **المعالجة:** حذف السجل المكرر مع الاحتفاظ بالسجل الأصلي.

---

### الفحص الرابع: الفجوات المحصورة (`CHK_GAP`)

#### أ. الأهمية المساحية
الفجوات المغلقة بين قطع الأراضي تمثل مساحات مهدرة غير مخصصة لأي مالك، وتنتج عن أخطاء الإسقاط وعدم استخدام الالتقاط (Snapping) أثناء رسم المخططات المجاورة.

#### ب. الخوارزمية
1. دمج المضلعات المتجاورة في كتلة موحدة (Union): $\mathcal{U} = \bigcup F_i$.
2. استخراج الحلقات الداخلية (Holes) الناتجة داخل الكتلة المندمجة.
3. تحويل كل حلقة داخلية إلى مضلع مستقل وحساب مساحته.
4. إذا تجاوزت المساحة حد التفاوت ($\ge 0.001\text{ m}^2 = 10\text{ cm}^2$)، يتم تسجيلها كفجوة محصورة مغلقة.
- **المعالجة:** دمج الفجوة في أحد المضلعات المجاورة عبر **Align Features**.

---

### الفحص الخامس: المعالم متعددة الأجزاء (`CHK_MULTIPART`)

#### أ. الأهمية المساحية
في قواعد البيانات العقارية، يشترط أن يرتبط كل رقم قطعة أرض (PIN) بمضلع جغرافي مفرد واحد (Singlepart). وجود مضلعين منفصلين مكانياً في سجل وصفي واحد يربك حسابات المساحة، تحديد الواجهات، وإصدار التراخيص.

#### ب. الخوارزمية
- فحص خاصية `PartCount` للمضلع؛ إذا كانت أكثر من 1، يُسجل المعلم فوراً كمعلم متعدد الأجزاء.
- **المعالجة:** تشغيل أداة **Multipart To Singlepart**.

---

### الفحص السادس: الأضلاع متناهية الصغر (`CHK_SHORT_SEG`)

#### أ. الأهمية المساحية
الأضلاع شديدة القصر (أقل من 10 سم مثلاً) تنشأ من النقرات المزدوجة الخاطئة بالماوس أو عيوب التحويل الآلي للخرائط الورقية. تزيد هذه الأضلاع من حجم قاعدة البيانات وتسبب انكسارات غير مرئية في الحدود.

#### ب. الخوارزمية
- قياس الطول الإقليدي لكل ضلع في المضلع (أو طول القوس للمنحنيات).
- إذا كان الطول يقع بين حد النقطة المتطابقة ($10^{-6}\text{ m}$) وحد التفاوت ($0.10\text{ m}$)، يُسجل الضلع كخطأ ضلع قصير.
- **استبعاد النقاط المتطابقة:** المسافات الصفرية تُستثنى هنا لتُعالج في فحص الالتقاط دون إحداث تشويش.
- **المعالجة:** تبسيط المضلع عبر **Simplify** أو حذف الرأس المسبب.

---

### الفحص السابع: الزوايا الحادة والشاذة (`CHK_ANGLE`)

#### أ. الأهمية المساحية
الزوايا الحادة المتطرفة (أقل من $5^\circ$) لا تمثل حدوداً طبيعية للأراضي، بل هي عبارة عن "إبر" أو أسنان حادة (Spikes) ناجمة عن خطأ في حركة يد الراسم أثناء الرقمنة.

#### ب. الخوارزمية والمعادلات
عند كل رأس $V_i$، يتم تشكيل المتجه القادم والمتجه الخارج، وتُحسب الزاوية الداخلية عبر الضرب القياسي:
$$\cos(\alpha) = \frac{\vec{v}_1 \cdot \vec{v}_2}{\|\vec{v}_1\| \|\vec{v}_2\|}, \quad \alpha = \arccos(\cos(\alpha)) \times \frac{180^\circ}{\pi}$$
إذا كانت الزاوية $\alpha < 5.0^\circ$، يُسجل الرأس كزاوية شاذة.
- تدعم الخوارزمية الالتفاف التلقائي لحلقة المضلع بين أول وآخر رأس، وتتخطى أي نقاط مكررة قبل الحساب.
- **المعالجة:** إزالة الرأس الحاد وتعديل استقامة الحد.

---

### الفحص الثامن: مشاكل الالتقاط والمسافات الدقيقة (`CHK_SNAP`)

#### أ. الأهمية المساحية
يحدث هذا العيب عندما تتقارب نقطتان من مضلعين متجاورين دون أن تنطبقا على نفس الإحداثي تماماً، وتفصل بينهما مسافة متناهية الصغر (مثل 2 ملليمتر أو 5 ملليمتر). يؤدي ذلك إلى فتحات ميكروسكوبية وتشوه طبولوجي.

#### ب. الخوارزمية ومقياس تحت الملليمتر
1. استعلام شبكة الرؤوس للبحث عن أي رأس مجاور يقع ضمن مسافة التفاوت ($\le 1.0\text{ cm}$).
2. حساب المسافة الدقيقة $d(V_i, V_j)$.
3. **الدقة تحت الملليمتر:** إذا كانت المسافة أقل من 1 سم، يتم التقرير بوحدة الملليمتر (`mm`) حتى 3 أو 4 خانات عشرية (مثل `0.35 mm` أو `1.20 mm`) بدلاً من التقريب المضلل `0.00 cm`.
4. **استبعاد الرؤوس المتطابقة تماماً:** إذا كانت المسافة أقل من تفاوت النظام ($d < \text{XYTolerance}$)، فهذا يعني أن الرأسين ملتقطان بشكل مثالي، ويتم استبعادهما من التنبيهات.
- **المعالجة:** تفعيل الـ Snapping واستخدام **Align Features**.

---

### الفحص التاسع: الرؤوس الزائدة والمنحنيات وحارس نقاط الربط (`CHK_REDUNDANT`)

#### أ. الأهمية المساحية
الرأس الزائد هو نقطة تقع على حد المضلع ولكنها **لا تضيف أي قيمة هندسية**:
1. نقطة واقعة على خط مستقيم بزاوية استقامة $180^\circ$.
2. نقطة تقسيم وسيطة غير مبررة واقعة على قوس دائري متصل أو منحنى بيزيير ذي انحناء منتظم.
- حذف هذه الرؤوس يقلل حجم البيانات ويسهل التعديل.
- **الخطر الأكبر:** الحذف الأعمى للرؤوس قد يؤدي إلى حذف نقاط التقاء مضلعات مجاورة في الحدود المشتركة!

#### ب. خوارزمية الخطوط المستقيمة
تُحسب زاوية الاستقامة $\Phi = 180^\circ - \theta$. إذا كانت $\Phi \ge 179.9^\circ$، تُعتبر النقطة زائدة خطياً.

#### ج. خوارزمية المنحنيات الحقيقية (True Curves)
عندما يقع الرأس بين قطاعين منحنيين (منحنى ← رأس ← منحنى):
1. **استثناء الانتقالات الصحيحة:** الانتقال بين خط مستقيم ومنحنى، أو بين قوس دائري ومنحنى بيزيير، يُعتبر انتقالاً هندسياً صحيحاً ومقصوداً ولا يُسجل كخطأ نهائياً.
2. **استمرارية الأقواس الدائرية (Circular Arcs):**
   - تطابق المركز الإحداثي للقوسين ضمن التفاوت.
   - تطابق نصفي القطرين $|R_1 - R_2| \le \text{Tol}$.
   - تطابق اتجاه الدوران (كلاهما في اتجاه عقارب الساعة أو كلاهما عكسها؛ نقطة الانقلاب S-Curve ليست خطأ).
   - استقامة المماسات عند الرأس المشترك ($\ge 179.9^\circ$).
   - عند تحقق هذه الشروط، يُصنف الرأس كرأس زائد على قوس دائري مستمر.
3. **استمرارية منحنيات بيزيير (Cubic Béziers):**
   - التحقق من استمرارية المماس ($C^1 Continuity$).
   - التحقق من استمرارية الانحناء والمشتقة الثانية ($C^2 Continuity$).
   - استثناء الزوايا والانكسارات الحادة المتعمدة.

#### د. نظام حارس نقاط الربط (Junction Guard)
قبل تسجيل أي رأس كخطأ، يقوم المحرك بالتحقق مما إذا كان هذا الرأس منطبقاً على رأس في مضلع مجاور:
- **الحالة الأولى:** إذا كان المضلع المجاور ينكسر عند هذه النقطة بزاوية حقيقية أو يبدأ منها حد جديد (T-Junction)، فهذا الرأس يعتبر **نقطة ربط طوبولوجية إلزامية (Required Topological Junction)**، ويقوم حارس الربط **بحمايته واستثنائه فوراً من الأخطاء**.
- **الحالة الثانية:** إذا كان الرأسان المنطبقان كلاهما على استقامة الخط (زاوية كل منهما تقترب من $180^\circ$) على طول الحد المشترك، يتم تسجيل الخطأ على كلا المضلعين ليتم تنظيفهما معاً دون ترك أحدهما.

---

### الفحص العاشر: العقد المفقودة في الوصلات التبادلية (`CHK_JUNCTION`)

#### أ. الأهمية المساحية
في المخططات التنظيمية، عندما تنتهي حدود قطعة أرض أو شارع عمودياً على حد قطعة أرض أخرى (T-Junction)، يجب أن تحتوي القطعة المقابلة على رأس ملتقط عند نقطة التماس بالضبط.
- غياب هذا الرأس المشترك يؤدي إلى فتح شقوق وفجوات شعرية عند تصدير البيانات أو تغيير أنظمة الإسقاط.

#### ب. خوارزمية الإسقاط العمودي
لكل رأس $V_A$ في المضلع الأول، يتم البحث عن الأضلاع المجاورة $P_1 P_2$ للمضلع الثاني:
1. يُحسب معامل الإسقاط العمودي $t$:
   $$t = \frac{(V_A - P_1) \cdot (P_2 - P_1)}{\|P_2 - P_1\|^2}$$
2. إذا كان $0 < t < 1$ (النقطة تسقط داخل جسم الضلع وليس عند نهاياته):
   - يُحسب البعد العمودي $d_\perp$.
   - إذا كان $d_\perp \le 10\text{ cm}$، ولا يوجد رأس ملتقط في المضلع الثاني، يُسجل خطأ **عقدة مفقودة (Missing Junction)**.
- **المعالجة:** إدراج رأس جديد (Insert Vertex / Split) في المضلع المقابل عند نقطة الإسقاط.

---

## 4. جدول المواصفات والمقارنات الشاملة

| كود الفحص | نوع الخلل المكتشف | وحدة القياس المعروضة | التفاوت القياسي الافتراضي | التقنية المانعة للإنذار الخاطئ |
| :--- | :--- | :---: | :---: | :--- |
| `CHK_INVALID_GEOM` | تقاطعات ذاتية وحلقات معكوسة | نص الخلل الطوبولوجي | — | تدقيق مواصفات OGC/Esri |
| `CHK_OVERLAP` | تداخلات غير قانونية | `m²` أو `cm²` | $0.0001\text{ m}^2$ | استبعاد التماس السطحي ومفتاح الزوج الفريد |
| `CHK_DUPLICATE` | مضلعات متطابقة 100% | نسبة التطابق | $\epsilon$ | حساب مساحة الفارق المتماثل |
| `CHK_GAP` | فجوات هوائية مغلقة | `m²` أو `cm²` | $0.001\text{ m}^2$ | استبعاد الفراغات الخارجية المفتوحة |
| `CHK_MULTIPART` | معالم متعددة الأجزاء | عدد الأجزاء | $> 1$ | فحص بنية أجزاء المضلع |
| `CHK_SHORT_SEG` | أضلاع متناهية الصغر | `cm` أو `m` | $0.10\text{ m}$ | استبعاد النقاط ذات المسافة الصفرية |
| `CHK_ANGLE` | زوايا حادة وإبر شاذة | درجات $\alpha^\circ$ | $5.0^\circ$ | حساب زوايا المتجهات مع تخطي المكررات |
| `CHK_SNAP` | رؤوس غير ملتقطة | `mm` أو `cm` | $0.01\text{ m}$ | دقة الملليمتر واستبعاد النقاط المتطابقة |
| `CHK_REDUNDANT` | رؤوس زائدة على الخطوط والمنحنيات | زاوية الاستقامة | $179.9^\circ$ | **حارس نقاط الربط** وتماثل مشتقات المنحنيات |
| `CHK_JUNCTION` | عقد مفقودة في T-Junction | مسافة الإسقاط | $0.10\text{ m}$ | الإسقاط الداخلي للضلع واستبعاد النهايات |

---
*صُمم هذا المحرك لخدمة مجتمعات نظم المعلومات الجغرافية والمساحة وإدارة الأراضي بأعلى مقاييس الدقة والاحترافية.*
