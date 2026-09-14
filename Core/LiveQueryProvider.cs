using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core
{
    /// <summary>
    /// Mode 2: Live Query Provider.
    /// Manually enabled by user. Queries polygon features using SpatialQueryFilter constrained by
    /// active map extent and selection OIDs, fetching only Shape and ObjectID fields.
    /// </summary>
    public class LiveQueryProvider : GeometryDataProvider
    {
        public override async Task<DataAcquisitionResult> AcquireFeaturesAsync(
            MapView mapView,
            GeometryQCSettings settings,
            string? targetLayerUri,
            CancellationToken cancellationToken)
        {
            var result = new DataAcquisitionResult();
            if (mapView == null || mapView.Map == null) return result;

            await QueuedTask.Run(() =>
            {
                var extent = mapView.Extent;
                var layers = mapView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();

                if (!string.IsNullOrEmpty(targetLayerUri))
                {
                    layers = layers.Where(l => l.URI == targetLayerUri || l.Name == targetLayerUri).ToList();
                }

                foreach (var layer in layers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!IsPolygonLayer(layer))
                    {
                        continue;
                    }

                    if (IsServiceLayer(layer) && !settings.EnableForServiceLayers)
                    {
                        result.Warnings.Add($"Skipped Service Layer '{layer.Name}' (Enable for Service Layer is disabled in settings).");
                        continue;
                    }

                    var selection = layer.GetSelection();
                    if (selection == null || selection.GetCount() == 0)
                    {
                        continue;
                    }

                    var oids = selection.GetObjectIDs().ToList();
                    result.TotalSelectedCount += oids.Count;
                    if (oids.Count == 0) continue;

                    using (var fc = layer.GetFeatureClass())
                    {
                        if (fc == null) continue;

                        var fcDef = fc.GetDefinition();
                        string shapeFieldName = fcDef.GetShapeField();
                        string oidFieldName = fcDef.GetObjectIDField();

                        Geometry? filterGeom = extent;
                        try
                        {
                            var fcSr = fcDef.GetSpatialReference();
                            if (extent != null && fcSr != null && extent.SpatialReference != null && !extent.SpatialReference.IsEqual(fcSr))
                            {
                                filterGeom = GeometryEngine.Instance.Project(extent, fcSr);
                            }
                        }
                        catch
                        {
                            // Fallback to unprojected extent
                        }

                        var spatialFilter = new SpatialQueryFilter
                        {
                            FilterGeometry = filterGeom,
                            SpatialRelationship = SpatialRelationship.Intersects,
                            ObjectIDs = oids,
                            SubFields = $"{oidFieldName},{shapeFieldName}"
                        };

                        using (var rowCursor = fc.Search(spatialFilter, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                using (var feature = (Feature)rowCursor.Current)
                                {
                                    var shape = feature.GetShape() as Polygon;
                                    if (shape == null || shape.IsEmpty)
                                    {
                                        result.UnavailableGeometriesCount++;
                                        continue;
                                    }

                                    var qcFeature = new QCFeature(
                                        feature.GetObjectID(),
                                        layer.Name,
                                        layer.URI ?? layer.Name,
                                        shape);

                                    result.Features.Add(qcFeature);
                                }
                            }
                        }
                    }
                }
            });

            return result;
        }
    }
}
