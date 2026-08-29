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

        public void NotifyCardPlayed(GameContext context, Player player, CardInstance card, RowType row, int slotIndex)
        {
            var opponent = context.GetOpponent(player);

            if (card is UnitInstance unit)
            {
                Send(player, new OwnCardPlayedMessage
                {
                    CardInstanceId = unit.Id.ToString(),
                    Row = row,
                    SlotIndex = slotIndex
                });
                Send(opponent, new EnemyCardPlayedMessage
                {
                    CardInstanceId = unit.Id.ToString(),
                    DefinitionId = unit.Definition.Id,
                    CurrentPower = unit.CurrentPower.Value,
                    Armor = unit.Armor.Value,
                    CardAmount = player.Hand.Count,
                    Row = row,
                    SlotIndex = slotIndex,
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
            => MatchSnapshotBuilder.ToMessage(MatchSnapshotBuilder.Build(context, player));

        public void NotifyUnitStates(GameContext context, IReadOnlyList<UnitInstance> changed)
        {
            var data = changed.Select(u => new UnitStateChangedData
            {
                InstanceId = u.Id.ToString(),
                CurrentPower = u.CurrentPower.Value,
                Armor = u.Armor.Value,
                IsDead = u.CurrentPower.Value <= 0
            }).ToArray();

            var msg = new UnitsStateChangedMessage { Units = data };
            Send(context.Player1, msg);
            Send(context.Player2, msg);
        }

        public void NotifyDamageDealt(GameContext context, IReadOnlyList<DamageRecord> records)
        {
            var hits = records.Where(r => r.IsVisible).Select(r => new DamageInstance
            {
                SourceInstanceId = r.Source != null ? r.Source.Id.ToString() : string.Empty,
                SourceCardId = r.Source != null ? r.Source.Definition.Id : string.Empty,
                TargetInstanceId = r.Target.Id.ToString(),
                Amount = r.Amount,
                Kind = r.Kind
            }).ToArray();

            if (hits.Length == 0) return;

            var msg = new DamageDealtMessage { Hits = hits };
            Send(context.Player1, msg);
            Send(context.Player2, msg);
        }

        public void NotifyUnitLinks(GameContext context, IReadOnlyList<UnitLinkRecord> records)
        {
            if (records.Count == 0) return;

            var links = records.Select(r => new UnitLinkData
            {
                SourceInstanceId = r.Source.Id.ToString(),
                TargetInstanceId = r.Target.Id.ToString(),
                Kind = r.Kind,
                TargetRow = r.TargetRow,
                TargetSlot = r.TargetSlot
            }).ToArray();

            var msg = new UnitLinksMessage { Links = links };
            Send(context.Player1, msg);
            Send(context.Player2, msg);
        }

        public void NotifyCardDrawn(Player player, CardInstance card)
        {
            Send(player, new CardDrawnMessage { Card = CardDataFactory.Create(card) });
        }

        public void NotifyPendingPlay(GameContext context, Player player)
        {
            var cards = player.PendingPlays.Select(CardDataFactory.Create).ToArray();

            Send(player, new PendingPlayMessage { Cards = cards, IsMine = true });
            Send(context.GetOpponent(player), new PendingPlayMessage { Cards = cards, IsMine = false });
        }

        public void NotifyEnemyCardDrawn(Player opponent, int enemyCardAmount)
        {
            Send(opponent, new EnemyCardDrawnMessage { EnemyCardAmount = enemyCardAmount });
        }

        public void NotifyRedrawStarted(GameContext context, Player player, int amount)
        {
            Send(player, new RedrawStartedMessage
            {
                RedrawsLeft = amount,
                RoundNumber = context.RoundNumber
            });
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
                OwnMeleeRow = MatchSnapshotBuilder.SlotData(p1.MeleeRow),
                OwnRangedRow = MatchSnapshotBuilder.SlotData(p1.RangedRow),
                EnemyMeleeRow = MatchSnapshotBuilder.SlotData(p2.MeleeRow),
                EnemyRangedRow = MatchSnapshotBuilder.SlotData(p2.RangedRow),
                OwnGraveyard = p1.Graveyard.Select(CardDataFactory.Create).ToArray(),
                EnemyGraveyard = p2.Graveyard.Select(CardDataFactory.Create).ToArray(),
                OwnRowStatus = MatchSnapshotBuilder.RowStatus(p1),
                EnemyRowStatus = MatchSnapshotBuilder.RowStatus(p2),
            });

            Send(p2, new BoardSyncMessage
            {
                OwnMeleeRow = MatchSnapshotBuilder.SlotData(p2.MeleeRow),
                OwnRangedRow = MatchSnapshotBuilder.SlotData(p2.RangedRow),
                EnemyMeleeRow = MatchSnapshotBuilder.SlotData(p1.MeleeRow),
                EnemyRangedRow = MatchSnapshotBuilder.SlotData(p1.RangedRow),
                OwnGraveyard = p2.Graveyard.Select(CardDataFactory.Create).ToArray(),
                EnemyGraveyard = p1.Graveyard.Select(CardDataFactory.Create).ToArray(),
                OwnRowStatus = MatchSnapshotBuilder.RowStatus(p2),
                EnemyRowStatus = MatchSnapshotBuilder.RowStatus(p1),
            });
        }

        public void NotifyDecks(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            Send(p1, new DeckSyncMessage
            {
                OwnDeck = DeckData(p1),
                EnemyDeckCount = p2.Deck.Count,
            });

            Send(p2, new DeckSyncMessage
            {
                OwnDeck = DeckData(p2),
                EnemyDeckCount = p1.Deck.Count,
            });
        }

        private static CardData[] DeckData(Player player)
            => player.Deck.OrderBy(card => card.Id).Select(CardDataFactory.Create).ToArray();

        private static void Send<T>(Player player, T message) where T : struct, NetworkMessage
        {
            player?.Seat?.Send(message);
        }
    }
}
