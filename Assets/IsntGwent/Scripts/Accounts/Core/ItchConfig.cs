namespace IsntGwent.Scripts.Accounts.Core
{
    /// <summary>
    /// Всё, что знает игра об OAuth-приложении на itch.io. Одно место на клиент и сервер.
    /// </summary>
    public static class ItchConfig
    {
        /// Публичный идентификатор приложения. Секретом не является намеренно: itch поддерживает
        /// только implicit flow, client secret'а там не выдают вовсе, и этот id всё равно уезжает
        /// в адресную строку браузера игрока. Меняется вместе с приложением на itch.io.
        public const string ClientId = "e918b3f40e5300e04d1268367486aa11";

        /// Права. Просим ровно то, что нужно для личности игрока, — профиль и ничего больше.
        public const string Scope = "profile:me";

        /// Адрес возврата. Обязан совпадать с настройками приложения на itch буква в букву.
        public const string RedirectUri = "https://dodgerf.com/oauth/callback";

        /// Страница разрешения на itch.
        public const string AuthUrl = "https://itch.io/user/oauth";

        /// Профиль игрока по токену: Authorization: Bearer <token>.
        /// Старый адрес itch.io/api/1/&lt;token&gt;/me помечен в документации как legacy.
        public const string ProfileUrl = "https://api.itch.io/profile";

        /// Куда клиент опрашивает результат входа. Тот же домен, что и wss, — за ним стоит Caddy.
        public const string AuthOrigin = "https://dodgerf.com";

        /// Префикс id аккаунта, заведённого через itch. Дальше идёт числовой user.id.
        public const string ItchIdPrefix = "itch:";

        /// Префикс id гостевого аккаунта: он живёт только в памяти сервера и в таблицу не попадает.
        public const string GuestIdPrefix = "guest:";

        public static string BuildAuthUrl(string state)
        {
            return AuthUrl
                   + "?client_id=" + ClientId
                   + "&scope=" + Escape(Scope)
                   + "&redirect_uri=" + Escape(RedirectUri)
                   + "&response_type=token"
                   + "&state=" + Escape(state);
        }

        public static string BuildPollUrl(string state)
        {
            return AuthOrigin + "/auth/poll?state=" + Escape(state);
        }

        public static string AccountId(long itchUserId)
        {
            return ItchIdPrefix + itchUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool IsGuestId(string accountId)
        {
            return !string.IsNullOrEmpty(accountId) && accountId.StartsWith(GuestIdPrefix, System.StringComparison.Ordinal);
        }

        private static string Escape(string value)
        {
            return System.Uri.EscapeDataString(value ?? string.Empty);
        }
    }
}
