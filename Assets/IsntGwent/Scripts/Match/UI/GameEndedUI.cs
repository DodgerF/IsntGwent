using IsntGwent.Scripts.Localization;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using IsntGwent.Scripts.Match.Client;
namespace IsntGwent.Scripts.Match.UI
{
    public class GameEndedUI : MonoBehaviour
    {
        public Image background;
        public TextMeshProUGUI title;
        public LeaveButtonUI leaveButton;
        
        [Inject] private readonly MatchState _matchState;
        

        private void Start()
        {
            background.gameObject.SetActive(false);
            title.gameObject.SetActive(false);
            leaveButton.gameObject.SetActive(false);
            
            _matchState.IsGameEnded
                .Where(v => v)
                .First()
                .Subscribe(_ =>
                {
                    if (_matchState.IsConnectionLost.Value)
                        ShowGameState("Connection lost");
                    else if (_matchState.IsEnemyLeft.Value)
                        ShowGameState("Enemy left");
                    else if (_matchState.IsEnemyGiveUp.Value)
                        ShowGameState("Enemy surrendered");
                    else if (_matchState.AmIGiveUp.Value ||
                             _matchState.MyHp.Value == 0 && _matchState.EnemyHp.Value != 0)
                        ShowGameState("Defeat");
                    else if (_matchState.MyHp.Value != 0 && _matchState.EnemyHp.Value == 0)
                        ShowGameState("You won");
                    else if (_matchState.MyHp.Value == 0 && _matchState.EnemyHp.Value == 0)
                        ShowGameState("Friendship won");
                    else if (_matchState.IsTie.Value)
                        ShowGameState("Draw");
                    else if (_matchState.AmIWinner.Value)
                        ShowGameState("You won");
                    else
                        ShowGameState("Defeat");
                })
                .AddTo(this);
        }

        private void ShowGameState(string text)
        {
            title.text = Loc.T("Game Over") + "\n\n" + Loc.T(text);
            
            title.gameObject.SetActive(true);
            background.gameObject.SetActive(true);
            leaveButton.gameObject.SetActive(true);
        }
    }
}