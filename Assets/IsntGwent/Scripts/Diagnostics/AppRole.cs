using System;
using UnityEngine;

namespace IsntGwent.Scripts.Diagnostics
{
    public static class AppRole
    {
        public static bool IsServer => Application.isBatchMode || Has("-server");

        public static bool Has(string name)
        {
            return Array.Exists(Environment.GetCommandLineArgs(), arg => arg == name);
        }

        public static string Value(string name)
        {
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }

            return null;
        }

        public static string CommandLine()
        {
            return string.Join(" ", Environment.GetCommandLineArgs());
        }
    }
}
