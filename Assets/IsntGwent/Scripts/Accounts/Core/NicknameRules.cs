namespace IsntGwent.Scripts.Accounts.Core
{
    public static class NicknameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 16;

        public static string Trim(string nickname)
        {
            return string.IsNullOrEmpty(nickname) ? string.Empty : nickname.Trim();
        }

        public static string Normalize(string nickname)
        {
            return Trim(nickname).ToLowerInvariant();
        }

        public static bool IsValid(string nickname)
        {
            var trimmed = Trim(nickname);

            if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
                return false;

            foreach (var c in trimmed)
            {
                if (char.IsControl(c)) return false;
            }

            return true;
        }
    }
}
