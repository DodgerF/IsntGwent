using IsntGwent.Scripts.Localization;
using IsntGwent.Scripts.Match.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class ReconnectPanelUI : MonoBehaviour
    {
        public Image background;
        public TextMeshProUGUI title;

        public string opponentMessage = "Opponent lost connection\n\nWaiting for reconnect...";
        public string selfMessage = "Connection lost\n\nReconnecting...";

        [Inject] private readonly MatchState _matchState;

        private void Start()
        {
            Hide();

            _matchState.IsSelfReconnecting
                .CombineLatest(_matchState.IsOpponentReconnecting, (self, opponent) => (self, opponent))
                .Subscribe(state =>
                {
                    if (_matchState.IsGameEnded.Value)
                    {
                        Hide();
                        return;
                    }

                    if (state.self)
                        Show(Loc.T(selfMessage));
                    else if (state.opponent)
                        Show(Loc.T(opponentMessage));
                    else
                        Hide();
                })
                .AddTo(this);

            _matchState.IsGameEnded
                .Where(v => v)
                .Subscribe(_ => Hide())
                .AddTo(this);
        }

        private void Show(string message)
        {
            title.text = message;

            title.gameObject.SetActive(true);
            background.gameObject.SetActive(true);
        }

        private void Hide()
        {
            title.gameObject.SetActive(false);
            background.gameObject.SetActive(false);
        }
    }
}
