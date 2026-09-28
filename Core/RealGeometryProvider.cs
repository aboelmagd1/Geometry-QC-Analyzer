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
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn.Core
{
    /// <summary>
    /// Mode 3: Real Geometry Provider.
    /// Fetches the true, unclipped geometry directly from the underlying FeatureClass
    /// for all selected polygon features, regardless of whether they are visible in the active viewport.
    /// </summary>
    public class RealGeometryProvider : GeometryDataProvider
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
                var mapSr = mapView.Map.SpatialReference;
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
                        if (fc == null)
                        {
                            // Fallback to layer search cursor if fc not accessible directly
                            const int fallbackBatchSize = 1000;
                            for (int i = 0; i < oids.Count; i += fallbackBatchSize)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                var batch = oids.Skip(i).Take(fallbackBatchSize).ToList();
                                var qf = new QueryFilter { ObjectIDs = batch };
                                using var cursor = layer.Search(qf);
                                while (cursor.MoveNext())
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    using var row = cursor.Current;
                                    if (row is Feature feature)
                                    {
                                        var shape = feature.GetShape() as Polygon;
                                        if (shape == null || shape.IsEmpty)
                                        {
                                            result.UnavailableGeometriesCount++;
                                            continue;
                                        }

                                        if (mapSr != null && shape.SpatialReference != null && !mapSr.IsEqual(shape.SpatialReference))
                                        {
                                            try
                                            {
                                                shape = (Polygon)GeometryEngine.Instance.Project(shape, mapSr);
                                            }
                                            catch { }
                                        }

                                        result.Features.Add(new QCFeature(feature.GetObjectID(), layer.Name, layer.URI ?? layer.Name, shape));
                                    }
                                }
                            }
                            continue;
                        }

                        var fcDef = fc.GetDefinition();
                        string shapeFieldName = fcDef.GetShapeField();
                        string oidFieldName = fcDef.GetObjectIDField();

                        // Query directly by ObjectIDs without any spatial / extent filter
                        const int batchSize = 1000;
                        for (int i = 0; i < oids.Count; i += batchSize)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var batchOids = oids.Skip(i).Take(batchSize).ToList();

                            var queryFilter = new QueryFilter
                            {
                                ObjectIDs = batchOids,
                                SubFields = $"{oidFieldName},{shapeFieldName}"
                            };

                            using (var rowCursor = fc.Search(queryFilter, false))
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

                                        // Ensure projection matches map spatial reference for consistent tolerance calculation
                                        if (mapSr != null && shape.SpatialReference != null && !mapSr.IsEqual(shape.SpatialReference))
                                        {
                                            try
                                            {
                                                shape = (Polygon)GeometryEngine.Instance.Project(shape, mapSr);
                                            }
                                            catch
                                            {
                                                // Keep unprojected shape if project fails
                                            }
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
                }

                if (result.UnavailableGeometriesCount > 0)
                {
                    result.Warnings.Add(
                        $"Real Geometry Warning: {result.UnavailableGeometriesCount} selected features had empty or invalid shapes in the data source.");
                }

                LoggingService.Info($"RealGeometryProvider: Acquired {result.Features.Count} features from selection across map.");
            });

            return result;
        }
    }
}
