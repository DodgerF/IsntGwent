using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class SummonFromDeckEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (SummonFromDeckDefinition)context.Definition;

            var cardId = string.IsNullOrEmpty(definition.CardId)
                ? context.Source?.Definition.Id
                : definition.CardId;
            if (cardId == null && !definition.AnyCard) return;

            var pile = definition.FromGraveyard ? context.Owner.Graveyard : context.Owner.Deck;

            var matches = pile
                .OfType<UnitInstance>()
                .Where(u => u != context.Source)
                .Where(u => definition.AnyCard || u.Definition.Id == cardId)
                .Where(u => definition.MaxPower <= 0 || u.UnitDefinition.Power <= definition.MaxPower)
                .ToList();

            var take = definition.Count <= 0 ? matches.Count : Math.Min(definition.Count, matches.Count);

            for (var i = 0; i < take; i++)
            {
                if (context.Owner.FirstFreeSlot() == null) return;

                var unit = matches[i];
                pile.Remove(unit);
                SummonUtil.Place(context, unit, definition.Placement, definition.Row, i, fromDeck: true);
            }
        }
    }

    public class SummonFromDeckDefinition : EffectDefinition
    {
        public string CardId;
        public bool AnyCard;
        public bool FromGraveyard;
        public int Count;
        public RowType Row;
        public int MaxPower;
        public SummonPlacement Placement;
    }
}
