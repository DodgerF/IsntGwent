using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Messages;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class NotificationUI : MonoBehaviour
    {
        public Image background;
        public TextMeshProUGUI title;
        public GameObject enemyPassed;
        
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly MatchClientHandler _handler;
        
        private readonly Queue<string> _notifications = new();
        private bool _isShowing;

        private void Start()
        {
            background.gameObject.SetActive(false);
            title.gameObject.SetActive(false);
            enemyPassed.SetActive(false);
            
            _matchState.LastRoundResult
                .Skip(1)
                .Subscribe(result =>
                {
                    ShowNotification(result switch
                    {
                        RoundResult.Win => "You won the round",
                        RoundResult.Lose => "You lost the round!",
                        RoundResult.Tie => "Tie",
                        _ => ""
                    });
                    enemyPassed.SetActive(false);
                })
                .AddTo(this);
            
            _matchState.IsGameEnded
                .Where(v => v)
                .Subscribe(_ =>
                {
                    ShowNotification(_matchState.IsTie.Value ? "Friendship won"
                        : _matchState.AmIWinner.Value ? "You won!"
                        : "Defeat");
                })
                .AddTo(this);
            _matchState.IsMyTurn
                .First()
                .Subscribe(isMyTurn =>
                {
                    if (_matchState.IsGameEnded.Value) return;

                    ShowNotification(isMyTurn
                        ? "You go first!"
                        : "Opponent goes first!");
                })
                .AddTo(this);
            _matchState.IsMyTurn
                .Skip(1)
                .Where(_ => !_matchState.IsGameEnded.Value)
                .Where(isMyTurn => isMyTurn)
                .Subscribe(_ =>
                {
                    ShowNotification("Your turn!");
                })
                .AddTo(this);
            
            _handler.OnEnemyPassed
                .Subscribe(_ =>
                {
                    ShowNotification("Opponent has passed" );
                    enemyPassed.SetActive(true);
                })
                .AddTo(this);
        }
        private void ShowNotification(string message)
        {
            _notifications.Enqueue(message);

            if (!_isShowing)
                StartCoroutine(ProcessNotifications());
        }
        private IEnumerator ProcessNotifications()
        {
            _isShowing = true;

            while (_notifications.Count > 0)
            {
                var message = _notifications.Dequeue();

                title.text = message;

                yield return ShowNotificationAnimation();
            }

            _isShowing = false;
        }
        
        private IEnumerator ShowNotificationAnimation()
        {
            background.gameObject.SetActive(true);
            title.gameObject.SetActive(true);

            yield return Fade(0f, 1f, 0.1f);
            yield return new WaitForSeconds(1f);
            yield return Fade(1f, 0f, 0.3f);

            background.gameObject.SetActive(false);
            title.gameObject.SetActive(false);
        }
        
        private IEnumerator Fade(float from, float to, float duration)
        {
            var textColor = title.color;
            var bgColor = background.color;

            var timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;

                var alpha = Mathf.Lerp(from, to, timer / duration);

                textColor.a = alpha;
                bgColor.a = alpha;

                title.color = textColor;
                background.color = bgColor;

                yield return null;
            }
        }
    }
}
