using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Messages;
using UniRx;

namespace IsntGwent.Scripts.Match
{
    public static class GameControllerServer
    {
        public static void StartGame(GameContext context)
        {
            Shuffle(context.Player1.Deck);
            Shuffle(context.Player2.Deck);
            
            DrawCards(context.Player1, 10);
            DrawCards(context.Player2, 10);
            
            var rnd = UnityEngine.Random.Range(0, 2);
            context.CurrentPlayer = rnd == 0 ? context.Player1 : context.Player2;
            
            Send(context.Player1, context);
            Send(context.Player2, context);
        }
        
        public static void SyncPower(GameContext context)
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
            SyncUnitStates(context);
        }

        public static void Send(Player player, GameContext context)
        {
            var cardsInHand = player.Hand.Select(CardDataFactory.Create).ToArray();

            player.Connection.Send(new GameStartedMessage
            {
                CardsInHand = cardsInHand,
                EnemyCardAmount = context.GetOpponent(player).Hand.Count,
                IsMyTurn = context.CurrentPlayer == player,
            });
        }
        
        public static void PlayCard(GameContext context, Player player, CardInstance card,
            RowType row, List<string> selectedIds, CardResolver cardResolver)
        {
            if (!player.Hand.Contains(card))
                return;
            
            if (!PlaceCard(context, player, card, row))
                return;
            
            player.Hand.Remove(card);
            SendCardPlayed(context, player, card, row);
            player.Connection.Send(new CardRemovedFromHandMessage { CardInstanceId = card.Id.ToString() });
            
            cardResolver.PlayCard(context, player, card, selectedIds);
            
            SyncPower(context);
            if (player.Hand.Count == 0)
            {
                PassTurn(context, player);
            }
            else
            {
                ChangeTurn(context);
            }
        }
        
        private static void SendCardPlayed(GameContext context, Player player, CardInstance card, RowType row)
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
        
        private static bool PlaceCard(GameContext context, Player player, CardInstance card, RowType row)
        {
            if (card is UnitInstance unit)
            {
                if (unit.UnitDefinition.Row != row) return false;
                
                player.GetRow(row).Add(unit);
                context.OnUnitAddedToRow(unit);
            }
            else
            {
                player.Graveyard.Add(card);
            }
            
            return true;
        }
        
        
        public static void PassTurn(GameContext context, Player player)
        {
            player.IsPassed = true;
            var opponent = context.GetOpponent(player);
            if (opponent.IsPassed)
            {
                EndRound(context);
                return;
            }
            player.Connection.Send(new TurnChangedMessage
            {
                IsMyTurn = false
            });
            
            opponent.Connection.Send(new EnemyPassedMessage());

            context.CurrentPlayer = opponent;
            opponent.Connection.Send(new TurnChangedMessage { IsMyTurn = true });
        }
        
        public static void EndRound(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;
    
            var isTie = p1.TotalPower == p2.TotalPower;
            var winner = isTie ? null : p1.TotalPower > p2.TotalPower ? p1 : p2;
            var loser = winner != null ? context.GetOpponent(winner) : null;
            
            if (isTie)
            {
                p1.Hp--;
                p2.Hp--;
            }
            else
            {
                loser.Hp--;
            }
            p1.Connection.Send(new HpChangedMessage()
            {
                MyHp = p1.Hp,
                EnemyHp = p2.Hp,
            });
            p2.Connection.Send(new HpChangedMessage()
            {
                MyHp = p2.Hp,
                EnemyHp = p1.Hp,
            });

            var isGameEnded = p1.Hp <= 0 || p2.Hp <= 0;

            if (isGameEnded)
            {
                SendGameEnded(context, isTie, winner, loser);
                return;
            }
            
            MoveAllToGraveyard(p1);
            MoveAllToGraveyard(p2);
            SyncPower(context);
            SyncBoard(context);

            p1.IsPassed = false;
            p2.IsPassed = false;
            
            DrawAndSync(context, p1, 1);
            DrawAndSync(context, p2, 1);
            
            context.CurrentPlayer = isTie
                ? context.GetOpponent(context.CurrentPlayer)
                : winner;
            
            p1.Connection.Send(new RoundEndedMessage
            {
                Result = isTie ? RoundResult.Tie : winner == p1 ? RoundResult.Win : RoundResult.Lose
            });
            p1.Connection.Send(new TurnChangedMessage
            {
                IsMyTurn = context.CurrentPlayer == p1,
            });

            p2.Connection.Send(new RoundEndedMessage
            {
                
                Result = isTie ? RoundResult.Tie : winner == p2 ? RoundResult.Win : RoundResult.Lose
            });
            p2.Connection.Send(new TurnChangedMessage
            {
                IsMyTurn = context.CurrentPlayer == p2,
            });
        }
        
