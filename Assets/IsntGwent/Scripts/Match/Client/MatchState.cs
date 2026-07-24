using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Messages;
using UniRx;

namespace IsntGwent.Scripts.Match.Client
{
    public class MatchState
    {
        public readonly ReactiveProperty<bool> IsWaitingImageActive = new(true);
        public readonly ReactiveProperty<bool> IsMyTurn = new(false);
        public readonly Subject<Unit> TurnChanged = new();
        public readonly Subject<Unit> GameStarted = new();

        public readonly ReactiveProperty<bool> IsActionPending = new(false);
        public readonly Subject<Unit> PassRequested = new();

        public readonly ReactiveCollection<CardInstance> Hand = new();
        public readonly ReactiveProperty<int> EnemyCardAmount = new(0);
        
        public readonly ReactiveCollection<CardInstance> OwnMeleeRow = new();
        public readonly ReactiveCollection<CardInstance> OwnRangedRow = new();
        public readonly ReactiveCollection<CardInstance> OwnGraveyard = new();
        
        public readonly ReactiveCollection<CardInstance> EnemyMeleeRow = new();
        public readonly ReactiveCollection<CardInstance> EnemyRangedRow = new();
        public readonly ReactiveCollection<CardInstance> EnemyGraveyard = new();
        
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
        public readonly ReactiveProperty<bool> AmIWinner = new(false);
        public readonly ReactiveProperty<bool> IsTie = new(false);
        
    }
}