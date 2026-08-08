using System.Collections.Generic;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Lobby.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class MenuUI : MonoBehaviour
    {
        [Inject] private LobbyViewModel _vm;
        [Inject] private DiContainer _container;
        [Inject] private SceneService _scenes;
        [Inject] private DeckSelectService _deckSelect;
        [Inject] private DeckDatabase _deckDatabase;
        [SerializeField] private Button createButton;
        [SerializeField] private Button deckBuilderButton;
        [SerializeField] private Transform parent;
        [SerializeField] private LobbyEntryView prefab;
        
        private readonly Dictionary<string, GameObject> _lobbyEntries = new();

        private void Start()
        {
            foreach (var lobby in _vm.Lobbies)
                CreateEntry(lobby);
            
            _vm.Lobbies.ObserveAdd()
                .Subscribe(addEvent => CreateEntry(addEvent.Value))
                .AddTo(this);
            _vm.Lobbies.ObserveRemove()
                .Subscribe(removeEvent => RemoveEntry(removeEvent.Value))
                .AddTo(this);
            _vm.CanCreateOrJoinLobby
                .Subscribe(canCreate =>  
                {  
                    createButton.interactable = canCreate;
                })  
                .AddTo(this);
            
            foreach (var lobby in _vm.Lobbies)
            {
                CreateEntry(lobby);
            }
            
            createButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _vm.IsCreateLobbyWindowOpen.Value = true;
                })
                .AddTo(this);

            if (deckBuilderButton != null)
            {
                deckBuilderButton.OnClickAsObservable()
                    .Subscribe(_ => EditSelectedDeck())
                    .AddTo(this);
            }
        }

        private void EditSelectedDeck()
        {
            var deck = _deckSelect.SelectedDeck.Value;
            _deckSelect.RequestEdit(deck, deck != null && _deckDatabase.Contains(deck.Id));
            _scenes.LoadDeckBuilder();
        }

        private void RemoveEntry(LobbyData data)
        {
            if (_lobbyEntries.Remove(data.LobbyId, out var entry))
            {
                Destroy(entry);
            }
        }

        private void CreateEntry(LobbyData data)
        {
            if (_lobbyEntries.ContainsKey(data.LobbyId)) return;
            
            var instance = _container.InstantiatePrefabForComponent<LobbyEntryView>(prefab, parent);
            instance.Setup(data);
            
            _lobbyEntries.Add(data.LobbyId, instance.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
        }
    }
}