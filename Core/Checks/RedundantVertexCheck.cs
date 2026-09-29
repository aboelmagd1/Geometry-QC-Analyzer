using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core.Checks
{
    /// <summary>
    /// Check 9: Detects redundant (collinear) excess vertices along polygon boundaries where the
    /// angle around the vertex is approximately 180 degrees (exceeds threshold, default 179.9 deg),
    /// as well as redundant vertices located along continuous curves (Curve → Vertex → Curve)
    /// where the intermediate vertex introduces no change in the curve's geometry, radius, center, or tangent.
    /// </summary>
    public class RedundantVertexCheck : IGeometryCheck
    {
        public string Id => "CHK_REDUNDANT";
        public string Name => "Redundant Vertex";
        public string Description => "Detects superfluous collinear vertices along straight polygon edges and redundant vertices along continuous curves.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckRedundantVertices;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;
                double thresholdDeg = context.Settings.RedundantVertexAngleDegrees;

                var sr = context.SpatialReference;
                double junctionTolCm = Math.Max(context.Settings.MissingJunctionToleranceCm, context.Settings.SnapToleranceCm);

                // Pre-map feature parts and segments to support fast O(1) vertex and curve evaluation across features
                var featurePartsMap = new Dictionary<string, List<List<MapPoint>>>();
                var featureSegmentsMap = new Dictionary<string, List<List<Segment>>>();
                for (int i = 0; i < features.Count; i++)
                {
                    var feat = features[i];
                    if (feat.Geometry != null)
                    {
                        string key = $"{feat.LayerUri}##{feat.Oid}";
                        featurePartsMap[key] = GeometryHelpers.GetPartsAsPoints(feat.Geometry);
                        featureSegmentsMap[key] = GeometryHelpers.GetPartsAsSegments(feat.Geometry);
                    }
                }

                for (int i = 0; i < features.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var f = features[i];
                    var poly = f.Geometry;
                    if (poly == null) continue;

                    string fKey = $"{f.LayerUri}##{f.Oid}";
                    if (!featurePartsMap.TryGetValue(fKey, out var parts)) continue;

                    List<List<Segment>>? partSegsList = null;
                    featureSegmentsMap.TryGetValue(fKey, out partSegsList);

                    for (int partIdx = 0; partIdx < parts.Count; partIdx++)
                    {
                        var pts = parts[partIdx];
                        int count = pts.Count;
                        if (count < 3) continue;

                        int n = (pts[0].X == pts[count - 1].X && pts[0].Y == pts[count - 1].Y) ? count - 1 : count;
                        if (n < 3) continue;

                        List<Segment>? segs = null;
                        if (partSegsList != null && partIdx < partSegsList.Count)
                        {
                            segs = partSegsList[partIdx];
                        }

                        for (int v = 0; v < n; v++)
                        {
                            var currPt = pts[v];

                            // Retrieve incoming and outgoing boundary segments
                            Segment? segIn = null;
                            Segment? segOut = null;
                            if (segs != null && segs.Count >= n)
                            {
                                segIn = GetIncomingSegment(segs, v, n);
                                segOut = GetOutgoingSegment(segs, v, n);
                            }

                            bool segInIsCurve = segIn?.IsCurve ?? false;
                            bool segOutIsCurve = segOut?.IsCurve ?? false;

                            // Curve → Vertex → Line or Line → Vertex → Curve:
                            // Valid transition between different segment types; do not flag as redundant.
                            if (segInIsCurve != segOutIsCurve)
                            {
                                continue;
                            }

                            double straightAngle;
                            string desc;

                            if (segInIsCurve && segOutIsCurve)
                            {
                                // Curve → Vertex → Curve:
                                // Analyze actual segment geometry depending on the curve type
                                double tolMapUnits = UnitConverter.CentimetersToMapUnits(junctionTolCm, sr, currPt);
                                tolMapUnits = Math.Max(tolMapUnits, sr?.XYTolerance ?? 0.001);

                                if (!AreCurvesContinuousRedundant(segIn!, segOut!, currPt, thresholdDeg, tolMapUnits, sr, out double? curveAngleOpt, out string curveDetails))
                                {
                                    // Genuine geometric difference (e.g. different center, radius, or tangent direction).
                                    // Valid transition point between curves; not reported as error.
                                    continue;
                                }

                                straightAngle = curveAngleOpt ?? 180.0;

                                // Topological Junction Vertex Guard:
                                // Protect vertex if an adjacent feature meets at this point with a true corner or termination.
                                if (IsJunctionVertex(f, currPt, thresholdDeg, context, tolMapUnits, featurePartsMap, featureSegmentsMap, out var coincidentRedundantOids))
                                {
                                    continue;
                                }

                                desc = (coincidentRedundantOids.Count > 0)
                                    ? $"Redundant curve vertex: continuous {curveDetails} with tangent angle {straightAngle:F3}° (Threshold: {thresholdDeg}°) at vertex {v + 1}, part {partIdx + 1} (coincident with redundant vertex in Feature OID {string.Join(", ", coincidentRedundantOids)})."
                                    : $"Redundant curve vertex: continuous {curveDetails} with tangent angle {straightAngle:F3}° (Threshold: {thresholdDeg}°) at vertex {v + 1}, part {partIdx + 1}.";

                                issues.Add(new IssueResult
                                {
                                    CheckId = Id,
                                    IssueType = Name,
                                    Oid = f.Oid,
                                    RelatedOids = coincidentRedundantOids,
                                    LayerName = f.LayerName,
                                    LayerUri = f.LayerUri,
                                    Location = currPt,
                                    IssueGeometry = currPt,
                                    Value = Math.Round(straightAngle, 3),
                                    Unit = "deg",
                                    Description = desc,
                                    Severity = nameof(IssueSeverity.Info)
                                });
                            }
                            else
                            {
                                // Line → Vertex → Line (or fallback if segment collection unavailable):
                                // Preserves the existing collinear angle check and behavior
                                double? straightAngleOpt = ComputeStraightAngle(pts, v);
                                if (!straightAngleOpt.HasValue) continue;

                                straightAngle = straightAngleOpt.Value;

                                if (straightAngle >= thresholdDeg)
                                {
                                    double tolMapUnits = UnitConverter.CentimetersToMapUnits(junctionTolCm, sr, currPt);
                                    tolMapUnits = Math.Max(tolMapUnits, sr?.XYTolerance ?? 0.001);

                                    // Topological Junction Vertex Guard:
                                    // A collinear vertex is considered a protected topological junction ONLY IF another feature
                                    // meets at this point with a true corner/bend (< thresholdDeg).
                                    // If two coincident vertices both have an angle approaching 180 degrees, neither is a junction,
                                    // and both are considered redundant vertex errors.
                                    if (IsJunctionVertex(f, currPt, thresholdDeg, context, tolMapUnits, featurePartsMap, featureSegmentsMap, out var coincidentRedundantOids))
                                    {
                                        continue;
                                    }

                                    desc = (coincidentRedundantOids.Count > 0)
                                        ? $"Redundant collinear vertex: angle is {straightAngle:F3}° (Threshold: {thresholdDeg}°) at vertex {v + 1}, part {partIdx + 1} (coincident with redundant vertex in Feature OID {string.Join(", ", coincidentRedundantOids)})."
                                        : $"Redundant collinear vertex: angle is {straightAngle:F3}° (Threshold: {thresholdDeg}°) at vertex {v + 1}, part {partIdx + 1}.";

                                    issues.Add(new IssueResult
                                    {
                                        CheckId = Id,
                                        IssueType = Name,
                                        Oid = f.Oid,
                                        RelatedOids = coincidentRedundantOids,
                                        LayerName = f.LayerName,
                                        LayerUri = f.LayerUri,
                                        Location = currPt,
                                        IssueGeometry = currPt,
                                        Value = Math.Round(straightAngle, 3),
                                        Unit = "deg",
                                        Description = desc,
                                        Severity = nameof(IssueSeverity.Info)
                                    });
                                }
                            }
                        }
                    }
                }

                return issues;
            });
        }

        /// <summary>
        /// Retrieves the incoming non-degenerate segment entering vertex v.
        /// </summary>
        public static Segment? GetIncomingSegment(List<Segment> segs, int v, int n)
        {
            if (segs == null || segs.Count == 0 || n <= 0) return null;
            for (int step = 1; step <= n; step++)
            {
                int idx = (v - step + n) % n;
                if (idx >= 0 && idx < segs.Count)
                {
                    var s = segs[idx];
                    if (s.Length > 1e-12) return s;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the outgoing non-degenerate segment leaving vertex v.
        /// </summary>
        public static Segment? GetOutgoingSegment(List<Segment> segs, int v, int n)
        {
            if (segs == null || segs.Count == 0 || n <= 0) return null;
            for (int step = 0; step < n; step++)
            {
                int idx = (v + step) % n;
                if (idx >= 0 && idx < segs.Count)
                {
                    var s = segs[idx];
                    if (s.Length > 1e-12) return s;
                }
            }
            return null;
        }

        /// <summary>
        /// Evaluates whether two consecutive curve segments represent the same continuous geometric path,
        /// where the intermediate vertex introduces no change in the curve's direction, radius, center, or shape.
        /// Curve-type aware: handles EllipticArcSegment and CubicBezierSegment appropriately without assuming
        /// that every curve provides properties like Center or Radius.
        /// </summary>
        public static bool AreCurvesContinuousRedundant(
            Segment segIn,
            Segment segOut,
            MapPoint currPt,
            double thresholdDeg,
            double tolMapUnits,
            SpatialReference? sr,
            out double? straightAngle,
            out string curveDetails)
        {
            straightAngle = null;
            curveDetails = string.Empty;

            if (segIn == null || segOut == null) return false;
            if (!segIn.IsCurve || !segOut.IsCurve) return false;

            // Curve segment types must match (e.g. both EllipticArc or both Bezier)
            if (segIn.SegmentType != segOut.SegmentType)
            {
                return false;
            }

            // 1. EllipticArcSegment (Circular Arc or Elliptic Arc)
            if (segIn is EllipticArcSegment arcIn && segOut is EllipticArcSegment arcOut)
            {
                return EvaluateEllipticArcsRedundancy(arcIn, arcOut, currPt, thresholdDeg, tolMapUnits, sr, out straightAngle, out curveDetails);
            }

            // 2. CubicBezierSegment
            if (segIn is CubicBezierSegment bezIn && segOut is CubicBezierSegment bezOut)
            {
                return EvaluateCubicBeziersRedundancy(bezIn, bezOut, currPt, thresholdDeg, tolMapUnits, out straightAngle, out curveDetails);
            }

            // 3. Other/generic curve types: Do not assume Center or Radius exist.
            // If the curve type does not provide recognizable continuous path parameters,
            // treat as a valid transition point and do not report an error.
            return false;
        }

        /// <summary>
        /// Evaluates whether two consecutive elliptic/circular arc segments represent the same continuous circle or ellipse.
        /// </summary>
        private static bool EvaluateEllipticArcsRedundancy(
            EllipticArcSegment arcIn,
            EllipticArcSegment arcOut,
            MapPoint currPt,
            double thresholdDeg,
            double tolMapUnits,
            SpatialReference? sr,
            out double? straightAngle,
            out string curveDetails)
        {
            straightAngle = null;
            curveDetails = string.Empty;

            // Must both be circular or both non-circular
            if (arcIn.IsCircular != arcOut.IsCircular) return false;

            // Direction of curvature must match (both CCW or both CW).
            // A change in curvature direction (inflection / reverse curve) is a valid geometric transition.
            if (arcIn.IsCounterClockwise != arcOut.IsCounterClockwise) return false;

            // Center point comparison
            var cIn = arcIn.CenterPoint;
            var cOut = arcOut.CenterPoint;
            double cDx = cIn.X - cOut.X;
            double cDy = cIn.Y - cOut.Y;
            if (Math.Sqrt(cDx * cDx + cDy * cDy) > tolMapUnits) return false;

            if (arcIn.IsCircular)
            {
                // Circular arc: SemiMajorAxis represents the radius
                double rIn = arcIn.SemiMajorAxis;
                double rOut = arcOut.SemiMajorAxis;
                if (Math.Abs(rIn - rOut) > tolMapUnits) return false;

                // Tangent direction continuity at vertex currPt
                var tIn = GetEllipticArcTangent(arcIn, currPt);
                var tOut = GetEllipticArcTangent(arcOut, currPt);
                double angle = ComputeVectorStraightAngle(tIn, tOut);
                if (angle < thresholdDeg) return false;

                straightAngle = angle;
                var (val, unit) = UnitConverter.FormatLinearDistance(rIn, sr, currPt);
                curveDetails = $"circular arc (Radius: {val} {unit})";
                return true;
            }
            else
            {
                // Non-circular ellipse: compare semi-major axis, semi-minor axis, and rotation angle
                double majorDiff = Math.Abs(arcIn.SemiMajorAxis - arcOut.SemiMajorAxis);
                double minorDiff = Math.Abs(arcIn.SemiMinorAxis - arcOut.SemiMinorAxis);
                if (majorDiff > tolMapUnits || minorDiff > tolMapUnits) return false;

                // Compare rotation angle modulo PI (due to 180° ellipse symmetry)
                double rotIn = NormalizeAnglePi(arcIn.RotationAngle);
                double rotOut = NormalizeAnglePi(arcOut.RotationAngle);
                double rotDiff = Math.Abs(rotIn - rotOut);
                if (rotDiff > Math.PI / 2.0) rotDiff = Math.PI - rotDiff;
                double rotTolRad = Math.Max((180.0 - thresholdDeg) * (Math.PI / 180.0), 0.002);
                if (rotDiff > rotTolRad) return false;

                // Tangent direction continuity at vertex currPt
                var tIn = GetEllipticArcTangent(arcIn, currPt);
                var tOut = GetEllipticArcTangent(arcOut, currPt);
                double angle = ComputeVectorStraightAngle(tIn, tOut);
                if (angle < thresholdDeg) return false;

                straightAngle = angle;
                curveDetails = "elliptic arc";
                return true;
            }
        }

        /// <summary>
        /// Computes the forward tangent vector of an elliptic arc at the given point.
        /// </summary>
        private static (double X, double Y) GetEllipticArcTangent(EllipticArcSegment arc, MapPoint pt)
        {
            var center = arc.CenterPoint;
            double px = pt.X;
            double py = pt.Y;

            if (arc.IsCircular)
            {
                // Radial vector from center to point
                double rx = px - center.X;
                double ry = py - center.Y;

                // Tangent orthogonal to radial vector in traversal direction
                return arc.IsCounterClockwise ? (-ry, rx) : (ry, -rx);
            }
            else
            {
                // General ellipse: transform point to ellipse coordinate frame
                double phi = arc.RotationAngle;
                double cosPhi = Math.Cos(phi);
                double sinPhi = Math.Sin(phi);

                double dx = px - center.X;
                double dy = py - center.Y;

                double u = dx * cosPhi + dy * sinPhi;
                double v = -dx * sinPhi + dy * cosPhi;

                double a = arc.SemiMajorAxis;
                double b = arc.SemiMinorAxis;
                if (a <= 1e-12 || b <= 1e-12) return (0, 0);

                double tu = -v / (b * b);
                double tv = u / (a * a);

                if (!arc.IsCounterClockwise)
                {
                    tu = -tu;
                    tv = -tv;
                }

                // Transform back to map coordinate system
                double tx = tu * cosPhi - tv * sinPhi;
                double ty = tu * sinPhi + tv * cosPhi;
                return (tx, ty);
            }
        }

        /// <summary>
        /// Evaluates whether two consecutive cubic Bezier segments represent the same continuous cubic curve.
        /// </summary>
        private static bool EvaluateCubicBeziersRedundancy(
            CubicBezierSegment bezIn,
            CubicBezierSegment bezOut,
            MapPoint currPt,
            double thresholdDeg,
            double tolMapUnits,
            out double? straightAngle,
            out string curveDetails)
        {
            straightAngle = null;
            curveDetails = string.Empty;

            // Incoming tangent vector at end of bezIn (t = 1): V - ControlPoint2
            var cp2In = bezIn.ControlPoint2;
            double tinX = currPt.X - cp2In.X;
            double tinY = currPt.Y - cp2In.Y;
            double lenIn = Math.Sqrt(tinX * tinX + tinY * tinY);
            if (lenIn < 1e-10)
            {
                var cp1In = bezIn.ControlPoint1;
                tinX = currPt.X - cp1In.X;
                tinY = currPt.Y - cp1In.Y;
                lenIn = Math.Sqrt(tinX * tinX + tinY * tinY);
            }

            // Outgoing tangent vector at start of bezOut (t = 0): ControlPoint1 - V
            var cp1Out = bezOut.ControlPoint1;
            double toutX = cp1Out.X - currPt.X;
            double toutY = cp1Out.Y - currPt.Y;
            double lenOut = Math.Sqrt(toutX * toutX + toutY * toutY);
            if (lenOut < 1e-10)
            {
                var cp2Out = bezOut.ControlPoint2;
                toutX = cp2Out.X - currPt.X;
                toutY = cp2Out.Y - currPt.Y;
                lenOut = Math.Sqrt(toutX * toutX + toutY * toutY);
            }

            if (lenIn < 1e-12 || lenOut < 1e-12) return false;

            // C1 Continuity: Tangent direction continuity (angle approaches 180°)
            double dot = (tinX * toutX + tinY * toutY) / (lenIn * lenOut);
            dot = Math.Clamp(dot, -1.0, 1.0);
            double deflAngle = Math.Acos(dot) * (180.0 / Math.PI);
            double angle = 180.0 - deflAngle;

            if (angle < thresholdDeg) return false;

            // C2 Continuity: Curvature consistency
            double k = lenOut / lenIn;
            if (k < 1e-6 || k > 1e6) return false;

            var cp1InCoord = bezIn.ControlPoint1;
            double d2inX = cp1InCoord.X - 2.0 * cp2In.X + currPt.X;
            double d2inY = cp1InCoord.Y - 2.0 * cp2In.Y + currPt.Y;

            var cp2OutCoord = bezOut.ControlPoint2;
            double d2outX = currPt.X - 2.0 * cp1Out.X + cp2OutCoord.X;
            double d2outY = currPt.Y - 2.0 * cp1Out.Y + cp2OutCoord.Y;

            // Second derivative of bezOut scaled by k^2 must match second derivative of bezIn
            double diffX = (d2outX / (k * k)) - d2inX;
            double diffY = (d2outY / (k * k)) - d2inY;
            double curvatureDiff = Math.Sqrt(diffX * diffX + diffY * diffY);

            double curveScale = Math.Max(Math.Max(bezIn.Length, bezOut.Length), 1.0);
            double maxCurvatureTol = Math.Max(tolMapUnits * 10.0, curveScale * 0.01);

            if (curvatureDiff > maxCurvatureTol) return false;

            straightAngle = angle;
            curveDetails = "bezier curve";
            return true;
        }

        /// <summary>
        /// Computes the straight angle (180° - deflection angle) between two direction vectors.
        /// </summary>
        private static double ComputeVectorStraightAngle((double X, double Y) v1, (double X, double Y) v2)
        {
            double len1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
            double len2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);
            if (len1 < 1e-12 || len2 < 1e-12) return 0.0;

            double dot = (v1.X * v2.X + v1.Y * v2.Y) / (len1 * len2);
            dot = Math.Clamp(dot, -1.0, 1.0);
            return 180.0 - (Math.Acos(dot) * (180.0 / Math.PI));
        }

        /// <summary>
        /// Normalizes an angle into [0, PI).
        /// </summary>
        private static double NormalizeAnglePi(double angle)
        {
            while (angle < 0.0) angle += Math.PI;
            while (angle >= Math.PI) angle -= Math.PI;
            return angle;
        }

        /// <summary>
        /// Computes the straight angle (180° - deflection angle) at vertex v of a polygon ring.
        /// Automatically skips any duplicate/coincident vertices to find true incoming and outgoing segments.
        /// </summary>
        private static double? ComputeStraightAngle(IReadOnlyList<MapPoint> pts, int v)
        {
            if (pts == null) return null;
            int count = pts.Count;
            if (count < 3) return null;

            int n = (pts[0].X == pts[count - 1].X && pts[0].Y == pts[count - 1].Y) ? count - 1 : count;
            if (n < 3) return null;

            v = ((v % n) + n) % n;
            var currPt = pts[v];

            // Find previous distinct point (skipping coincident vertices)
            MapPoint? prevPt = null;
            for (int step = 1; step < n; step++)
            {
                var p = pts[(v - step + n) % n];
                double dx = currPt.X - p.X;
                double dy = currPt.Y - p.Y;
                if (dx * dx + dy * dy > 1e-12)
                {
                    prevPt = p;
                    break;
                }
            }

            // Find next distinct point (skipping coincident vertices)
            MapPoint? nextPt = null;
            for (int step = 1; step < n; step++)
            {
                var p = pts[(v + step) % n];
                double dx = p.X - currPt.X;
                double dy = p.Y - currPt.Y;
                if (dx * dx + dy * dy > 1e-12)
                {
                    nextPt = p;
                    break;
                }
            }

            if (prevPt == null || nextPt == null) return null;

            // Incoming vector: prev -> curr
            double inX = currPt.X - prevPt.X;
            double inY = currPt.Y - prevPt.Y;
            // Outgoing vector: curr -> next
            double outX = nextPt.X - currPt.X;
            double outY = nextPt.Y - currPt.Y;

            double lenIn = Math.Sqrt(inX * inX + inY * inY);
            double lenOut = Math.Sqrt(outX * outX + outY * outY);

            if (lenIn <= 1e-12 || lenOut <= 1e-12) return null;

            double dot = (inX * outX + inY * outY) / (lenIn * lenOut);
            dot = Math.Clamp(dot, -1.0, 1.0);

            // Deflection angle from straight continuation
            double deflectionAngle = Math.Acos(dot) * (180.0 / Math.PI);
            return 180.0 - deflectionAngle;
        }

        /// <summary>
        /// Determines whether a vertex is a required topological junction vertex.
        /// A vertex is a required junction vertex ONLY IF an adjacent feature meets at this point
        /// with a legitimate corner/bend or non-redundant transition. If two coincident vertices both
        /// represent redundant vertices along the shared boundary, neither is a junction and both
        /// are considered redundant errors.
        /// </summary>
        private static bool IsJunctionVertex(
            QCFeature currentFeature,
            MapPoint pt,
            double thresholdDeg,
            GeometryQCContext context,
            double tolMapUnits,
            Dictionary<string, List<List<MapPoint>>> featurePartsMap,
            Dictionary<string, List<List<Segment>>> featureSegmentsMap,
            out List<long> coincidentRedundantOids)
        {
            coincidentRedundantOids = new List<long>();

            // Query coincident or near-coincident vertices from all features
            var nearby = context.VertexIndex.QueryRadius(pt, tolMapUnits);
            bool hasLegitimateJunction = false;

            for (int i = 0; i < nearby.Count; i++)
            {
                var v = nearby[i].Vertex;
                if (v.FeatureOid == currentFeature.Oid && v.LayerUri == currentFeature.LayerUri)
                {
                    continue;
                }

                string otherKey = $"{v.LayerUri}##{v.FeatureOid}";
                if (featurePartsMap.TryGetValue(otherKey, out var otherParts) &&
                    v.PartIndex >= 0 && v.PartIndex < otherParts.Count)
                {
                    var otherPts = otherParts[v.PartIndex];
                    int countOther = otherPts.Count;
                    int nOther = (countOther >= 3 && otherPts[0].X == otherPts[countOther - 1].X && otherPts[0].Y == otherPts[countOther - 1].Y)
                        ? countOther - 1
                        : countOther;

                    bool isOtherRedundant = false;

                    if (featureSegmentsMap.TryGetValue(otherKey, out var otherPartSegs) &&
                        v.PartIndex < otherPartSegs.Count &&
                        otherPartSegs[v.PartIndex].Count >= nOther &&
                        nOther >= 3)
                    {
                        var otherSegs = otherPartSegs[v.PartIndex];
                        var segInOther = GetIncomingSegment(otherSegs, v.VertexIndex, nOther);
                        var segOutOther = GetOutgoingSegment(otherSegs, v.VertexIndex, nOther);

                        bool inCurve = segInOther?.IsCurve ?? false;
                        bool outCurve = segOutOther?.IsCurve ?? false;

                        if (inCurve && outCurve)
                        {
                            var vPt = otherPts[v.VertexIndex % nOther];
                            if (AreCurvesContinuousRedundant(segInOther!, segOutOther!, vPt, thresholdDeg, tolMapUnits, context.SpatialReference, out _, out _))
                            {
                                isOtherRedundant = true;
                            }
                        }
                        else if (!inCurve && !outCurve)
                        {
                            double? otherAngle = ComputeStraightAngle(otherPts, v.VertexIndex);
                            if (otherAngle.HasValue && otherAngle.Value >= thresholdDeg)
                            {
                                isOtherRedundant = true;
                            }
                        }
                    }
                    else
                    {
                        double? otherAngle = ComputeStraightAngle(otherPts, v.VertexIndex);
                        if (otherAngle.HasValue && otherAngle.Value >= thresholdDeg)
                        {
                            isOtherRedundant = true;
                        }
                    }

                    if (isOtherRedundant)
                    {
                        // Coincident vertex is also redundant -> Both are redundant along the shared boundary
                        if (!coincidentRedundantOids.Contains(v.FeatureOid))
                        {
                            coincidentRedundantOids.Add(v.FeatureOid);
                        }
                    }
                    else
                    {
                        // The other vertex has a legitimate turn, bend, or transition, making this a required topological junction.
                        hasLegitimateJunction = true;
                    }
                }
                else
                {
                    hasLegitimateJunction = true;
                }
            }

            return hasLegitimateJunction;
        }
    }
}
