using System;

namespace GeometryQCAddIn.Services
{
    /// <summary>
    /// Progress notification service for long-running geometry validation operations.
    /// </summary>
    public class ProgressService : IProgress<double>
    {
        public event Action<double, string>? ProgressChanged;
        public string CurrentStatus { get; private set; } = string.Empty;
        public double CurrentPercentage { get; private set; } = 0.0;

        public void Report(double value)
        {
            CurrentPercentage = value;
            ProgressChanged?.Invoke(value, CurrentStatus);
        }

        public void Report(double value, string status)
        {
            CurrentPercentage = value;
            CurrentStatus = status;
            ProgressChanged?.Invoke(value, status);
        }

        public void Reset()
        {
            CurrentPercentage = 0.0;
            CurrentStatus = string.Empty;
            ProgressChanged?.Invoke(0.0, string.Empty);
        }
    }
}
