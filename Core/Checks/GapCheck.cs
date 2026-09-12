using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core.Checks
{
    /// <summary>
    /// Check 4: Detects unintended internal enclosed gaps (holes) between adjacent polygons.
    /// Uses localized cluster unions to find internal voids surrounded by polygons while avoiding global union bottlenecks.
    /// </summary>
    public class GapCheck : IGeometryCheck
    {
        public string Id => "CHK_GAP";
        public string Name => "Gap";
        public string Description => "Detects internal enclosed voids/gaps between adjacent polygons.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckGaps;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;
                if (features.Count < 2) return issues;

                var sr = context.SpatialReference;
                double minArea = UnitConverter.SqMetersToMapUnitsSq(context.Settings.GapMinAreaSqMeters, sr);

                // Group features into localized spatial clusters based on touching/overlapping bounds
                var visited = new HashSet<long>();
                var clusters = new List<List<QCFeature>>();

                for (int i = 0; i < features.Count; i++)
                {
                    var f = features[i];
                    if (visited.Contains(f.Oid)) continue;

                    var cluster = new List<QCFeature> { f };
                    visited.Add(f.Oid);

                    var queue = new Queue<QCFeature>();
                    queue.Enqueue(f);

                    while (queue.Count > 0 && cluster.Count < 50) // limit cluster size for memory safety
                    {
                        var current = queue.Dequeue();
                        // Expand envelope slightly to catch adjacent borders
                        var expEnv = current.Extent.Clone() as Envelope;
                        if (expEnv != null)
                        {
                            var neighbors = context.SpatialIndex.QueryIntersects(expEnv);
                            foreach (var n in neighbors)
                            {
                                if (!visited.Contains(n.Oid))
                                {
                                    visited.Add(n.Oid);
                                    cluster.Add(n);
                                    queue.Enqueue(n);
                                }
                            }
                        }
                    }

                    if (cluster.Count >= 2)
                    {
                        clusters.Add(cluster);
                    }
                }

                // Process each local cluster
                for (int c = 0; c < clusters.Count; c++)
                {
                    context.ThrowIfCancellationRequested();
                    var cluster = clusters[c];
                    var geoms = cluster.Select(x => x.Geometry).Cast<Geometry>().ToList();

                    var unionGeom = GeometryEngine.Instance.Union(geoms) as Polygon;
                    if (unionGeom == null || unionGeom.IsEmpty) continue;

                    // Detect internal enclosed holes / voids
                    var convexHull = GeometryEngine.Instance.ConvexHull(unionGeom) as Polygon;
                    if (convexHull != null && !convexHull.IsEmpty)
                    {
                        var chBoundary = GeometryEngine.Instance.Boundary(convexHull);
                        var difference = GeometryEngine.Instance.Difference(convexHull, unionGeom) as Polygon;
                        if (difference != null && !difference.IsEmpty)
                        {
                            // Deconstruct difference parts into individual candidate polygons
                            var parts = GeometryEngine.Instance.MultipartToSinglePart(difference);
                            foreach (var partGeom in parts)
                            {
                                if (partGeom is Polygon gapPoly && !gapPoly.IsEmpty)
                                {
                                    double area = Math.Abs(gapPoly.Area);
                                    if (area < minArea) continue;

                                    // Strictly distinguish internal gaps from external empty space:
                                    // An internal enclosed gap is surrounded on all sides by the polygon cluster
                                    // and does NOT touch or share boundary with the cluster's convex hull.
                                    if (chBoundary != null && !chBoundary.IsEmpty)
                                    {
                                        var sharedOuter = GeometryEngine.Instance.Intersection(gapPoly, chBoundary);
                                        if (sharedOuter != null && !sharedOuter.IsEmpty && sharedOuter.Length > 1e-4)
                                        {
                                            // Touches external space outside the cluster; ignore
                                            continue;
                                        }
                                    }

                                    // Find features in the cluster that actually border this gap
                                    var borderingOids = new List<long>();
                                    string primaryLayerName = cluster.First().LayerName;
                                    string primaryLayerUri = cluster.First().LayerUri;

                                    for (int k = 0; k < cluster.Count; k++)
                                    {
                                        var cf = cluster[k];
                                        if (GeometryEngine.Instance.Intersects(cf.Geometry, gapPoly))
                                        {
                                            borderingOids.Add(cf.Oid);
                                        }
                                    }

                                    var repPoint = GeometryEngine.Instance.LabelPoint(gapPoly) ?? gapPoly.Extent.Center;

                                    issues.Add(new IssueResult
                                    {
                                        CheckId = Id,
                                        IssueType = Name,
                                        Oid = borderingOids.FirstOrDefault(),
                                        RelatedOids = borderingOids,
                                        LayerName = primaryLayerName,
                                        LayerUri = primaryLayerUri,
                                        Location = repPoint,
                                        IssueGeometry = gapPoly,
                                        Value = area,
                                        Unit = "sq units",
                                        Description = $"Enclosed internal gap detected between features [{string.Join(", ", borderingOids)}]. Area: {area:F4} map units.",
                                        Severity = nameof(IssueSeverity.Warning)
                                    });
                                }
                            }
                        }
                    }
                }

                return issues;
            });
        }
    }
}
