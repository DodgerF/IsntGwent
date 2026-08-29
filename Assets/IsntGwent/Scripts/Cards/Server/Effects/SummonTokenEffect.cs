using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class SummonTokenEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (SummonTokenDefinition)context.Definition;
            if (string.IsNullOrEmpty(definition.TokenId)) return;

            for (var i = 0; i < definition.Count; i++)
            {
                if (context.Owner.FirstFreeSlot() == null) return;
                if (context.Game.CreateCard(definition.TokenId) is not UnitInstance token) continue;

                SummonUtil.Place(context, token, definition.Placement, definition.Row, i, fromDeck: false,
                    definition.StrictNeighbors);
            }
        }
    }

    public class SummonTokenDefinition : EffectDefinition
    {
        public string TokenId;
        public int Count;
        public RowType Row;
        public SummonPlacement Placement;
        public bool StrictNeighbors;
    }
}
