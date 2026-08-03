using DG.Tweening;

namespace IsntGwent.Scripts.Cards.UI
{
    public static class CardAnimConfig
    {
        public const float RowLayoutDuration = 0.22f;
        public const float PlayFlightDuration = 0.4f;
        public const float GraveyardFlightDuration = 0.35f;
        public const float ProjectileDuration = 0.3f;
        public const float HitReactDuration = 0.25f;
        public const float PowerPopDuration = 0.2f;
        public const float EnemyPreviewDuration = 1f;
        public const float DrawBeatDuration = 0.18f;
        public const float RedrawDiscardDuration = 0.3f;
        public const float EnemyDrawFlightDuration = 0.4f;
        public const float RoundResultBeatDuration = 1.2f;
        public const float HpBeatDuration = 0.6f;
        public const float RoundClearBeatDuration = GraveyardFlightDuration;

        public const Ease RowLayoutEase = Ease.OutQuad;
        public const Ease FlightEase = Ease.OutCubic;
        public const Ease ProjectileEase = Ease.InQuad;

        public const float PowerPopScale = 1.35f;
        public const float HitShakeStrength = 18f;
    }
}
