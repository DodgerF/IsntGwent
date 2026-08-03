using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Messages;
using Mirror;

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
            Send(player, new GameStartedMessage
            {
                CardsInHand = player.Hand.Select(CardDataFactory.Create).ToArray(),
                EnemyCardAmount = context.GetOpponent(player).Hand.Count,
                ReconnectToken = player.ReconnectToken,
            });
        }

        public void NotifyPower(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            Send(p1, new PowerUpdatedMessage
            {
                OwnMeleePower = p1.MeleePower,
                OwnRangedPower = p1.RangedPower,
                OwnTotalPower = p1.TotalPower,
                EnemyMeleePower = p2.MeleePower,
                EnemyRangedPower = p2.RangedPower,
                EnemyTotalPower = p2.TotalPower,
            });

            Send(p2, new PowerUpdatedMessage
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
                Send(player, new OwnCardPlayedMessage
                {
                    CardInstanceId = unit.Id.ToString(),
                    Row = row
                });
                Send(opponent, new EnemyCardPlayedMessage
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
                Send(player, new OwnCardPlayedMessage
                {
                    CardInstanceId = card.Id.ToString()
                });
                Send(opponent, new EnemyCardPlayedMessage
                {
                    CardInstanceId = card.Id.ToString(),
                    DefinitionId = card.Definition.Id,
                    CardAmount = player.Hand.Count,
                });
            }
        }

        public void NotifyCardRemovedFromHand(Player player, CardInstance card)
        {
            Send(player, new CardRemovedFromHandMessage { CardInstanceId = card.Id.ToString() });
        }

        public void NotifyTurnChanged(Player player, bool isMyTurn)
        {
            Send(player, new TurnChangedMessage { IsMyTurn = isMyTurn });
        }

        public void NotifyEnemyPassed(Player player)
        {
            Send(player, new EnemyPassedMessage());
        }

        public void NotifyHpChanged(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            Send(p1, new HpChangedMessage { MyHp = p1.Hp, EnemyHp = p2.Hp });
            Send(p2, new HpChangedMessage { MyHp = p2.Hp, EnemyHp = p1.Hp });
        }

        public void NotifyRoundResult(GameContext context, bool isTie, Player winner)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            Send(p1, new RoundEndedMessage
            {
                Result = isTie ? RoundResult.Tie : winner == p1 ? RoundResult.Win : RoundResult.Lose
            });

            Send(p2, new RoundEndedMessage
            {
                Result = isTie ? RoundResult.Tie : winner == p2 ? RoundResult.Win : RoundResult.Lose
            });
        }

        public void NotifyGameEnded(GameContext context, bool isTie, Player winner, Player loser)
        {
            if (isTie)
            {
                Send(context.Player1, new GameEndedMessage { IsTie = true });
                Send(context.Player2, new GameEndedMessage { IsTie = true });
            }
            else
            {
                Send(winner, new GameEndedMessage { AmIWinner = true });
                Send(loser, new GameEndedMessage { AmIWinner = false });
            }
        }

        public void NotifyGiveUp(Player winner, Player loser)
        {
            Send(winner, new GiveUpMessage { IsMyLose = false });
            Send(loser, new GiveUpMessage { IsMyLose = true });
        }

        public void NotifyEnemyDisconnected(Player winner)
        {
            Send(winner, new EnemyDisconnectedMessage());
        }

        public void NotifyOpponentReconnecting(Player player, bool isReconnecting)
        {
            Send(player, new OpponentReconnectingMessage { IsReconnecting = isReconnecting });
        }

        public void NotifySnapshot(GameContext context, Player player)
        {
            Send(player, BuildSnapshot(context, player));
        }

        public MatchSnapshotMessage BuildSnapshot(GameContext context, Player player)
        {
            var opponent = context.GetOpponent(player);

            return new MatchSnapshotMessage
            {
                CardsInHand = player.Hand.Select(CardDataFactory.Create).ToArray(),
                EnemyCardAmount = opponent.Hand.Count,

                OwnMeleeRow = player.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                OwnRangedRow = player.RangedRow.Select(CardDataFactory.Create).ToArray(),
                EnemyMeleeRow = opponent.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                EnemyRangedRow = opponent.RangedRow.Select(CardDataFactory.Create).ToArray(),
                OwnGraveyard = player.Graveyard.Select(CardDataFactory.Create).ToArray(),
                EnemyGraveyard = opponent.Graveyard.Select(CardDataFactory.Create).ToArray(),

                OwnMeleePower = player.MeleePower,
                OwnRangedPower = player.RangedPower,
                OwnTotalPower = player.TotalPower,
                EnemyMeleePower = opponent.MeleePower,
                EnemyRangedPower = opponent.RangedPower,
                EnemyTotalPower = opponent.TotalPower,

                MyHp = player.Hp,
                EnemyHp = opponent.Hp,

                IsMyTurn = !context.IsRedrawPhase && context.CurrentPlayer == player,
                IsEnemyPassed = opponent.IsPassed,

                IsRedrawPhase = context.IsRedrawPhase,
                RedrawsLeft = player.RedrawsLeft,
                IsRedrawReady = player.IsRedrawReady,

                RoundNumber = context.RoundNumber,
            };
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
            Send(context.Player1, msg);
            Send(context.Player2, msg);
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
            Send(context.Player1, msg);
            Send(context.Player2, msg);
        }

        public void NotifyCardDrawn(Player player, CardInstance card)
        {
            Send(player, new CardDrawnMessage { Card = CardDataFactory.Create(card) });
        }

        public void NotifyEnemyCardDrawn(Player opponent, int enemyCardAmount)
        {
            Send(opponent, new EnemyCardDrawnMessage { EnemyCardAmount = enemyCardAmount });
        }

        public void NotifyRedrawStarted(GameContext context, int amount)
        {
            var msg = new RedrawStartedMessage
            {
                RedrawsLeft = amount,
                RoundNumber = context.RoundNumber
            };

            Send(context.Player1, msg);
            Send(context.Player2, msg);
        }

        public void NotifyCardRedrawn(Player player, CardInstance removed, CardInstance drawn, int redrawsLeft)
        {
            Send(player, new CardRedrawnMessage
            {
                RemovedInstanceId = removed.Id.ToString(),
                NewCard = CardDataFactory.Create(drawn),
                RedrawsLeft = redrawsLeft
            });
        }

        public void NotifyRedrawEnded(GameContext context)
        {
            var msg = new RedrawEndedMessage();

            Send(context.Player1, msg);
            Send(context.Player2, msg);
        }

        public void NotifyBoardSync(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            Send(p1, new BoardSyncMessage
            {
                OwnMeleeRow = p1.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                OwnRangedRow = p1.RangedRow.Select(CardDataFactory.Create).ToArray(),
                EnemyMeleeRow = p2.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                EnemyRangedRow = p2.RangedRow.Select(CardDataFactory.Create).ToArray(),
                OwnGraveyard = p1.Graveyard.Select(CardDataFactory.Create).ToArray(),
                EnemyGraveyard = p2.Graveyard.Select(CardDataFactory.Create).ToArray(),
            });

            Send(p2, new BoardSyncMessage
            {
                OwnMeleeRow = p2.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                OwnRangedRow = p2.RangedRow.Select(CardDataFactory.Create).ToArray(),
                EnemyMeleeRow = p1.MeleeRow.Select(CardDataFactory.Create).ToArray(),
                EnemyRangedRow = p1.RangedRow.Select(CardDataFactory.Create).ToArray(),
                OwnGraveyard = p2.Graveyard.Select(CardDataFactory.Create).ToArray(),
                EnemyGraveyard = p1.Graveyard.Select(CardDataFactory.Create).ToArray(),
            });
        }

        private static void Send<T>(Player player, T message) where T : struct, NetworkMessage
        {
            if (player is not { IsConnected: true }) return;
            if (player.Connection == null) return;

            player.Connection.Send(message);
        }
    }
}
