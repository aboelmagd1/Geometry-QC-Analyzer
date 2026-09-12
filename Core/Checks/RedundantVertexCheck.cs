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

                            // Incoming vector: prev -> curr
                            double inX = currPt.X - prevPt.X;
                            double inY = currPt.Y - prevPt.Y;
                            // Outgoing vector: curr -> next
                            double outX = nextPt.X - currPt.X;
                            double outY = nextPt.Y - currPt.Y;

                            double lenIn = Math.Sqrt(inX * inX + inY * inY);
                            double lenOut = Math.Sqrt(outX * outX + outY * outY);

                            if (lenIn <= 1e-12 || lenOut <= 1e-12) continue;

                            double dot = (inX * outX + inY * outY) / (lenIn * lenOut);
                            dot = Math.Clamp(dot, -1.0, 1.0);

                            // Deflection angle from straight continuation
                            double deflectionAngle = Math.Acos(dot) * (180.0 / Math.PI);
                            double straightAngle = 180.0 - deflectionAngle;

                            if (straightAngle >= thresholdDeg)
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
                                    Value = Math.Round(straightAngle, 3),
                                    Unit = "deg",
                                    Description = $"Redundant collinear vertex: angle is {straightAngle:F3}° (Threshold: {thresholdDeg}°) at vertex {v + 1}, part {partIdx + 1}.",
                                    Severity = nameof(IssueSeverity.Info)
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
