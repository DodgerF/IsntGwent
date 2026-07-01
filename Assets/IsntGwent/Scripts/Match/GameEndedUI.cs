using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
namespace IsntGwent.Scripts.Match
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
                    if (_matchState.IsEnemyLeft.Value)
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
                })
                .AddTo(this);
        }

        private void ShowGameState(string text)
        {
            title.text = "Game Over \n\n" +
                         text;
            
            title.gameObject.SetActive(true);
            background.gameObject.SetActive(true);
            leaveButton.gameObject.SetActive(true);
        }
    }
}