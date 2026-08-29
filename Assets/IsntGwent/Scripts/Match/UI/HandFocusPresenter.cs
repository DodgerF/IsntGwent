using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class HandFocusPresenter : MonoBehaviour
    {
        public CardLaneView hand;
        public HandFocusZone handZone;
        public HandFocusZone dimmerZone;
        public GameObject dimmer;

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardSelectionService _selection;
        [Inject] private readonly AudioService _audio;

        private void Start()
        {
            if (dimmer != null) dimmer.SetActive(false);

            if (handZone != null)
                handZone.Clicked += () => SetFocused(!hand.IsFocused);

            if (dimmerZone != null)
                dimmerZone.Clicked += () => SetFocused(false);

            _selection.IsChoosing
                .Where(choosing => choosing)
                .Subscribe(_ => SetFocused(false))
                .AddTo(this);

            _matchState.IsMyTurn
                .Where(myTurn => !myTurn)
                .Subscribe(_ => SetFocused(false))
                .AddTo(this);

            _matchState.IsPileWindowOpen
                .Where(open => open)
                .Subscribe(_ => SetFocused(false))
                .AddTo(this);
        }

        private void SetFocused(bool focused)
        {
            if (hand == null || hand.IsFocused == focused) return;
            if (focused && (!_matchState.IsMyTurn.Value || hand.CardCount == 0)) return;

            hand.SetFocused(focused);

            if (dimmer != null) dimmer.SetActive(focused);

            _audio?.Play("ui_click");
        }
    }
}
