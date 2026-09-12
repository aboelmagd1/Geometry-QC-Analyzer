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
    /// angle around the vertex is approximately 180 degrees (exceeds threshold, default 179.9 deg).
    /// </summary>
    public class RedundantVertexCheck : IGeometryCheck
    {
        public string Id => "CHK_REDUNDANT";
        public string Name => "Redundant Vertex";
        public string Description => "Detects superfluous collinear vertices along straight polygon edges.";

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

                // Pre-map feature parts to support fast O(1) vertex angle evaluation across features
                var featurePartsMap = new Dictionary<string, List<List<MapPoint>>>();
                for (int i = 0; i < features.Count; i++)
                {
                    var feat = features[i];
                    if (feat.Geometry != null)
                    {
                        string key = $"{feat.LayerUri}##{feat.Oid}";
                        featurePartsMap[key] = GeometryHelpers.GetPartsAsPoints(feat.Geometry);
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

                    for (int partIdx = 0; partIdx < parts.Count; partIdx++)
                    {
                        var pts = parts[partIdx];
                        int count = pts.Count;
                        if (count < 3) continue;

                        int n = (pts[0].X == pts[count - 1].X && pts[0].Y == pts[count - 1].Y) ? count - 1 : count;
                        if (n < 3) continue;

                        for (int v = 0; v < n; v++)
                        {
                            var currPt = pts[v];
                            double? straightAngleOpt = ComputeStraightAngle(pts, v);
                            if (!straightAngleOpt.HasValue) continue;

                            double straightAngle = straightAngleOpt.Value;

                            if (straightAngle >= thresholdDeg)
                            {
                                double tolMapUnits = UnitConverter.CentimetersToMapUnits(junctionTolCm, sr, currPt);
                                tolMapUnits = Math.Max(tolMapUnits, sr?.XYTolerance ?? 0.001);

                                // Topological Junction Vertex Guard:
                                // A collinear vertex is considered a protected topological junction ONLY IF another feature
                                // meets at this point with a true corner/bend (< thresholdDeg).
                                // If two coincident vertices both have an angle approaching 180 degrees, neither is a junction,
                                // and both are considered redundant vertex errors.
                                if (IsJunctionVertex(f, currPt, thresholdDeg, context, tolMapUnits, featurePartsMap, out var coincidentRedundantOids))
                                {
                                    continue;
                                }

                                string desc = (coincidentRedundantOids.Count > 0)
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

                return issues;
            });
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
        /// Determines whether a collinear vertex is a required topological junction vertex.
        /// A vertex is a required junction vertex ONLY IF an adjacent feature meets at this point
        /// with a legitimate corner/bend (< thresholdDeg). If two coincident vertices both have an
        /// angle approaching 180 degrees, neither is a junction and both are considered redundant errors.
        /// </summary>
        private static bool IsJunctionVertex(
            QCFeature currentFeature,
            MapPoint pt,
            double thresholdDeg,
            GeometryQCContext context,
            double tolMapUnits,
            Dictionary<string, List<List<MapPoint>>> featurePartsMap,
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
                    double? otherAngle = ComputeStraightAngle(otherPts, v.VertexIndex);

                    if (otherAngle.HasValue && otherAngle.Value >= thresholdDeg)
                    {
                        // Coincident vertex also approaches 180° -> Both are redundant on a straight boundary!
                        if (!coincidentRedundantOids.Contains(v.FeatureOid))
                        {
                            coincidentRedundantOids.Add(v.FeatureOid);
                        }
                    }
                    else
                    {
                        // The other vertex has a corner/turn (< thresholdDeg), making this a required topological junction.
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
