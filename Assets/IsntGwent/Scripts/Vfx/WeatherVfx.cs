namespace IsntGwent.Scripts.Vfx
{
    public static class WeatherVfx
    {
        public const string RowFallback = "vfx_weather_row";
        public const string HitFallback = "vfx_weather_hit";

        public static string Row(VfxService vfx, string cardId) => Pick(vfx, RowFallback, cardId);

        public static string Hit(VfxService vfx, string cardId) => Pick(vfx, HitFallback, cardId);

        private static string Pick(VfxService vfx, string fallback, string cardId)
        {
            if (vfx == null) return null;
            if (string.IsNullOrEmpty(cardId)) return fallback;

            return vfx.Resolve(fallback + "_" + cardId, fallback);
        }
    }
}
