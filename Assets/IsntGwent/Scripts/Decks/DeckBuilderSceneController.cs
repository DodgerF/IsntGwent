using System;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Network;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Decks
{
    public class DeckBuilderSceneController : IInitializable, IDisposable
    {
        [Inject] private readonly CardDatabase _cards;
        [Inject] private readonly DeckRulesProvider _rules;
        [Inject] private readonly DeckDraft _draft;
        [Inject] private readonly DeckSelectService _deckSelect;
        [Inject] private readonly ConnectionService _connection;

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _connection.SetAutoReconnect(true);

            Observable.CombineLatest(_cards.OnLoaded, _rules.OnLoaded)
                .Where(values => values[0] && values[1])
                .Take(1)
                .Subscribe(_ => LoadEditTarget())
                .AddTo(_disposables);
        }

        private void LoadEditTarget()
        {
            var deck = _deckSelect.ConsumeEditTarget(out var isBuiltIn);
            _draft.LoadFrom(deck, isBuiltIn);
        }

        public void Dispose()
        {
            _connection.SetAutoReconnect(false);
            _disposables.Dispose();
        }
    }
}
