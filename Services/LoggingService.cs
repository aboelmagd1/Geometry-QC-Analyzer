using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GeometryQCAddIn.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LogMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public LogLevel Level { get; }
        public string Message { get; }

        public LogMessage(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public override string ToString() => $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
    }

    /// <summary>
    /// Thread-safe diagnostic logging service for Geometry QC Analyzer.
    /// Emits to Debug output and maintains an in-memory queue for diagnostic display.
    /// </summary>
    public static class LoggingService
    {
        private static readonly ConcurrentQueue<LogMessage> _logs = new ConcurrentQueue<LogMessage>();
        public static event Action<LogMessage>? OnLogAdded;

        public static void Log(LogLevel level, string message)
        {
            var log = new LogMessage(level, message);
            _logs.Enqueue(log);

            // Keep bounded size
            while (_logs.Count > 1000)
            {
                _logs.TryDequeue(out _);
            }

            Debug.WriteLine($"[GeometryQC] {log}");
            OnLogAdded?.Invoke(log);
        }

        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Error(string message) => Log(LogLevel.Error, message);
        public static void Error(string message, Exception? ex) => Log(LogLevel.Error, ex != null ? $"{message}: {ex.Message}" : message);
        public static void DebugLog(string message) => Log(LogLevel.Debug, message);
    }
}
