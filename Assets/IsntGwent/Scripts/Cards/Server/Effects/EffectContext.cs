using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class EffectContext
    {
        public GameContext Game;

        public CardInstance Source;
        public Player Owner;

        public IGameEvent Event;

        public EffectDefinition Definition;

        public DamageKind DamageKind => Definition != null && IsWeather(Definition.Trigger)
            ? DamageKind.Weather
            : DamageKind.Card;

        private static bool IsWeather(EffectTrigger trigger)
            => trigger == EffectTrigger.OnWeatherEnter;

        public RowType PlayedRow;

        public int PlayedSlot = -1;

        public bool TargetsEnemyRow;

        public BoardSlot Slot => Source is UnitInstance unit ? Game?.FindSlot(unit) : null;

        public BoardSlot SourceSlot
        {
            get
            {
                var slot = Slot;
                if (slot != null) return slot;

                if (Owner == null || PlayedRow == RowType.None || !BoardConfig.IsValidSlot(PlayedSlot))
                    return null;

                return Owner.GetRow(PlayedRow).Slots[PlayedSlot];
            }
        }

        public BoardSlot Anchor(SlotAnchor anchor)
        {
            if (anchor != SlotAnchor.ManualTarget) return SourceSlot;

            return ManualTargets.Count > 0 ? Game?.FindSlot(ManualTargets[0]) : null;
        }

        public List<UnitInstance> ManualTargets = new();

        public int ManualCursor;

        public int DestroyedPower;

        public int KilledCount;

        public List<UnitInstance> Targets = new();
    }
}
