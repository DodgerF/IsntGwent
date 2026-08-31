using System;
using System.IO;
using System.Text;

namespace IsntGwent.Scripts.Diagnostics
{
    public sealed class LogFileWriter : IDisposable
    {
        private readonly object _lock = new();

        private StreamWriter _writer;

        public LogFileWriter(string path)
        {
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        }

        public void Write(LogLevel level, string message, string stackTrace)
        {
            lock (_lock)
            {
                if (_writer == null) return;

                try
                {
                    _writer.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    _writer.Write(' ');
                    _writer.Write(Name(level));
                    _writer.Write(' ');
                    _writer.WriteLine(message);

                    if (!string.IsNullOrEmpty(stackTrace))
                        _writer.WriteLine(stackTrace.TrimEnd());
                }
                catch (Exception)
                {
                    _writer = null;
                }
            }
        }

        public static void Sweep(string directory, int days)
        {
            if (days <= 0) return;
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;

            var edge = DateTime.Now.AddDays(-days);

            try
            {
                foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    if (File.GetLastWriteTime(file) < edge) File.Delete(file);
                }

                foreach (var folder in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
                {
                    if (!Directory.Exists(folder)) continue;
                    if (Directory.GetFiles(folder).Length > 0) continue;
                    if (Directory.GetDirectories(folder).Length > 0) continue;

                    Directory.Delete(folder);
                }
            }
            catch (Exception)
            {
            }
        }

        private static string Name(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Info => "INF",
                LogLevel.Warn => "WRN",
                _ => "ERR",
            };
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}
