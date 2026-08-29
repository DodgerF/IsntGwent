using DG.Tweening;
using UnityEngine;

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
        public const float EnemyStageDuration = 1f;
        public const float EnemyStageFlightDuration = 0.28f;
        public const float DrawBeatDuration = 0.18f;
        public const float RedrawDiscardDuration = 0.3f;
        public const float RedrawCloseHold = 0.5f;
        public const float EnemyDrawFlightDuration = 0.4f;
        public const float RoundResultBeatDuration = 0.6f;
        public const float HpBeatDuration = 0.95f;
        public const float RoundClearBeatDuration = GraveyardFlightDuration;
        public const float SlotHighlightDuration = 0.15f;
        public const float SlotGlowPulseDuration = 0.9f;
        public const float SlotGlowPulseStagger = 0.07f;

        public const float ImpactVfxLife = 1.1f;
        public const float UnitLinkBeatDuration = 1.15f;
        public const float LureReachDuration = 0.3f;
        public const float LureGripDuration = 0.12f;
        public const float LureReleaseDuration = 0.2f;
        public const float LureTugStrength = 26f;
        public const float LureTugDuration = 0.4f;
        public const int LureTugVibrato = 7;
        public const float BoltTailDuration = 0.45f;
        public const float WeatherFadeDuration = 2.4f;
        public const float WeatherTintDuration = 0.35f;
        public const float WeatherTintAlpha = 0.13f;

        public const Ease RowLayoutEase = Ease.OutQuad;
        public const Ease FlightEase = Ease.OutCubic;
        public const Ease FlightGrowEase = Ease.InQuad;
        public const Ease FlightShrinkEase = Ease.OutQuad;
        public const Ease ProjectileEase = Ease.InQuad;

        public const float PowerPopScale = 1.35f;
        public const float HitShakeStrength = 18f;

        public const float PlayStageScale = 1.35f;

        public const float HandHoverScale = 1.12f;
        public const float HandHoverLift = 89f;
        public const float HandPickedScale = 1.18f;
        public const float HandPickedLift = 95f;
        public const float HoverDuration = 0.12f;
        public const float DimAlpha = 0.45f;
        public const float DimDuration = 0.15f;

        public static readonly Color PowerBaseColor = Color.white;
        public static readonly Color PowerRaisedColor = new(0.44f, 0.92f, 0.44f);
        public static readonly Color PowerLoweredColor = new(0.95f, 0.33f, 0.31f);
    }
}
