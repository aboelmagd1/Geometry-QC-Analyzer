using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Core;
using GeometryQCAddIn.Graphics;
using GeometryQCAddIn.Models;
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn.UI
{
    public class IssueGroupViewModel
    {
        public string CheckName { get; }
        public int Count => Issues.Count;
        public ObservableCollection<IssueResult> Issues { get; }

        public IssueGroupViewModel(string checkName, IEnumerable<IssueResult> issues)
        {
            CheckName = checkName;
            Issues = new ObservableCollection<IssueResult>(issues);
        }
    }

    public class ResultsDockPaneViewModel : DockPane
    {
        public const string DockPaneId = "GeometryQC_ResultsDockPane";

        private readonly GeometryValidator _validator = new GeometryValidator();
        private readonly ProgressService _progressService = new ProgressService();
        private CancellationTokenSource? _cts;

        private ObservableCollection<IssueGroupViewModel> _issueGroups = new ObservableCollection<IssueGroupViewModel>();
        public ObservableCollection<IssueGroupViewModel> IssueGroups
        {
            get => _issueGroups;
            set => SetProperty(ref _issueGroups, value);
        }

        private IssueResult? _selectedIssue;
        public IssueResult? SelectedIssue
        {
            get => _selectedIssue;
            set
            {
                if (SetProperty(ref _selectedIssue, value) && value != null)
                {
                    _ = ZoomToIssueAsync(value);
                }
            }
        }

        private string _statusText = "Ready to analyze selected polygons.";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _metricsText = string.Empty;
        public string MetricsText
        {
            get => _metricsText;
            set => SetProperty(ref _metricsText, value);
        }

        private int _totalIssuesCount = 0;
        public int TotalIssuesCount
        {
            get => _totalIssuesCount;
            set => SetProperty(ref _totalIssuesCount, value);
        }

        private bool _isBusyProcessing = false;
        public new bool IsBusy
        {
            get => _isBusyProcessing;
            set => SetProperty(ref _isBusyProcessing, value);
        }

        private double _progressPercent = 0.0;
        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        public ICommand RunQCCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ZoomToIssueCommand { get; }

        public ResultsDockPaneViewModel()
        {
            ThemeService.Initialize();
            RunQCCommand = new RelayCommand(async () => await RunValidationAsync(), () => !IsBusy);
            ClearResultsCommand = new RelayCommand(ClearResults);
            CancelCommand = new RelayCommand(CancelExecution, () => IsBusy);
            ZoomToIssueCommand = new RelayCommand(async (param) =>
            {
                if (param is IssueResult issue)
                {
                    await ZoomToIssueAsync(issue);
                }
            });

            _progressService.ProgressChanged += (val, status) =>
            {
                ProgressPercent = val;
                if (!string.IsNullOrEmpty(status))
                {
                    StatusText = status;
                }
            };
        }

        public static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            if (pane != null)
            {
                pane.Activate();
            }
        }

        public async Task RunValidationAsync()
        {
            if (IsBusy) return;

            var settings = GeometryQCSettings.Instance;
            if (!settings.IsEnabled)
            {
                StatusText = "Geometry QC is currently disabled. Enable it from the Ribbon or Settings.";
                return;
            }

            var mapView = MapView.Active;
            if (mapView == null)
            {
                StatusText = "No active map view. Please open a map.";
                return;
            }

            IsBusy = true;
            _progressService.Reset();
            _cts = new CancellationTokenSource();

            try
            {
                var result = await _validator.RunValidationAsync(mapView, settings, _progressService, _cts.Token);

                var groups = result.Issues
                    .GroupBy(i => i.IssueType)
                    .OrderBy(g => g.Key)
                    .Select(g => new IssueGroupViewModel(g.Key, g))
                    .ToList();

                IssueGroups = new ObservableCollection<IssueGroupViewModel>(groups);
                TotalIssuesCount = result.Issues.Count;

                if (result.Succeeded)
                {
                    StatusText = result.Message ?? $"Analysis complete. Found {result.Issues.Count} issues.";
                    MetricsText = $"Analyzed {result.Statistics.TotalFeaturesAnalyzed} features in {result.Statistics.ElapsedDuration.TotalSeconds:F2}s";
                    await QCGraphicManager.Instance.RenderIssuesAsync(result.Issues);
                }
                else
                {
                    StatusText = result.Message ?? "QC run did not complete.";
                }
            }
            catch (OperationCanceledException)
            {
                StatusText = "Operation cancelled by user.";
                QCGraphicManager.Instance.ClearGraphics();
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                LoggingService.Error($"Validation error: {ex}");
            }
            finally
            {
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public void CancelExecution()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                StatusText = "Cancelling analysis...";
            }
        }

        public void ClearResults()
        {
            CancelExecution();
            IssueGroups.Clear();
            TotalIssuesCount = 0;
            SelectedIssue = null;
            StatusText = "Results cleared. Ready for next analysis.";
            MetricsText = string.Empty;
            QCGraphicManager.Instance.ClearGraphics();
        }

        private async Task ZoomToIssueAsync(IssueResult issue)
        {
            await QCGraphicManager.Instance.ZoomToIssueAsync(issue);
        }
    }
}
