using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class PurgeGraveyardEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (PurgeGraveyardDefinition)context.Definition;
            if (context.Owner == null) return;

            var units = 0;

            foreach (var card in context.Owner.Graveyard)
            {
                if (card is UnitInstance) units++;
            }

            context.Owner.Graveyard.Clear();
            context.Game.MarkBoardDirty();

            if (definition.BuffPerUnit <= 0 || units == 0) return;
            if (context.Source is not UnitInstance self) return;

            self.CurrentPower.Value += definition.BuffPerUnit * units;
        }
    }

    public class PurgeGraveyardDefinition : EffectDefinition
    {
        public int BuffPerUnit;
    }
}
