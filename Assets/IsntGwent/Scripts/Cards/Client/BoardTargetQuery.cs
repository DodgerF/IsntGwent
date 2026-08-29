using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Match.Client;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class BoardTargetQuery
    {
        [Inject] private readonly MatchState _matchState;

        public List<BoardCell> Zone(BoardCell anchor, AimedTargetingDefinition definition)
        {
            var result = new List<BoardCell>();
            if (!anchor.IsValid) return result;

            var area = TargetArea.From(definition);
            if (area.IsUnlimited) return result;

            var anchorLine = anchor.Line;

            foreach (var (ownSide, row, _) in Rows())
            {
                if (ownSide && !definition.IncludeAllies) continue;
                if (!ownSide && !definition.IncludeEnemies) continue;

                var line = BoardGeometry.Line(ownSide, row);

                for (var index = 0; index < BoardConfig.SlotsPerRow; index++)
                    if (area.Contains(anchor.Index, anchorLine, index, line, row))
                        result.Add(new BoardCell(ownSide, row, index));
            }

            return result;
        }

        public List<string> Targets(BoardCell anchor, AimedTargetingDefinition definition)
        {
            var result = new List<string>();
            if (!anchor.IsValid) return result;

            var area = TargetArea.From(definition);
            var anchorLine = anchor.Line;

            foreach (var (ownSide, row, state) in Rows())
            {
                if (ownSide && !definition.IncludeAllies) continue;
                if (!ownSide && !definition.IncludeEnemies) continue;

                var line = BoardGeometry.Line(ownSide, row);

                for (var index = 0; index < state.Slots.Length; index++)
                {
                    var card = state.Slots[index];
                    if (card == null) continue;

                    if (area.Contains(anchor.Index, anchorLine, index, line, row))
                        result.Add(card.Id.ToString());
                }
            }

            return result;
        }

        private IEnumerable<(bool OwnSide, RowType Row, BoardRowState State)> Rows()
        {
            yield return (true, RowType.Melee, _matchState.OwnMeleeRow);
            yield return (true, RowType.Ranged, _matchState.OwnRangedRow);
            yield return (false, RowType.Melee, _matchState.EnemyMeleeRow);
            yield return (false, RowType.Ranged, _matchState.EnemyRangedRow);
        }
    }
}
