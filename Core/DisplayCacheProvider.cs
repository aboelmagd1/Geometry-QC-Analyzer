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
    /// Default Mode 1: Display Cache Provider.
    /// Extracts geometry strictly from already-loaded selection context in memory without issuing new database/server queries.
    /// Explicitly reports unretrievable shapes rather than silently performing a database fallback.
    /// </summary>
    public class DisplayCacheProvider : GeometryDataProvider
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

                    long selectedInLayer = selection.GetCount();
                    result.TotalSelectedCount += (int)selectedInLayer;

                    // Retrieve shapes directly from selection cursor
                    using (var cursor = selection.Search(null, false))
                    {
                        while (cursor.MoveNext())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            using (var row = cursor.Current)
                            {
                                if (row is Feature feature)
                                {
                                    var shape = feature.GetShape() as Polygon;
                                    if (shape == null || shape.IsEmpty)
                                    {
                                        result.UnavailableGeometriesCount++;
                                        continue;
                                    }

                                    // Map Extent Filtering: check bounding box intersection with active extent
                                    if (extent != null && !extent.IsEmpty)
                                    {
                                        if (!GeometryEngine.Instance.Intersects(extent, shape.Extent))
                                        {
                                            // Outside extent; skip without clipping
                                            result.ExtentFilteredCount++;
                                            continue;
                                        }
                                    }

                                    var qcFeature = new QCFeature(
                                        feature.GetObjectID(),
                                        layer.Name,
                                        layer.URI ?? layer.Name,
                                        shape);

                                    result.Features.Add(qcFeature);
                                }
                                else
                                {
                                    result.UnavailableGeometriesCount++;
                                }
                            }
                        }
                    }
                }

                if (result.UnavailableGeometriesCount > 0)
                {
                    result.Warnings.Add(
                        $"Display Cache Warning: {result.UnavailableGeometriesCount} selected features could not provide geometry " +
                        $"from the currently available display/selection context. No automatic Live Query was performed.");
                }
            });

            return result;
        }
    }
}
