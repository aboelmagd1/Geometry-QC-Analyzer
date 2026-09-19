using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GeometryQCAddIn.Models;
using DdlFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace GeometryQCAddIn.Services
{
    /// <summary>
    /// Represents the outcome of an export operation to the Geodatabase.
    /// </summary>
    public class GdbExportResult
    {
        public bool Success { get; set; }
        public string DatasetName { get; set; } = string.Empty;
        public string GdbPath { get; set; } = string.Empty;
        public int FeatureClassCount { get; set; }
        public int TotalFeaturesExported { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> CreatedFeatureClasses { get; set; } = new List<string>();
    }

    /// <summary>
    /// Service responsible for exporting QC error results into the project's Default Geodatabase
    /// within a Feature Dataset named 'QC_Errors' (or incremented with numeric suffix if already exists).
    /// </summary>
    public static class GdbExportService
    {
        private const string BaseDatasetName = "QC_Errors";

        /// <summary>
        /// Exports the given issues to the default geodatabase inside dataset QC_Errors (or QC_Errors_N).
        /// </summary>
        public static async Task<GdbExportResult> ExportErrorsToDefaultGdbAsync(
            IEnumerable<IssueResult> issues,
            SpatialReference? defaultSpatialRef = null,
            IProgress<string>? progress = null)
        {
            var result = new GdbExportResult();

            var issuesList = issues?.Where(i => i != null).ToList() ?? new List<IssueResult>();
            if (issuesList.Count == 0)
            {
                result.Success = false;
                result.Message = "No QC issues to export.";
                return result;
            }

            var gdbPath = Project.Current?.DefaultGeodatabasePath;
            if (string.IsNullOrWhiteSpace(gdbPath) || !Directory.Exists(gdbPath))
            {
                result.Success = false;
                result.Message = "Default Geodatabase for current project was not found.";
                return result;
            }

            result.GdbPath = gdbPath;

            return await QueuedTask.Run(() =>
            {
                try
                {
                    progress?.Report("Connecting to Default Geodatabase...");

                    using var gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(gdbPath)));

                    // 1. Resolve Dataset Name with automatic numeric increment if already exists
                    var existingDatasets = new HashSet<string>(
                        gdb.GetDefinitions<FeatureDatasetDefinition>().Select(d => d.GetName()),
                        StringComparer.OrdinalIgnoreCase);

                    string targetDatasetName = BaseDatasetName;
                    if (existingDatasets.Contains(targetDatasetName))
                    {
                        int counter = 1;
                        while (existingDatasets.Contains($"{BaseDatasetName}_{counter}"))
                        {
                            counter++;
                        }
                        targetDatasetName = $"{BaseDatasetName}_{counter}";
                    }

                    result.DatasetName = targetDatasetName;
                    progress?.Report($"Creating Feature Dataset '{targetDatasetName}'...");

                    // Query all existing feature classes and tables across the entire GDB to prevent any naming collision
                    var existingGdbItemNames = new HashSet<string>(
                        gdb.GetDefinitions<FeatureClassDefinition>().Select(d => d.GetName())
                           .Concat(gdb.GetDefinitions<TableDefinition>().Select(d => d.GetName())),
                        StringComparer.OrdinalIgnoreCase);

                    // 2. Determine Coordinate System
                    var sr = defaultSpatialRef
                             ?? MapView.Active?.Map?.SpatialReference
                             ?? issuesList.FirstOrDefault(i => i.IssueGeometry?.SpatialReference != null)?.IssueGeometry?.SpatialReference
                             ?? issuesList.FirstOrDefault(i => i.Location?.SpatialReference != null)?.Location?.SpatialReference
                             ?? SpatialReferences.WGS84;

                    // 3. Group issues by sanitized Feature Class name and Geometry Type (ensuring GDB-wide uniqueness)
                    var exportGroups = PrepareExportGroups(issuesList, targetDatasetName, existingGdbItemNames);
                    if (exportGroups.Count == 0)
                    {
                        result.Success = false;
                        result.Message = "No exportable geometries found among issues.";
                        return result;
                    }

                    // 4. Build Schema (Feature Dataset + Feature Classes)
                    var schemaBuilder = new SchemaBuilder(gdb);
                    var fdDesc = new FeatureDatasetDescription(targetDatasetName, sr);
                    schemaBuilder.Create(fdDesc);

                    foreach (var group in exportGroups)
                    {
                        var fields = new List<DdlFieldDescription>
                        {
                            new DdlFieldDescription("Issue_Type", FieldType.String) { Length = 100 },
                            new DdlFieldDescription("Check_ID", FieldType.String) { Length = 50 },
                            new DdlFieldDescription("Source_OID", FieldType.Integer),
                            new DdlFieldDescription("Related_OIDs", FieldType.String) { Length = 255 },
                            new DdlFieldDescription("Layer_Name", FieldType.String) { Length = 100 },
                            new DdlFieldDescription("Severity", FieldType.String) { Length = 20 },
                            new DdlFieldDescription("Metric_Val", FieldType.Double),
                            new DdlFieldDescription("Metric_Unit", FieldType.String) { Length = 20 },
                            new DdlFieldDescription("Description", FieldType.String) { Length = 500 },
                            new DdlFieldDescription("Export_Time", FieldType.String) { Length = 50 }
                        };

                        var shapeDesc = new ShapeDescription(group.TargetGeometryType, sr);
                        var fcDesc = new FeatureClassDescription(group.FeatureClassName, fields, shapeDesc);
                        schemaBuilder.Create(fdDesc, fcDesc);
                    }

                    progress?.Report("Applying schema changes...");
                    bool built = schemaBuilder.Build();
                    if (!built)
                    {
                        string err = string.Join("; ", schemaBuilder.ErrorMessages);
                        result.Success = false;
                        result.Message = $"Failed to create dataset in GDB: {err}";
                        LoggingService.Error(result.Message);
                        return result;
                    }

                    // 5. Insert Issue Records into newly created Feature Classes via InsertCursor
                    using var fd = gdb.OpenDataset<FeatureDataset>(targetDatasetName);

                    int totalExported = 0;
                    string exportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var insertErrors = new List<string>();

                    foreach (var group in exportGroups)
                    {
                        progress?.Report($"Writing {group.Issues.Count} features to '{group.FeatureClassName}'...");

                        try
                        {
                            using var fc = fd.OpenDataset<FeatureClass>(group.FeatureClassName);
                            using var rowBuffer = fc.CreateRowBuffer();
                            using var cursor = fc.CreateInsertCursor();
                            string shapeFieldName = fc.GetDefinition().GetShapeField();

                            int groupExported = 0;
                            foreach (var item in group.Issues)
                            {
                                try
                                {
                                    var geom = item.geom;
                                    if (geom == null || geom.IsEmpty) continue;

                                    // Ensure spatial reference is properly assigned and projected
                                    geom = EnsureSpatialReference(geom, sr);
                                    if (geom == null || geom.IsEmpty) continue;

                                    // Ensure geometry matches target shape type of the feature class
                                    if (geom.GeometryType != group.TargetGeometryType) continue;

                                    rowBuffer[shapeFieldName] = geom;
                                    rowBuffer["Issue_Type"] = Truncate(item.issue.IssueType, 100);
                                    rowBuffer["Check_ID"] = Truncate(item.issue.CheckId, 50);
                                    if (item.issue.Oid.HasValue)
                                        rowBuffer["Source_OID"] = unchecked((int)item.issue.Oid.Value);
                                    if (item.issue.RelatedOids != null && item.issue.RelatedOids.Count > 0)
                                        rowBuffer["Related_OIDs"] = Truncate(string.Join(", ", item.issue.RelatedOids), 255);
                                    rowBuffer["Layer_Name"] = Truncate(item.issue.LayerName, 100);
                                    rowBuffer["Severity"] = Truncate(item.issue.Severity, 20);
                                    if (item.issue.Value.HasValue)
                                        rowBuffer["Metric_Val"] = item.issue.Value.Value;
                                    rowBuffer["Metric_Unit"] = Truncate(item.issue.Unit, 20);
                                    rowBuffer["Description"] = Truncate(item.issue.Description, 500);
                                    rowBuffer["Export_Time"] = exportTime;

                                    cursor.Insert(rowBuffer);
                                    groupExported++;
                                }
                                catch (Exception rowEx)
                                {
                                    if (insertErrors.Count < 5)
                                    {
                                        insertErrors.Add($"[{group.FeatureClassName}] {rowEx.Message}");
                                    }
                                    LoggingService.Warning($"Skipped issue in '{group.FeatureClassName}': {rowEx.Message}");
                                }
                            }

                            cursor.Flush();
                            totalExported += groupExported;
                            result.CreatedFeatureClasses.Add(group.FeatureClassName);
                            LoggingService.Info($"Exported {groupExported} features to '{group.FeatureClassName}'.");
                        }
                        catch (Exception fcEx)
                        {
                            insertErrors.Add($"Failed opening FC '{group.FeatureClassName}': {fcEx.Message}");
                            LoggingService.Error($"Failed to write to feature class '{group.FeatureClassName}': {fcEx}");
                        }
                    }

                    result.FeatureClassCount = result.CreatedFeatureClasses.Count;
                    result.TotalFeaturesExported = totalExported;

                    if (totalExported == 0 && issuesList.Count > 0)
                    {
                        result.Success = false;
                        string errDetail = insertErrors.Count > 0 ? string.Join("; ", insertErrors) : "No features could be written.";
                        result.Message = $"Feature classes created in '{targetDatasetName}', but 0 records were inserted. Cause: {errDetail}";
                        LoggingService.Error(result.Message);
                        return result;
                    }

                    result.Success = true;
                    var summaryCounts = exportGroups.Select(g => $"{g.FeatureClassName} ({g.Issues.Count})");
                    result.Message = $"Successfully exported {totalExported} errors across {result.FeatureClassCount} feature classes to '{targetDatasetName}' in Default Geodatabase.\n\nLayers:\n• " + string.Join("\n• ", summaryCounts);
                    LoggingService.Info(result.Message);

                    // 6. Add exported layers to Active Map inside a Group Layer
                    TryAddExportedLayersToMap(gdbPath, targetDatasetName, result.CreatedFeatureClasses);

                    return result;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Export error: {ex.Message}";
                    LoggingService.Error($"GDB Export Error: {ex}");
                    return result;
                }
            });
        }

        private static void TryAddExportedLayersToMap(string gdbPath, string datasetName, List<string> featureClassNames)
        {
            try
            {
                var map = MapView.Active?.Map;
                if (map == null) return;

                var groupLayer = LayerFactory.Instance.CreateGroupLayer(map, 0, datasetName);

                foreach (var fcName in featureClassNames)
                {
                    try
                    {
                        // Inside a Feature Dataset, the URI includes the dataset name
                        var fcUri = new Uri(Path.Combine(gdbPath, datasetName, fcName));
                        LayerFactory.Instance.CreateLayer(fcUri, groupLayer);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Warning($"Could not auto-add layer '{fcName}' to map: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Warning($"Auto-add group layer to map skipped: {ex.Message}");
            }
        }

        private static Geometry? EnsureSpatialReference(Geometry? geom, SpatialReference? targetSr)
        {
            if (geom == null || geom.IsEmpty || targetSr == null) return geom;

            // Step 1: Assign or project coordinate system
            if (geom.SpatialReference == null)
            {
                try
                {
                    if (geom is Polygon poly)
                        geom = new PolygonBuilderEx(poly) { SpatialReference = targetSr }.ToGeometry();
                    else if (geom is Polyline line)
                        geom = new PolylineBuilderEx(line) { SpatialReference = targetSr }.ToGeometry();
                    else if (geom is MapPoint pt)
                        geom = MapPointBuilderEx.CreateMapPoint(pt.X, pt.Y, targetSr);
                }
                catch
                {
                    // Non-fatal
                }
            }
            else if (!geom.SpatialReference.IsEqual(targetSr))
            {
                try
                {
                    var projected = GeometryEngine.Instance.Project(geom, targetSr);
                    if (projected != null && !projected.IsEmpty)
                    {
                        geom = projected;
                    }
                }
                catch
                {
                    try
                    {
                        if (geom is Polygon poly)
                            geom = new PolygonBuilderEx(poly) { SpatialReference = targetSr }.ToGeometry();
                        else if (geom is Polyline line)
                            geom = new PolylineBuilderEx(line) { SpatialReference = targetSr }.ToGeometry();
                        else if (geom is MapPoint pt)
                            geom = MapPointBuilderEx.CreateMapPoint(pt.X, pt.Y, targetSr);
                    }
                    catch
                    {
                        // Non-fatal
                    }
                }
            }

            // Step 2: Crucial - Always strip Z and M dimensions to guarantee compatibility with 2D feature classes
            try
            {
                if (geom is Polygon poly2 && (poly2.HasZ || poly2.HasM))
                {
                    geom = new PolygonBuilderEx(poly2) { HasZ = false, HasM = false, SpatialReference = targetSr }.ToGeometry();
                }
                else if (geom is Polyline line2 && (line2.HasZ || line2.HasM))
                {
                    geom = new PolylineBuilderEx(line2) { HasZ = false, HasM = false, SpatialReference = targetSr }.ToGeometry();
                }
                else if (geom is MapPoint pt2 && (pt2.HasZ || pt2.HasM))
                {
                    geom = MapPointBuilderEx.CreateMapPoint(pt2.X, pt2.Y, targetSr);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Warning($"Failed removing Z/M dimensions: {ex.Message}");
            }

            return geom;
        }

        private class ExportGroup
        {
            public string FeatureClassName { get; set; } = string.Empty;
            public GeometryType TargetGeometryType { get; set; }
            public List<(IssueResult issue, Geometry geom)> Issues { get; set; } = new List<(IssueResult, Geometry)>();
        }

        private static List<ExportGroup> PrepareExportGroups(
            List<IssueResult> issues, 
            string targetDatasetName, 
            HashSet<string> existingGdbItemNames)
        {
            var groupsDict = new Dictionary<string, ExportGroup>(StringComparer.OrdinalIgnoreCase);

            foreach (var issue in issues)
            {
                // Prefer exact issue geometry; fallback to point location
                var geom = issue.IssueGeometry;
                if (geom == null || geom.IsEmpty)
                {
                    geom = issue.Location;
                }
                if (geom == null || geom.IsEmpty) continue;

                var geomType = geom.GeometryType;
                string baseName = SanitizeName(issue.IssueType);
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    baseName = SanitizeName(issue.CheckId);
                }
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    baseName = "Issue";
                }

                // Group key combines base name and geometry type
                string groupKey = $"{baseName}_{geomType}";

                if (!groupsDict.TryGetValue(groupKey, out var group))
                {
                    group = new ExportGroup
                    {
                        FeatureClassName = baseName,
                        TargetGeometryType = geomType
                    };
                    groupsDict[groupKey] = group;
                }

                group.Issues.Add((issue, geom));
            }

            // Ensure all feature class names are unique across the dataset AND the entire Geodatabase
            var result = new List<ExportGroup>();
            var usedNamesInThisExport = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in groupsDict)
            {
                var group = kvp.Value;
                string baseName = group.FeatureClassName;

                // If multiple geometry types shared the same base name, append geom type
                if (groupsDict.Values.Count(g => g.FeatureClassName.Equals(baseName, StringComparison.OrdinalIgnoreCase)) > 1)
                {
                    baseName = $"{baseName}_{group.TargetGeometryType}";
                }

                // Scope feature class name with target dataset name so it's clean, organized, and unique in GDB
                string candidateName = $"{targetDatasetName}_{baseName}";

                string finalName = candidateName;
                int suffix = 1;
                while (existingGdbItemNames.Contains(finalName) || usedNamesInThisExport.Contains(finalName))
                {
                    finalName = $"{candidateName}_{suffix++}";
                }

                usedNamesInThisExport.Add(finalName);
                existingGdbItemNames.Add(finalName);
                group.FeatureClassName = finalName;
                result.Add(group);
            }

            return result;
        }

        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var chars = raw.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            string clean = new string(chars);
            while (clean.Contains("__")) clean = clean.Replace("__", "_");
            clean = clean.Trim('_');

            if (clean.Length > 0 && char.IsDigit(clean[0]))
            {
                clean = "QC_" + clean;
            }

            if (clean.Length > 30)
            {
                clean = clean.Substring(0, 30).TrimEnd('_');
            }

            return clean;
        }

        private static string Truncate(string? val, int maxLen)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Length <= maxLen ? val : val.Substring(0, maxLen);
        }
    }
}
