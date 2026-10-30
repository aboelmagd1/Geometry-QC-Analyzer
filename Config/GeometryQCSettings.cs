using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeometryQCAddIn.Config
{
    /// <summary>
    /// User configurable settings for Geometry QC analyzer.
    /// Supports JSON serialization and persistence in the user profile.
    /// </summary>
    public class GeometryQCSettings
    {
        // General
        public bool IsEnabled { get; set; } = true;
        public bool EnableForServiceLayers { get; set; } = false;
        public int MaxFeaturesToProcess { get; set; } = 500;

        // 1. Invalid Geometry
        public bool CheckInvalidGeometry { get; set; } = true;

        // 2. Overlaps
        public bool CheckOverlaps { get; set; } = true;
        public double OverlapMinAreaSqMeters { get; set; } = 0.00001; // 0.00001 m²

        // 3. Duplicates
        public bool CheckDuplicates { get; set; } = true;

        // 4. Gaps
        public bool CheckGaps { get; set; } = true;
        public double GapMinAreaSqMeters { get; set; } = 0.00001; // 0.00001 m²

        // 5. Multi-Part Features
        public bool CheckMultiPart { get; set; } = true;

        // 6. Short Segments
        public bool CheckShortSegments { get; set; } = true;
        public double ShortSegmentThresholdCm { get; set; } = 15.0; // 15.0 cm default

        // 7. Angle Issues
        public bool CheckAngleIssues { get; set; } = true;
        public double AngleThresholdDegrees { get; set; } = 30.0; // 30° default

        // 8. Snap Issues
        public bool CheckSnapIssues { get; set; } = true;
        public double SnapToleranceCm { get; set; } = 15.0; // 15 cm default

        // 9. Redundant Vertices
        public bool CheckRedundantVertices { get; set; } = true;
        public double RedundantVertexAngleDegrees { get; set; } = 175.0; // 175° default
        public bool FlagArcLineJunctions { get; set; } = false; // Arc-Line junction warning sub-check
        public double ArcLineAngleToleranceDegrees { get; set; } = 175.0; // Arc-Line tolerance default
        public bool ShowWarningsOnMap { get; set; } = true; // Control map graphics for warnings

        // 10. Missing Junction Vertices
        public bool CheckMissingJunctions { get; set; } = true;
        public double MissingJunctionToleranceCm { get; set; } = 10.0; // 10 cm default (CAD/GIS parcel cadastre standard)

        // 11. Flat / Pseudo-Arcs (Disabled by default)
        public bool CheckFlatPseudoArcs { get; set; } = false;
        public double FlatArcSagittaToleranceCm { get; set; } = 0.01; // 0.01 cm (0.1 mm)
        public double FlatArcAreaToleranceCm2 { get; set; } = 10.0; // 10.0 cm²

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
                        // Ensure sane defaults for new properties if loading older json
                        if (settings.MaxFeaturesToProcess < 1)
                        {
                            settings.MaxFeaturesToProcess = 500;
                        }
                        if (settings.ArcLineAngleToleranceDegrees <= 0)
                        {
                            settings.ArcLineAngleToleranceDegrees = settings.RedundantVertexAngleDegrees > 0 
                                ? settings.RedundantVertexAngleDegrees 
                                : 175.0;
                        }
                        if (settings.FlatArcSagittaToleranceCm <= 0)
                        {
                            settings.FlatArcSagittaToleranceCm = 0.01;
                        }
                        if (settings.FlatArcAreaToleranceCm2 <= 0)
                        {
                            settings.FlatArcAreaToleranceCm2 = 10.0;
                        }
                        // Self-healing clamp: if tolerance was accidentally set below 1.0 cm, restore to 10.0 cm
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
