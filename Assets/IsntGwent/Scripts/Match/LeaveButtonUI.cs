using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    [RequireComponent(typeof(Button))]
    public class LeaveButtonUI : MonoBehaviour
    {
        [Inject] private readonly MatchClientHandler _handler;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            _handler.SendLeave();
        }
    }
}