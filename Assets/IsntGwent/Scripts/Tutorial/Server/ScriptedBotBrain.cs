using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Match.Server.Bot;
using IsntGwent.Scripts.Tutorial.Definitions;

namespace IsntGwent.Scripts.Tutorial.Server
{
    public class ScriptedBotBrain : IBotBrain
    {
        private readonly List<TutorialBotMove> _moves;

        private int _index;

        public ScriptedBotBrain(IReadOnlyList<TutorialBotMove> moves)
        {
            _moves = moves == null ? new List<TutorialBotMove>() : moves.ToList();
        }

        public BotMove Decide(GameContext context, Player me)
        {
            if (me.NextPendingPlay is UnitInstance pending)
                return PlayPending(me, pending);

            var move = Next();

            if (move == null || move.Pass)
                return BotMove.Pass();

            var card = me.Hand.FirstOrDefault(c => c.Definition.Id == move.Card);

            if (card == null)
            {
                Log.Warn(LogTag.Tutorial, $"scripted card is not in hand: {move.Card}");
                return BotMove.Pass();
            }

            var owner = move.EnemyRow ? context.GetOpponent(me) : me;
            var row = ClearOfWeather(owner, ParseRow(move.Row));
            var slot = FreeSlot(owner, row, move.Slot);

            if (slot < 0)
            {
                Log.Warn(LogTag.Tutorial, $"no free slot for {move.Card} in {row}");
                return BotMove.Pass();
            }

            return BotMove.Play(card, row, slot, move.EnemyRow, ResolveTargets(context, move.Targets));
        }

        public List<string> ChooseAim(GameContext context, Player me, IReadOnlyList<string> pool)
        {
            var move = Current();
            var chosen = ResolveTargets(context, move?.Targets);

            return chosen.Where(id => pool == null || pool.Contains(id)).ToList();
        }

        public string ChooseRedraw(GameContext context, Player me) => null;

        private TutorialBotMove Next()
        {
            if (_index >= _moves.Count) return null;

            return _moves[_index++];
        }

        private TutorialBotMove Current()
        {
            var index = _index - 1;

            return index >= 0 && index < _moves.Count ? _moves[index] : null;
        }

        private static BotMove PlayPending(Player me, UnitInstance pending)
        {
            var slot = me.FirstFreeSlot();

            if (slot == null) return BotMove.Pass();

            return BotMove.Play(pending, slot.Row, slot.Index, false, new List<string>());
        }

        private static RowType ParseRow(string row)
        {
            return string.Equals(row, "Ranged", System.StringComparison.OrdinalIgnoreCase)
                ? RowType.Ranged
                : RowType.Melee;
        }

        private static RowType ClearOfWeather(Player owner, RowType row)
        {
            if (owner.GetWeather(row) == null) return row;

            var other = row == RowType.Melee ? RowType.Ranged : RowType.Melee;

            return owner.GetWeather(other) == null ? other : row;
        }

        private static int FreeSlot(Player owner, RowType row, int preferred)
        {
            var board = owner.GetRow(row);

            if (board.IsFree(preferred)) return preferred;

            return board.FirstFreeIndex;
        }

        private static List<string> ResolveTargets(GameContext context, List<string> cardIds)
        {
            var targets = new List<string>();

            if (cardIds == null || cardIds.Count == 0) return targets;

            foreach (var cardId in cardIds)
            {
                var unit = Units(context).FirstOrDefault(u =>
                    u.Definition.Id == cardId && !targets.Contains(u.Id.ToString()));

                if (unit == null) continue;

                targets.Add(unit.Id.ToString());
            }

            return targets;
        }

        private static IEnumerable<UnitInstance> Units(GameContext context)
        {
            return context.Player1.MeleeRow
                .Concat(context.Player1.RangedRow)
                .Concat(context.Player2.MeleeRow)
                .Concat(context.Player2.RangedRow);
        }
    }
}
