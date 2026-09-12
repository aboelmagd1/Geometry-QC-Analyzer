using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core
{
    /// <summary>
    /// Fast 2D Spatial Index using hierarchical grid bucketing for high-performance bounding box queries.
    /// Eliminates unrestricted O(n²) comparisons by quickly finding candidate overlapping features.
    /// </summary>
    public class SpatialIndex
    {
        private readonly double _cellSize;
        private readonly Dictionary<long, List<QCFeature>> _grid = new Dictionary<long, List<QCFeature>>();
        private readonly List<QCFeature> _allFeatures = new List<QCFeature>();

        public int FeatureCount => _allFeatures.Count;

        public SpatialIndex(IReadOnlyList<QCFeature> features)
        {
            if (features == null || features.Count == 0)
            {
                _cellSize = 100.0;
                return;
            }

            _allFeatures.AddRange(features);

            // Calculate overall bounding envelope to dynamically determine cell size
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var f in features)
            {
                var env = f.Extent;
                if (env.XMin < minX) minX = env.XMin;
                if (env.YMin < minY) minY = env.YMin;
                if (env.XMax > maxX) maxX = env.XMax;
                if (env.YMax > maxY) maxY = env.YMax;
            }

            double width = Math.Max(maxX - minX, 1.0);
            double height = Math.Max(maxY - minY, 1.0);
            
            // Heuristic cell size: average ~10-20 items per cell based on sqrt(N)
            int targetBucketsAxis = Math.Max(4, (int)Math.Sqrt(features.Count * 2));
            _cellSize = Math.Max(width, height) / targetBucketsAxis;
            if (_cellSize <= 0) _cellSize = 100.0;

            foreach (var feature in features)
            {
                Insert(feature);
            }
        }

        private long GetCellHash(int cellX, int cellY)
        {
            return ((long)cellX << 32) ^ (uint)cellY;
        }

        private void Insert(QCFeature feature)
        {
            var env = feature.Extent;
            int minCellX = (int)Math.Floor(env.XMin / _cellSize);
            int maxCellX = (int)Math.Floor(env.XMax / _cellSize);
            int minCellY = (int)Math.Floor(env.YMin / _cellSize);
            int maxCellY = (int)Math.Floor(env.YMax / _cellSize);

            for (int x = minCellX; x <= maxCellX; x++)
            {
                for (int y = minCellY; y <= maxCellY; y++)
                {
                    long hash = GetCellHash(x, y);
                    if (!_grid.TryGetValue(hash, out var list))
                    {
                        list = new List<QCFeature>();
                        _grid[hash] = list;
                    }
                    list.Add(feature);
                }
            }
        }

        public HashSet<QCFeature> QueryIntersects(Envelope envelope)
        {
            var results = new HashSet<QCFeature>();
            if (_allFeatures.Count == 0 || envelope == null) return results;

            int minCellX = (int)Math.Floor(envelope.XMin / _cellSize);
            int maxCellX = (int)Math.Floor(envelope.XMax / _cellSize);
            int minCellY = (int)Math.Floor(envelope.YMin / _cellSize);
            int maxCellY = (int)Math.Floor(envelope.YMax / _cellSize);

            for (int x = minCellX; x <= maxCellX; x++)
            {
                for (int y = minCellY; y <= maxCellY; y++)
                {
                    long hash = GetCellHash(x, y);
                    if (_grid.TryGetValue(hash, out var list))
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var candidate = list[i];
                            if (EnvelopesIntersect(candidate.Extent, envelope))
                            {
                                results.Add(candidate);
                            }
                        }
                    }
                }
            }

            return results;
        }

        public List<(QCFeature FeatureA, QCFeature FeatureB)> FindCandidateOverlappingPairs()
        {
            var pairs = new List<(QCFeature, QCFeature)>();
            var visitedPairs = new HashSet<string>();

            for (int i = 0; i < _allFeatures.Count; i++)
            {
                var fA = _allFeatures[i];
                var candidates = QueryIntersects(fA.Extent);

                foreach (var fB in candidates)
                {
                    if (fA.Oid == fB.Oid && fA.LayerUri == fB.LayerUri) continue;

                    string pairKey = string.CompareOrdinal($"{fA.LayerUri}_{fA.Oid}", $"{fB.LayerUri}_{fB.Oid}") < 0
                        ? $"{fA.LayerUri}_{fA.Oid}##{fB.LayerUri}_{fB.Oid}"
                        : $"{fB.LayerUri}_{fB.Oid}##{fA.LayerUri}_{fA.Oid}";

                    if (visitedPairs.Add(pairKey))
                    {
                        pairs.Add((fA, fB));
                    }
                }
            }

            return pairs;
        }

        private static bool EnvelopesIntersect(Envelope a, Envelope b)
        {
            return a.XMin <= b.XMax && a.XMax >= b.XMin &&
                   a.YMin <= b.YMax && a.YMax >= b.YMin;
        }
    }
}
