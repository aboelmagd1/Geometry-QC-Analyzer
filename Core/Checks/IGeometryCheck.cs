using System.Collections.Generic;
using System.Threading.Tasks;
using GeometryQCAddIn.Models;

namespace GeometryQCAddIn.Core.Checks
{
    /// <summary>
    /// Contract for all modular, independent Geometry QC checks.
    /// Operates in read-only mode over the shared GeometryQCContext.
    /// </summary>
    public interface IGeometryCheck
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        bool IsEnabled(GeometryQCAddIn.Config.GeometryQCSettings settings);

        Task<List<IssueResult>> RunAsync(GeometryQCContext context);
    }
}
