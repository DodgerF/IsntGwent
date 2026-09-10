using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class PilePresenter : MonoBehaviour
    {
        public TextMeshProUGUI ownDeckCount;
        public TextMeshProUGUI enemyDeckCount;
        public TextMeshProUGUI ownGraveyardCount;
        public TextMeshProUGUI enemyGraveyardCount;

        public Button ownDeckButton;
        public Button ownGraveyardButton;
        public Button enemyGraveyardButton;

        public PileWindowView window;

        [Inject] private readonly MatchState _matchState;

        private void Start()
        {
            BindCount(_matchState.OwnDeck, ownDeckCount);
            BindCount(_matchState.OwnGraveyard, ownGraveyardCount);
            BindCount(_matchState.EnemyGraveyard, enemyGraveyardCount);

            _matchState.EnemyDeckCount
                .Subscribe(count => SetText(enemyDeckCount, count))
                .AddTo(this);

            BindButton(ownDeckButton, PileKind.OwnDeck);
            BindButton(ownGraveyardButton, PileKind.OwnGraveyard);
            BindButton(enemyGraveyardButton, PileKind.EnemyGraveyard);
        }

        private void BindCount(ReactiveCollection<CardInstance> collection, TextMeshProUGUI text)
        {
            if (text == null) return;

            collection
                .ObserveCountChanged(true)
                .Subscribe(count => SetText(text, count))
                .AddTo(this);
        }

        private void BindButton(Button button, PileKind kind)
        {
            if (button == null || window == null) return;

            button.onClick.AddListener(() => window.Open(kind));
        }

        private static void SetText(TextMeshProUGUI text, int count)
        {
            if (text == null) return;

            text.text = count.ToString();
        }
    }
}
