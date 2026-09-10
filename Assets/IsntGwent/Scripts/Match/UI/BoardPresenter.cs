using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.Definitions;
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

        public BoardRowView ownMeleeRow;
        public BoardRowView ownRangedRow;
        public BoardRowView enemyMeleeRow;
        public BoardRowView enemyRangedRow;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;
        [Inject] private readonly AudioService _audio;

        private void Start()
        {
            ownMeleeRow.MarkAsBoardRow(true, RowType.Melee);
            ownRangedRow.MarkAsBoardRow(true, RowType.Ranged);
            enemyMeleeRow.MarkAsBoardRow(false, RowType.Melee);
            enemyRangedRow.MarkAsBoardRow(false, RowType.Ranged);

            BindRow(_matchState.OwnMeleeRow, ownMeleeRow);
            BindRow(_matchState.OwnRangedRow, ownRangedRow);
            BindRow(_matchState.EnemyMeleeRow, enemyMeleeRow);
            BindRow(_matchState.EnemyRangedRow, enemyRangedRow);

            BindWeather(_matchState.OwnMeleeWeather, ownMeleeRow);
            BindWeather(_matchState.OwnRangedWeather, ownRangedRow);
            BindWeather(_matchState.EnemyMeleeWeather, enemyMeleeRow);
            BindWeather(_matchState.EnemyRangedWeather, enemyRangedRow);
        }

        private void BindWeather(RowWeatherState state, BoardRowView row)
        {
            state.CardId
                .Subscribe(row.ShowWeather)
                .AddTo(this);
        }

        private void BindRow(BoardRowState state, BoardRowView row)
        {
            state.Placed
                .Subscribe(e =>
                {
                    var view = _registry.Get(e.Card.Id);
                    var summoned = view == null;

                    if (summoned)
                    {
                        view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab);
                        view.Setup(e.Card);
                        view.transform.position = row.transform.position;
                        _registry.Register(view);
                    }

                    PlayPlaceSfx(view, e.Card, summoned);
                    view.mode = CardMode.OnBoard;
                    view.gameObject.SetActive(true);
                    view.Group().blocksRaycasts = true;
                    row.PlaceCard(view.gameObject, e.Index);
                })
                .AddTo(this);
        }

        private void PlayPlaceSfx(CardView view, CardInstance card, bool summoned)
        {
            if (_matchState.IsRestoring) return;

            if (summoned)
            {
                var id = card?.Definition?.SoundId;
                _audio.Play(string.IsNullOrEmpty(id) ? "card_play" : id);
                return;
            }

            if (view.mode != CardMode.OnBoard) return;

            _audio.Play("card_move");
        }
    }
}
