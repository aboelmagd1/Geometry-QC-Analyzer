using System;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping.Events;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.UI;

namespace GeometryQCAddIn.Services
{
    /// <summary>
    /// Monitors active map selection changes and triggers debounced QC execution when Auto-run mode is enabled.
    /// </summary>
    public class SelectionService : IDisposable
    {
        private static SelectionService? _instance;
        public static SelectionService Instance => _instance ??= new SelectionService();

        private CancellationTokenSource? _debounceCts;
        private bool _isSubscribed = false;

        public void Initialize()
        {
            if (!_isSubscribed)
            {
                MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
                _isSubscribed = true;
                LoggingService.Info("SelectionService initialized and subscribed to MapSelectionChangedEvent.");
            }
        }

        private void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
        {
            var settings = GeometryQCSettings.Instance;
            if (!settings.IsEnabled || settings.ExecutionMode != ExecutionMode.Auto)
            {
                return;
            }

            // Cancel any in-flight debounce timer
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            int delay = Math.Max(settings.DebounceIntervalMs, 200);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, token);
                    if (!token.IsCancellationRequested)
                    {
                        LoggingService.DebugLog("Debounce window elapsed; auto-triggering QC run.");
                        await RunAutoQCAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Debounce cancelled by newer selection event
                }
            }, token);
        }

        private async Task RunAutoQCAsync()
        {
            var vm = FrameworkApplication.DockPaneManager.Find(ResultsDockPaneViewModel.DockPaneId) as ResultsDockPaneViewModel;
            if (vm != null)
            {
                // Ensure Results DockPane is activated/visible
                ResultsDockPaneViewModel.Show();
                await vm.RunValidationAsync();
            }
        }

        public void Dispose()
        {
            if (_isSubscribed)
            {
                MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);
                _isSubscribed = false;
            }
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }
    }
}
