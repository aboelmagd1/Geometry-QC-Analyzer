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
            string? targetLayerUri,
            ProgressService? progressService,
            CancellationToken cancellationToken)
        {
            var result = new ValidationRunResult();
            var stats = result.Statistics;
            stats.StartTime = DateTime.Now;

            LoggingService.Info($"QC Run initiated. Data Source Mode: {settings.DataSource}, Target Layer: {targetLayerUri ?? "<All>"}");

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

                var acqResult = await provider.AcquireFeaturesAsync(mapView, settings, targetLayerUri, cancellationToken);
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
                var rawIssues = new List<IssueResult>();

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
                        rawIssues.AddRange(issues);

                        LoggingService.Info($"Check '{check.Name}' completed: {issues.Count} raw issues found in {swSingleCheck.ElapsedMilliseconds} ms.");
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

                // Step 4: Verification & Deduplication Pipeline
                progressService?.Report(95.0, "Verifying issues and removing duplicates...");
                var verifiedIssues = DeduplicateAndVerifyIssues(rawIssues, context);
                result.Issues.AddRange(verifiedIssues);

                // Update category statistics with verified counts
                foreach (var issue in verifiedIssues)
                {
                    stats.IncrementIssueCount(issue.IssueType, 1);
                }

                stats.EndTime = DateTime.Now;

                progressService?.Report(100.0, $"QC Analysis completed. Found {result.Issues.Count} verified issues.");
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

        /// <summary>
        /// Rigorously verifies that detected issues genuinely exist (non-empty geometry, finite coordinates,
        /// meaningful above-resolution values) and deduplicates repeated or reciprocal issues.
        /// </summary>
        private List<IssueResult> DeduplicateAndVerifyIssues(List<IssueResult> rawIssues, GeometryQCContext context)
        {
            var verified = new List<IssueResult>();
            var seenIssueKeys = new HashSet<string>();
            var duplicateFeaturePairs = new HashSet<string>();

            // 1. Identify 100% duplicate feature pairs first to suppress redundant Overlap reports
            for (int i = 0; i < rawIssues.Count; i++)
            {
                var issue = rawIssues[i];
                if (issue.CheckId == "CHK_DUPLICATE" && issue.Oid.HasValue && issue.RelatedOids.Count > 0)
                {
                    long minOid = Math.Min(issue.Oid.Value, issue.RelatedOids[0]);
                    long maxOid = Math.Max(issue.Oid.Value, issue.RelatedOids[0]);
                    duplicateFeaturePairs.Add($"{minOid}_{maxOid}");
                }
            }

            for (int i = 0; i < rawIssues.Count; i++)
            {
                var issue = rawIssues[i];

                // 2. Verification: Ensure issue has valid, finite location
                if (issue.Location == null ||
                    double.IsNaN(issue.Location.X) || double.IsNaN(issue.Location.Y) ||
                    double.IsInfinity(issue.Location.X) || double.IsInfinity(issue.Location.Y))
                {
                    continue;
                }

                // Ensure geometry exists and is non-empty
                if (issue.IssueGeometry != null && issue.IssueGeometry.IsEmpty)
                {
                    continue;
                }

                // 3. Suppress redundant Overlap for identical duplicate features
                if (issue.CheckId == "CHK_OVERLAP" && issue.Oid.HasValue && issue.RelatedOids.Count > 0)
                {
                    long minOid = Math.Min(issue.Oid.Value, issue.RelatedOids[0]);
                    long maxOid = Math.Max(issue.Oid.Value, issue.RelatedOids[0]);
                    if (duplicateFeaturePairs.Contains($"{minOid}_{maxOid}"))
                    {
                        continue;
                    }
                }

                // 4. Construct normalized deduplication key
                // Round coordinates to ~1 millimeter precision in map units
                double locX = Math.Round(issue.Location.X, 3);
                double locY = Math.Round(issue.Location.Y, 3);

                // For Missing Junction: exactly one defect per target feature at this specific physical coordinate
                string issueKey;
                if (issue.CheckId == "CHK_JUNCTION")
                {
                    long gX = (long)Math.Round(issue.Location.X * 50.0);
                    long gY = (long)Math.Round(issue.Location.Y * 50.0);
                    issueKey = $"{issue.CheckId}##{issue.LayerUri}##{issue.Oid}##{gX}_{gY}";
                }
                else if (issue.Oid.HasValue && issue.RelatedOids.Count > 0)
                {
                    var allOids = new List<long>(issue.RelatedOids) { issue.Oid.Value };
                    allOids.Sort();
                    issueKey = $"{issue.CheckId}##{issue.LayerUri}##{string.Join("_", allOids)}##{locX}_{locY}";
                }
                else
                {
                    issueKey = $"{issue.CheckId}##{issue.LayerUri}##{issue.Oid}##{locX}_{locY}";
                }

                if (seenIssueKeys.Add(issueKey))
                {
                    verified.Add(issue);
                }
            }

            return verified;
        }
    }
}
