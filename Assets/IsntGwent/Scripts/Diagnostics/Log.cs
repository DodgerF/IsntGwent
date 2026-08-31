using System;
using UnityEngine;

namespace IsntGwent.Scripts.Diagnostics
{
    public static class Log
    {
        public static LogLevel MinLevel = LogLevel.Info;

        private static LogLevel _routed;
        private static bool _isRouting;

        public static void Trace(string tag, string message) => Write(LogLevel.Trace, tag, message);

        public static void Debug(string tag, string message) => Write(LogLevel.Debug, tag, message);

        public static void Info(string tag, string message) => Write(LogLevel.Info, tag, message);

        public static void Warn(string tag, string message) => Write(LogLevel.Warn, tag, message);

        public static void Error(string tag, string message) => Write(LogLevel.Error, tag, message);

        public static void Exception(string tag, Exception exception)
        {
            if (exception == null) return;

            Write(LogLevel.Error, tag, exception.ToString());
        }

        public static LogLevel LevelOf(LogType type)
        {
            if (_isRouting) return _routed;

            return type switch
            {
                LogType.Warning => LogLevel.Warn,
                LogType.Error => LogLevel.Error,
                LogType.Assert => LogLevel.Error,
                LogType.Exception => LogLevel.Error,
                _ => LogLevel.Info,
            };
        }

        private static void Write(LogLevel level, string tag, string message)
        {
            if (level < MinLevel) return;

            var line = string.IsNullOrEmpty(tag) ? message : "[" + tag + "] " + message;

            _routed = level;
            _isRouting = true;

            try
            {
                if (level == LogLevel.Error)
                    UnityEngine.Debug.LogError(line);
                else if (level == LogLevel.Warn)
                    UnityEngine.Debug.LogWarning(line);
                else
                    UnityEngine.Debug.Log(line);
            }
            finally
            {
                _isRouting = false;
            }
        }
    }
}
