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
    /// Check 10: Detects missing junction (T-junction) vertices where a vertex of Polygon A
    /// touches or intersects the interior of a boundary segment of Polygon B, but Polygon B
    /// lacks a matching coincident vertex at that location.
    /// </summary>
    public class JunctionVertexCheck : IGeometryCheck
    {
        public string Id => "CHK_JUNCTION";
        public string Name => "Missing Junction Vertex";
        public string Description => "Detects vertices touching a neighboring polygon segment without a matching junction vertex.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckMissingJunctions;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var pairs = context.SpatialIndex.FindCandidateOverlappingPairs();
                var sr = context.SpatialReference;
                double tolCm = Math.Max(context.Settings.MissingJunctionToleranceCm, 0.5);
                var reportedJunctions = new Dictionary<string, (IssueResult Issue, List<long> TouchingOids)>();

                for (int p = 0; p < pairs.Count; p++)
                {
                    context.ThrowIfCancellationRequested();
                    var (fA, fB) = pairs[p];

                    InspectMissingJunctions(fA, fB, context, tolCm, sr, reportedJunctions);
                    InspectMissingJunctions(fB, fA, context, tolCm, sr, reportedJunctions);
                }

                foreach (var kvp in reportedJunctions.Values)
                {
                    issues.Add(kvp.Issue);
                }

                return issues;
            });
        }

        private void InspectMissingJunctions(
            QCFeature sourceFeature,
            QCFeature targetFeature,
            GeometryQCContext context,
            double tolCm,
            SpatialReference? sr,
            Dictionary<string, (IssueResult Issue, List<long> TouchingOids)> reportedJunctions)
        {
            var polyA = sourceFeature.Geometry;
            var polyB = targetFeature.Geometry;
            if (polyA == null || polyB == null) return;

            // Extract all target segments
            var targetSegments = GeometryHelpers.GetAllSegments(polyB);

            // Inspect vertices of sourceFeature
            var partsA = GeometryHelpers.GetPartsAsPoints(polyA);

            for (int partIdx = 0; partIdx < partsA.Count; partIdx++)
            {
                var part = partsA[partIdx];
                int vLimit = (part.Count > 1 && part[0].X == part[part.Count - 1].X && part[0].Y == part[part.Count - 1].Y)
                    ? part.Count - 1
                    : part.Count;

                for (int v = 0; v < vLimit; v++)
                {
                    var vA = part[v];
                    double tolMapUnits = Math.Max(UnitConverter.CentimetersToMapUnits(tolCm, sr, vA), Math.Max(sr?.XYTolerance ?? 0.005, 0.02));
                    double tolSq = tolMapUnits * tolMapUnits;

                    for (int s = 0; s < targetSegments.Count; s++)
                    {
                        var (p1, p2, tPartIdx, segIdx) = targetSegments[s];

                        double segDx = p2.X - p1.X;
                        double segDy = p2.Y - p1.Y;
                        double segLenSq = segDx * segDx + segDy * segDy;
                        if (segLenSq <= 1e-12) continue;

                        // Projection of point vA onto segment (p1, p2)
                        double t = ((vA.X - p1.X) * segDx + (vA.Y - p1.Y) * segDy) / segLenSq;

                        // Must project within the segment extent
                        if (t >= 0.0 && t <= 1.0)
                        {
                            double projX = p1.X + t * segDx;
                            double projY = p1.Y + t * segDy;

                            // Must be strictly interior (separated from segment endpoints by more than tolerance)
                            double d1Sq = (projX - p1.X) * (projX - p1.X) + (projY - p1.Y) * (projY - p1.Y);
                            double d2Sq = (projX - p2.X) * (projX - p2.X) + (projY - p2.Y) * (projY - p2.Y);

                            if (d1Sq > tolSq && d2Sq > tolSq)
                            {
                                double distSq = (vA.X - projX) * (vA.X - projX) + (vA.Y - projY) * (vA.Y - projY);

                                if (distSq <= tolSq)
                                {
                                    // Check if target polygon has a coincident vertex at vA
                                    bool hasVertex = context.VertexIndex.HasCoincidentVertex(
                                        targetFeature.Oid, targetFeature.LayerUri, vA, tolMapUnits);

                                    if (!hasVertex)
                                    {
                                        // Spatial clustering key by projection coordinate (2 cm bins) on target polygon
                                        long gridX = (long)Math.Round(projX * 50.0);
                                        long gridY = (long)Math.Round(projY * 50.0);
                                        string key = $"{targetFeature.LayerUri}_{targetFeature.Oid}_{gridX}_{gridY}";

                                        if (reportedJunctions.TryGetValue(key, out var entry))
                                        {
                                            if (!entry.TouchingOids.Contains(sourceFeature.Oid))
                                            {
                                                entry.TouchingOids.Add(sourceFeature.Oid);
                                                if (!entry.Issue.RelatedOids.Contains(sourceFeature.Oid))
                                                {
                                                    entry.Issue.RelatedOids.Add(sourceFeature.Oid);
                                                }
                                                entry.Issue.Description = $"Feature OID {targetFeature.Oid} is missing a junction vertex (Part {tPartIdx + 1}, Segment {segIdx + 1}) where it touches Feature OID {string.Join(", ", entry.TouchingOids)}.";
                                            }
                                        }
                                        else
                                        {
                                            var touchingOids = new List<long> { sourceFeature.Oid };
                                            var issue = new IssueResult
                                            {
                                                CheckId = Id,
                                                IssueType = Name,
                                                Oid = targetFeature.Oid,
                                                RelatedOids = new List<long>(touchingOids),
                                                LayerName = targetFeature.LayerName,
                                                LayerUri = targetFeature.LayerUri,
                                                Location = vA,
                                                IssueGeometry = vA,
                                                Value = Math.Round(Math.Sqrt(distSq), 4),
                                                Unit = "map units",
                                                Description = $"Feature OID {targetFeature.Oid} is missing a junction vertex (Part {tPartIdx + 1}, Segment {segIdx + 1}) where it touches Feature OID {sourceFeature.Oid}.",
                                                Severity = nameof(IssueSeverity.Warning)
                                            };

                                            reportedJunctions[key] = (issue, touchingOids);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
