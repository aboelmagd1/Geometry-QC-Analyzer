using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GeometryQCAddIn.Models;
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn.Graphics
{
    /// <summary>
    /// Manages temporary, in-memory overlay graphics over the active MapView.
    /// Completely non-destructive: no permanent layers or feature classes are created or modified on disk.
    /// </summary>
    public class QCGraphicManager : IDisposable
    {
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
                foreach (var overlay in _activeOverlays)
                {
                    overlay?.Dispose();
                }
                _activeOverlays.Clear();
                _issueGeometries.Clear();

                _highlightOverlay?.Dispose();
                _highlightOverlay = null;

                LoggingService.DebugLog("Cleared all QC overlay graphics.");
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

                foreach (var issue in issues)
                {
                    var geom = issue.IssueGeometry ?? issue.Location;
                    if (geom == null || geom.IsEmpty) continue;

                    var symbol = QCGraphicSymbolProvider.GetSymbolForIssue(issue.CheckId, issue.IssueType);
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
            });
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
