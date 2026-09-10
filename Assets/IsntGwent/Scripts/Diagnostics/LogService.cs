using System;
using System.IO;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Diagnostics
{
    public class LogService : IInitializable, IDisposable
    {
        public const bool EnableInEditor = false;
        public const int DefaultRetentionDays = 30;

        private const string LogsFolder = "Logs";
        private const string MatchesFolder = "Matches";
        private const string StatsFolder = "Stats";

        private LogFileWriter _writer;
        private string _root;

        public bool IsEnabled => _writer != null;

        public bool IsJournalEnabled => IsEnabled && !AppRole.Has("-noMatchJournal");

        public int RetentionDays { get; private set; } = DefaultRetentionDays;

        public string MatchesDirectory =>
            string.IsNullOrEmpty(_root) ? null : Path.Combine(_root, MatchesFolder);

        public string StatsDirectory =>
            string.IsNullOrEmpty(_root) ? null : Path.Combine(_root, StatsFolder);

        public void Initialize()
        {
            Log.MinLevel = ParseLevel(AppRole.Value("-logLevel"));
            RetentionDays = ParseDays(AppRole.Value("-logRetentionDays"));

            if (Application.isEditor && !EnableInEditor) return;

            _root = AppRole.Value("-logDir");
            if (string.IsNullOrEmpty(_root)) _root = Application.persistentDataPath;

            var folder = Path.Combine(_root, LogsFolder);

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Failed to create log folder: " + e.Message);
                _root = null;
                return;
            }

            LogFileWriter.Sweep(folder, RetentionDays);
            LogFileWriter.Sweep(MatchesDirectory, RetentionDays);

            var prefix = AppRole.IsServer ? "server-" : "client-";
            var path = Path.Combine(folder, prefix + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");

            try
            {
                _writer = new LogFileWriter(path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Failed to open log file: " + e.Message);
                return;
            }

            Application.logMessageReceived += OnUnityLog;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            WriteBanner(folder);
        }

        private void WriteBanner(string folder)
        {
            Log.Info(LogTag.App, $"{Application.productName} {Application.version} " +
                                 $"(Unity {Application.unityVersion}, {Application.platform}) " +
                                 $"role={(AppRole.IsServer ? "server" : "client")}");
            Log.Info(LogTag.App, "command line: " + AppRole.CommandLine());
            Log.Info(LogTag.App, $"logs: {folder} (level {Log.MinLevel}, keep {RetentionDays} d)");

            if (IsJournalEnabled)
                Log.Info(LogTag.App, "match journal: " + MatchesDirectory);
            else
                Log.Info(LogTag.App, "match journal disabled");
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            var level = Log.LevelOf(type);
            var trace = level == LogLevel.Error ? stackTrace : null;

            _writer?.Write(level, condition, trace);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            _writer?.Write(LogLevel.Error, "[App] unhandled exception", args.ExceptionObject?.ToString());
        }

        private static LogLevel ParseLevel(string value)
        {
            if (string.IsNullOrEmpty(value)) return LogLevel.Info;

            return Enum.TryParse<LogLevel>(value, true, out var level) ? level : LogLevel.Info;
        }

        private static int ParseDays(string value)
        {
            if (string.IsNullOrEmpty(value)) return DefaultRetentionDays;

            return int.TryParse(value, out var days) && days > 0 ? days : DefaultRetentionDays;
        }

        public void Dispose()
        {
            if (_writer == null) return;

            Log.Info(LogTag.App, "shutdown");

            Application.logMessageReceived -= OnUnityLog;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;

            _writer.Dispose();
            _writer = null;
        }
    }
}
