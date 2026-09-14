using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;

namespace GeometryQCAddIn.Core
{
    public static class GeometryHelpers
    {
        /// <summary>
        /// Extracts parts of a polygon as lists of MapPoints.
        /// </summary>
        public static List<List<MapPoint>> GetPartsAsPoints(Polygon poly)
        {
            var partsList = new List<List<MapPoint>>();
            if (poly == null) return partsList;

            foreach (var part in poly.Parts)
            {
                var points = new List<MapPoint>();
                for (int i = 0; i < part.Count; i++)
                {
                    var seg = part[i];
                    points.Add(seg.StartPoint);
                    if (i == part.Count - 1)
                    {
                        points.Add(seg.EndPoint);
                    }
                }
                partsList.Add(points);
            }
            return partsList;
        }

        /// <summary>
        /// Extracts all segments with part and segment indexing.
        /// </summary>
        public static List<(MapPoint P1, MapPoint P2, int PartIndex, int SegmentIndex)> GetAllSegments(Polygon poly)
        {
            var segments = new List<(MapPoint, MapPoint, int, int)>();
            if (poly == null) return segments;

            int partIdx = 0;
            foreach (var part in poly.Parts)
            {
                for (int s = 0; s < part.Count; s++)
                {
                    var seg = part[s];
                    segments.Add((seg.StartPoint, seg.EndPoint, partIdx, s));
                }
                partIdx++;
            }
            return segments;
        }

        /// <summary>
        /// Checks if a FeatureLayer is a Service / Web Layer.
        /// </summary>
        public static bool IsServiceLayer(FeatureLayer layer)
        {
            if (layer == null) return false;
            try
            {
                if (layer.URI != null && (layer.URI.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                          layer.URI.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                var dataConn = layer.GetDataConnection();
                if (dataConn != null)
                {
                    string typeName = dataConn.GetType().Name;
                    if (typeName.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        typeName.IndexOf("AGS", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    if (dataConn is ArcGIS.Core.CIM.CIMStandardDataConnection sdc &&
                        !string.IsNullOrEmpty(sdc.WorkspaceConnectionString) &&
                        (sdc.WorkspaceConnectionString.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         sdc.WorkspaceConnectionString.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }
                }

                using (var fc = layer.GetFeatureClass())
                {
                    if (fc != null)
                    {
                        using (var ds = fc.GetDatastore())
                        {
                            var connector = ds?.GetConnector();
                            if (connector is ServiceConnectionProperties)
                            {
                                return true;
                            }
                            if (ds != null && ds.GetType().Name.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Non-fatal fallback
            }
            return false;
        }
    }
}
