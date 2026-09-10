using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server
{
    public class AimPause
    {
        public int EffectIndex;
        public List<string> Pool;
        public int DestroyedPower;
        public int KilledCount;
    }

    public class CardResolver
    {
        private readonly EffectRegistry _effectRegistry;

        public CardResolver(EffectRegistry effectRegistry)
        {
            _effectRegistry = effectRegistry;
        }

        public AimPause PlayCard(GameContext context, Player caster, CardInstance card, RowType playedRow,
            int playedSlot, List<string> selectedIds, bool enemyRow = false)
        {
            var manualTargets = ResolveManualTargets(context, selectedIds);

            if (!ValidateManualTargets(context, caster, card, manualTargets, playedRow, playedSlot))
                return null;

            var effectContext = BuildContext(context, caster, card, null, manualTargets, playedRow, playedSlot,
                enemyRow);

            return Run(effectContext, card, EffectTrigger.OnPlay, 0, allowPause: true);
        }

        public AimPause ContinuePlay(GameContext context, PendingAim pending)
        {
            var manualTargets = ResolveManualTargets(context, pending.TargetIds);

            var effectContext = BuildContext(context, pending.BoardOwner, pending.Card, null, manualTargets,
                pending.Row, pending.Slot, pending.EnemyRow);

            effectContext.DestroyedPower = pending.DestroyedPower;
            effectContext.KilledCount = pending.KilledCount;

            return Run(effectContext, pending.Card, EffectTrigger.OnPlay, pending.EffectIndex, allowPause: true);
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
            var cursor = 0;

            foreach (var effectDef in card.Definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (effectDef.RequiredRow != RowType.None && effectDef.RequiredRow != playedRow) continue;
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

                var maxAvailable = Math.Min(manualDef.Count, pool.Count);
                var expected = manualDef.AllowPartial ? maxAvailable : manualDef.Count;

                if (cursor + expected > manualTargets.Count)
                    return effectDef is AimedTargetingDefinition && cursor == manualTargets.Count;

                var poolIds = new HashSet<Guid>(pool.Select(u => u.Id));

                for (var i = cursor; i < cursor + expected; i++)
                {
                    if (!poolIds.Contains(manualTargets[i].Id)) return false;
                }

                cursor += expected;
            }

            return cursor == manualTargets.Count;
        }
        
        public void RunEffects(GameContext context, Player owner, CardInstance card,
            EffectTrigger trigger, IGameEvent gameEvent, List<UnitInstance> manualTargets,
            RowType playedRow = RowType.None, int playedSlot = -1, bool enemyRow = false)
        {
            var effectContext = BuildContext(context, owner, card, gameEvent, manualTargets, playedRow, playedSlot,
                enemyRow);

            Run(effectContext, card, trigger, 0, allowPause: false);
        }

        private static EffectContext BuildContext(GameContext context, Player owner, CardInstance card,
            IGameEvent gameEvent, List<UnitInstance> manualTargets, RowType playedRow, int playedSlot, bool enemyRow)
        {
            return new EffectContext
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
        }

        private AimPause Run(EffectContext effectContext, CardInstance card, EffectTrigger trigger,
            int startIndex, bool allowPause)
        {
            var effects = card.Definition.Effects;

            for (var i = startIndex; i < effects.Count; i++)
            {
                var effectDef = effects[i];

                if (effectDef.Trigger != trigger) continue;
                if (effectDef.RequiredRow != RowType.None && effectDef.RequiredRow != effectContext.PlayedRow)
                    continue;

                effectContext.Definition = effectDef;

                if (!SlotConditions.IsMet(effectContext, effectDef)) continue;
                if (!EffectConditions.IsMet(effectContext, effectDef)) continue;

                var effect = _effectRegistry.Get(effectDef);
                if (!effect.CanTrigger(effectContext)) continue;

                if (allowPause && effectDef is AimedTargetingDefinition { Count: > 0 }
                                && effectContext.ManualCursor >= effectContext.ManualTargets.Count)
                {
                    var pool = ((ManualTargetingEffect)effect).GetPool(effectContext);

                    if (pool.Count > 0)
                        return new AimPause
                        {
                            EffectIndex = i,
                            Pool = pool.Select(u => u.Id.ToString()).ToList(),
                            DestroyedPower = effectContext.DestroyedPower,
                            KilledCount = effectContext.KilledCount,
                        };
                }

                if (effect is TargetingEffect)
                {
                    effectContext.Targets = effect.ResolveTargets(effectContext);
                    continue;
                }

                effect.Execute(effectContext);
            }

            return null;
        }

        private List<UnitInstance> ResolveManualTargets(GameContext context, List<string> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0) return new List<UnitInstance>();

            var board = context.Player1.MeleeRow
                .Concat(context.Player1.RangedRow)
                .Concat(context.Player2.MeleeRow)
                .Concat(context.Player2.RangedRow)
                .ToDictionary(u => u.Id.ToString());

            var result = new List<UnitInstance>();

            foreach (var id in selectedIds)
            {
                if (board.TryGetValue(id, out var unit) && !result.Contains(unit))
                    result.Add(unit);
            }

            return result;
        }

        public List<AimedTargetingDefinition> AimSequence(CardDefinition definition,
            Func<EffectDefinition, bool> conditionFilter = null)
        {
            var result = new List<AimedTargetingDefinition>();

            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (effectDef is not AimedTargetingDefinition aimed) continue;
                if (conditionFilter != null && !conditionFilter(effectDef)) continue;

                result.Add(aimed);
            }

            return result;
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
