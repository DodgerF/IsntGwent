using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class BoardPresenter : MonoBehaviour
    {
        public GameObject cardPrefab;

        public RowView ownMeleeRow;
        public RowView ownRangedRow;
        public RowView enemyMeleeRow;
        public RowView enemyRangedRow;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;

        private void Start()
        {
            BindOwnRow(_matchState.OwnMeleeRow, ownMeleeRow);
            BindOwnRow(_matchState.OwnRangedRow, ownRangedRow);
            BindEnemyRow(_matchState.EnemyMeleeRow, enemyMeleeRow);
            BindEnemyRow(_matchState.EnemyRangedRow, enemyRangedRow);
        }

        private void BindOwnRow(ReactiveCollection<CardInstance> collection, RowView row)
        {
            collection
                .ObserveAdd()
                .Subscribe(e =>
                {
                    var view = _registry.Get(e.Value.Id);
                    if (view == null) return;

                    view.mode = CardMode.OnBoard;
                    row.AddCard(view.gameObject);
                })
                .AddTo(this);
        }

        private void BindEnemyRow(ReactiveCollection<CardInstance> collection, RowView row)
        {
            collection
                .ObserveAdd()
                .Subscribe(e =>
                {
                    var view = _registry.Get(e.Value.Id);

                    if (view == null)
                    {
                        view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab);
                        view.Setup(e.Value);
                        view.transform.position = row.transform.position;
                        _registry.Register(view);
                    }

                    view.mode = CardMode.OnBoard;
                    row.AddCard(view.gameObject);
                })
                .AddTo(this);
        }
    }
}
