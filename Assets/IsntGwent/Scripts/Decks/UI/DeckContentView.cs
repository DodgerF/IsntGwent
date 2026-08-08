using System.Collections.Generic;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.UI;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    public class DeckContentView : MonoBehaviour
    {
        public RowView meleeRow;
        public RowView rangedRow;
        public RowView spellRow;
        public DeckStackView stackPrefab;

        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly DeckDraft _draft;
        [Inject] private readonly AudioService _audio;
        [Inject] private readonly CardTooltipView _tooltip;
        [Inject] private readonly DiContainer _container;

        private readonly Dictionary<string, DeckStackView> _stacks = new();

        private void Start()
        {
            foreach (var pair in _draft.Cards)
                CreateStack(pair.Key, pair.Value);

            _draft.Cards.ObserveAdd()
                .Subscribe(added => CreateStack(added.Key, added.Value))
                .AddTo(this);

            _draft.Cards.ObserveReplace()
                .Subscribe(replaced => UpdateStack(replaced.Key, replaced.NewValue))
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
                UpdateStack(cardId, count);
                return;
            }

            if (!_cardDatabase.Cards.TryGetValue(cardId, out var definition)) return;

            var row = RowFor(definition);
            if (row == null) return;

            var stack = _container.InstantiatePrefabForComponent<DeckStackView>(stackPrefab);
            stack.Setup(definition, count);

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
            row.AddCard(stack.gameObject);
        }

        private void UpdateStack(string cardId, int count)
        {
            if (_stacks.TryGetValue(cardId, out var stack))
                stack.SetCount(count);
        }

        private void DestroyStack(string cardId)
        {
            if (!_stacks.TryGetValue(cardId, out var stack)) return;

            _stacks.Remove(cardId);
            _tooltip.Hide();

            var row = stack.transform.parent != null
                ? stack.transform.parent.GetComponent<RowView>()
                : null;

            if (row != null)
                row.RemoveCard(stack.gameObject);
            else
                Destroy(stack.gameObject);
        }

        private void Clear()
        {
            var hadCards = _stacks.Count > 0;

            _tooltip.Hide();
            meleeRow.ClearCards();
            rangedRow.ClearCards();
            spellRow.ClearCards();
            _stacks.Clear();

            if (hadCards)
                _audio.Play("card_draw");
        }

        private void Remove(string cardId)
        {
            if (_draft.Remove(cardId))
                _audio.Play("deck_card_remove");
        }

        private RowView RowFor(CardDefinition definition)
        {
            if (definition is not UnitDefinition unit) return spellRow;

            return unit.Row switch
            {
                RowType.Melee => meleeRow,
                RowType.Ranged => rangedRow,
                _ => spellRow
            };
        }
    }
}