        public static void SyncUnitStates(GameContext context)
        {
            var changed = context.FlushChangedUnits();
            if (changed.Count == 0) return;

            var data = changed.Select(u => new UnitStateChangedData
            {
                InstanceId = u.Id.ToString(),
                CurrentPower = u.CurrentPower.Value,
                IsDead = u.CurrentPower.Value <= 0
            }).ToArray();

            var msg = new UnitsStateChangedMessage { Units = data };
            context.Player1.Connection.Send(msg);
            context.Player2.Connection.Send(msg);
            
            foreach (var unit in changed.Where(u => u.CurrentPower.Value <= 0))
            {
                MoveToGraveyard(context, unit);
                unit.OnDead.OnNext(Unit.Default);
                unit.CurrentPower.Value = unit.UnitDefinition.Power;
            }
        }
        
        private static void MoveToGraveyard(GameContext context, UnitInstance unit)
        {
            foreach (var player in new[] { context.Player1, context.Player2 })
            {
                if (player.MeleeRow.Remove(unit) || player.RangedRow.Remove(unit))
                {
                    player.Graveyard.Add(unit);
                    return;
                }
            }
        }
        
        public static void MoveAllToGraveyard(Player player)
        {
            foreach (var unit in player.MeleeRow)
                player.Graveyard.Add(unit);
            foreach (var unit in player.RangedRow)
                player.Graveyard.Add(unit);
    
            player.MeleeRow.Clear();
            player.RangedRow.Clear();
        }
        
        public static void SendGameEnded(GameContext context, bool isTie, Player winner, Player loser)
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
            context.GameEnded.OnNext(Unit.Default);
        }

        public static void ChangeTurn(GameContext context)
        {
            var currentPlayer = context.CurrentPlayer;
            var opponent = context.GetOpponent(currentPlayer);
            
            if (opponent.IsPassed)
            {
                currentPlayer.Connection.Send(new TurnChangedMessage { IsMyTurn = true });
                return;
            }
            
            context.CurrentPlayer = opponent;
            currentPlayer.Connection.Send(new TurnChangedMessage { IsMyTurn = false });
            opponent.Connection.Send(new TurnChangedMessage { IsMyTurn = true });
        }
        
        public static void Shuffle(List<CardInstance> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);

                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }

        public static void DrawCards(Player player, int amount)
        {
            if (player.Deck.Count == 0) return; 

            for (int i = 0; i < amount; i++)
            {
                if (player.Deck.Count == 0) break;
                if (player.Hand.Count == player.MaxCardInHand) break;
                
                var card = player.Deck[0];
                player.Deck.RemoveAt(0);
                
                player.Hand.Add(card);
            }
        }
        
        public static void DrawAndSync(GameContext context, Player player, int amount)
        {
            var before = player.Hand.Count;
            DrawCards(player, amount);
            var drawn = player.Hand.Count - before;
            
            for (var i = player.Hand.Count - drawn; i < player.Hand.Count; i++)
            {
                var card = player.Hand[i];
                player.Connection.Send(new CardDrawnMessage
                {
                   Card = CardDataFactory.Create(card),
                });
            }
            
            var opponent = context.GetOpponent(player);
            opponent.Connection.Send(new EnemyCardDrawnMessage
            {
                EnemyCardAmount = player.Hand.Count
            });
        }
        
        public static void SyncBoard(GameContext context)
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