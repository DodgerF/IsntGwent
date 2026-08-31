using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Lobby.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class WaitingRoomCodeView : MonoBehaviour
    {
        [Inject] private PlaySession _play;
        [Inject] private AudioService _audio;

        [SerializeField] private GameObject group;
        [SerializeField] private TextMeshProUGUI codeText;
        [SerializeField] private Button copyButton;

        private void Start()
        {
            _play.JoinCode
                .CombineLatest(_play.Phase, (code, phase) => phase == PlayPhase.WaitingForFriend ? code : string.Empty)
                .Subscribe(code =>
                {
                    group.SetActive(!string.IsNullOrEmpty(code));
                    codeText.text = code;
                })
                .AddTo(this);

            copyButton.OnClickAsObservable()
                .Subscribe(_ => Copy())
                .AddTo(this);
        }

        private void Copy()
        {
            if (!_play.HasCode) return;

            GUIUtility.systemCopyBuffer = _play.JoinCode.Value;
            _audio.Play("ui_click");
        }
    }
}
