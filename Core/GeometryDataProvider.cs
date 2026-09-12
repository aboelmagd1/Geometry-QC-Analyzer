using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core
{
    public class DataAcquisitionResult
    {
        public List<QCFeature> Features { get; } = new List<QCFeature>();
        public List<string> Warnings { get; } = new List<string>();
        public int TotalSelectedCount { get; set; }
        public int ExtentFilteredCount { get; set; }
        public int UnavailableGeometriesCount { get; set; }
    }

    /// <summary>
    /// Base class for acquiring geometries from active MapView selection and extent.
    /// Strictly read-only; never mutates feature layers or tables.
    /// </summary>
    public abstract class GeometryDataProvider
    {
        public abstract Task<DataAcquisitionResult> AcquireFeaturesAsync(
            MapView mapView,
            GeometryQCSettings settings,
            CancellationToken cancellationToken);

        protected static bool IsPolygonLayer(FeatureLayer layer)
        {
            if (layer == null) return false;
            return layer.ShapeType == esriGeometryType.esriGeometryPolygon;
        }

        protected static bool IsServiceLayer(FeatureLayer layer)
        {
            return GeometryHelpers.IsServiceLayer(layer);
        }
    }
}
