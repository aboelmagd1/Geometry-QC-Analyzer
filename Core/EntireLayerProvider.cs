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
    /// Mode 4: Entire Layer Provider (Local Layers Only).
    /// Queries all features from a specified local polygon feature layer without requiring selection.
    /// Strictly rejects service/web layers.
    /// </summary>
    public class EntireLayerProvider : GeometryDataProvider
    {
        public override async Task<DataAcquisitionResult> AcquireFeaturesAsync(
            MapView mapView,
            GeometryQCSettings settings,
            string? targetLayerUri,
            CancellationToken cancellationToken)
        {
            var result = new DataAcquisitionResult();
            if (mapView == null || mapView.Map == null) return result;

            if (string.IsNullOrEmpty(targetLayerUri))
            {
                result.Warnings.Add("Entire Layer mode requires specifying a specific local polygon layer.");
                return result;
            }

            await QueuedTask.Run(() =>
            {
                var mapSr = mapView.Map.SpatialReference;
                var layers = mapView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
                var targetLayer = layers.FirstOrDefault(l => l.URI == targetLayerUri || l.Name == targetLayerUri);

                if (targetLayer == null)
                {
                    result.Warnings.Add($"Target layer could not be found in active map.");
                    return;
                }

                if (!IsPolygonLayer(targetLayer))
                {
                    result.Warnings.Add($"Target layer '{targetLayer.Name}' is not a polygon layer.");
                    return;
                }

                if (IsServiceLayer(targetLayer))
                {
                    result.Warnings.Add($"Layer '{targetLayer.Name}' is a Service/Web Layer. Entire Layer mode is strictly restricted to local layers (Geodatabase, Shapefile).");
                    return;
                }

                using (var fc = targetLayer.GetFeatureClass())
                {
                    if (fc == null)
                    {
                        result.Warnings.Add($"Unable to open feature class for layer '{targetLayer.Name}'.");
                        return;
                    }

                    var fcDef = fc.GetDefinition();
                    string shapeFieldName = fcDef.GetShapeField();
                    string oidFieldName = fcDef.GetObjectIDField();

                    var queryFilter = new QueryFilter
                    {
                        SubFields = $"{oidFieldName},{shapeFieldName}"
                    };

                    using (var cursor = fc.Search(queryFilter, false))
                    {
                        while (cursor.MoveNext())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            using (var feature = (Feature)cursor.Current)
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
                                    catch
                                    {
                                        // Keep unprojected shape if project fails
                                    }
                                }

                                var qcFeature = new QCFeature(
                                    feature.GetObjectID(),
                                    targetLayer.Name,
                                    targetLayer.URI ?? targetLayer.Name,
                                    shape);

                                result.Features.Add(qcFeature);
                            }
                        }
                    }

                    result.TotalSelectedCount = result.Features.Count;
                    LoggingService.Info($"EntireLayerProvider: Successfully acquired all {result.Features.Count} features from '{targetLayer.Name}'.");
                }

                if (result.UnavailableGeometriesCount > 0)
                {
                    result.Warnings.Add(
                        $"Entire Layer Warning: {result.UnavailableGeometriesCount} features had empty or unreadable geometry in layer '{targetLayer.Name}'.");
                }
            });

            return result;
        }
    }
}
