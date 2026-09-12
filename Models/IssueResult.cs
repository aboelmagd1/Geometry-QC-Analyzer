using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;

namespace GeometryQCAddIn.Models
{
    /// <summary>
    /// Unified standardized model representing a detected geometry quality control issue.
    /// Used across all QC checks, dockpanes, and graphics renderers.
    /// </summary>
    public class IssueResult
    {
        /// <summary>
        /// Unique identifier for this specific issue instance (used for correlating graphics and list items).
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Type/Name of the issue (e.g. "Invalid Geometry", "Overlap", "Short Segment").
        /// </summary>
        public string IssueType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the check that produced this issue (e.g. "CHK_INVALID_GEOM", "CHK_OVERLAP").
        /// </summary>
        public string CheckId { get; set; } = string.Empty;

        /// <summary>
        /// Primary Feature Object ID associated with the problem.
        /// </summary>
        public long? Oid { get; set; }

        /// <summary>
        /// Secondary or candidate Feature Object IDs involved (e.g. overlapping or duplicate feature).
        /// </summary>
        public List<long> RelatedOids { get; set; } = new List<long>();

        /// <summary>
        /// Primary representative point location of the issue in map coordinates.
        /// </summary>
        public MapPoint? Location { get; set; }

        /// <summary>
        /// Exact geometric representation of the issue (Point, Polyline, or Polygon).
        /// For example, the overlap polygon, the short segment line, or the snap point.
        /// </summary>
        public Geometry? IssueGeometry { get; set; }

        /// <summary>
        /// Human-readable explanation and diagnostic details of the issue.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Measured numerical value (e.g., segment length, angle in degrees, overlap area, snap distance).
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        /// Unit of measurement for the numerical value (e.g., "cm", "m", "deg", "sq m").
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Severity classification.
        /// </summary>
        public string Severity { get; set; } = nameof(IssueSeverity.Warning);

        /// <summary>
        /// Name of the source layer where the issue originated.
        /// </summary>
        public string LayerName { get; set; } = string.Empty;

        /// <summary>
        /// Unique URI / Identifier of the source layer.
        /// </summary>
        public string LayerUri { get; set; } = string.Empty;

        /// <summary>
        /// String representation for display in UI summaries and logs.
        /// </summary>
        public override string ToString()
        {
            var oidText = Oid.HasValue ? $"OID: {Oid.Value}" : "No OID";
            var valText = Value.HasValue ? $" [{Value.Value:0.###} {Unit}]" : string.Empty;
            return $"{IssueType} - {oidText}{valText}: {Description}";
        }
    }
}
