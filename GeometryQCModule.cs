using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using GeometryQCAddIn.Services;

namespace GeometryQCAddIn
{
    /// <summary>
    /// ArcGIS Pro Add-in Module entry point for Geometry QC Analyzer.
    /// Handles lifecycle initialization and cleanup.
    /// </summary>
    public class GeometryQCModule : Module
    {
        private static GeometryQCModule? _this;
        public static GeometryQCModule Current => _this ??= (GeometryQCModule)FrameworkApplication.FindModule("GeometryQC_Module");

        protected override bool Initialize()
        {
            LoggingService.Info("Initializing Geometry QC Add-In Module...");
            try
            {
                ThemeService.Initialize();
                SelectionService.Instance.Initialize();
            }
            catch (System.Exception ex)
            {
                LoggingService.Error("Failed to initialize module services", ex);
            }
            return base.Initialize();
        }

        protected override bool CanUnload()
        {
            try
            {
                SelectionService.Instance.Dispose();
            }
            catch (System.Exception ex)
            {
                LoggingService.Error("Error during SelectionService disposal", ex);
            }
            return true;
        }
    }
}
