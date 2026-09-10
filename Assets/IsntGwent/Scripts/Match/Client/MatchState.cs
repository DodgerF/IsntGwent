using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Messages;
using UniRx;

namespace IsntGwent.Scripts.Match.Client
{
    public class MatchState
    {
        public readonly ReactiveProperty<bool> IsWaitingImageActive = new(true);
        public readonly ReactiveProperty<bool> IsMyTurn = new(false);
        public readonly ReactiveProperty<bool> IsEnemyPassed = new(false);
        public readonly Subject<Unit> TurnChanged = new();
        public readonly Subject<Unit> GameStarted = new();

        public readonly ReactiveProperty<bool> IsActionPending = new(false);
        public readonly Subject<Unit> ActionTimedOut = new();
        public readonly Subject<Unit> PassRequested = new();
        public readonly Subject<Unit> PassSent = new();

        public readonly ReactiveProperty<bool> IsRedrawPhase = new(false);
        public readonly ReactiveProperty<int> RedrawsLeft = new(0);
        public readonly ReactiveProperty<bool> IsRedrawReady = new(false);
        public readonly Subject<CardInstance> RedrawRequested = new();
        public readonly Subject<Unit> RedrawReadyRequested = new();
        public readonly Subject<CardInstance> CardRedrawn = new();

        public readonly Subject<DamageInstance[]> DamageDealt = new();
        public readonly Subject<UnitLinkData[]> UnitsLinked = new();
        public readonly Subject<CardInstance> CardStaged = new();
        public readonly Subject<Unit> CardDrawn = new();
        public readonly Subject<Unit> EnemyCardDrawn = new();
        public readonly Subject<int> UnitsDied = new();
        public readonly Subject<int> HpLost = new();
        public readonly Subject<int> GraveyardPurged = new();

        public bool IsRestoring;

        public readonly ReactiveCollection<CardInstance> Hand = new();
        public readonly ReactiveCollection<CardInstance> PendingPlays = new();
        public readonly ReactiveProperty<bool> IsPendingMine = new(false);
        public readonly Subject<CardInstance> PendingPlayGranted = new();
        public readonly ReactiveProperty<int> EnemyCardAmount = new(0);
        public readonly ReactiveCollection<CardInstance> OwnDeck = new();
        public readonly ReactiveProperty<int> EnemyDeckCount = new(0);
        public readonly ReactiveProperty<bool> IsPileWindowOpen = new(false);
        
        public readonly BoardRowState OwnMeleeRow = new();
        public readonly BoardRowState OwnRangedRow = new();
        public readonly ReactiveCollection<CardInstance> OwnGraveyard = new();

        public readonly BoardRowState EnemyMeleeRow = new();
        public readonly BoardRowState EnemyRangedRow = new();
        public readonly ReactiveCollection<CardInstance> EnemyGraveyard = new();

        public readonly RowWeatherState OwnMeleeWeather = new();
        public readonly RowWeatherState OwnRangedWeather = new();
        public readonly RowWeatherState EnemyMeleeWeather = new();
        public readonly RowWeatherState EnemyRangedWeather = new();
        
        public readonly ReactiveProperty<int> OwnMeleePower = new(0);
        public readonly ReactiveProperty<int> OwnRangedPower = new(0);
        public readonly ReactiveProperty<int> OwnTotalPower = new(0);
    
        public readonly ReactiveProperty<int> EnemyMeleePower = new(0);
        public readonly ReactiveProperty<int> EnemyRangedPower = new(0);
        public readonly ReactiveProperty<int> EnemyTotalPower = new(0);
        
        public readonly ReactiveProperty<int> MyHp = new(2);
        public readonly ReactiveProperty<int> EnemyHp = new(2);
        
        public readonly ReactiveProperty<RoundResult> LastRoundResult = new(RoundResult.None);
        public readonly ReactiveProperty<bool> IsGameEnded = new(false);
        public readonly ReactiveProperty<bool> AmIGiveUp = new(false);
        public readonly ReactiveProperty<bool> IsEnemyGiveUp = new(false);
        public readonly ReactiveProperty<bool> IsEnemyLeft = new(false);
        public readonly ReactiveProperty<bool> IsConnectionLost = new(false);
        public readonly ReactiveProperty<bool> IsOpponentReconnecting = new(false);
        public readonly ReactiveProperty<bool> IsSelfReconnecting = new(false);
        public readonly ReactiveProperty<bool> IsMatchPaused = new(false);
        public readonly ReactiveProperty<bool> AmIWinner = new(false);
        public readonly ReactiveProperty<bool> IsTie = new(false);
        
    }
}