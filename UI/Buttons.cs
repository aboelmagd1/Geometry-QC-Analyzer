using System;
using System.Windows.Media.Imaging;
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
            try
            {
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Run32.png", UriKind.Absolute));
                SmallImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Run16.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback to Config.daml declaration
            }
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
            bool isEnabled = true;
            try
            {
                isEnabled = GeometryQCSettings.Instance.IsEnabled;
            }
            catch
            {
                isEnabled = true;
            }

            IsChecked = isEnabled;
            UpdateAppearance(isEnabled);
        }

        protected override void OnClick()
        {
            try
            {
                var settings = GeometryQCSettings.Instance;
                settings.IsEnabled = !settings.IsEnabled;
                settings.Save();
                IsChecked = settings.IsEnabled;
                UpdateAppearance(settings.IsEnabled);
            }
            catch (Exception ex)
            {
                LoggingService.Error("ToggleQCButton error", ex);
                MessageBox.Show($"Error toggling QC: {ex.Message}", "Geometry QC");
            }
        }

        protected override void OnUpdate()
        {
            try
            {
                var isEnabled = GeometryQCSettings.Instance?.IsEnabled ?? false;
                if (IsChecked != isEnabled)
                {
                    IsChecked = isEnabled;
                    UpdateAppearance(isEnabled);
                }
            }
            catch
            {
            }
            Enabled = true;
        }

        private void UpdateAppearance(bool isEnabled)
        {
            Caption = isEnabled ? "QC (ON)" : "QC (OFF)";
            Tooltip = isEnabled
                ? "Geometry QC is active. Click to disable automatic/interactive checks."
                : "Geometry QC is disabled. Click to enable.";

            try
            {
                string suffix = isEnabled ? "ON" : "OFF";
                LargeImage = new BitmapImage(new Uri($"pack://application:,,,/GeometryQCAddIn;component/Images/QC_Toggle_{suffix}32.png", UriKind.Absolute));
                SmallImage = new BitmapImage(new Uri($"pack://application:,,,/GeometryQCAddIn;component/Images/QC_Toggle_{suffix}16.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback to Config.daml declaration
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
            try
            {
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Settings32.png", UriKind.Absolute));
                SmallImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Settings16.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback to Config.daml declaration
            }
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
            try
            {
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Results32.png", UriKind.Absolute));
                SmallImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Results16.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback to Config.daml declaration
            }
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
            try
            {
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Clear32.png", UriKind.Absolute));
                SmallImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Clear16.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback to Config.daml declaration
            }
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
            try
            {
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Cancel32.png", UriKind.Absolute));
                SmallImage = new BitmapImage(new Uri("pack://application:,,,/GeometryQCAddIn;component/Images/QC_Cancel16.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback to Config.daml declaration
            }
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
