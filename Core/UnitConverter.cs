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
        /// Converts internal coordinate system units squared to square meters.
        /// </summary>
        public static double MapUnitsToSqMeters(double mapUnitsSq, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            if (sr == null) return mapUnitsSq;

            if (sr.IsProjected)
            {
                double factor = sr.Unit?.ConversionFactor ?? 1.0;
                if (factor <= 0) factor = 1.0;
                return mapUnitsSq * (factor * factor);
            }
            else if (sr.IsGeographic)
            {
                double lat = sampleLocation != null ? Math.Abs(sampleLocation.Y) : 0.0;
                if (lat > 89.0) lat = 89.0;
                double rad = lat * Math.PI / 180.0;
                double metersPerDegLon = MetersPerDegreeLat * Math.Cos(rad);
                double metersPerDegLat = MetersPerDegreeLat;
                double sqMetersPerSqDeg = metersPerDegLon * metersPerDegLat;
                return mapUnitsSq * sqMetersPerSqDeg;
            }

            return mapUnitsSq;
        }

        /// <summary>
        /// Formats an area in map units squared to a human-readable area string (e.g. "12.5 m²" or "45 cm²").
        /// </summary>
        public static (double Value, string Unit) FormatArea(double mapUnitsSq, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            double sqMeters = MapUnitsToSqMeters(mapUnitsSq, sr, sampleLocation);
            if (sqMeters < 0.01)
            {
                double sqCm = sqMeters * 10000.0;
                return (Math.Round(sqCm, 2), "cm²");
            }
            else if (sqMeters < 10000.0)
            {
                return (Math.Round(sqMeters, 4), "m²");
            }
            else
            {
                return (Math.Round(sqMeters / 10000.0, 4), "ha");
            }
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
        /// Returns a formatted distance string (e.g. "0.04 mm", "2.5 mm", "6.2 cm", or "1.4 m").
        /// Sub-centimeter distances are formatted in millimeters (mm) to avoid rounding to 0.00 cm.
        /// </summary>
        public static (double Value, string Unit) FormatLinearDistance(double mapUnits, SpatialReference? sr, MapPoint? sampleLocation = null)
        {
            double cm = MapUnitsToCentimeters(mapUnits, sr, sampleLocation);
            if (cm < 0.1)
            {
                // Sub-millimeter: display in millimeters (mm) with up to 3 decimal places so it's never 0.00
                double mm = cm * 10.0;
                double val = Math.Round(mm, 3);
                if (val <= 0.0 && mm > 0.0) val = Math.Round(mm, 4);
                return (val > 0.0 ? val : mm, "mm");
            }
            else if (cm < 1.0)
            {
                // Under 1 cm: display in mm with 2 decimals (e.g. 2.45 mm, 0.8 mm)
                double mm = cm * 10.0;
                return (Math.Round(mm, 2), "mm");
            }
            else if (cm < 100.0)
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
