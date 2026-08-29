using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server
{
    public class CardResolver
    {
        private readonly EffectRegistry _effectRegistry;

        public CardResolver(EffectRegistry effectRegistry)
        {
            _effectRegistry = effectRegistry;
        }

        public bool PlayCard(GameContext context, Player caster, CardInstance card, RowType playedRow,
            int playedSlot, List<string> selectedIds, bool enemyRow = false)
        {
            var manualTargets = ResolveManualTargets(context, selectedIds);

            if (!ValidateManualTargets(context, caster, card, manualTargets, playedRow, playedSlot))
                return false;

            RunEffects(context, caster, card, EffectTrigger.OnPlay, null, manualTargets, playedRow, playedSlot,
                enemyRow);
            return true;
        }

        public bool CanPlay(GameContext context, Player caster, CardInstance card, List<string> selectedIds,
            RowType playedRow = RowType.None, int playedSlot = -1)
        {
            var manualTargets = ResolveManualTargets(context, selectedIds);
            return ValidateManualTargets(context, caster, card, manualTargets, playedRow, playedSlot);
        }

        private bool ValidateManualTargets(GameContext context, Player caster, CardInstance card,
            List<UnitInstance> manualTargets, RowType playedRow, int playedSlot)
        {
            foreach (var effectDef in card.Definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (effectDef is not ManualTargetingDefinition manualDef) continue;

                var effectContext = new EffectContext
                {
                    Game = context,
                    Source = card,
                    Owner = caster,
                    Definition = effectDef,
                    PlayedRow = playedRow,
                    PlayedSlot = playedSlot,
                };

                if (!EffectConditions.IsMet(effectContext, effectDef)) continue;

                var effect = (ManualTargetingEffect)_effectRegistry.Get(effectDef);
                var pool = effect.GetPool(effectContext);

                var poolIds = new HashSet<Guid>(pool.Select(u => u.Id));
                if (manualTargets.Any(t => !poolIds.Contains(t.Id)))
                    return false;

                var maxAvailable = Math.Min(manualDef.Count, pool.Count);
                var expected = manualDef.AllowPartial ? maxAvailable : manualDef.Count;

                if (manualTargets.Count != expected) return false;
            }

            return true;
        }
        
        public void RunEffects(GameContext context, Player owner, CardInstance card,
            EffectTrigger trigger, IGameEvent gameEvent, List<UnitInstance> manualTargets,
            RowType playedRow = RowType.None, int playedSlot = -1, bool enemyRow = false)
        {
            var effectContext = new EffectContext
            {
                Game = context,
                Source = card,
                Owner = owner,
                Event = gameEvent,
                PlayedRow = playedRow != RowType.None
                    ? playedRow
                    : (card as UnitInstance)?.RowType ?? RowType.None,
                PlayedSlot = playedSlot >= 0
                    ? playedSlot
                    : (card is UnitInstance unit ? context.FindSlot(unit)?.Index ?? -1 : -1),
                ManualTargets = manualTargets ?? new List<UnitInstance>(),
                TargetsEnemyRow = enemyRow,
            };

            foreach (var effectDef in card.Definition.Effects)
            {
                if (effectDef.Trigger != trigger) continue;
                if (effectDef.RequiredRow != RowType.None && effectDef.RequiredRow != effectContext.PlayedRow)
                    continue;

                effectContext.Definition = effectDef;

                if (!SlotConditions.IsMet(effectContext, effectDef)) continue;
                if (!EffectConditions.IsMet(effectContext, effectDef)) continue;

                var effect = _effectRegistry.Get(effectDef);
                if (!effect.CanTrigger(effectContext)) continue;

                if (effect is TargetingEffect)
                {
                    effectContext.Targets = effect.ResolveTargets(effectContext);
                    continue;
                }

                effect.Execute(effectContext);
            }
        }

        private List<UnitInstance> ResolveManualTargets(GameContext context, List<string> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0) return new List<UnitInstance>();

            return context.Player1.MeleeRow
                .Concat(context.Player1.RangedRow)
                .Concat(context.Player2.MeleeRow)
                .Concat(context.Player2.RangedRow)
                .Where(u => selectedIds.Contains(u.Id.ToString()))
                .ToList();
        }

        public bool NeedsRowChoice(CardDefinition definition)
        {
            if (TargetsAnyRow(definition)) return true;

            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (effectDef is TargetingEffectDefinition { RestrictToPlayedRow: true })
                    return true;
            }

            return false;
        }

        public bool TargetsAnyRow(CardDefinition definition)
        {
            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (effectDef is ApplyWeatherDefinition) return true;
            }

            return false;
        }

        public bool NeedsAimAfterPlacement(CardDefinition definition, out AimedTargetingDefinition aim,
            Func<EffectDefinition, bool> conditionFilter = null)
        {
            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (effectDef is not AimedTargetingDefinition aimed) continue;
                if (conditionFilter != null && !conditionFilter(effectDef)) continue;

                aim = aimed;
                return true;
            }

            aim = null;
            return false;
        }

        public bool NeedsManualTargets(CardDefinition definition, out int count,
            Func<EffectDefinition, bool> conditionFilter = null)
        {
            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (conditionFilter != null && !conditionFilter(effectDef)) continue;

                var effect = _effectRegistry.Get(effectDef);
                if (effect.NeedsManualTargets(effectDef))
                {
                    count = effect.ManualTargetCount(effectDef);
                    return true;
                }
            }

            count = 0;
            return false;
        }

        public List<string> GetTargetPool(
            CardDefinition definition,
            IEnumerable<UnitInstance> ownUnits,
            IEnumerable<UnitInstance> enemyUnits,
            Func<EffectDefinition, bool> conditionFilter = null)
        {
            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (conditionFilter != null && !conditionFilter(effectDef)) continue;

                if (effectDef is not TargetingEffectDefinition targetingDef) continue;

                var result = new List<string>();

                if (targetingDef.IncludeEnemies)
                    result.AddRange(enemyUnits.Select(u => u.Id.ToString()));
                if (targetingDef.IncludeAllies)
                    result.AddRange(ownUnits.Select(u => u.Id.ToString()));

                return result;
            }

            return new List<string>();
        }
    }
}
