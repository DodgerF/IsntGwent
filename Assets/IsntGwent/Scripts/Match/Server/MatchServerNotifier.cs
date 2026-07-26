using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Match.Server
{
    public class MatchServerNotifier
    {
        public void NotifyGameStarted(GameContext context)
        {
            NotifyGameStartedTo(context, context.Player1);
            NotifyGameStartedTo(context, context.Player2);
        }

        private void NotifyGameStartedTo(GameContext context, Player player)
        {
            player.Connection.Send(new GameStartedMessage
            {
                CardsInHand = player.Hand.Select(CardDataFactory.Create).ToArray(),
                EnemyCardAmount = context.GetOpponent(player).Hand.Count,
                IsMyTurn = context.CurrentPlayer == player,
            });
        }

        public void NotifyPower(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            p1.Connection.Send(new PowerUpdatedMessage
            {
                OwnMeleePower = p1.MeleePower,
                OwnRangedPower = p1.RangedPower,
                OwnTotalPower = p1.TotalPower,
                EnemyMeleePower = p2.MeleePower,
                EnemyRangedPower = p2.RangedPower,
                EnemyTotalPower = p2.TotalPower,
            });

            p2.Connection.Send(new PowerUpdatedMessage
            {
                OwnMeleePower = p2.MeleePower,
                OwnRangedPower = p2.RangedPower,
                OwnTotalPower = p2.TotalPower,
                EnemyMeleePower = p1.MeleePower,
                EnemyRangedPower = p1.RangedPower,
                EnemyTotalPower = p1.TotalPower,
            });
        }

        public void NotifyCardPlayed(GameContext context, Player player, CardInstance card, RowType row)
        {
            var opponent = context.GetOpponent(player);

            if (card is UnitInstance unit)
            {
                player.Connection.Send(new OwnCardPlayedMessage
                {
                    CardInstanceId = unit.Id.ToString(),
                    Row = row
                });
                opponent.Connection.Send(new EnemyCardPlayedMessage
                {
                    CardInstanceId = unit.Id.ToString(),
                    DefinitionId = unit.Definition.Id,
                    CurrentPower = unit.CurrentPower.Value,
                    CardAmount = player.Hand.Count,
                    Row = row,
                });
            }
            else
            {
                player.Connection.Send(new OwnCardPlayedMessage
                {
                    CardInstanceId = card.Id.ToString()
                });
                opponent.Connection.Send(new EnemyCardPlayedMessage
                {
                    CardInstanceId = card.Id.ToString(),
                    DefinitionId = card.Definition.Id,
                    CardAmount = player.Hand.Count,
                });
            }
        }

        public void NotifyCardRemovedFromHand(Player player, CardInstance card)
        {
            player.Connection.Send(new CardRemovedFromHandMessage { CardInstanceId = card.Id.ToString() });
        }

        public void NotifyTurnChanged(Player player, bool isMyTurn)
        {
            player.Connection.Send(new TurnChangedMessage { IsMyTurn = isMyTurn });
        }

        public void NotifyEnemyPassed(Player player)
        {
            player.Connection.Send(new EnemyPassedMessage());
        }

        public void NotifyHpChanged(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            p1.Connection.Send(new HpChangedMessage { MyHp = p1.Hp, EnemyHp = p2.Hp });
            p2.Connection.Send(new HpChangedMessage { MyHp = p2.Hp, EnemyHp = p1.Hp });
        }

        public void NotifyRoundResult(GameContext context, bool isTie, Player winner)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            p1.Connection.Send(new RoundEndedMessage
            {
                Result = isTie ? RoundResult.Tie : winner == p1 ? RoundResult.Win : RoundResult.Lose
            });

            p2.Connection.Send(new RoundEndedMessage
            {
                Result = isTie ? RoundResult.Tie : winner == p2 ? RoundResult.Win : RoundResult.Lose
            });
        }

        public void NotifyGameEnded(GameContext context, bool isTie, Player winner, Player loser)
        {
            if (isTie)
            {
                context.Player1.Connection.Send(new GameEndedMessage { IsTie = true });
                context.Player2.Connection.Send(new GameEndedMessage { IsTie = true });
            }
            else
            {
                winner.Connection.Send(new GameEndedMessage { AmIWinner = true });
                loser.Connection.Send(new GameEndedMessage { AmIWinner = false });
            }
        }

        public void NotifyGiveUp(Player winner, Player loser)
        {
            if (winner.Connection != null && winner.Connection.isReady)
                winner.Connection.Send(new GiveUpMessage { IsMyLose = false });

            if (loser?.Connection != null && loser.Connection.isReady)
                loser.Connection.Send(new GiveUpMessage { IsMyLose = true });
        }

        public void NotifyEnemyDisconnected(Player winner)
        {
            if (winner.Connection != null && winner.Connection.isReady)
                winner.Connection.Send(new EnemyDisconnectedMessage());
        }

        public void NotifyUnitStates(GameContext context, IReadOnlyList<UnitInstance> changed)
        {
            var data = changed.Select(u => new UnitStateChangedData
            {
                InstanceId = u.Id.ToString(),
                CurrentPower = u.CurrentPower.Value,
                IsDead = u.CurrentPower.Value <= 0
            }).ToArray();

            var msg = new UnitsStateChangedMessage { Units = data };
            context.Player1.Connection.Send(msg);
            context.Player2.Connection.Send(msg);
        }

        public void NotifyDamageDealt(GameContext context, IReadOnlyList<DamageRecord> records)
        {
            if (records.Count == 0) return;

            var hits = records.Select(r => new DamageInstance
            {
                SourceInstanceId = r.Source != null ? r.Source.Id.ToString() : string.Empty,
                TargetInstanceId = r.Target.Id.ToString(),
                Amount = r.Amount
            }).ToArray();

            var msg = new DamageDealtMessage { Hits = hits };
            context.Player1.Connection.Send(msg);
            context.Player2.Connection.Send(msg);
        }

        public void NotifyCardDrawn(Player player, CardInstance card)
        {
            player.Connection.Send(new CardDrawnMessage { Card = CardDataFactory.Create(card) });
        }

        public void NotifyEnemyCardDrawn(Player opponent, int enemyCardAmount)
        {
            opponent.Connection.Send(new EnemyCardDrawnMessage { EnemyCardAmount = enemyCardAmount });
        }

        public void NotifyBoardSync(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            p1.Connection.Send(new BoardSyncMessage
            {
                OwnMeleeRow = p1.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                OwnRangedRow = p1.RangedRow.Select(CardDataFactory.Create).ToArray(),
                EnemyMeleeRow = p2.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                EnemyRangedRow = p2.RangedRow.Select(CardDataFactory.Create).ToArray(),
                OwnGraveyard = p1.Graveyard.Select(CardDataFactory.Create).ToArray(),
                EnemyGraveyard = p2.Graveyard.Select(CardDataFactory.Create).ToArray(),
            });

            p2.Connection.Send(new BoardSyncMessage
            {
                OwnMeleeRow = p2.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                OwnRangedRow = p2.RangedRow.Select(CardDataFactory.Create).ToArray(),
                EnemyMeleeRow = p1.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                EnemyRangedRow = p1.RangedRow.Select(CardDataFactory.Create).ToArray(),
                OwnGraveyard = p2.Graveyard.Select(CardDataFactory.Create).ToArray(),
                EnemyGraveyard = p1.Graveyard.Select(CardDataFactory.Create).ToArray(),
            });
        }
    }
}
