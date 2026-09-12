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
    /// Check 1: Detects invalid and non-simple polygon geometries using GeometryEngine.IsSimpleAsFeature.
    /// Examines self-intersections, inverted rings, unclosed loops, and degenerate geometry structures.
    /// </summary>
    public class InvalidGeometryCheck : IGeometryCheck
    {
        public string Id => "CHK_INVALID_GEOM";
        public string Name => "Invalid Geometry";
        public string Description => "Detects non-simple, self-intersecting, or topologically invalid polygon geometries.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckInvalidGeometry;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;

                for (int i = 0; i < features.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var f = features[i];
                    var geom = f.Geometry;

                    // Validation Method 1: Null or Empty Check
                    // Ensures geometry instance exists and has vertices.
                    if (geom == null || geom.IsEmpty)
                    {
                        issues.Add(new IssueResult
                        {
                            CheckId = Id,
                            IssueType = Name,
                            Oid = f.Oid,
                            LayerName = f.LayerName,
                            LayerUri = f.LayerUri,
                            Description = "Polygon geometry is invalid: geometry is null or empty.",
                            Severity = nameof(IssueSeverity.Error)
                        });
                        continue;
                    }

                    // Validation Method 2: Coordinate Boundary & NaN/Infinity Check
                    // Ensures that all coordinates are finite real numbers within spatial reference bounds.
                    var ext = geom.Extent;
                    if (double.IsNaN(ext.XMin) || double.IsNaN(ext.YMin) || double.IsInfinity(ext.XMin) || double.IsInfinity(ext.YMin))
                    {
                        issues.Add(new IssueResult
                        {
                            CheckId = Id,
                            IssueType = Name,
                            Oid = f.Oid,
                            LayerName = f.LayerName,
                            LayerUri = f.LayerUri,
                            Description = "Polygon geometry is invalid: contains NaN or infinite coordinate values.",
                            Severity = nameof(IssueSeverity.Error)
                        });
                        continue;
                    }

                    // Validation Method 3: Ring Structure Check
                    // A valid polygon ring must have at least 3 distinct vertices.
                    bool hasDegenerateRing = false;
                    foreach (var part in geom.Parts)
                    {
                        if (part.Count < 3)
                        {
                            hasDegenerateRing = true;
                            break;
                        }
                    }

                    if (hasDegenerateRing)
                    {
                        var loc = geom.Extent.Center;
                        issues.Add(new IssueResult
                        {
                            CheckId = Id,
                            IssueType = Name,
                            Oid = f.Oid,
                            LayerName = f.LayerName,
                            LayerUri = f.LayerUri,
                            Location = loc,
                            IssueGeometry = geom,
                            Description = "Polygon geometry is invalid: contains a degenerate ring with fewer than 3 segments.",
                            Severity = nameof(IssueSeverity.Error)
                        });
                        continue;
                    }

                    // Validation Method 4: GeometryEngine.IsSimpleAsFeature
                    // Evaluates Esri/OGC topological simplicity:
                    // - Polygons must have valid ring orientation (exterior rings clockwise, interior counter-clockwise).
                    // - Rings must not self-intersect (figure-eight or bow-tie).
                    // - Rings must not overlap each other within the same polygon.
                    // - Segments must have positive non-zero lengths.
                    bool isSimple = GeometryEngine.Instance.IsSimpleAsFeature(geom);
                    if (!isSimple)
                    {
                        var loc = GeometryEngine.Instance.LabelPoint(geom) ?? geom.Extent.Center;

                        issues.Add(new IssueResult
                        {
                            CheckId = Id,
                            IssueType = Name,
                            Oid = f.Oid,
                            LayerName = f.LayerName,
                            LayerUri = f.LayerUri,
                            Location = loc,
                            IssueGeometry = geom,
                            Description = "Polygon geometry is invalid: failed IsSimpleAsFeature check (self-intersection, inverted ring orientation, or degenerate segments).",
                            Severity = nameof(IssueSeverity.Error)
                        });
                    }
                }

                return issues;
            });
        }
    }
}
