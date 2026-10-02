using System;
using UnityEngine;

namespace IsntGwent.Scripts.Diagnostics
{
    public static class AppRole
    {
        public static bool IsServer => Application.isBatchMode || Has("-server");

        public static bool Has(string name)
        {
            return Array.Exists(Args(), arg => arg == name);
        }

        public static string Value(string name)
        {
            var args = Args();

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }

            return null;
        }

        public static string CommandLine()
        {
            return string.Join(" ", Args());
        }

        /// В браузере командной строки нет вовсе: аргументы там не читаем,
        /// чтобы веб-клиент не зависел от того, что вернёт рантайм на несуществующий процесс.
        private static string[] Args()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) return Array.Empty<string>();

            return Environment.GetCommandLineArgs();
        }
    }
}
