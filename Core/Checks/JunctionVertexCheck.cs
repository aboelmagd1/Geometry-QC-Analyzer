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
                double tolCm = context.Settings.MissingJunctionToleranceCm;
                var reportedIssues = new HashSet<string>();

                for (int p = 0; p < pairs.Count; p++)
                {
                    context.ThrowIfCancellationRequested();
                    var (fA, fB) = pairs[p];

                    InspectMissingJunctions(fA, fB, context, tolCm, sr, issues, reportedIssues);
                    InspectMissingJunctions(fB, fA, context, tolCm, sr, issues, reportedIssues);
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
            List<IssueResult> issues,
            HashSet<string> reportedIssues)
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
                for (int v = 0; v < part.Count; v++)
                {
                    var vA = part[v];
                    double tolMapUnits = UnitConverter.CentimetersToMapUnits(tolCm, sr, vA);

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
                            double tolSq = tolMapUnits * tolMapUnits;

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
                                        string key = $"{sourceFeature.Oid}_{targetFeature.Oid}_{Math.Round(vA.X, 4)}_{Math.Round(vA.Y, 4)}";
                                        if (reportedIssues.Add(key))
                                        {
                                            issues.Add(new IssueResult
                                            {
                                                CheckId = Id,
                                                IssueType = Name,
                                                Oid = sourceFeature.Oid,
                                                RelatedOids = new List<long> { targetFeature.Oid },
                                                LayerName = sourceFeature.LayerName,
                                                LayerUri = sourceFeature.LayerUri,
                                                Location = vA,
                                                IssueGeometry = vA,
                                                Value = Math.Round(Math.Sqrt(distSq), 4),
                                                Unit = "map units",
                                                Description = $"Vertex on Feature OID {sourceFeature.Oid} touches boundary of Feature OID {targetFeature.Oid} (Part {tPartIdx + 1}, Segment {segIdx + 1}) without a matching junction vertex.",
                                                Severity = nameof(IssueSeverity.Warning)
                                            });
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
