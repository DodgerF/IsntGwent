using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Lobby.Client;
using TMPro;
using UniRx;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class MenuUI : MonoBehaviour
    {
        private const string PlayLabel = "Play";
        private const string CancelLabel = "Cancel search";
        private const string SearchingLabel = "Looking for an opponent...";

        [Inject] private LobbyViewModel _vm;
        [Inject] private SceneService _scenes;
        [Inject] private DeckSelectService _deckSelect;
        [Inject] private DeckDatabase _deckDatabase;

        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI playButtonLabel;
        [SerializeField] private Button privateRoomButton;
        [SerializeField] private Button joinByCodeButton;
        [SerializeField] private Button deckBuilderButton;
        [SerializeField] private TextMeshProUGUI searchStatus;

        private void Start()
        {
            if (playButton == null)
            {
                Log.Error(LogTag.Client, "MenuUI: playButton is not assigned");
                return;
            }

            _vm.CanPlay
                .CombineLatest(_vm.IsSearching, (canPlay, isSearching) => canPlay || isSearching)
                .Subscribe(canPress => playButton.interactable = canPress)
                .AddTo(this);

            _vm.CanPlay
                .Subscribe(canPlay =>
                {
                    if (privateRoomButton != null) privateRoomButton.interactable = canPlay;
                    if (joinByCodeButton != null) joinByCodeButton.interactable = canPlay;
                })
                .AddTo(this);

            _vm.IsSearching
                .Subscribe(OnSearchingChanged)
                .AddTo(this);

            playButton.OnClickAsObservable()
                .Subscribe(_ => _vm.TogglePlay())
                .AddTo(this);

            if (privateRoomButton != null)
            {
                privateRoomButton.OnClickAsObservable()
                    .Subscribe(_ => _vm.CreatePrivateRoom())
                    .AddTo(this);
            }

            if (joinByCodeButton != null)
            {
                joinByCodeButton.OnClickAsObservable()
                    .Subscribe(_ => _vm.OpenJoinCodeWindow())
                    .AddTo(this);
            }

            if (deckBuilderButton != null)
            {
                deckBuilderButton.OnClickAsObservable()
                    .Subscribe(_ => EditSelectedDeck())
                    .AddTo(this);
            }
        }

        private void OnSearchingChanged(bool isSearching)
        {
            if (playButtonLabel != null)
                playButtonLabel.text = isSearching ? CancelLabel : PlayLabel;

            if (searchStatus == null) return;

            searchStatus.text = isSearching ? SearchingLabel : string.Empty;
            searchStatus.gameObject.SetActive(isSearching);
        }

        private void EditSelectedDeck()
        {
            var deck = _deckSelect.SelectedDeck.Value;
            _deckSelect.RequestEdit(deck, deck != null && _deckDatabase.Contains(deck.Id));
            _scenes.LoadDeckBuilder();
        }
    }
}
