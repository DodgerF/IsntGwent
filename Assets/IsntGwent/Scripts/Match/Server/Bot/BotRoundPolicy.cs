using System.Collections.Generic;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotRoundPolicy
    {
        [Inject] private readonly BotProfileProvider _profiles;

        public bool ShouldPass(GameContext context, Player me, bool hasMoves, int bestSwing, bool hasStrategicMove)
        {
            var profile = _profiles.Default;
            var opponent = context.GetOpponent(me);

            if (me.PendingPlays.Count > 0) return false;

            var diff = me.TotalPower - opponent.TotalPower;
            var gap = -diff;
            var lead = opponent.Hand.Count - me.Hand.Count;

            if (lead <= 0 && diff >= (lead < 0 ? profile.SmallGap : profile.LeadToPass))
                return true;

            if (!hasMoves) return true;

            if (bestSwing < 0 && !hasStrategicMove) return true;

            if (!CanPass(context, me, profile)) return false;

            if (opponent.IsPassed)
                return diff > 0 || !CanCatchUp(me, gap);

            var decisive = me.Hp <= 1 && opponent.Hp <= 1;

            if (decisive || me.Hp <= 1) return false;

            if (diff > 0 && HoldsEnemyTraitor(me)) return true;

            if (context.RoundNumber >= 2 && me.Hp > opponent.Hp)
            {
                if (gap > 0) return true;

                return me.Hand.Count <= profile.SaveHand && diff <= profile.SmallGap;
            }

            if (me.Hand.Count <= profile.EndgameHand && gap > 0 && bestSwing > 0) return false;

            if (lead >= 2) return true;
            if (lead == 1 && bestSwing <= gap) return true;

            if (gap > 0 && CardsToClose(me, gap) >= profile.MaxCardsToChase) return true;

            return false;
        }

        private static bool HoldsEnemyTraitor(Player me)
        {
            foreach (var unit in me.MeleeRow)
                if (unit.UnitDefinition.Traitor)
                    return true;

            foreach (var unit in me.RangedRow)
                if (unit.UnitDefinition.Traitor)
                    return true;

            return false;
        }

        private static bool CanPass(GameContext context, Player me, BotProfile profile)
        {
            if (context.RoundNumber > 1) return true;

            return me.Hand.Count <= profile.FirstRoundPassHand;
        }

        private static bool CanCatchUp(Player me, int gap)
        {
            if (gap <= 0) return true;

            var total = 0;

            foreach (var card in me.Hand)
                total += Contribution(card.Definition);

            return total >= gap;
        }

        private static int CardsToClose(Player me, int gap)
        {
            if (gap <= 0) return 0;

            var values = new List<int>();

            foreach (var card in me.Hand)
                values.Add(Contribution(card.Definition));

            values.Sort((left, right) => right.CompareTo(left));

            var total = 0;

            for (var i = 0; i < values.Count; i++)
            {
                total += values[i];

                if (total >= gap) return i + 1;
            }

            return int.MaxValue;
        }

        private static int Contribution(Cards.Definitions.CardDefinition definition)
        {
            return BotCardTraits.PowerOf(definition) + BotCardTraits.DirectDamage(definition);
        }
    }
}
