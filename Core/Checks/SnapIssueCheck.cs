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

                for (int i = 0; i < allVertices.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var vA = allVertices[i];
                    double tolMapUnits = UnitConverter.CentimetersToMapUnits(snapToleranceCm, sr, vA.Point);

                    var neighbors = vIndex.QueryRadius(vA.Point, tolMapUnits);

                    for (int j = 0; j < neighbors.Count; j++)
                    {
                        var (vB, dist) = neighbors[j];

                        // Skip identical vertex
                        if (vA.FeatureOid == vB.FeatureOid &&
                            vA.LayerUri == vB.LayerUri &&
                            vA.PartIndex == vB.PartIndex &&
                            vA.VertexIndex == vB.VertexIndex)
                        {
                            continue;
                        }

                        // Skip adjacent vertices along the same ring (handled by ShortSegmentCheck if too short)
                        if (vA.FeatureOid == vB.FeatureOid &&
                            vA.LayerUri == vB.LayerUri &&
                            vA.PartIndex == vB.PartIndex)
                        {
                            int diff = Math.Abs(vA.VertexIndex - vB.VertexIndex);
                            if (diff <= 1)
                            {
                                continue;
                            }
                        }

                        // Must have non-zero distance (0 means perfectly snapped / coincident)
                        if (dist <= 1e-9 || dist > tolMapUnits) continue;

                        // Deduplicate symmetric pair (A, B) and (B, A)
                        string keyA = $"{vA.LayerUri}_{vA.FeatureOid}_{vA.PartIndex}_{vA.VertexIndex}";
                        string keyB = $"{vB.LayerUri}_{vB.FeatureOid}_{vB.PartIndex}_{vB.VertexIndex}";
                        string pairKey = string.CompareOrdinal(keyA, keyB) < 0
                            ? $"{keyA}##{keyB}"
                            : $"{keyB}##{keyA}";

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
