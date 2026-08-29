using System.Collections.Generic;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Network.Http
{
    public class ErrorWire
    {
        public string Code;
        public string Message;
        public object Details;
    }

    public class ViolationWire
    {
        public string Code;
        public Dictionary<string, object> Data;
    }

    public class SessionRequest
    {
        public string Name;
        public string Uuid;
    }

    public class SessionWire
    {
        public string SessionId;
        public string PlayerId;
        public long ExpiresAt;
        public int ContentVersion;
    }

    public class SessionStateWire
    {
        public string Phase;
        public string LobbyId;
        public string MatchId;
        public int StateVersion;
    }

    public class LobbyWire
    {
        public string LobbyId;
        public string Name;
        public bool IsPrivate;
        public int Players;
        public int MaxPlayers;
    }

    public class LobbyListWire
    {
        public int Version;
        public List<LobbyWire> Lobbies = new();
    }

    public class CreateLobbyRequest
    {
        public string Name;
        public string Password;
        public DeckDefinition Deck;
    }

    public class JoinLobbyRequest
    {
        public string Password;
        public DeckDefinition Deck;
    }

    public class CreatedLobbyWire
    {
        public string LobbyId;
    }

    public class OkWire
    {
        public bool Ok = true;
    }

    public class CardStateWire
    {
        public string InstanceId;
        public string CardId;
        public int Power;
    }

    public class RowsWire
    {
        public CardStateWire[] Melee;
        public CardStateWire[] Ranged;
    }

    public class PowerWire
    {
        public int Melee;
        public int Ranged;
        public int Total;
    }

    public class SideWire
    {
        public int Hp;
        public bool Passed;
        public CardStateWire[] Hand;
        public int HandCount;
        public int DeckCount;
        public int GraveyardCount;
        public CardStateWire[] MustPlay;
        public RowsWire Rows;
        public PowerWire Power;
    }

    public class LimitsWire
    {
        public int MaxHand;
        public int StartHp;
        public int SlotsPerRow;
    }

    public class RedrawWire
    {
        public int Left;
        public bool IAmReady;
        public bool EnemyReady;
    }

    public class OutcomeWire
    {
        public string Result;
        public string Reason;
    }

    public class MatchStateWire
    {
        public int Version;
        public string MatchId;
        public string Phase;
        public int Round;
        public bool IsMyTurn;
        public LimitsWire Limits;
        public RedrawWire Redraw;
        public SideWire You;
        public SideWire Enemy;
        public string RoundResult;
        public OutcomeWire Outcome;
        public List<Dictionary<string, object>> Events = new();
    }

    public class IntentRequest
    {
        public string IntentId;
        public string Type;
        public string CardInstanceId;
        public string Row;
        public int SlotIndex = -1;
        public string[] TargetIds;
    }

    public class IntentAcceptedWire
    {
        public bool Accepted = true;
        public int Version;
    }

    public static class GatewayMapper
    {
        public static CardStateWire Card(CardData data)
        {
            if (data.IsEmpty) return null;

            return new CardStateWire
            {
                InstanceId = data.InstanceId,
                CardId = data.DefinitionId,
                Power = data.CurrentPower,
            };
        }

        public static CardStateWire[] Cards(CardData[] data)
        {
            if (data == null) return new CardStateWire[0];

            var result = new CardStateWire[data.Length];

            for (var i = 0; i < data.Length; i++)
                result[i] = Card(data[i]);

            return result;
        }

        public static SideWire Side(SideSnapshot side, bool own)
        {
            return new SideWire
            {
                Hp = side.Hp,
                Passed = side.Passed,
                Hand = own ? Cards(side.Hand) : null,
                HandCount = side.HandCount,
                DeckCount = side.DeckCount,
                GraveyardCount = side.GraveyardCount,
                MustPlay = Cards(side.MustPlay),
                Rows = new RowsWire
                {
                    Melee = Cards(side.MeleeRow),
                    Ranged = Cards(side.RangedRow),
                },
                Power = new PowerWire
                {
                    Melee = side.MeleePower,
                    Ranged = side.RangedPower,
                    Total = side.TotalPower,
                },
            };
        }

        public static MatchStateWire State(MatchSnapshot snapshot, string matchId)
        {
            return new MatchStateWire
            {
                MatchId = matchId,
                Phase = Phase(snapshot.Phase),
                Round = snapshot.Round,
                IsMyTurn = snapshot.IsMyTurn,
                Limits = new LimitsWire
                {
                    MaxHand = snapshot.Limits.MaxHand,
                    StartHp = snapshot.Limits.StartHp,
                    SlotsPerRow = snapshot.Limits.SlotsPerRow,
                },
                Redraw = snapshot.Redraw == null
                    ? null
                    : new RedrawWire
                    {
                        Left = snapshot.Redraw.Left,
                        IAmReady = snapshot.Redraw.IAmReady,
                        EnemyReady = snapshot.Redraw.EnemyReady,
                    },
                You = Side(snapshot.You, true),
                Enemy = Side(snapshot.Enemy, false),
            };
        }

        public static string Phase(MatchPhase phase)
        {
            return phase switch
            {
                MatchPhase.Redraw => "redraw",
                MatchPhase.Ended => "ended",
                _ => "play"
            };
        }

        public static string Result(RoundResult result)
        {
            return result switch
            {
                RoundResult.Win => "win",
                RoundResult.Lose => "lose",
                RoundResult.Tie => "tie",
                _ => null
            };
        }

        public static string Reason(MatchEndReason reason)
        {
            return reason switch
            {
                MatchEndReason.Surrender => "surrender",
                MatchEndReason.Disconnect => "disconnect",
                _ => "normal"
            };
        }
    }
}
