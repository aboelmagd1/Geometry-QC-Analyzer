using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using GeometryQCAddIn.Services;
using GeometryQCAddIn.UI;

namespace GeometryQCAddIn
{
    /// <summary>
    /// ArcGIS Pro Add-in Module entry point for Geometry QC Analyzer.
    /// Handles lifecycle initialization, cleanup, and project-level settings persistence.
    /// </summary>
    public class GeometryQCModule : Module
    {
        private static GeometryQCModule? _this;
        public static GeometryQCModule Current => _this ??= (GeometryQCModule)FrameworkApplication.FindModule("GeometryQC_Module");

        public string? PersistedTargetLayerUri { get; set; }
        public string? PersistedTargetLayerName { get; set; }

        protected override bool Initialize()
        {
            LoggingService.Info("Initializing Geometry QC Add-In Module...");
            try
            {
                ThemeService.Initialize();
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to initialize module services", ex);
            }
            return base.Initialize();
        }

        protected override bool CanUnload()
        {
            return true;
        }

        protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
        {
            try
            {
                if (!string.IsNullOrEmpty(PersistedTargetLayerUri))
                {
                    settings.Add("TargetLayerUri", PersistedTargetLayerUri);
                }
                if (!string.IsNullOrEmpty(PersistedTargetLayerName))
                {
                    settings.Add("TargetLayerName", PersistedTargetLayerName);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to write project settings", ex);
            }

            return base.OnWriteSettingsAsync(settings);
        }

        protected override async Task OnReadSettingsAsync(ModuleSettingsReader settings)
        {
            try
            {
                PersistedTargetLayerUri = settings.Get("TargetLayerUri") as string;
                PersistedTargetLayerName = settings.Get("TargetLayerName") as string;

                var vm = FrameworkApplication.DockPaneManager.Find(ResultsDockPaneViewModel.DockPaneId) as ResultsDockPaneViewModel;
                if (vm != null)
                {
                    await vm.RefreshLayersAsync();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to read project settings", ex);
            }

            await base.OnReadSettingsAsync(settings);
        }
    }
}
