using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core.Checks
{
    /// <summary>
    /// Check 8: Detects vertices that are nearly coincident (0 < distance <= tolerance)
    /// both within the same feature and between different adjacent features.
    /// Uses spatial vertex grid indexing to maintain fast sub-second performance.
    /// </summary>
    public class SnapIssueCheck : IGeometryCheck
    {
        public string Id => "CHK_SNAP";
        public string Name => "Snap Issue";
        public string Description => "Detects nearly coincident vertices separated by less than the snap tolerance.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckSnapIssues;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var vIndex = context.VertexIndex;
                var allVertices = vIndex.GetAllVertices();
                var sr = context.SpatialReference;
                double snapToleranceCm = context.Settings.SnapToleranceCm;
                var reportedPairs = new HashSet<string>();

                // Pre-map feature parts to identify closing duplicate vertices and ring lengths
                var featurePartCounts = new Dictionary<string, List<int>>();
                for (int i = 0; i < context.Features.Count; i++)
                {
                    var f = context.Features[i];
                    if (f.Geometry != null)
                    {
                        string key = $"{f.LayerUri}##{f.Oid}";
                        var parts = GeometryHelpers.GetPartsAsPoints(f.Geometry);
                        var counts = new List<int>(parts.Count);
                        for (int p = 0; p < parts.Count; p++)
                        {
                            counts.Add(parts[p].Count);
                        }
                        featurePartCounts[key] = counts;
                    }
                }

                // Minimum threshold for exact coincidence: only identical points (within double precision / 1e-7 map units)
                // are considered cleanly snapped. Any separation larger than this is an unsnapped gap.
                double minSnapDist = Math.Max(sr?.XYResolution ?? 1e-7, 1e-7);

                for (int i = 0; i < allVertices.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var vA = allVertices[i];

                    // If this vertex is the closing duplicate point of a closed ring (last point == first point),
                    // skip it to avoid duplicate checks; vertex 0 already represents this location.
                    string featKeyA = $"{vA.LayerUri}##{vA.FeatureOid}";
                    if (featurePartCounts.TryGetValue(featKeyA, out var partCountsA) &&
                        vA.PartIndex >= 0 && vA.PartIndex < partCountsA.Count)
                    {
                        int ptCount = partCountsA[vA.PartIndex];
                        if (ptCount > 1 && vA.VertexIndex == ptCount - 1)
                        {
                            continue;
                        }
                    }

                    double tolMapUnits = UnitConverter.CentimetersToMapUnits(snapToleranceCm, sr, vA.Point);
                    var neighbors = vIndex.QueryRadius(vA.Point, tolMapUnits);

                    for (int j = 0; j < neighbors.Count; j++)
                    {
                        var (vB, dist) = neighbors[j];

                        // Skip identical vertex entry
                        if (vA.FeatureOid == vB.FeatureOid &&
                            vA.LayerUri == vB.LayerUri &&
                            vA.PartIndex == vB.PartIndex &&
                            vA.VertexIndex == vB.VertexIndex)
                        {
                            continue;
                        }

                        // Skip closing duplicate point of neighbor
                        string featKeyB = $"{vB.LayerUri}##{vB.FeatureOid}";
                        if (featurePartCounts.TryGetValue(featKeyB, out var partCountsB) &&
                            vB.PartIndex >= 0 && vB.PartIndex < partCountsB.Count)
                        {
                            int ptCountB = partCountsB[vB.PartIndex];
                            if (ptCountB > 1 && vB.VertexIndex == ptCountB - 1)
                            {
                                continue;
                            }
                        }

                        // Skip adjacent vertices along the same ring (handled by ShortSegmentCheck if too short)
                        if (vA.FeatureOid == vB.FeatureOid &&
                            vA.LayerUri == vB.LayerUri &&
                            vA.PartIndex == vB.PartIndex)
                        {
                            int diff = Math.Abs(vA.VertexIndex - vB.VertexIndex);
                            int ringLen = (featurePartCounts.TryGetValue(featKeyA, out var pc) && vA.PartIndex < pc.Count)
                                ? pc[vA.PartIndex] - 1
                                : 0;

                            if (diff <= 1 || (ringLen > 2 && diff == ringLen - 1))
                            {
                                continue;
                            }
                        }

                        // Must be separated by more than zero/resolution (otherwise they are cleanly snapped / coincident)
                        if (dist <= minSnapDist || dist > tolMapUnits) continue;

                        // Deduplicate symmetric pair (A, B) and (B, A) using canonical vertex identifiers
                        string idA = $"{vA.LayerUri}_{vA.FeatureOid}_{vA.PartIndex}_{vA.VertexIndex}";
                        string idB = $"{vB.LayerUri}_{vB.FeatureOid}_{vB.PartIndex}_{vB.VertexIndex}";

                        // Ensure each unordered pair is processed exactly once
                        if (string.CompareOrdinal(idA, idB) >= 0) continue;

                        string pairKey = $"{idA}##{idB}";
                        if (reportedPairs.Add(pairKey))
                        {
                            var (distVal, unit) = UnitConverter.FormatLinearDistance(dist, sr, vA.Point);

                            issues.Add(new IssueResult
                            {
                                CheckId = Id,
                                IssueType = Name,
                                Oid = vA.FeatureOid,
                                RelatedOids = (vA.FeatureOid != vB.FeatureOid) ? new List<long> { vB.FeatureOid } : new List<long>(),
                                LayerName = vA.LayerName,
                                LayerUri = vA.LayerUri,
                                Location = vA.Point,
                                IssueGeometry = vA.Point,
                                Value = distVal,
                                Unit = unit,
                                Description = (vA.FeatureOid == vB.FeatureOid)
                                    ? $"Intra-feature snap issue: distance is {distVal} {unit} (Tolerance: {snapToleranceCm} cm)."
                                    : $"Inter-feature snap issue with Feature OID {vB.FeatureOid} ({vB.LayerName}): distance is {distVal} {unit} (Tolerance: {snapToleranceCm} cm).",
                                Severity = nameof(IssueSeverity.Warning)
                            });
                        }
                    }
                }

                return issues;
            });
        }
    }
}
