using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;
using GeometryQCAddIn.Services;

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
                double minThickness = Math.Max(sr?.XYTolerance ?? 0.001, 0.0005);

                for (int i = 0; i < pairs.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var (fA, fB) = pairs[i];

                    try
                    {
                        // Check exact polygon intersection
                        var overlapGeom = GeometryEngine.Instance.Intersection(fA.Geometry, fB.Geometry);
                        if (overlapGeom is Polygon overlapPoly && !overlapPoly.IsEmpty)
                        {
                            // If they are identical duplicate features, skip here so DuplicateCheck handles it
                            if (context.Settings.CheckDuplicates && GeometryEngine.Instance.Equals(fA.Geometry, fB.Geometry))
                            {
                                continue;
                            }

                            // Decompose multipart overlaps into individual discrete overlap patches
                            var overlapParts = GeometryEngine.Instance.MultipartToSinglePart(overlapPoly);
                            if (overlapParts == null || overlapParts.Count == 0)
                            {
                                overlapParts = new List<Geometry> { overlapPoly };
                            }

                            for (int p = 0; p < overlapParts.Count; p++)
                            {
                                if (overlapParts[p] is Polygon partPoly && !partPoly.IsEmpty)
                                {
                                    double partArea = Math.Abs(partPoly.Area);
                                    var repPoint = GeometryEngine.Instance.LabelPoint(partPoly) ?? partPoly.Extent.Center;
                                    double minArea = UnitConverter.SqMetersToMapUnitsSq(context.Settings.OverlapMinAreaSqMeters, sr, repPoint);

                                    if (partArea < minArea) continue;

                                    // Filter out micro-slivers from adjacent shared boundaries (where width and height are below tolerance)
                                    if (partPoly.Extent.Width < minThickness && partPoly.Extent.Height < minThickness)
                                    {
                                        continue;
                                    }

                                    var (val, unit) = UnitConverter.FormatArea(partArea, sr, repPoint);

                                    var issue = new IssueResult
                                    {
                                        CheckId = Id,
                                        IssueType = Name,
                                        Oid = fA.Oid,
                                        RelatedOids = new List<long> { fB.Oid },
                                        LayerName = fA.LayerName,
                                        LayerUri = fA.LayerUri,
                                        Location = repPoint,
                                        IssueGeometry = partPoly,
                                        Value = val,
                                        Unit = unit,
                                        Description = $"Overlaps with Feature OID {fB.Oid} (Layer: '{fB.LayerName}'). Overlap Area: {val} {unit}.",
                                        Severity = nameof(IssueSeverity.Error)
                                    };

                                    issues.Add(issue);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Warning($"Overlap check encountered an issue between Feature {fA.Oid} and {fB.Oid}: {ex.Message}");
                    }
                }

                return issues;
            });
        }
    }
}
