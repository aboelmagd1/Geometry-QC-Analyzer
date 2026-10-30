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
    /// Check 11: Detects true circular arcs that have negligible sagitta or enclosed area and represent straight lines.
    /// Disabled by default.
    /// </summary>
    public class FlatPseudoArcCheck : IGeometryCheck
    {
        public string Id => "CHK_FLAT_PSEUDO_ARC";
        public string Name => "Flat / Pseudo-Arc";
        public string Description => "Detects true circular arcs with negligible sagitta or enclosed area that functionally represent straight lines.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckFlatPseudoArcs;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;
                var sr = context.SpatialReference;
                double sagittaLimitCm = context.Settings.FlatArcSagittaToleranceCm;
                double areaLimitCm2 = context.Settings.FlatArcAreaToleranceCm2;

                for (int i = 0; i < features.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var f = features[i];
                    var poly = f.Geometry;
                    if (poly == null) continue;

                    var parts = GeometryHelpers.GetPartsAsSegments(poly);

                    for (int partIdx = 0; partIdx < parts.Count; partIdx++)
                    {
                        var segs = parts[partIdx];
                        for (int segIdx = 0; segIdx < segs.Count; segIdx++)
                        {
                            var seg = segs[segIdx];
                            if (seg is EllipticArcSegment arc && arc.IsCircular)
                            {
                                double deltaTheta = Math.Abs(arc.CentralAngle);
                                // Skip full circles or degenerate angles
                                if (deltaTheta <= 1e-7 || deltaTheta >= 2.0 * Math.PI - 1e-4) continue;

                                var p1 = arc.StartPoint;
                                var p2 = arc.EndPoint;
                                double dx = p2.X - p1.X;
                                double dy = p2.Y - p1.Y;
                                double chordMapUnits = Math.Sqrt(dx * dx + dy * dy);

                                // Skip degenerate zero-length chords
                                if (chordMapUnits < 1e-6) continue;

                                double r = arc.SemiMajorAxis; // Radius in map units
                                if (r <= 1e-6) continue;

                                // 1. Calculate Sagitta (h) in map units: h = r * (1 - cos(deltaTheta / 2))
                                double hMapUnits = r * (1.0 - Math.Cos(deltaTheta / 2.0));

                                // 2. Calculate Circular Segment Area (A) in map units squared: A = 0.5 * r^2 * (deltaTheta - sin(deltaTheta))
                                double areaMapUnits = 0.5 * r * r * (deltaTheta - Math.Sin(deltaTheta));

                                // Convert thresholds to internal map units
                                double sagittaLimitMapUnits = UnitConverter.CentimetersToMapUnits(sagittaLimitCm, sr, p1);
                                double areaLimitSqMeters = areaLimitCm2 / 10000.0;
                                double areaLimitMapUnitsSq = UnitConverter.SqMetersToMapUnitsSq(areaLimitSqMeters, sr, p1);

                                bool isFlatSagitta = hMapUnits <= sagittaLimitMapUnits;
                                bool isFlatArea = areaMapUnits <= areaLimitMapUnitsSq;

                                if (isFlatSagitta || isFlatArea)
                                {
                                    double hCm = UnitConverter.MapUnitsToCentimeters(hMapUnits, sr, p1);
                                    double hMm = hCm * 10.0;
                                    double areaSqM = UnitConverter.MapUnitsToSqMeters(areaMapUnits, sr, p1);
                                    double areaCm2 = areaSqM * 10000.0;

                                    var (chordVal, chordUnit) = UnitConverter.FormatLinearDistance(chordMapUnits, sr, p1);
                                    var (rVal, rUnit) = UnitConverter.FormatLinearDistance(r, sr, p1);

                                    var midX = (p1.X + p2.X) / 2.0;
                                    var midY = (p1.Y + p2.Y) / 2.0;
                                    var midPoint = MapPointBuilderEx.CreateMapPoint(midX, midY, sr);

                                    var polylineBuilder = new PolylineBuilderEx(sr);
                                    polylineBuilder.AddPart(new Segment[] { arc });
                                    var issueLine = polylineBuilder.ToGeometry();

                                    issues.Add(new IssueResult
                                    {
                                        CheckId = Id,
                                        IssueType = Name,
                                        Oid = f.Oid,
                                        LayerName = f.LayerName,
                                        LayerUri = f.LayerUri,
                                        Location = midPoint,
                                        IssueGeometry = issueLine,
                                        Value = Math.Round(hMm, 4),
                                        Unit = "mm",
                                        Description = $"Flat pseudo-arc: sagitta is {hMm:F3} mm (limit: {sagittaLimitCm * 10.0:F2} mm), area is {areaCm2:F2} cm² (limit: {areaLimitCm2:F1} cm²), radius {rVal:N1} {rUnit}, chord {chordVal:F2} {chordUnit} in part {partIdx + 1}, segment {segIdx + 1}.",
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

