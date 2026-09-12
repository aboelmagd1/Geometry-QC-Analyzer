using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeometryQCAddIn.Config
{
    public enum DataSourceMode
    {
        DisplayCache = 0,
        LiveQuery = 1
    }

    public enum ExecutionMode
    {
        Auto = 0,
        Manual = 1
    }

    /// <summary>
    /// User configurable settings for Geometry QC analyzer.
    /// Supports JSON serialization and persistence in the user profile.
    /// </summary>
    public class GeometryQCSettings
    {
        // General
        public bool IsEnabled { get; set; } = true;
        public DataSourceMode DataSource { get; set; } = DataSourceMode.DisplayCache;
        public bool EnableForServiceLayers { get; set; } = false;
        public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Manual;
        public int DebounceIntervalMs { get; set; } = 400;

        // 1. Invalid Geometry
        public bool CheckInvalidGeometry { get; set; } = true;

        // 2. Overlaps
        public bool CheckOverlaps { get; set; } = true;
        public double OverlapMinAreaSqMeters { get; set; } = 0.0001;

        // 3. Duplicates
        public bool CheckDuplicates { get; set; } = true;

        // 4. Gaps
        public bool CheckGaps { get; set; } = true;
        public double GapMinAreaSqMeters { get; set; } = 0.001;

        // 5. Multi-Part Features
        public bool CheckMultiPart { get; set; } = true;

        // 6. Short Segments
        public bool CheckShortSegments { get; set; } = true;
        public double ShortSegmentThresholdCm { get; set; } = 10.0; // 10 cm default

        // 7. Angle Issues
        public bool CheckAngleIssues { get; set; } = true;
        public double AngleThresholdDegrees { get; set; } = 5.0; // 5 deg default

        // 8. Snap Issues
        public bool CheckSnapIssues { get; set; } = true;
        public double SnapToleranceCm { get; set; } = 1.0; // 1 cm default

        // 9. Redundant Vertices
        public bool CheckRedundantVertices { get; set; } = true;
        public double RedundantVertexAngleDegrees { get; set; } = 179.9; // 179.9 deg default

        // 10. Missing Junction Vertices
        public bool CheckMissingJunctions { get; set; } = true;
        public double MissingJunctionToleranceCm { get; set; } = 10.0; // 10 cm default (CAD/GIS parcel cadastre standard)

        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArcGISPro_GeometryQC",
            "settings.json");

        private static GeometryQCSettings? _instance;
        public static GeometryQCSettings Instance => _instance ??= Load();

        public static GeometryQCSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<GeometryQCSettings>(json);
                    if (settings != null)
                    {
                        // Self-healing clamp: if tolerance was accidentally set below 1.0 cm (e.g. 0.1 cm = 1 mm), restore to 10.0 cm
                        if (settings.MissingJunctionToleranceCm < 1.0)
                        {
                            settings.MissingJunctionToleranceCm = 10.0;
                            settings.Save();
                        }
                        return settings;
                    }
                }
            }
            catch
            {
                // Fallback to default
            }

            return new GeometryQCSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Non-fatal if saving to disk fails
            }
        }
    }
}
