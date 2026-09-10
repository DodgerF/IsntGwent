using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotRedrawPolicy
    {
        [Inject] private readonly BotProfileProvider _profiles;

        public string Choose(GameContext context, Player me)
        {
            var plan = _profiles.Default.Redraw;

            var duplicate = FindDuplicate(me, plan.DiscardDuplicates);
            if (duplicate != null) return duplicate.Id.ToString();

            var always = FindAny(me, plan.DiscardAlways);
            if (always != null) return always.Id.ToString();

            foreach (var rule in plan.DiscardWith)
            {
                if (rule == null || !Holds(me, rule.With)) continue;

                var conditional = Find(me, rule.Card);
                if (conditional != null) return conditional.Id.ToString();
            }

            if (!NeedsSeek(me, plan)) return null;

            var cheapest = Cheapest(me, plan);

            return cheapest?.Id.ToString();
        }

        private static bool NeedsSeek(Player me, BotRedrawPlan plan)
        {
            var hasKeyCard = false;

            foreach (var id in plan.Seek)
                if (Holds(me, id))
                    hasKeyCard = true;

            if (!hasKeyCard) return true;

            foreach (var card in me.Hand)
            {
                var partner = BotCardTraits.BondPartner(card.Definition);

                if (!string.IsNullOrEmpty(partner) && !Holds(me, partner))
                    return true;
            }

            return false;
        }

        private static CardInstance Cheapest(Player me, BotRedrawPlan plan)
        {
            CardInstance best = null;
            var bestPower = int.MaxValue;

            foreach (var card in me.Hand)
            {
                if (card is not UnitInstance) continue;
                if (plan.Seek.Contains(card.Definition.Id)) continue;
                if (BotCardTraits.HasBond(card.Definition)) continue;

                var power = BotCardTraits.PowerOf(card.Definition);
                if (power >= bestPower) continue;

                bestPower = power;
                best = card;
            }

            return best;
        }

        private static CardInstance FindDuplicate(Player me, List<string> ids)
        {
            foreach (var id in ids)
            {
                CardInstance first = null;

                foreach (var card in me.Hand)
                {
                    if (card.Definition.Id != id) continue;

                    if (first == null)
                    {
                        first = card;
                        continue;
                    }

                    return card;
                }
            }

            return null;
        }

        private static CardInstance FindAny(Player me, List<string> ids)
        {
            foreach (var id in ids)
            {
                var card = Find(me, id);
                if (card != null) return card;
            }

            return null;
        }

        private static CardInstance Find(Player me, string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;

            foreach (var card in me.Hand)
                if (card.Definition.Id == cardId)
                    return card;

            return null;
        }

        private static bool Holds(Player me, string cardId) => Find(me, cardId) != null;
    }
}
