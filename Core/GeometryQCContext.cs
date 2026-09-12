using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Geometry;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core
{
    /// <summary>
    /// Immutable context passed to all geometry quality control checks for a given run.
    /// Encapsulates prepared features, prebuilt spatial & vertex indexes, map extent, and settings.
    /// </summary>
    public class GeometryQCContext
    {
        public IReadOnlyList<QCFeature> Features { get; }
        public SpatialIndex SpatialIndex { get; }
        public VertexIndex VertexIndex { get; }
        public SpatialReference? SpatialReference { get; }
        public Envelope? MapExtent { get; }
        public GeometryQCSettings Settings { get; }
        public CancellationToken CancellationToken { get; }
        public IProgress<double>? Progress { get; }
        public List<string> Warnings { get; } = new List<string>();

        public GeometryQCContext(
            IReadOnlyList<QCFeature> features,
            SpatialReference? spatialReference,
            Envelope? mapExtent,
            GeometryQCSettings settings,
            CancellationToken cancellationToken,
            IProgress<double>? progress = null)
        {
            Features = features ?? Array.Empty<QCFeature>();
            SpatialReference = spatialReference;
            MapExtent = mapExtent;
            Settings = settings;
            CancellationToken = cancellationToken;
            Progress = progress;

            // Build shared spatial indexes once
            SpatialIndex = new SpatialIndex(Features);

            // Compute conservative max tolerance for vertex index bucketing
            double maxTolMeters = Math.Max(
                Math.Max(settings.SnapToleranceCm, settings.MissingJunctionToleranceCm) / 100.0,
                settings.ShortSegmentThresholdCm / 100.0);
            double maxTolMapUnits = UnitConverter.MetersToMapUnits(maxTolMeters, SpatialReference);
            VertexIndex = new VertexIndex(Features, maxTolMapUnits);
        }

        public void ReportProgress(double percentage)
        {
            Progress?.Report(percentage);
        }

        public void ThrowIfCancellationRequested()
        {
            CancellationToken.ThrowIfCancellationRequested();
        }
    }
}
