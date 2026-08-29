using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Match.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public enum PileKind
    {
        OwnDeck,
        OwnGraveyard,
        EnemyGraveyard
    }

    public class PileWindowView : MonoBehaviour
    {
        public GameObject window;
        public CardTileView cardPrefab;
        public Transform content;
        public TextMeshProUGUI title;
        public ScrollRect scroll;

        [SerializeField] private Button dimmer;

        [SerializeField] private string ownDeckTitle = "Deck";
        [SerializeField] private string ownGraveyardTitle = "Graveyard";
        [SerializeField] private string enemyGraveyardTitle = "Opponent's Graveyard";

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;

        private readonly List<GameObject> _tiles = new();
        private readonly SerialDisposable _watch = new();

        private bool _isOpen;
        private PileKind _kind;

        private void Start()
        {
            _watch.AddTo(this);

            if (dimmer != null)
                dimmer.onClick.AddListener(Close);

            _matchState.IsGameEnded
                .Where(ended => ended)
                .Subscribe(_ => Close())
                .AddTo(this);

            window.SetActive(false);
            _matchState.IsPileWindowOpen.Value = false;
        }

        public void Open(PileKind kind)
        {
            if (_isOpen && _kind == kind)
            {
                Close();
                return;
            }

            if (Source(kind).Count == 0) return;

            _kind = kind;
            _isOpen = true;

            window.SetActive(true);
            _matchState.IsPileWindowOpen.Value = true;

            _watch.Disposable = Source(kind)
                .ObserveCountChanged()
                .BatchFrame()
                .Subscribe(_ => Refresh());

            Rebuild();

            if (scroll == null) return;

            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }

        public void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _watch.Disposable = null;

            ClearTiles();

            window.SetActive(false);
            _matchState.IsPileWindowOpen.Value = false;
        }

        private void Refresh()
        {
            if (Source(_kind).Count == 0)
            {
                Close();
                return;
            }

            Rebuild();
        }

        private void Rebuild()
        {
            ClearTiles();

            var cards = Source(_kind);

            if (title != null)
                title.text = $"{TitleOf(_kind)} ({cards.Count})";

            foreach (var card in cards)
            {
                var tile = _container.InstantiatePrefabForComponent<CardTileView>(cardPrefab, content);

                tile.Setup(card, interactive: false);
                tile.SetCount(1);

                if (tile.cardView != null)
                    tile.cardView.mode = CardMode.InGraveyard;

                _tiles.Add(tile.gameObject);
            }
        }

        private void ClearTiles()
        {
            foreach (var tile in _tiles)
            {
                if (tile != null)
                    Destroy(tile);
            }

            _tiles.Clear();
        }

        private ReactiveCollection<CardInstance> Source(PileKind kind) => kind switch
        {
            PileKind.OwnDeck => _matchState.OwnDeck,
            PileKind.OwnGraveyard => _matchState.OwnGraveyard,
            _ => _matchState.EnemyGraveyard,
        };

        private string TitleOf(PileKind kind) => kind switch
        {
            PileKind.OwnDeck => ownDeckTitle,
            PileKind.OwnGraveyard => ownGraveyardTitle,
            _ => enemyGraveyardTitle,
        };
    }
}
