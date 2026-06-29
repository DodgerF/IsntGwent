using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Services;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Messages;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class MatchUI : MonoBehaviour
    {
        public GameObject cardPrefab;
        public GameObject waitingImage;
        public GameObject ui;
        
        public Button passButton;
        
        public TextMeshProUGUI enemyCardCounter;
        public TextMeshProUGUI ownCardCounter;
        
        public TextMeshProUGUI ownMeleePowerText;
        public TextMeshProUGUI ownRangedPowerText;
        public TextMeshProUGUI ownTotalPowerText;

        public TextMeshProUGUI enemyMeleePowerText;
        public TextMeshProUGUI enemyRangedPowerText;
        public TextMeshProUGUI enemyTotalPowerText;

        public GameObject turnTracker;
        
        public RowView ownMeleeRow;
        public RowView ownRangedRow;
        public RowView enemyMeleeRow;
        public RowView enemyRangedRow;

        public RowView hand;
        
        public GameObject roundEndedPanel;
        public TextMeshProUGUI roundResultText;
        public GameObject gameEndedPanel;
        public TextMeshProUGUI gameEndedText;
        
        public GraveyardView ownGraveyard;
        public GraveyardView enemyGraveyard;

        public PlayerHpUI ownHp;
        public PlayerHpUI enemyHp;
        
        [Inject] private readonly MatchViewModel _vm;
        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardSelectionService _selectionService;
        
        private readonly Dictionary<Guid, GameObject> _cardViews = new();
        

        private void Start()
        {
            _vm.IsWaitingImageActive
                .Subscribe(value =>
                {
                    waitingImage.SetActive(value);
                    ui.SetActive(!value);
                })
                .AddTo(this);
            
            BindHand(_matchState.Hand, hand);
            BindOwnRow(_matchState.OwnMeleeRow, ownMeleeRow);
            BindOwnRow(_matchState.OwnRangedRow, ownRangedRow);
            BindEnemyRow(_matchState.EnemyMeleeRow, enemyMeleeRow);
            BindEnemyRow(_matchState.EnemyRangedRow, enemyRangedRow);
            
            _matchState.IsMyTurn
                .Subscribe(value => turnTracker.SetActive(value))
                .AddTo(this);
            
            _matchState.OwnMeleePower
                .Subscribe(v => ownMeleePowerText.text = v.ToString())
                .AddTo(this);
            _matchState.OwnRangedPower
                .Subscribe(v => ownRangedPowerText.text = v.ToString())
                .AddTo(this);
            _matchState.OwnTotalPower
                .Subscribe(v => ownTotalPowerText.text = v.ToString())
                .AddTo(this);

            _matchState.EnemyMeleePower
                .Subscribe(v => enemyMeleePowerText.text = v.ToString())
                .AddTo(this);
            _matchState.EnemyRangedPower
                .Subscribe(v => enemyRangedPowerText.text = v.ToString())
                .AddTo(this);
            _matchState.EnemyTotalPower
                .Subscribe(v => enemyTotalPowerText.text = v.ToString())
                .AddTo(this);
            
            _matchState.EnemyCardAmount
                .Subscribe(value => enemyCardCounter.text = value.ToString())
                .AddTo(this);
            
            passButton.OnClickAsObservable()
                .Subscribe(_ => OnPass())
                .AddTo(this);

            _matchState.MyHp
                .Subscribe(value =>
                {
                    ownHp.SetHp(value);
                })
                .AddTo(this);
            _matchState.EnemyHp
                .Subscribe(value =>
                {
                    enemyHp.SetHp(value);
                })
                .AddTo(this);
            
            _matchState.OwnGraveyard
                .ObserveAdd()
                .Subscribe(e =>
                {
                    _cardViews.TryGetValue(e.Value.Id, out var go);
        
                    if (go != null)
                    {
                        var currentRow = go.transform.parent?.GetComponent<RowView>();
                        currentRow?.DetachCard(go);
                    }
        
                    ownGraveyard.AddCard(e.Value, go);
                })
                .AddTo(this);

            _matchState.EnemyGraveyard
                .ObserveAdd()
                .Subscribe(e =>
                {
                    _cardViews.TryGetValue(e.Value.Id, out var go);
        
                    if (go != null)
                    {
                        var currentRow = go.transform.parent?.GetComponent<RowView>();
                        currentRow?.DetachCard(go);
                    }
        
                    enemyGraveyard.AddCard(e.Value, go);
                })
                .AddTo(this);
            
            _matchState.LastRoundResult
                .Subscribe(result =>
                {
                    var text = result switch
                    {
                        RoundResult.Win => "Раунд выигран",
                        RoundResult.Lose => "Раунд проигран",
                        RoundResult.Tie => "Ничья в раунде",
                        _ => ""
                    };
                    Debug.Log(text);
                    // roundEndedPanel.SetActive(true);
                    // roundResultText.text = result switch
                    // {
                    //     RoundResult.Win => "Раунд выигран",
                    //     RoundResult.Lose => "Раунд проигран",
                    //     RoundResult.Tie => "Ничья в раунде",
                    //     _ => ""
                    // };
                })
                .AddTo(this);
            
            _matchState.IsGameEnded
                .Where(v => v)
                .Subscribe(_ =>
                {
                    var text = _matchState.IsTie.Value ? "Ничья" : (_matchState.AmIWinner.Value
                        ? "Победа"
                        : "Поражение");
                    Debug.Log(text);
                    // gameEndedPanel.SetActive(true);
                    // gameEndedText.text = _matchState.IsTie.Value ? "Ничья" 
                    //     : _matchState.AmIWinner.Value ? "Победа" 
                    //     : "Поражение";
                })
                .AddTo(this);
            
            _selectionService.HighlightTargets
                .Subscribe(pool =>
                {
                    foreach (var kvp in _cardViews)
                    {
                        var view = kvp.Value.GetComponent<CardView>();
                        var state = pool.Contains(kvp.Key.ToString())
                            ? CardView.TargetHighlightState.Available
                            : CardView.TargetHighlightState.None;
                        view.SetTargetHighlight(state);
                    }
                })
                .AddTo(this);

            _selectionService.TargetSelected
                .Subscribe(id =>
                {
                    var card = _cardViews.FirstOrDefault(kvp => kvp.Key.ToString() == id).Value;
                    card?.GetComponent<CardView>().SetTargetHighlight(CardView.TargetHighlightState.Selected);
                })
                .AddTo(this);

            _selectionService.TargetDeselected
                .Subscribe(id =>
                {
                    var card = _cardViews.FirstOrDefault(kvp => kvp.Key.ToString() == id).Value;
                    card?.GetComponent<CardView>().SetTargetHighlight(CardView.TargetHighlightState.Available);
                })
                .AddTo(this);

            _selectionService.ClearHighlights
                .Subscribe(_ =>
                {
                    foreach (var kvp in _cardViews)
                        kvp.Value.GetComponent<CardView>().SetTargetHighlight(CardView.TargetHighlightState.None);
                })
                .AddTo(this);
            
            _vm.Ready();
        }

        public void OnPass()
        {
            _vm.Pass();
        }
        
        private void BindHand(ReactiveCollection<CardInstance> collection, RowView row)
        {
            collection
                .ObserveAdd()
                .Subscribe(e =>
                {
                    var view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab, row.transform);
                    view.Setup(e.Value);
                    _cardViews[e.Value.Id] = view.gameObject;
                    row.AddCard(view.gameObject);
                })
                .AddTo(this);
            
            collection
                .ObserveCountChanged()
                .Subscribe(e =>
                {
                    ownCardCounter.text = e.ToString();
                })
                .AddTo(this);
        }
        
        private void BindOwnRow(ReactiveCollection<CardInstance> collection, RowView row)
        {
            collection
                .ObserveAdd()
                .Subscribe(e =>
                {
                    _cardViews.TryGetValue(e.Value.Id, out var go);
                    if (go != null)
                    {
                        go.GetComponent<CardView>().mode = CardMode.OnBoard;
                        row.AddCard(go);
                        hand.RefreshLayout();
                    }
                })
                .AddTo(this);
        }
        private void BindEnemyRow(ReactiveCollection<CardInstance> collection, RowView row)
        {
            collection
                .ObserveAdd()
                .Subscribe(e =>
                {
                    var view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab);
                    view.Setup(e.Value);
                    view.mode = CardMode.OnBoard;
                    view.transform.position = Vector3.zero;
                    _cardViews[e.Value.Id] = view.gameObject;
                    row.AddCard(view.gameObject);
                })
                .AddTo(this);
        }
       
    }
}