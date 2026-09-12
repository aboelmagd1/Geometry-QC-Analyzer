using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace GeometryQCAddIn.Services
{
    /// <summary>
    /// MapView and Spatial Reference utility service.
    /// </summary>
    public static class MapViewService
    {
        public static async Task<Envelope?> GetActiveExtentAsync()
        {
            return await QueuedTask.Run(() =>
            {
                var mv = MapView.Active;
                return mv?.Extent;
            });
        }

        public static async Task<SpatialReference?> GetActiveSpatialReferenceAsync()
        {
            return await QueuedTask.Run(() =>
            {
                var mv = MapView.Active;
                return mv?.Map?.SpatialReference;
            });
        }
    }
}
