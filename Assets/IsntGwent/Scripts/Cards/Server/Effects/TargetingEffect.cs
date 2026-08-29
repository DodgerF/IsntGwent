using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public abstract class TargetingEffect : CardEffectBase
    {
        public abstract List<UnitInstance> GetPool(EffectContext context);

        public override void Execute(EffectContext context) { }

        public static bool MatchesSide(TargetingEffectDefinition definition, bool isAlly)
        {
            if (!definition.IncludeAllies && !definition.IncludeEnemies) return true;

            return isAlly ? definition.IncludeAllies : definition.IncludeEnemies;
        }

        protected static List<UnitInstance> BuildPool(EffectContext context)
        {
            var definition = (TargetingEffectDefinition)context.Definition;
            var result = new List<UnitInstance>();
            var opponent = context.Game.GetOpponent(context.Owner);

            if (definition.IncludeEnemies)
            {
                result.AddRange(opponent.MeleeRow);
                result.AddRange(opponent.RangedRow);
            }

            if (definition.IncludeAllies)
            {
                result.AddRange(context.Owner.MeleeRow);
                result.AddRange(context.Owner.RangedRow);
            }

            if (definition.SameRowAsSource && context.Source is UnitInstance source)
                result = result.Where(u => u.RowType == source.RowType).ToList();

            if (definition.RestrictToPlayedRow && context.PlayedRow != RowType.None)
                result = result.Where(u => u.RowType == context.PlayedRow).ToList();

            if (definition.ExcludeSource && context.Source is UnitInstance self)
                result = result.Where(u => u != self).ToList();

            return FilterByArea(context, definition, result);
        }

        private static List<UnitInstance> FilterByArea(EffectContext context,
            TargetingEffectDefinition definition, List<UnitInstance> pool)
        {
            var area = TargetArea.From(definition);

            if (area.Row != RowType.None)
                pool = pool.Where(u => area.MatchesRow(u.RowType)).ToList();

            if (!area.NeedsAnchor) return pool;

            var anchor = context.Anchor(definition.RangeAnchor);
            if (anchor == null) return new List<UnitInstance>();

            var anchorColumn = anchor.Index;
            var anchorLine = BoardGeometry.Line(anchor, context.Owner);

            var result = new List<UnitInstance>();

            foreach (var unit in pool)
            {
                var slot = context.Game.FindSlot(unit);
                if (slot == null) continue;

                if (area.Contains(anchorColumn, anchorLine, slot.Index,
                        BoardGeometry.Line(slot, context.Owner), slot.Row))
                    result.Add(unit);
            }

            return result;
        }
    }

    public abstract class TargetingEffectDefinition : EffectDefinition
    {
        public bool IncludeAllies;
        public bool IncludeEnemies;
        public bool SameRowAsSource;
        public bool RestrictToPlayedRow;
        public int MinRange;
        public int MaxRange;
        public int Width;
        public RowType TargetRow;
        public bool ExcludeAnchor;
        public bool SameLine;
        public bool ExcludeSource;
        public SlotAnchor RangeAnchor;
    }
}
