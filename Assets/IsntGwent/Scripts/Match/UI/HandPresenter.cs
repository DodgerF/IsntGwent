using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class HandPresenter : MonoBehaviour
    {
        public CardLaneView hand;
        public HandOutlineView handOutline;
        public GameObject cardPrefab;
        public RectTransform deckAnchor;
        public RedrawPresenter redraw;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;
        [Inject] private readonly CardSelectionService _selection;

        private void Start()
        {
            _matchState.Hand
                .ObserveAdd()
                .Subscribe(e =>
                {
                    var parent = deckAnchor != null ? deckAnchor : hand.transform;

                    var view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab, parent);
                    view.Setup(e.Value);
                    view.hoverScale = true;
                    _registry.Register(view);

                    if (redraw != null && redraw.IsPhaseActive)
                        redraw.AddCard(view.gameObject);
                    else
                        hand.AddCard(view.gameObject);
                })
                .AddTo(this);

            _matchState.IsMyTurn
                .Subscribe(myTurn => hand.SetFanOpen(myTurn))
                .AddTo(this);

            _selection.IsChoosing
                .Subscribe(choosing => hand.SetLowered(choosing))
                .AddTo(this);

            if (handOutline == null) return;

            _matchState.IsMyTurn
                .CombineLatest(_selection.IsChoosing, (myTurn, choosing) => myTurn && !choosing)
                .DistinctUntilChanged()
                .Subscribe(shown => handOutline.SetShown(shown))
                .AddTo(this);
        }
    }
}
