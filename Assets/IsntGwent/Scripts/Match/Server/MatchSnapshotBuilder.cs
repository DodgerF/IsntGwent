using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Match.Server
{
    public static class MatchSnapshotBuilder
    {
        public static MatchSnapshot Build(GameContext context, Player player)
        {
            var opponent = context.GetOpponent(player);

            return new MatchSnapshot
            {
                Phase = PhaseOf(context),
                Round = context.RoundNumber,
                IsMyTurn = !context.IsRedrawPhase && context.CurrentPlayer == player,

                Limits = new MatchLimits
                {
                    MaxHand = player.MaxCardInHand,
                    StartHp = Player.StartHp,
                    SlotsPerRow = BoardConfig.SlotsPerRow,
                },

                Redraw = context.IsRedrawPhase
                    ? new RedrawSnapshot
                    {
                        Left = player.RedrawsLeft,
                        IAmReady = player.IsRedrawReady,
                        EnemyReady = opponent.IsRedrawReady,
                    }
                    : null,

                You = Side(player),
                Enemy = Side(opponent),
            };
        }

        public static MatchSnapshotMessage ToMessage(MatchSnapshot snapshot)
        {
            var you = snapshot.You;
            var enemy = snapshot.Enemy;
            var pendingMine = you.MustPlay.Length > 0;

            return new MatchSnapshotMessage
            {
                CardsInHand = you.Hand,
                EnemyCardAmount = enemy.HandCount,

                OwnDeck = you.Deck,
                EnemyDeckCount = enemy.DeckCount,

                OwnMeleeRow = you.MeleeRow,
                OwnRangedRow = you.RangedRow,
                EnemyMeleeRow = enemy.MeleeRow,
                EnemyRangedRow = enemy.RangedRow,
                OwnGraveyard = you.Graveyard,
                EnemyGraveyard = enemy.Graveyard,
                OwnRowStatus = you.RowStatus,
                EnemyRowStatus = enemy.RowStatus,

                OwnMeleePower = you.MeleePower,
                OwnRangedPower = you.RangedPower,
                OwnTotalPower = you.TotalPower,
                EnemyMeleePower = enemy.MeleePower,
                EnemyRangedPower = enemy.RangedPower,
                EnemyTotalPower = enemy.TotalPower,

                MyHp = you.Hp,
                EnemyHp = enemy.Hp,

                IsMyTurn = snapshot.IsMyTurn,
                IsEnemyPassed = enemy.Passed,

                IsRedrawPhase = snapshot.Phase == MatchPhase.Redraw,
                RedrawsLeft = snapshot.Redraw?.Left ?? 0,
                IsRedrawReady = snapshot.Redraw?.IAmReady ?? false,

                RoundNumber = snapshot.Round,

                PendingPlays = pendingMine ? you.MustPlay : enemy.MustPlay,
                IsPendingMine = pendingMine,
            };
        }

        private static MatchPhase PhaseOf(GameContext context)
        {
            if (context.GameEnded.Value) return MatchPhase.Ended;

            return context.IsRedrawPhase ? MatchPhase.Redraw : MatchPhase.Play;
        }

        private static SideSnapshot Side(Player player)
        {
            var hand = player.Hand.Select(CardDataFactory.Create).ToArray();
            var graveyard = player.Graveyard.Select(CardDataFactory.Create).ToArray();

            return new SideSnapshot
            {
                Hp = player.Hp,
                Passed = player.IsPassed,

                Hand = hand,
                HandCount = hand.Length,
                Deck = player.Deck.OrderBy(card => card.Id).Select(CardDataFactory.Create).ToArray(),
                DeckCount = player.Deck.Count,

                Graveyard = graveyard,
                GraveyardCount = graveyard.Length,

                MustPlay = player.PendingPlays.Select(CardDataFactory.Create).ToArray(),

                MeleeRow = SlotData(player.MeleeRow),
                RangedRow = SlotData(player.RangedRow),
                RowStatus = RowStatus(player),

                MeleePower = player.MeleePower,
                RangedPower = player.RangedPower,
                TotalPower = player.TotalPower,
            };
        }

        public static RowStatusData[] RowStatus(Player player)
        {
            var rows = new[] { RowType.Melee, RowType.Ranged };
            var data = new RowStatusData[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var weather = player.GetWeather(rows[i]);

                data[i] = new RowStatusData
                {
                    Row = rows[i],
                    CardId = weather != null ? weather.CardId : string.Empty,
                    TurnsLeft = weather?.TurnsLeft ?? 0,
                };
            }

            return data;
        }

        public static CardData[] SlotData(BoardRow row)
        {
            var data = new CardData[row.Slots.Length];

            for (var i = 0; i < row.Slots.Length; i++)
            {
                var unit = row.Slots[i].Unit;
                data[i] = unit != null ? CardDataFactory.Create(unit) : default;
            }

            return data;
        }
    }
}
