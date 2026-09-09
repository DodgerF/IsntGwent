using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public enum BotMoveKind
    {
        None,
        Play,
        Pass,
    }

    public class BotMove
    {
        public BotMoveKind Kind = BotMoveKind.None;
        public CardInstance Card;
        public RowType Row = RowType.None;
        public int Slot = -1;
        public bool EnemyRow;
        public List<string> TargetIds = new();

        public static BotMove Pass() => new() { Kind = BotMoveKind.Pass };

        public static BotMove Play(CardInstance card, RowType row, int slot, bool enemyRow,
            List<string> targetIds)
        {
            return new BotMove
            {
                Kind = BotMoveKind.Play,
                Card = card,
                Row = row,
                Slot = slot,
                EnemyRow = enemyRow,
                TargetIds = targetIds ?? new List<string>(),
            };
        }
    }
}
