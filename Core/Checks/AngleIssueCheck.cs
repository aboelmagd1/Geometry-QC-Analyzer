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
    /// Check 7: Detects acute internal/external vertex angles smaller than configured threshold (default 5 degrees).
    /// Properly accounts for closed polygon ring connectivity across outer and inner rings.
    /// </summary>
    public class AngleIssueCheck : IGeometryCheck
    {
        public string Id => "CHK_ANGLE";
        public string Name => "Angle Issue";
        public string Description => "Detects sharp/acute angles smaller than configured threshold.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckAngleIssues;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;
                double thresholdDeg = context.Settings.AngleThresholdDegrees;

                for (int i = 0; i < features.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var f = features[i];
                    var poly = f.Geometry;
                    if (poly == null) continue;

                    var parts = GeometryHelpers.GetPartsAsPoints(poly);

                    for (int partIdx = 0; partIdx < parts.Count; partIdx++)
                    {
                        var pts = parts[partIdx];
                        int count = pts.Count;
                        if (count < 3) continue;

                        int n = (pts[0].X == pts[count - 1].X && pts[0].Y == pts[count - 1].Y) ? count - 1 : count;
                        if (n < 3) continue;

                        for (int v = 0; v < n; v++)
                        {
                            var prevPt = pts[(v - 1 + n) % n];
                            var currPt = pts[v];
                            var nextPt = pts[(v + 1) % n];

                            double v1x = prevPt.X - currPt.X;
                            double v1y = prevPt.Y - currPt.Y;
                            double v2x = nextPt.X - currPt.X;
                            double v2y = nextPt.Y - currPt.Y;

                            double len1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                            double len2 = Math.Sqrt(v2x * v2x + v2y * v2y);

                            if (len1 <= 1e-12 || len2 <= 1e-12) continue;

                            double dot = (v1x * v2x + v1y * v2y) / (len1 * len2);
                            dot = Math.Clamp(dot, -1.0, 1.0);

                            double angleDeg = Math.Acos(dot) * (180.0 / Math.PI);

                            if (angleDeg < thresholdDeg)
                            {
                                issues.Add(new IssueResult
                                {
                                    CheckId = Id,
                                    IssueType = Name,
                                    Oid = f.Oid,
                                    LayerName = f.LayerName,
                                    LayerUri = f.LayerUri,
                                    Location = currPt,
                                    IssueGeometry = currPt,
                                    Value = Math.Round(angleDeg, 2),
                                    Unit = "deg",
                                    Description = $"Acute angle detected: {angleDeg:F2}° (Threshold: {thresholdDeg}°) at vertex {v + 1}, part {partIdx + 1}.",
                                    Severity = nameof(IssueSeverity.Warning)
                                });
                            }
                        }
                    }
                }

                return issues;
            });
        }
    }
}
