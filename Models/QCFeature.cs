using ArcGIS.Core.Geometry;

namespace GeometryQCAddIn.Models
{
    /// <summary>
    /// Lightweight, immutable in-memory representation of a polygon feature prepared for QC analysis.
    /// Retains geometry, extent, and signatures to avoid repeated expensive geometry engine calls.
    /// </summary>
    public class QCFeature
    {
        public long Oid { get; }
        public string LayerName { get; }
        public string LayerUri { get; }
        public Polygon Geometry { get; }
        public Envelope Extent { get; }
        public double Area { get; }
        public double Length { get; }
        public int VertexCount { get; }
        public int PartCount { get; }
        public SpatialReference SpatialReference => Geometry.SpatialReference;

        public QCFeature(long oid, string layerName, string layerUri, Polygon geometry)
        {
            Oid = oid;
            LayerName = layerName ?? string.Empty;
            LayerUri = layerUri ?? string.Empty;
            Geometry = geometry;
            Extent = geometry.Extent;
            Area = geometry.Area;
            Length = geometry.Length;
            PartCount = geometry.PartCount;
            VertexCount = geometry.PointCount;
        }
    }
}
