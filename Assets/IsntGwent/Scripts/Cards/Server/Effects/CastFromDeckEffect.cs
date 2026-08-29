using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class CastFromDeckEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (CastFromDeckDefinition)context.Definition;

            if (context.Owner == null || string.IsNullOrEmpty(definition.CardId)) return;

            var card = context.Owner.Deck.FirstOrDefault(c => c.Definition.Id == definition.CardId);
            if (card == null) return;

            context.Owner.Deck.Remove(card);
            context.Owner.PendingPlays.Add(card);
            context.Game.PendingPlayNotice = context.Owner;
        }
    }

    public class CastFromDeckDefinition : EffectDefinition
    {
        public string CardId;
    }
}
