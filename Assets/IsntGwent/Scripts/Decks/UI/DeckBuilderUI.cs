using IsntGwent.Scripts.Localization;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    public class DeckBuilderUI : MonoBehaviour
    {
        private const string UnsavedMessage = "Unsaved changes will be lost. Continue?";

        public TMP_InputField nameInput;
        public TextMeshProUGUI sizeText;
        public TextMeshProUGUI problemText;
        public Button saveButton;
        public Button finishButton;
        public Button backButton;

        [Inject] private readonly DeckDraft _draft;
        [Inject] private readonly DeckValidator _validator;
        [Inject] private readonly DeckRulesProvider _rules;
        [Inject] private readonly UserDeckStore _store;
        [Inject] private readonly DeckSelectService _deckSelect;
        [Inject] private readonly SceneService _scenes;
        [Inject] private readonly ConfirmWindow _confirm;

        private void Start()
        {
            _draft.Name
                .Subscribe(name =>
                {
                    if (nameInput != null && nameInput.text != name)
                        nameInput.text = name;
                })
                .AddTo(this);

            if (nameInput != null)
            {
                nameInput.onValueChanged
                    .AsObservable()
                    .Subscribe(value => _draft.Name.Value = value)
                    .AddTo(this);
            }

            _draft.Changed
                .Subscribe(_ => Refresh())
                .AddTo(this);

            _rules.OnLoaded
                .Subscribe(_ => Refresh())
                .AddTo(this);

            saveButton.OnClickAsObservable()
                .Subscribe(_ => Save())
                .AddTo(this);

            if (finishButton != null)
            {
                finishButton.OnClickAsObservable()
                    .Subscribe(_ => Finish())
                    .AddTo(this);
            }

            backButton.OnClickAsObservable()
                .Subscribe(_ => Back())
                .AddTo(this);
        }

        private void Finish()
        {
            if (_draft.IsDirty)
                _confirm.Ask(Loc.T(UnsavedMessage), () => _draft.NewDeck());
            else
                _draft.NewDeck();
        }

        private void Back()
        {
            if (_draft.IsDirty)
                _confirm.Ask(Loc.T(UnsavedMessage), () => _scenes.LoadMenu());
            else
                _scenes.LoadMenu();
        }

        private void Refresh()
        {
            var violations = _validator.Validate(_draft.Build());

            if (sizeText != null)
                sizeText.text = Loc.F("{0} / min {1}", _draft.TotalCount.Value, _rules.Current.MinDeckSize);

            saveButton.interactable = _draft.TotalCount.Value > 0;

            if (problemText != null)
                problemText.text = DeckViolationText.DescribeFirst(violations);
        }

        private void Save()
        {
            var deck = _draft.Build();
            if (deck.Cards.Length == 0) return;

            _store.Save(deck);

            if (_validator.IsValid(deck))
                _deckSelect.SelectedDeck.Value = deck;

            _draft.NewDeck();
        }
    }
}
