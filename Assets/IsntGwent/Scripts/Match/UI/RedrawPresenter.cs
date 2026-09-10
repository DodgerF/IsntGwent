using IsntGwent.Scripts.Localization;
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
        public RedrawTrayView tray;
        public CardLaneView handRow;
        public RectTransform deckAnchor;
        public Button doneButton;
        public Button hideButton;
        public Button showButton;
        public TextMeshProUGUI counterText;
        public GameObject waitingLabel;

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;
        [Inject] private readonly InputRouter _inputRouter;

        private CanvasGroup _panelGroup;
        private bool _isOpen;
        private bool _isHidden;
        private bool _isClosing;
        private float _lastCardTime;
        private Tween _closeDelay;

        public bool IsPhaseActive => _isOpen && !_isClosing;

        public void AddCard(GameObject card)
        {
            _lastCardTime = Time.time;
            tray.AddCard(card);
        }

        private void Start()
        {
            _panelGroup = panel.GetComponent<CanvasGroup>();
            if (_panelGroup == null)
                _panelGroup = panel.AddComponent<CanvasGroup>();

            panel.SetActive(false);

            SetHidden(false);

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
                .Subscribe(left => counterText.text = Loc.F("Redraws left: {0}", left))
                .AddTo(this);

            _matchState.IsGameEnded
                .Where(ended => ended)
                .Subscribe(_ => Finish())
                .AddTo(this);

            _matchState.CardRedrawn
                .Subscribe(Discard)
                .AddTo(this);

            _matchState.IsPileWindowOpen
                .Subscribe(_ => RefreshShowButton())
                .AddTo(this);

            doneButton.OnClickAsObservable()
                .Subscribe(_ => _matchState.RedrawReadyRequested.OnNext(Unit.Default))
                .AddTo(this);

            if (hideButton != null)
                hideButton.OnClickAsObservable()
                    .Subscribe(_ => SetHidden(true))
                    .AddTo(this);

            if (showButton != null)
                showButton.OnClickAsObservable()
                    .Subscribe(_ => SetHidden(false))
                    .AddTo(this);

            _inputRouter.CardPressed
                .Where(_ => IsPhaseActive)
                .Where(_ => !_matchState.IsRedrawReady.Value)
                .Where(view => view != null && _matchState.Hand.Contains(view.Instance))
                .Subscribe(view => _matchState.RedrawRequested.OnNext(view.Instance))
                .AddTo(this);
        }

        private void Open()
        {
            CancelCloseDelay();

            if (_isOpen) return;

            _isOpen = true;
            _lastCardTime = Time.time;
            panel.SetActive(true);

            SetHidden(false);

            foreach (var card in new List<CardInstance>(_matchState.Hand))
            {
                var view = _registry.Get(card.Id);
                if (view == null) continue;

                tray.AddCard(view.gameObject);
            }
        }

        private void Close()
        {
            if (!_isOpen || _isClosing) return;

            var wait = _lastCardTime
                       + CardAnimConfig.PlayFlightDuration
                       + CardAnimConfig.RedrawCloseHold
                       - Time.time;

            if (!Application.isPlaying || wait <= 0f)
            {
                Finish();
                return;
            }

            _isClosing = true;
            _closeDelay = DOVirtual.DelayedCall(wait, Finish, false);
        }

        private void Finish()
        {
            CancelCloseDelay();

            if (!_isOpen) return;

            _isOpen = false;

            foreach (var card in _matchState.Hand)
            {
                var view = _registry.Get(card.Id);
                if (view == null) continue;

                tray.Release(view.gameObject);
                handRow.AddCard(view.gameObject);
            }

            tray.ReleaseAll();

            SetHidden(false);

            panel.SetActive(false);
        }

        private void SetHidden(bool hidden)
        {
            _isHidden = hidden;

            if (_panelGroup != null)
            {
                _panelGroup.alpha = hidden ? 0f : 1f;
                _panelGroup.blocksRaycasts = !hidden;
                _panelGroup.interactable = !hidden;
            }

            RefreshShowButton();
        }

        private void RefreshShowButton()
        {
            if (showButton == null) return;

            showButton.gameObject.SetActive(_isOpen && _isHidden && !_matchState.IsPileWindowOpen.Value);
        }

        private void CancelCloseDelay()
        {
            _isClosing = false;

            _closeDelay?.Kill();
            _closeDelay = null;
        }

        private void OnDestroy() => CancelCloseDelay();

        private void Discard(CardInstance card)
        {
            var view = _registry.Get(card.Id);
            if (view == null) return;

            _registry.Remove(card.Id);

            var moved = view.transform;
            var start = moved.position;

            tray.Release(view.gameObject);

            moved.SetParent(panel.transform, false);
            moved.position = start;

            view.SetBaseScale(1f, CardAnimConfig.RedrawDiscardDuration, CardAnimConfig.FlightEase);
            moved.DOMove(deckAnchor.position, CardAnimConfig.RedrawDiscardDuration)
                .SetEase(CardAnimConfig.FlightEase)
                .OnComplete(() => Destroy(view.gameObject));
        }
    }
}
