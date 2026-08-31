using System;
using System.Text;

namespace IsntGwent.Scripts.Lobby.Core
{
    public static class JoinCodes
    {
        public const int Length = 6;

        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Generate(Func<string, bool> isTaken)
        {
            for (var attempt = 0; attempt < 64; attempt++)
            {
                var code = Build();
                if (isTaken == null || !isTaken(code)) return code;
            }

            return Build();
        }

        public static string Normalize(string code)
        {
            return string.IsNullOrEmpty(code) ? string.Empty : code.Trim().ToUpperInvariant();
        }

        public static bool IsWellFormed(string code)
        {
            var normalized = Normalize(code);

            if (normalized.Length != Length) return false;

            foreach (var c in normalized)
            {
                if (Alphabet.IndexOf(c) < 0) return false;
            }

            return true;
        }

        private static string Build()
        {
            var builder = new StringBuilder(Length);

            for (var i = 0; i < Length; i++)
                builder.Append(Alphabet[UnityEngine.Random.Range(0, Alphabet.Length)]);

            return builder.ToString();
        }
    }
}
