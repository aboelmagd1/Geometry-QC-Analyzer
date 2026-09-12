using System;
using ArcGIS.Core.Geometry;

namespace GeometryQCAddIn.Core
{
    /// <summary>
    /// Handles coordinate system aware unit conversions between user-facing units (cm, m, deg)
    /// and dataset map units (projected linear units or geographic degrees).
    /// </summary>
    public static class UnitConverter
    {
        private const double EarthCircumferenceMeters = 40075017.0;
        private const double MetersPerDegreeLat = 111319.5;

        /// <summary>
        /// Converts a length in centimeters to the internal coordinate system units of the geometry.
        /// </summary>
        public static double CentimetersToMapUnits(double cm, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            double meters = cm / 100.0;
            return MetersToMapUnits(meters, sr, sampleLocation);
        }

        /// <summary>
        /// Converts a length in meters to internal coordinate system units.
        /// </summary>
        public static double MetersToMapUnits(double meters, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            if (sr == null) return meters;

            if (sr.IsProjected)
            {
                // In ArcGIS Pro SDK, Unit.ConversionFactor gives ratio to base unit (meters for linear)
                double factor = sr.Unit?.ConversionFactor ?? 1.0;
                if (factor <= 0) factor = 1.0;
                return meters / factor;
            }
            else if (sr.IsGeographic)
            {
                // Geographic: degrees. Use latitude of sample location (or 0) for approximation.
                double lat = sampleLocation != null ? Math.Abs(sampleLocation.Y) : 0.0;
                if (lat > 89.0) lat = 89.0;
                double rad = lat * Math.PI / 180.0;
                double metersPerDeg = MetersPerDegreeLat * Math.Cos(rad);
                if (metersPerDeg <= 0.0001) metersPerDeg = MetersPerDegreeLat;
                return meters / metersPerDeg;
            }

            return meters;
        }

        /// <summary>
        /// Converts an area in square meters to internal coordinate system units squared.
        /// </summary>
        public static double SqMetersToMapUnitsSq(double sqMeters, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            if (sr == null) return sqMeters;

            if (sr.IsProjected)
            {
                double factor = sr.Unit?.ConversionFactor ?? 1.0;
                if (factor <= 0) factor = 1.0;
                return sqMeters / (factor * factor);
            }
            else if (sr.IsGeographic)
            {
                double lat = sampleLocation != null ? Math.Abs(sampleLocation.Y) : 0.0;
                if (lat > 89.0) lat = 89.0;
                double rad = lat * Math.PI / 180.0;
                double metersPerDegLon = MetersPerDegreeLat * Math.Cos(rad);
                double metersPerDegLat = MetersPerDegreeLat;
                double sqMetersPerSqDeg = metersPerDegLon * metersPerDegLat;
                return sqMeters / sqMetersPerSqDeg;
            }

            return sqMeters;
        }

        /// <summary>
        /// Converts internal map units back to centimeters for reporting.
        /// </summary>
        public static double MapUnitsToCentimeters(double mapUnits, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            if (sr == null) return mapUnits * 100.0;

            if (sr.IsProjected)
            {
                double factor = sr.Unit?.ConversionFactor ?? 1.0;
                if (factor <= 0) factor = 1.0;
                return (mapUnits * factor) * 100.0;
            }
            else if (sr.IsGeographic)
            {
                double lat = sampleLocation != null ? Math.Abs(sampleLocation.Y) : 0.0;
                double rad = lat * Math.PI / 180.0;
                double metersPerDeg = MetersPerDegreeLat * Math.Cos(rad);
                return (mapUnits * metersPerDeg) * 100.0;
            }

            return mapUnits * 100.0;
        }

        /// <summary>
        /// Returns a formatted distance string (e.g. "6.2 cm" or "1.4 m").
        /// </summary>
        public static (double Value, string Unit) FormatLinearDistance(double mapUnits, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            double cm = MapUnitsToCentimeters(mapUnits, sr, sampleLocation);
            if (cm < 100.0)
            {
                return (Math.Round(cm, 2), "cm");
            }
            else if (cm < 100000.0)
            {
                return (Math.Round(cm / 100.0, 3), "m");
            }
            else
            {
                return (Math.Round(cm / 100000.0, 3), "km");
            }
        }
    }
}
