using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Graphics;
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn.UI
{
    /// <summary>
    /// Ribbon Button: Run Geometry QC.
    /// </summary>
    public class RunQCButton : Button
    {
        public RunQCButton()
        {
            Enabled = true;
        }

        protected override async void OnClick()
        {
            try
            {
                ResultsDockPaneViewModel.Show();
                var vm = FrameworkApplication.DockPaneManager.Find(ResultsDockPaneViewModel.DockPaneId) as ResultsDockPaneViewModel;
                if (vm != null)
                {
                    await vm.RunValidationAsync();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("RunQCButton error", ex);
                MessageBox.Show($"Error running QC: {ex.Message}", "Geometry QC");
            }
        }
    }

    /// <summary>
    /// Ribbon Button: Toggle Enable Geometry QC.
    /// </summary>
    public class ToggleQCButton : Button
    {
        public ToggleQCButton()
        {
            Enabled = true;
            try
            {
                IsChecked = GeometryQCSettings.Instance.IsEnabled;
            }
            catch
            {
                IsChecked = true;
            }
        }

        protected override void OnClick()
        {
            try
            {
                var settings = GeometryQCSettings.Instance;
                settings.IsEnabled = !settings.IsEnabled;
                settings.Save();
                IsChecked = settings.IsEnabled;
            }
            catch (Exception ex)
            {
                LoggingService.Error("ToggleQCButton error", ex);
                MessageBox.Show($"Error toggling QC: {ex.Message}", "Geometry QC");
            }
        }
    }

    /// <summary>
    /// Ribbon Button: Open Settings DockPane.
    /// </summary>
    public class SettingsButton : Button
    {
        public SettingsButton()
        {
            Enabled = true;
        }

        protected override void OnClick()
        {
            try
            {
                SettingsDockPaneViewModel.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Error("SettingsButton error", ex);
                MessageBox.Show($"Error opening settings: {ex.Message}", "Geometry QC");
            }
        }
    }

    /// <summary>
    /// Ribbon Button: Open Results DockPane.
    /// </summary>
    public class ResultsButton : Button
    {
        public ResultsButton()
        {
            Enabled = true;
        }

        protected override void OnClick()
        {
            try
            {
                ResultsDockPaneViewModel.Show();
            }
            catch (Exception ex)
            {
                LoggingService.Error("ResultsButton error", ex);
                MessageBox.Show($"Error opening results: {ex.Message}", "Geometry QC");
            }
        }
    }

    /// <summary>
    /// Ribbon Button: Clear Results and Overlays.
    /// </summary>
    public class ClearResultsButton : Button
    {
        public ClearResultsButton()
        {
            Enabled = true;
        }

        protected override void OnClick()
        {
            try
            {
                var vm = FrameworkApplication.DockPaneManager.Find(ResultsDockPaneViewModel.DockPaneId) as ResultsDockPaneViewModel;
                if (vm != null)
                {
                    vm.ClearResults();
                }
                else
                {
                    QCGraphicManager.Instance.ClearGraphics();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("ClearResultsButton error", ex);
                MessageBox.Show($"Error clearing results: {ex.Message}", "Geometry QC");
            }
        }
    }

    /// <summary>
    /// Ribbon Button: Cancel running QC execution.
    /// </summary>
    public class CancelQCButton : Button
    {
        public CancelQCButton()
        {
            Enabled = true;
        }

        protected override void OnClick()
        {
            try
            {
                var vm = FrameworkApplication.DockPaneManager.Find(ResultsDockPaneViewModel.DockPaneId) as ResultsDockPaneViewModel;
                vm?.CancelExecution();
            }
            catch (Exception ex)
            {
                LoggingService.Error("CancelQCButton error", ex);
                MessageBox.Show($"Error cancelling QC: {ex.Message}", "Geometry QC");
            }
        }
    }
}
