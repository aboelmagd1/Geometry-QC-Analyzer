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
    /// Check 6: Detects boundary segments shorter than a configured threshold (default 10 cm).
    /// Properly converts thresholds according to spatial reference (projected or geographic).
    /// </summary>
    public class ShortSegmentCheck : IGeometryCheck
    {
        public string Id => "CHK_SHORT_SEG";
        public string Name => "Short Segment";
        public string Description => "Detects polygon segments shorter than the configured tolerance.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckShortSegments;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;
                var sr = context.SpatialReference;
                double thresholdCm = context.Settings.ShortSegmentThresholdCm;

                for (int i = 0; i < features.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var f = features[i];
                    var poly = f.Geometry;
                    if (poly == null) continue;

                    var segments = GeometryHelpers.GetAllSegments(poly);

                    for (int s = 0; s < segments.Count; s++)
                    {
                        var (p1, p2, partIdx, segIdx) = segments[s];

                        double thresholdMapUnits = UnitConverter.CentimetersToMapUnits(thresholdCm, sr, p1);
                        double dx = p2.X - p1.X;
                        double dy = p2.Y - p1.Y;
                        double segLength = Math.Sqrt(dx * dx + dy * dy);

                        double minSegmentLength = Math.Max(sr?.XYTolerance ?? 0.0005, 1e-4);
                        if (segLength > minSegmentLength && segLength < thresholdMapUnits)
                        {
                            var (val, unit) = UnitConverter.FormatLinearDistance(segLength, sr, p1);
                            if (val <= 0) continue;
                            var midX = (p1.X + p2.X) / 2.0;
                            var midY = (p1.Y + p2.Y) / 2.0;
                            var midPoint = MapPointBuilderEx.CreateMapPoint(midX, midY, sr);

                            var lineBuilder = new PolylineBuilderEx(sr);
                            lineBuilder.AddPart(new[] { p1, p2 });
                            var segLine = lineBuilder.ToGeometry();

                            issues.Add(new IssueResult
                            {
                                CheckId = Id,
                                IssueType = Name,
                                Oid = f.Oid,
                                LayerName = f.LayerName,
                                LayerUri = f.LayerUri,
                                Location = midPoint,
                                IssueGeometry = segLine,
                                Value = val,
                                Unit = unit,
                                Description = $"Short segment length: {val} {unit} (Threshold: {thresholdCm} cm) in part {partIdx + 1}, segment {segIdx + 1}.",
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
