using System.Collections.Generic;
using DG.Tweening;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Match.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class RedrawPresenter : MonoBehaviour
    {
        public GameObject panel;
        public RowView topRow;
        public RowView bottomRow;
        public RowView handRow;
        public RectTransform deckAnchor;
        public Button doneButton;
        public TextMeshProUGUI counterText;
        public GameObject waitingLabel;

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;
        [Inject] private readonly InputRouter _inputRouter;

        private bool _isOpen;

        public bool IsPhaseActive => _isOpen;

        public RowView NextRow() => bottomRow.CardCount < topRow.CardCount ? bottomRow : topRow;

        private void Start()
        {
            panel.SetActive(false);

            _matchState.IsRedrawPhase
                .Subscribe(active =>
                {
                    if (active) Open();
                    else Close();
                })
                .AddTo(this);

            _matchState.IsRedrawReady
                .Subscribe(ready =>
                {
                    doneButton.gameObject.SetActive(!ready);
                    waitingLabel.SetActive(ready);
                })
                .AddTo(this);

            _matchState.RedrawsLeft
                .Subscribe(left => counterText.text = $"Redraws left: {left}")
                .AddTo(this);

            _matchState.IsGameEnded
                .Where(ended => ended)
                .Subscribe(_ => Close())
                .AddTo(this);

            _matchState.CardRedrawn
                .Subscribe(Discard)
                .AddTo(this);

            doneButton.OnClickAsObservable()
                .Subscribe(_ => _matchState.RedrawReadyRequested.OnNext(Unit.Default))
                .AddTo(this);

            _inputRouter.CardPressed
                .Where(_ => _isOpen)
                .Where(_ => !_matchState.IsRedrawReady.Value)
                .Where(view => view != null && _matchState.Hand.Contains(view.Instance))
                .Subscribe(view => _matchState.RedrawRequested.OnNext(view.Instance))
                .AddTo(this);
        }

        private void Open()
        {
            if (_isOpen) return;

            _isOpen = true;
            panel.SetActive(true);

            var cards = new List<CardInstance>(_matchState.Hand);
            var half = (cards.Count + 1) / 2;

            for (var i = 0; i < cards.Count; i++)
            {
                var view = _registry.Get(cards[i].Id);
                if (view == null) continue;

                (i < half ? topRow : bottomRow).AddCard(view.gameObject);
            }
        }

        private void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;

            foreach (var card in _matchState.Hand)
            {
                var view = _registry.Get(card.Id);
                if (view == null) continue;

                handRow.AddCard(view.gameObject);
            }

            panel.SetActive(false);
        }

        private void Discard(CardInstance card)
        {
            var view = _registry.Get(card.Id);
            if (view == null) return;

            _registry.Remove(card.Id);

            var moved = view.transform;
            var start = moved.position;

            var row = moved.parent != null ? moved.parent.GetComponent<RowView>() : null;
            if (row != null)
                row.DetachCard(view.gameObject);

            moved.SetParent(panel.transform, false);
            moved.position = start;

            moved.DOMove(deckAnchor.position, CardAnimConfig.RedrawDiscardDuration)
                .SetEase(CardAnimConfig.FlightEase)
                .OnComplete(() => Destroy(view.gameObject));
        }
    }
}
