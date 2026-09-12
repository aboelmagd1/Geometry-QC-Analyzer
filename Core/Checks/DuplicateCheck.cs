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
    /// Check 3: Detects duplicate (coincident) polygon geometries.
    /// Employs a multi-tier signature filter (Envelope + Area + Length + Vertex Count) before exact GeometryEngine.Equals.
    /// </summary>
    public class DuplicateCheck : IGeometryCheck
    {
        public string Id => "CHK_DUPLICATE";
        public string Name => "Duplicate Geometry";
        public string Description => "Detects features with identical geometries.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckDuplicates;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;
                var reportedPairs = new HashSet<string>();

                // Fast grouping by (PartCount, VertexCount, rounded Area, rounded Length)
                var buckets = new Dictionary<string, List<QCFeature>>();

                for (int i = 0; i < features.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var f = features[i];
                    string key = $"{f.PartCount}_{f.VertexCount}_{Math.Round(f.Area, 4)}_{Math.Round(f.Length, 4)}";

                    if (!buckets.TryGetValue(key, out var list))
                    {
                        list = new List<QCFeature>();
                        buckets[key] = list;
                    }
                    list.Add(f);
                }

                foreach (var kvp in buckets)
                {
                    var group = kvp.Value;
                    if (group.Count < 2) continue;

                    for (int i = 0; i < group.Count; i++)
                    {
                        for (int j = i + 1; j < group.Count; j++)
                        {
                            context.ThrowIfCancellationRequested();
                            var fA = group[i];
                            var fB = group[j];

                            if (fA.Oid == fB.Oid && fA.LayerUri == fB.LayerUri) continue;

                            string pairKey = string.CompareOrdinal($"{fA.LayerUri}_{fA.Oid}", $"{fB.LayerUri}_{fB.Oid}") < 0
                                ? $"{fA.LayerUri}_{fA.Oid}##{fB.LayerUri}_{fB.Oid}"
                                : $"{fB.LayerUri}_{fB.Oid}##{fA.LayerUri}_{fA.Oid}";

                            if (reportedPairs.Contains(pairKey)) continue;

                            // Bounding box equality check
                            if (!IsEnvelopeEqual(fA.Extent, fB.Extent)) continue;

                            // Exact geometry equality test
                            if (GeometryEngine.Instance.Equals(fA.Geometry, fB.Geometry))
                            {
                                reportedPairs.Add(pairKey);

                                issues.Add(new IssueResult
                                {
                                    CheckId = Id,
                                    IssueType = Name,
                                    Oid = fA.Oid,
                                    RelatedOids = new List<long> { fB.Oid },
                                    LayerName = fA.LayerName,
                                    LayerUri = fA.LayerUri,
                                    Location = fA.Extent.Center,
                                    IssueGeometry = fA.Geometry,
                                    Description = $"Duplicate geometry of Feature OID {fB.Oid} (Layer: '{fB.LayerName}').",
                                    Severity = nameof(IssueSeverity.Error)
                                });
                            }
                        }
                    }
                }

                return issues;
            });
        }

        private static bool IsEnvelopeEqual(Envelope a, Envelope b, double epsilon = 1e-6)
        {
            return Math.Abs(a.XMin - b.XMin) < epsilon &&
                   Math.Abs(a.YMin - b.YMin) < epsilon &&
                   Math.Abs(a.XMax - b.XMax) < epsilon &&
                   Math.Abs(a.YMax - b.YMax) < epsilon;
        }
    }
}
