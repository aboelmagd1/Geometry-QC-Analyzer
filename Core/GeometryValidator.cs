using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Core.Checks;
using GeometryQCAddIn.Models;
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn.Core
{
    public class ValidationRunResult
    {
        public List<IssueResult> Issues { get; } = new List<IssueResult>();
        public QCStatistics Statistics { get; } = new QCStatistics();
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Central quality control orchestrator.
    /// Manages data acquisition, context creation, check scheduling, error isolation, and metrics collection.
    /// Strictly read-only: never alters or locks source data.
    /// </summary>
    public class GeometryValidator
    {
        private readonly List<IGeometryCheck> _allChecks;

        public GeometryValidator()
        {
            _allChecks = new List<IGeometryCheck>
            {
                new InvalidGeometryCheck(),
                new OverlapCheck(),
                new DuplicateCheck(),
                new GapCheck(),
                new MultiPartCheck(),
                new ShortSegmentCheck(),
                new AngleIssueCheck(),
                new SnapIssueCheck(),
                new RedundantVertexCheck(),
                new JunctionVertexCheck()
            };
        }

        public async Task<ValidationRunResult> RunValidationAsync(
            MapView mapView,
            GeometryQCSettings settings,
            ProgressService? progressService,
            CancellationToken cancellationToken)
        {
            var result = new ValidationRunResult();
            var stats = result.Statistics;
            stats.StartTime = DateTime.Now;

            LoggingService.Info($"QC Run initiated. Data Source Mode: {settings.DataSource}");

            if (mapView == null || mapView.Map == null)
            {
                result.Message = "No active map view available.";
                LoggingService.Warning(result.Message);
                return result;
            }

            try
            {
                progressService?.Report(5.0, "Acquiring selected polygon features...");
                var swAcquisition = Stopwatch.StartNew();

                // Step 1: Data Acquisition based on configured DataSource Mode
                GeometryDataProvider provider = settings.DataSource == DataSourceMode.LiveQuery
                    ? new LiveQueryProvider()
                    : new DisplayCacheProvider();

                var acqResult = await provider.AcquireFeaturesAsync(mapView, settings, cancellationToken);
                swAcquisition.Stop();
                stats.GeometryAcquisitionTime = swAcquisition.Elapsed;

                foreach (var warn in acqResult.Warnings)
                {
                    stats.Warnings.Add(warn);
                    LoggingService.Warning(warn);
                }

                if (acqResult.Features.Count == 0)
                {
                    stats.EndTime = DateTime.Now;
                    result.Succeeded = true;
                    result.Message = acqResult.TotalSelectedCount == 0
                        ? "No polygon features selected. Please select one or more polygon features and run QC."
                        : "Selected polygon features are outside current map view extent.";
                    LoggingService.Info(result.Message);
                    return result;
                }

                stats.TotalFeaturesAnalyzed = acqResult.Features.Count;
                stats.TotalLayersProcessed = acqResult.Features.Select(f => f.LayerUri).Distinct().Count();

                LoggingService.Info($"Acquired {stats.TotalFeaturesAnalyzed} features across {stats.TotalLayersProcessed} layers in {swAcquisition.ElapsedMilliseconds} ms.");

                // Step 2: Build Shared Spatial & Vertex Index Context
                progressService?.Report(15.0, "Building spatial and vertex indexes...");
                var swIndex = Stopwatch.StartNew();

                SpatialReference? sr = null;
                Envelope? extent = null;
                await QueuedTask.Run(() =>
                {
                    sr = mapView.Map.SpatialReference;
                    extent = mapView.Extent;
                });

                var context = new GeometryQCContext(
                    acqResult.Features,
                    sr,
                    extent,
                    settings,
                    cancellationToken,
                    progressService);

                swIndex.Stop();
                stats.IndexConstructionTime = swIndex.Elapsed;
                LoggingService.Info($"Indexed {context.Features.Count} features and {context.VertexIndex.TotalVertexCount} vertices in {swIndex.ElapsedMilliseconds} ms.");

                // Step 3: Run Enabled QC Checks with Error Isolation
                var swChecks = Stopwatch.StartNew();
                var enabledChecks = _allChecks.Where(c => c.IsEnabled(settings)).ToList();
                int totalChecks = enabledChecks.Count;

                for (int i = 0; i < totalChecks; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var check = enabledChecks[i];
                    double pct = 20.0 + (70.0 * (i / (double)Math.Max(totalChecks, 1)));
                    progressService?.Report(pct, $"Running check {i + 1}/{totalChecks}: {check.Name}...");

                    var swSingleCheck = Stopwatch.StartNew();
                    try
                    {
                        var issues = await check.RunAsync(context);
                        swSingleCheck.Stop();

                        stats.CheckDurations[check.Name] = swSingleCheck.Elapsed;
                        stats.IncrementIssueCount(check.Name, issues.Count);
                        result.Issues.AddRange(issues);

                        LoggingService.Info($"Check '{check.Name}' completed: {issues.Count} issues found in {swSingleCheck.ElapsedMilliseconds} ms.");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        swSingleCheck.Stop();
                        string errorMsg = $"Check '{check.Name}' encountered an error: {ex.Message}";
                        stats.Errors.Add(errorMsg);
                        LoggingService.Error(errorMsg);
                    }
                }

                swChecks.Stop();
                stats.CheckExecutionTime = swChecks.Elapsed;
                stats.EndTime = DateTime.Now;

                progressService?.Report(100.0, $"QC Analysis completed. Found {result.Issues.Count} issues.");
                result.Succeeded = true;
                result.Message = stats.GetSummaryString();
                LoggingService.Info(result.Message);
            }
            catch (OperationCanceledException)
            {
                stats.EndTime = DateTime.Now;
                stats.WasCancelled = true;
                result.Succeeded = false;
                result.Message = "Geometry QC run was cancelled by user.";
                LoggingService.Warning(result.Message);
            }
            catch (Exception ex)
            {
                stats.EndTime = DateTime.Now;
                result.Succeeded = false;
                result.Message = $"Unexpected failure during QC run: {ex.Message}";
                stats.Errors.Add(result.Message);
                LoggingService.Error(result.Message);
            }

            return result;
        }
    }
}
