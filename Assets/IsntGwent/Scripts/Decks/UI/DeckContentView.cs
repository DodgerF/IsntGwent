using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.UI;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    public class DeckContentView : MonoBehaviour
    {
        public CardLaneView[] lanes;
        public CardTileView stackPrefab;

        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly DeckDraft _draft;
        [Inject] private readonly AudioService _audio;
        [Inject] private readonly CardTooltipView _tooltip;
        [Inject] private readonly DiContainer _container;
        [InjectOptional] private readonly CollectionView _collection;

        private readonly Dictionary<string, CardTileView> _stacks = new();
        private readonly Dictionary<string, (CardLaneView Lane, int Position)> _placement = new();

        private RectTransform _spawnOrigin;
        private bool _ready;

        public void SpawnFrom(RectTransform origin)
        {
            Init();
            _spawnOrigin = origin;
        }

        private void OnRectTransformDimensionsChange()
        {
            DeckLaneLayout.Apply((RectTransform)transform, lanes);
        }

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            if (_ready) return;
            _ready = true;

            DeckLaneLayout.Apply((RectTransform)transform, lanes);

            foreach (var pair in _draft.Cards)
                CreateStack(pair.Key, pair.Value);

            _draft.Cards.ObserveAdd()
                .Subscribe(added => CreateStack(added.Key, added.Value))
                .AddTo(this);

            _draft.Cards.ObserveReplace()
                .Subscribe(replaced => UpdateStack(replaced.Key, replaced.OldValue, replaced.NewValue))
                .AddTo(this);

            _draft.Cards.ObserveRemove()
                .Subscribe(removed => DestroyStack(removed.Key))
                .AddTo(this);

            _draft.Cards.ObserveReset()
                .Subscribe(_ => Clear())
                .AddTo(this);
        }

        private void CreateStack(string cardId, int count)
        {
            if (_stacks.ContainsKey(cardId))
            {
                UpdateStack(cardId, count - 1, count);
                return;
            }

            if (!_cardDatabase.Cards.TryGetValue(cardId, out var definition)) return;

            var stack = _container.InstantiatePrefabForComponent<CardTileView>(stackPrefab);
            stack.Setup(definition);
            stack.SetCount(count);

            var origin = OriginFor(cardId);
            PlaceAtOrigin(stack, origin);

            stack.Clicked
                .Subscribe(_ => Remove(cardId))
                .AddTo(stack);

            stack.HoldStarted
                .Subscribe(_ => _tooltip.Show(stack.Definition, (RectTransform)stack.transform))
                .AddTo(stack);

            stack.HoldEnded
                .Subscribe(_ => _tooltip.Hide())
                .AddTo(stack);

            _stacks[cardId] = stack;
            Reflow();

            if (_spawnOrigin == null)
                ScaleBetween(stack.transform, origin, (RectTransform)stack.transform);
        }

        private void PlaceAtOrigin(CardTileView stack, RectTransform origin)
        {
            if (origin == null) return;

            stack.transform.SetParent(origin, false);
            stack.transform.position = origin.position;
        }

        private RectTransform OriginFor(string cardId)
            => _spawnOrigin != null ? _spawnOrigin : _collection?.TileOf(cardId);

        private void UpdateStack(string cardId, int oldCount, int newCount)
        {
            if (!_stacks.TryGetValue(cardId, out var stack)) return;

            if (newCount > oldCount && FlyCopyIn(stack, cardId)) return;

            stack.SetCount(newCount);

            if (newCount < oldCount)
                FlyCopyOut(stack, cardId);
        }

        private bool FlyCopyIn(CardTileView stack, string cardId)
        {
            var origin = OriginFor(cardId);
            if (origin == null) return false;

            var copy = CreateFlyingCopy(cardId, origin.position);
            if (copy == null) return false;

            Fly(copy.transform, origin, (RectTransform)stack.transform, () =>
            {
                if (_stacks.TryGetValue(cardId, out var current))
                    current.SetCount(_draft.CountOf(cardId));
            });

            return true;
        }

        private void FlyCopyOut(CardTileView stack, string cardId)
        {
            var target = _collection?.TileOf(cardId);
            if (target == null) return;

            var copy = CreateFlyingCopy(cardId, stack.transform.position);
            if (copy == null) return;

            _collection.HoldIncoming(cardId);

            Fly(copy.transform, (RectTransform)stack.transform, target, () => _collection.ReleaseIncoming(cardId));
        }

        private CardTileView CreateFlyingCopy(string cardId, Vector3 worldPosition)
        {
            if (!_cardDatabase.Cards.TryGetValue(cardId, out var definition)) return null;

            var copy = _container.InstantiatePrefabForComponent<CardTileView>(stackPrefab, transform);
            copy.Setup(definition, false);
            copy.SetCount(1);

            Detach(copy, worldPosition);

            return copy;
        }

        private void Detach(CardTileView card, Vector3 worldPosition)
        {
            if (card.canvasGroup != null)
                card.canvasGroup.blocksRaycasts = false;

            card.cardView.hoverSfx = false;
            card.transform.SetParent(transform, false);
            card.transform.SetAsLastSibling();
            card.transform.position = worldPosition;
        }

        private void Fly(Transform card, RectTransform origin, RectTransform target, Action onDone)
        {
            card.DOKill();

            ScaleBetween(card, origin, target);

            card
                .DOMove(target.position, CardAnimConfig.PlayFlightDuration)
                .SetEase(CardAnimConfig.FlightEase)
                .OnComplete(() =>
                {
                    Destroy(card.gameObject);
                    onDone?.Invoke();
                });
        }

        private static void ScaleBetween(Transform card, RectTransform origin, RectTransform target)
        {
            var from = FitScale(card, origin);
            var to = FitScale(card, target);

            if (Mathf.Approximately(from, to)) return;

            var baseScale = card.localScale;

            card.localScale = baseScale * from;
            card
                .DOScale(baseScale * to, CardAnimConfig.PlayFlightDuration)
                .SetEase(CardAnimConfig.FlightEase);
        }

        private static float FitScale(Transform card, RectTransform frame)
        {
            if (frame == null) return 1f;

            var size = ((RectTransform)card).rect.size;
            if (size.x <= 0f || size.y <= 0f) return 1f;

            var frameSize = frame.rect.size;
            if (frameSize.x <= 0f || frameSize.y <= 0f) return 1f;

            return Mathf.Min(frameSize.x / size.x, frameSize.y / size.y);
        }

        private void DestroyStack(string cardId)
        {
            if (!_stacks.TryGetValue(cardId, out var stack)) return;

            _stacks.Remove(cardId);
            _placement.Remove(cardId);
            _tooltip.Hide();

            var lane = stack.transform.parent != null
                ? stack.transform.parent.GetComponent<CardLaneView>()
                : null;

            var target = _collection?.TileOf(cardId);

            if (lane != null && target != null)
                FlyStackOut(stack, cardId, lane, target);
            else if (lane != null)
                lane.RemoveCard(stack.gameObject);
            else
                Destroy(stack.gameObject);

            Reflow();
        }

        private void FlyStackOut(CardTileView stack, string cardId, CardLaneView lane, RectTransform target)
        {
            var from = stack.transform.position;

            lane.DetachCard(stack.gameObject);
            Detach(stack, from);

            if (stack.holdButton != null)
                stack.holdButton.enabled = false;

            _collection.HoldIncoming(cardId);

            Fly(stack.transform, (RectTransform)stack.transform, target, () => _collection.ReleaseIncoming(cardId));
        }

        private void Reflow()
        {
            if (lanes == null || lanes.Length == 0) return;

            var ordered = _stacks
                .OrderBy(pair => pair.Value.Definition, DeckCardOrder.Comparer)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var cardId = ordered[i].Key;
                var stack = ordered[i].Value;

                var lane = lanes[DeckCardOrder.LaneOf(i, DeckLaneLayout.LaneCapacity, lanes.Length)];
                var position = DeckCardOrder.PositionInLane(i, DeckLaneLayout.LaneCapacity, lanes.Length);

                if (_placement.TryGetValue(cardId, out var current)
                    && current.Lane == lane
                    && current.Position == position
                    && stack.transform.parent == lane.transform)
                    continue;

                lane.AddCardAt(stack.gameObject, position);
                _placement[cardId] = (lane, position);
            }
        }

        private void Clear()
        {
            var hadCards = _stacks.Count > 0;

            _tooltip.Hide();

            if (lanes != null)
                foreach (var lane in lanes)
                    lane.ClearCards();

            _stacks.Clear();
            _placement.Clear();

            if (hadCards)
                _audio.Play("card_draw");
        }

        private void Remove(string cardId)
        {
            if (_draft.Remove(cardId))
                _audio.Play("deck_card_remove");
        }
    }
}
