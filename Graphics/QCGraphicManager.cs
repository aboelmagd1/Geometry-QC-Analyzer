using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn.Graphics
{
    /// <summary>
    /// Manages visualization of QC issues over the active MapView.
    /// Supports two user-configurable display modes:
    /// 1. ScreenOverlay: Dynamic, in-memory screen cache overlays (Default).
    /// 2. GraphicsLayer: A Graphics Layer inside a 'QC_Geometry_Errors' Group Layer in the Map Contents pane.
    /// </summary>
    public class QCGraphicManager : IDisposable
    {
        public const string GroupLayerName = "QC_Geometry_Errors";

        private static QCGraphicManager? _instance;
        public static QCGraphicManager Instance => _instance ??= new QCGraphicManager();

        private readonly List<IDisposable> _activeOverlays = new List<IDisposable>();
        private readonly Dictionary<Guid, Geometry> _issueGeometries = new Dictionary<Guid, Geometry>();
        private IDisposable? _highlightOverlay;

        public int ActiveGraphicCount => _activeOverlays.Count;

        public void ClearGraphics()
        {
            try
            {
                // 1. Clear dynamic screen overlays
                foreach (var overlay in _activeOverlays)
                {
                    overlay?.Dispose();
                }
                _activeOverlays.Clear();
                _issueGeometries.Clear();

                _highlightOverlay?.Dispose();
                _highlightOverlay = null;

                // 2. Remove group layer and its child graphics layers from map
                _ = QueuedTask.Run(() =>
                {
                    try
                    {
                        var map = MapView.Active?.Map;
                        if (map == null) return;

                        var groupLayer = map.GetLayersAsFlattenedList().OfType<GroupLayer>()
                            .FirstOrDefault(g => g.Name.Equals(GroupLayerName, StringComparison.OrdinalIgnoreCase));
                        if (groupLayer != null)
                        {
                            var childGls = groupLayer.GetLayersAsFlattenedList().OfType<GraphicsLayer>().ToList();
                            foreach (var gl in childGls)
                            {
                                map.RemoveLayer(gl);
                            }
                            map.RemoveLayer(groupLayer);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Warning($"Error removing graphics layers from map: {ex.Message}");
                    }
                });

                LoggingService.DebugLog("Cleared all QC overlay graphics and layer elements.");
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error while clearing graphics: {ex.Message}");
            }
        }

        public async Task RenderIssuesAsync(IReadOnlyList<IssueResult> issues)
        {
            if (issues == null || issues.Count == 0) return;

            await QueuedTask.Run(() =>
            {
                ClearGraphics();
                var mapView = MapView.Active;
                if (mapView == null) return;

                var settings = GeometryQCSettings.Instance;
                bool showWarnings = settings.ShowWarningsOnMap;

                if (settings.GraphicsDisplayMode == MapGraphicsDisplayMode.GraphicsLayer)
                {
                    RenderToGraphicsLayer(issues, mapView, showWarnings);
                }
                else
                {
                    RenderToScreenOverlay(issues, mapView, showWarnings);
                }
            });
        }

        private void RenderToScreenOverlay(IReadOnlyList<IssueResult> issues, MapView mapView, bool showWarnings)
        {
            double transparency = GeometryQCSettings.Instance.MapGraphicsTransparencyPercent;

            foreach (var issue in issues)
            {
                if (!showWarnings && issue.Severity == nameof(IssueSeverity.Warning))
                {
                    continue;
                }

                var geom = issue.IssueGeometry ?? issue.Location;
                if (geom == null || geom.IsEmpty) continue;

                var symbol = QCGraphicSymbolProvider.GetSymbolForIssue(issue.CheckId, issue.IssueType, transparency);
                if (symbol == null) continue;

                var symRef = symbol.MakeSymbolReference();
                var overlay = mapView.AddOverlay(geom, symRef);
                if (overlay != null)
                {
                    _activeOverlays.Add(overlay);
                    _issueGeometries[issue.Id] = geom;
                }
            }

            LoggingService.Info($"Rendered {_activeOverlays.Count} temporary QC overlay graphics on active MapView.");
        }

        private void RenderToGraphicsLayer(IReadOnlyList<IssueResult> issues, MapView mapView, bool showWarnings)
        {
            var map = mapView.Map;
            if (map == null) return;

            // 1. Locate or create GroupLayer
            var groupLayer = map.GetLayersAsFlattenedList().OfType<GroupLayer>()
                .FirstOrDefault(g => g.Name.Equals(GroupLayerName, StringComparison.OrdinalIgnoreCase));
            if (groupLayer == null)
            {
                groupLayer = LayerFactory.Instance.CreateGroupLayer(map, 0, GroupLayerName);
            }

            // 2. Remove any previously created GraphicsLayers inside the GroupLayer to ensure fresh state
            var existingGraphicsLayers = groupLayer.GetLayersAsFlattenedList().OfType<GraphicsLayer>().ToList();
            foreach (var gl in existingGraphicsLayers)
            {
                try
                {
                    map.RemoveLayer(gl);
                }
                catch (Exception ex)
                {
                    LoggingService.Warning($"Failed removing old graphics layer {gl.Name}: {ex.Message}");
                }
            }

            // 3. Filter issues and group by error type
            var eligibleIssues = issues
                .Where(i => showWarnings || i.Severity != nameof(IssueSeverity.Warning))
                .ToList();

            if (eligibleIssues.Count == 0) return;

            var issuesByType = eligibleIssues
                .GroupBy(GetLayerNameForIssue)
                .OrderBy(g => g.Key);

            int totalAddedCount = 0;
            double transparency = GeometryQCSettings.Instance.MapGraphicsTransparencyPercent;

            foreach (var group in issuesByType)
            {
                string layerName = group.Key;
                var glParams = new GraphicsLayerCreationParams { Name = layerName };
                var graphicsLayer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(glParams, groupLayer);
                if (graphicsLayer == null) continue;

                int layerCount = 0;
                foreach (var issue in group)
                {
                    var geom = issue.IssueGeometry ?? issue.Location;
                    if (geom == null || geom.IsEmpty) continue;

                    var symbol = QCGraphicSymbolProvider.GetSymbolForIssue(issue.CheckId, issue.IssueType, transparency);
                    if (symbol == null) continue;

                    _issueGeometries[issue.Id] = geom;

                    try
                    {
                        if (AddGraphicElement(graphicsLayer, symbol, geom, issue.Location))
                        {
                            layerCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Warning($"Failed adding graphic element for issue {issue.CheckId}: {ex.Message}");
                    }
                }

                // Suppress selection handles on newly created graphic elements
                graphicsLayer.ClearSelection();
                graphicsLayer.UnSelectElements();

                totalAddedCount += layerCount;
                LoggingService.DebugLog($"Created GraphicsLayer '{layerName}' with {layerCount} elements in group '{GroupLayerName}'.");
            }

            LoggingService.Info($"Added {totalAddedCount} total graphic elements across {issuesByType.Count()} graphics layers in group '{GroupLayerName}'.");
        }

        private static bool AddGraphicElement(GraphicsLayer graphicsLayer, CIMSymbol symbol, Geometry geom, MapPoint? location)
        {
            if (symbol is CIMPointSymbol ptSym)
            {
                var pt = location ?? (geom is MapPoint p ? p : geom.Extent.Center);
                graphicsLayer.AddElement(new CIMPointGraphic { Location = pt, Symbol = ptSym.MakeSymbolReference() });
                return true;
            }
            else if (symbol is CIMLineSymbol lnSym)
            {
                Polyline? line = geom as Polyline;
                if (line == null && geom is Polygon polyGeom)
                {
                    line = new PolylineBuilderEx(polyGeom).ToGeometry();
                }
                if (line != null)
                {
                    graphicsLayer.AddElement(new CIMLineGraphic { Line = line, Symbol = lnSym.MakeSymbolReference() });
                    return true;
                }
            }
            else if (symbol is CIMPolygonSymbol polySym)
            {
                if (geom is Polygon polyGeom)
                {
                    graphicsLayer.AddElement(new CIMPolygonGraphic { Polygon = polyGeom, Symbol = polySym.MakeSymbolReference() });
                    return true;
                }
            }
            else
            {
                // Fallback generic element
                if (geom is MapPoint pt)
                {
                    graphicsLayer.AddElement(new CIMPointGraphic { Location = pt, Symbol = symbol.MakeSymbolReference() });
                    return true;
                }
                else if (geom is Polyline pl)
                {
                    graphicsLayer.AddElement(new CIMLineGraphic { Line = pl, Symbol = symbol.MakeSymbolReference() });
                    return true;
                }
                else if (geom is Polygon pg)
                {
                    graphicsLayer.AddElement(new CIMPolygonGraphic { Polygon = pg, Symbol = symbol.MakeSymbolReference() });
                    return true;
                }
            }
            return false;
        }

        private static string GetLayerNameForIssue(IssueResult issue)
        {
            if (!string.IsNullOrWhiteSpace(issue.IssueType))
            {
                return issue.IssueType.Trim();
            }

            return issue.CheckId switch
            {
                "CHK_INVALID_GEOM" => "Invalid Geometry",
                "CHK_OVERLAP" => "Polygon Overlap",
                "CHK_DUPLICATE" => "Duplicate Geometry",
                "CHK_GAP" => "Polygon Gap",
                "CHK_MULTIPART" => "Multipart Polygon",
                "CHK_SHORT_SEG" => "Short Segment",
                "CHK_ANGLE" => "Acute Angle Spike",
                "CHK_SNAP" => "Unsnapped Node",
                "CHK_REDUNDANT" => "Redundant Vertex",
                "CHK_REDUNDANT_ARCLINE" => "Redundant Vertex (Arc-Line)",
                "CHK_JUNCTION" => "Missing T-Junction Vertex",
                "CHK_FLAT_ARC" => "Flat Pseudo Arc",
                _ => !string.IsNullOrWhiteSpace(issue.CheckId) ? issue.CheckId : "Geometry QC Issue"
            };
        }

        public async Task ZoomToIssueAsync(IssueResult issue)
        {
            if (issue == null) return;

            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null) return;

                var geom = issue.IssueGeometry ?? issue.Location;
                if (geom == null || geom.IsEmpty) return;

                var env = geom.Extent;
                if (env.Width <= 1e-6 && env.Height <= 1e-6)
                {
                    if (issue.Location != null)
                    {
                        mapView.PanTo(issue.Location);
                    }
                    else
                    {
                        mapView.PanTo(env.Center);
                    }
                }
                else
                {
                    var expandedEnv = GeometryEngine.Instance.Expand(env, 2.0, 2.0, true);
                    mapView.ZoomTo(expandedEnv);
                }

                _highlightOverlay?.Dispose();
                var highlightSym = QCGraphicSymbolProvider.GetHighlightSymbol();
                if (highlightSym != null)
                {
                    var pt = issue.Location ?? env.Center;
                    _highlightOverlay = mapView.AddOverlay(pt, highlightSym.MakeSymbolReference());
                }
            });
        }

        public void Dispose()
        {
            ClearGraphics();
        }
    }
}
