using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Match.Client;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class PlayPreviewQuery
    {
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly ClientConditionQuery _conditions;

        public readonly struct Prediction
        {
            public readonly IReadOnlyList<string> Hostile;
            public readonly IReadOnlyList<string> Friendly;

            public Prediction(IReadOnlyList<string> hostile, IReadOnlyList<string> friendly)
            {
                Hostile = hostile ?? Array.Empty<string>();
                Friendly = friendly ?? Array.Empty<string>();
            }

            public static Prediction Empty => new(Array.Empty<string>(), Array.Empty<string>());

            public bool IsEmpty => Hostile.Count == 0 && Friendly.Count == 0;
        }

        private enum Sentiment { None, Hostile, Friendly }

        public Prediction Predict(CardDefinition definition, BoardCell cell)
        {
            if (definition == null || !cell.IsValid) return Prediction.Empty;

            List<string> hostile = null;
            List<string> friendly = null;
            List<CardInstance> targets = null;

            foreach (var effect in definition.Effects)
            {
                if (effect.Trigger != EffectTrigger.OnPlay) continue;
                if (effect.RequiredRow != RowType.None && effect.RequiredRow != cell.Row) continue;
                if (!IsSlotConditionMet(effect.SlotCondition, cell)) continue;
                if (!_conditions.IsMet(effect)) continue;

                if (effect is TargetingEffectDefinition targeting)
                {
                    targets = ResolveTargets(targeting, cell);
                    continue;
                }

                if (targets == null || targets.Count == 0) continue;

                switch (SentimentOf(effect))
                {
                    case Sentiment.Hostile:
                        Collect(ref hostile, targets);
                        break;
                    case Sentiment.Friendly:
                        Collect(ref friendly, targets);
                        break;
                }
            }

            return new Prediction(hostile, friendly);
        }

        public Prediction PredictOnTarget(CardDefinition definition, CardInstance target)
        {
            if (definition == null || target == null) return Prediction.Empty;

            var cell = CellOf(target);
            if (!cell.IsValid) return Prediction.Empty;

            List<string> hostile = null;
            List<string> friendly = null;
            List<CardInstance> targets = null;

            foreach (var effect in definition.Effects)
            {
                if (effect.Trigger != EffectTrigger.OnPlay) continue;
                if (!_conditions.IsMet(effect)) continue;

                if (effect is ManualTargetingDefinition)
                {
                    targets = One(target);
                    continue;
                }

                if (effect is TargetingEffectDefinition targeting)
                {
                    targets = ResolveFromTarget(targeting, cell);
                    continue;
                }

                if (targets == null || targets.Count == 0) continue;

                switch (SentimentOf(effect))
                {
                    case Sentiment.Hostile:
                        Collect(ref hostile, targets);
                        break;
                    case Sentiment.Friendly:
                        Collect(ref friendly, targets);
                        break;
                }
            }

            return new Prediction(hostile, friendly);
        }

        private List<CardInstance> ResolveFromTarget(TargetingEffectDefinition definition, BoardCell cell)
        {
            return definition switch
            {
                NeighborTargetingDefinition neighbor when neighbor.RelativeTo == NeighborRelativeTo.ManualTarget
                    => Neighbors(neighbor, cell),
                SlotTargetingDefinition slot when slot.Anchor == SlotAnchor.ManualTarget
                    => SlotFromTarget(slot, cell),
                _ => null
            };
        }

        private List<CardInstance> SlotFromTarget(SlotTargetingDefinition definition, BoardCell cell)
        {
            var opposite = new BoardCell(!cell.OwnSide, cell.Row, cell.Index);

            var target = definition.Direction switch
            {
                SlotDirection.Opposite => opposite,
                SlotDirection.Left => new BoardCell(cell.OwnSide, cell.Row, cell.Index - 1),
                SlotDirection.Right => new BoardCell(cell.OwnSide, cell.Row, cell.Index + 1),
                SlotDirection.OppositeLeft => new BoardCell(opposite.OwnSide, opposite.Row, opposite.Index - 1),
                SlotDirection.OppositeRight => new BoardCell(opposite.OwnSide, opposite.Row, opposite.Index + 1),
                _ => default
            };

            var card = At(target);
            if (card == null) return null;

            return TargetingEffect.MatchesSide(definition, target.OwnSide) ? One(card) : null;
        }

        private BoardCell CellOf(CardInstance card)
        {
            foreach (var ownSide in new[] { true, false })
            foreach (var row in new[] { RowType.Melee, RowType.Ranged })
            {
                var index = Row(ownSide, row).IndexOf(card);

                if (index >= 0) return new BoardCell(ownSide, row, index);
            }

            return default;
        }

        private static void Collect(ref List<string> bucket, List<CardInstance> targets)
        {
            bucket ??= new List<string>();

            foreach (var target in targets)
            {
                var id = target.Id.ToString();
                if (!bucket.Contains(id))
                    bucket.Add(id);
            }
        }

        private static Sentiment SentimentOf(EffectDefinition effect)
        {
            return effect switch
            {
                DealDamageEffectDefinition damage => damage.Self ? Sentiment.None : Sentiment.Hostile,
                DevourDefinition => Sentiment.Hostile,
                DestroyEffectDefinition => Sentiment.Hostile,
                BuffPowerDefinition => Sentiment.Friendly,
                HealDefinition => Sentiment.Friendly,
                GainArmorDefinition => Sentiment.Friendly,
                _ => Sentiment.None
            };
        }

        private List<CardInstance> ResolveTargets(TargetingEffectDefinition definition, BoardCell cell)
        {
            return definition switch
            {
                LineTargetingDefinition line when line.Anchor == SlotAnchor.Source => LineTarget(line, cell),
                SlotTargetingDefinition slot when slot.Anchor == SlotAnchor.Source => SlotTarget(slot, cell),
                NeighborTargetingDefinition neighbor when neighbor.RelativeTo == NeighborRelativeTo.Source
                    => Neighbors(neighbor, cell),
                _ => null
            };
        }

        private List<CardInstance> LineTarget(LineTargetingDefinition definition, BoardCell cell)
        {
            foreach (var line in BoardGeometry.LinesAhead(cell.Line))
            {
                var ahead = BoardGeometry.Cell(line, cell.Index);
                var card = At(ahead);
                if (card == null) continue;

                return TargetingEffect.MatchesSide(definition, ahead.OwnSide == cell.OwnSide)
                    ? One(card)
                    : null;
            }

            return null;
        }

        private List<CardInstance> SlotTarget(SlotTargetingDefinition definition, BoardCell cell)
        {
            var opposite = new BoardCell(!cell.OwnSide, cell.Row, cell.Index);

            var target = definition.Direction switch
            {
                SlotDirection.Opposite => opposite,
                SlotDirection.Left => new BoardCell(cell.OwnSide, cell.Row, cell.Index - 1),
                SlotDirection.Right => new BoardCell(cell.OwnSide, cell.Row, cell.Index + 1),
                SlotDirection.OppositeLeft => new BoardCell(opposite.OwnSide, opposite.Row, opposite.Index - 1),
                SlotDirection.OppositeRight => new BoardCell(opposite.OwnSide, opposite.Row, opposite.Index + 1),
                _ => default
            };

            var card = At(target);
            if (card == null) return null;

            return TargetingEffect.MatchesSide(definition, target.OwnSide == cell.OwnSide) ? One(card) : null;
        }

        private List<CardInstance> Neighbors(NeighborTargetingDefinition definition, BoardCell cell)
        {
            var neighbors = new List<CardInstance>();

            var left = At(new BoardCell(cell.OwnSide, cell.Row, cell.Index - 1));
            var right = At(new BoardCell(cell.OwnSide, cell.Row, cell.Index + 1));

            if (left != null) neighbors.Add(left);
            if (right != null) neighbors.Add(right);

            if (neighbors.Count == 0 || definition.Pick == NeighborPick.All) return neighbors;

            var edge = definition.Pick == NeighborPick.Highest ? int.MinValue : int.MaxValue;

            foreach (var neighbor in neighbors)
            {
                if (neighbor is not UnitInstance unit) continue;

                var power = unit.CurrentPower.Value;
                if (definition.Pick == NeighborPick.Highest ? power > edge : power < edge)
                    edge = power;
            }

            var chosen = new List<CardInstance>();

            foreach (var neighbor in neighbors)
                if (neighbor is UnitInstance unit && unit.CurrentPower.Value == edge)
                    chosen.Add(neighbor);

            return chosen;
        }

        private bool IsSlotConditionMet(SlotCondition condition, BoardCell cell)
        {
            if (condition == SlotCondition.None) return true;

            var hasNeighbor = At(new BoardCell(cell.OwnSide, cell.Row, cell.Index - 1)) != null
                              || At(new BoardCell(cell.OwnSide, cell.Row, cell.Index + 1)) != null;

            var opposite = At(new BoardCell(!cell.OwnSide, cell.Row, cell.Index));

            return condition switch
            {
                SlotCondition.NeighborsEmpty => !hasNeighbor,
                SlotCondition.HasNeighbor => hasNeighbor,
                SlotCondition.OppositeOccupied => opposite != null,
                SlotCondition.OppositeEmpty => opposite == null,
                _ => true
            };
        }

        private static List<CardInstance> One(CardInstance card) => new() { card };

        private CardInstance At(BoardCell cell)
        {
            if (!cell.IsValid) return null;

            return Row(cell.OwnSide, cell.Row).Slots[cell.Index];
        }

        private BoardRowState Row(bool ownSide, RowType row)
        {
            if (ownSide)
                return row == RowType.Melee ? _matchState.OwnMeleeRow : _matchState.OwnRangedRow;

            return row == RowType.Melee ? _matchState.EnemyMeleeRow : _matchState.EnemyRangedRow;
        }
    }
}
