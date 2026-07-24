using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Core
{
    public class MenuSettings : MonoBehaviour
    {
        [Inject] private readonly InputRouter _input;
        [Inject] private readonly MatchState _matchState;
        private void Start()
        {
            gameObject.SetActive(false);
            _input.Pressed
                .Where(_ => gameObject.activeSelf)
                .Where(go => go == null ||
                             go.GetComponentInParent<MenuSettings>() == null)
                .Subscribe(_ =>
                {
                    gameObject.SetActive(false);
                })
                .AddTo(this);

            _matchState.IsGameEnded
                .Where(v => v)
                .Subscribe(_ => gameObject.SetActive(false))
                .AddTo(this);
        }

        public void ShowMenu()
        {
            gameObject.SetActive(true);
        }
    }
}