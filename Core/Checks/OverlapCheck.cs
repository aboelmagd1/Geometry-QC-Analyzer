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
    /// Check 2: Detects overlapping areas between pairs of selected polygon features.
    /// Utilizes 2D Spatial Index candidate pair pruning to prevent O(n²) bottleneck.
    /// </summary>
    public class OverlapCheck : IGeometryCheck
    {
        public string Id => "CHK_OVERLAP";
        public string Name => "Overlap";
        public string Description => "Detects overlapping area between polygon features.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckOverlaps;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var pairs = context.SpatialIndex.FindCandidateOverlappingPairs();
                var sr = context.SpatialReference;
                double minArea = UnitConverter.SqMetersToMapUnitsSq(context.Settings.OverlapMinAreaSqMeters, sr);

                for (int i = 0; i < pairs.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var (fA, fB) = pairs[i];

                    // Check exact polygon intersection
                    var overlapGeom = GeometryEngine.Instance.Intersection(fA.Geometry, fB.Geometry);
                    if (overlapGeom is Polygon overlapPoly && !overlapPoly.IsEmpty)
                    {
                        double area = Math.Abs(overlapPoly.Area);
                        if (area >= minArea)
                        {
                            var repPoint = GeometryEngine.Instance.LabelPoint(overlapPoly) ?? overlapPoly.Extent.Center;
                            var issue = new IssueResult
                            {
                                CheckId = Id,
                                IssueType = Name,
                                Oid = fA.Oid,
                                RelatedOids = new List<long> { fB.Oid },
                                LayerName = fA.LayerName,
                                LayerUri = fA.LayerUri,
                                Location = repPoint,
                                IssueGeometry = overlapPoly,
                                Value = area,
                                Unit = "sq units",
                                Description = $"Overlaps with Feature OID {fB.Oid} (Layer: '{fB.LayerName}'). Overlap Area: {area:F4} map units.",
                                Severity = nameof(IssueSeverity.Error)
                            };

                            issues.Add(issue);
                        }
                    }
                }

                return issues;
            });
        }
    }
}
