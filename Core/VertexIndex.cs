using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core
{
    public struct QCVertexEntry
    {
        public long FeatureOid { get; }
        public string LayerName { get; }
        public string LayerUri { get; }
        public int PartIndex { get; }
        public int VertexIndex { get; }
        public double X { get; }
        public double Y { get; }
        public MapPoint Point { get; }

        public QCVertexEntry(long featureOid, string layerName, string layerUri, int partIndex, int vertexIndex, MapPoint pt)
        {
            FeatureOid = featureOid;
            LayerName = layerName;
            LayerUri = layerUri;
            PartIndex = partIndex;
            VertexIndex = vertexIndex;
            X = pt.X;
            Y = pt.Y;
            Point = pt;
        }
    }

    /// <summary>
    /// Spatial grid index specifically optimized for vertex-level proximity queries.
    /// Provides rapid tolerance-radius searches without comparing millions of vertex combinations.
    /// </summary>
    public class VertexIndex
    {
        private readonly double _cellSize;
        private readonly Dictionary<long, List<QCVertexEntry>> _grid = new Dictionary<long, List<QCVertexEntry>>();
        private readonly List<QCVertexEntry> _allVertices = new List<QCVertexEntry>();

        public int TotalVertexCount => _allVertices.Count;

        public VertexIndex(IReadOnlyList<QCFeature> features, double expectedMaxTolerance = 1.0)
        {
            _cellSize = Math.Max(expectedMaxTolerance * 5.0, 1.0);

            if (features == null) return;

            foreach (var feature in features)
            {
                var poly = feature.Geometry;
                if (poly == null) continue;

                var parts = GeometryHelpers.GetPartsAsPoints(poly);
                for (int partIdx = 0; partIdx < parts.Count; partIdx++)
                {
                    var part = parts[partIdx];
                    for (int vIdx = 0; vIdx < part.Count; vIdx++)
                    {
                        var pt = part[vIdx];
                        var entry = new QCVertexEntry(feature.Oid, feature.LayerName, feature.LayerUri, partIdx, vIdx, pt);
                        _allVertices.Add(entry);
                        Insert(entry);
                    }
                }
            }
        }

        private long GetCellHash(int cellX, int cellY)
        {
            return ((long)cellX << 32) ^ (uint)cellY;
        }

        private void Insert(QCVertexEntry entry)
        {
            int cellX = (int)Math.Floor(entry.X / _cellSize);
            int cellY = (int)Math.Floor(entry.Y / _cellSize);
            long hash = GetCellHash(cellX, cellY);

            if (!_grid.TryGetValue(hash, out var list))
            {
                list = new List<QCVertexEntry>();
                _grid[hash] = list;
            }
            list.Add(entry);
        }

        public List<(QCVertexEntry Vertex, double Distance)> QueryRadius(MapPoint center, double radius)
        {
            var results = new List<(QCVertexEntry, double)>();
            if (center == null || radius <= 0) return results;

            double radiusSq = radius * radius;
            int minCellX = (int)Math.Floor((center.X - radius) / _cellSize);
            int maxCellX = (int)Math.Floor((center.X + radius) / _cellSize);
            int minCellY = (int)Math.Floor((center.Y - radius) / _cellSize);
            int maxCellY = (int)Math.Floor((center.Y + radius) / _cellSize);

            for (int x = minCellX; x <= maxCellX; x++)
            {
                for (int y = minCellY; y <= maxCellY; y++)
                {
                    long hash = GetCellHash(x, y);
                    if (_grid.TryGetValue(hash, out var list))
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var v = list[i];
                            double dx = v.X - center.X;
                            double dy = v.Y - center.Y;
                            double distSq = dx * dx + dy * dy;

                            if (distSq <= radiusSq)
                            {
                                results.Add((v, Math.Sqrt(distSq)));
                            }
                        }
                    }
                }
            }

            return results;
        }

        public bool HasCoincidentVertex(long featureOid, string layerUri, MapPoint pt, double tolerance)
        {
            var nearby = QueryRadius(pt, tolerance);
            for (int i = 0; i < nearby.Count; i++)
            {
                var entry = nearby[i].Vertex;
                if (entry.FeatureOid == featureOid && entry.LayerUri == layerUri)
                {
                    return true;
                }
            }
            return false;
        }

        public IReadOnlyList<QCVertexEntry> GetAllVertices() => _allVertices;
    }
}
