using System;
using System.Collections.Generic;

namespace GeometryQCAddIn.Models
{
    /// <summary>
    /// Telemetry and diagnostic summary collected during a QC execution run.
    /// </summary>
    public class QCStatistics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan ElapsedDuration => EndTime - StartTime;

        public int TotalFeaturesAnalyzed { get; set; }
        public int TotalLayersProcessed { get; set; }
        public int TotalIssuesDetected { get; set; }

        public TimeSpan GeometryAcquisitionTime { get; set; }
        public TimeSpan IndexConstructionTime { get; set; }
        public TimeSpan CheckExecutionTime { get; set; }

        public Dictionary<string, int> IssueCountsByCheck { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TimeSpan> CheckDurations { get; } = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();
        public bool WasCancelled { get; set; }

        public void IncrementIssueCount(string checkName, int count = 1)
        {
            if (IssueCountsByCheck.TryGetValue(checkName, out var current))
            {
                IssueCountsByCheck[checkName] = current + count;
            }
            else
            {
                IssueCountsByCheck[checkName] = count;
            }
            TotalIssuesDetected += count;
        }

        public string GetSummaryString()
        {
            return $"QC Run {(WasCancelled ? "Cancelled" : "Completed")} in {ElapsedDuration.TotalSeconds:F2}s | Features: {TotalFeaturesAnalyzed} | Total Issues: {TotalIssuesDetected}";
        }
    }
}
