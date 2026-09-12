using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using GeometryQCAddIn.Config;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core.Checks
{
    /// <summary>
    /// Check 5: Detects multipart polygon features (PartCount > 1).
    /// </summary>
    public class MultiPartCheck : IGeometryCheck
    {
        public string Id => "CHK_MULTIPART";
        public string Name => "Multi-Part Feature";
        public string Description => "Detects polygon features containing multiple disconnected parts.";

        public bool IsEnabled(GeometryQCSettings settings) => settings.CheckMultiPart;

        public Task<List<IssueResult>> RunAsync(GeometryQCContext context)
        {
            return QueuedTask.Run(() =>
            {
                var issues = new List<IssueResult>();
                var features = context.Features;

                for (int i = 0; i < features.Count; i++)
                {
                    context.ThrowIfCancellationRequested();
                    var f = features[i];

                    if (f.PartCount > 1)
                    {
                        var repPoint = GeometryEngine.Instance.LabelPoint(f.Geometry) ?? f.Extent.Center;
                        issues.Add(new IssueResult
                        {
                            CheckId = Id,
                            IssueType = Name,
                            Oid = f.Oid,
                            LayerName = f.LayerName,
                            LayerUri = f.LayerUri,
                            Location = repPoint,
                            IssueGeometry = f.Geometry,
                            Value = f.PartCount,
                            Unit = "parts",
                            Description = $"Feature contains {f.PartCount} separate parts.",
                            Severity = nameof(IssueSeverity.Warning)
                        });
                    }
                }

                return issues;
            });
        }
    }
}
