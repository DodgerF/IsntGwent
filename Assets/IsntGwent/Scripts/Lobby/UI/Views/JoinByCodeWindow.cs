using IsntGwent.Scripts.Lobby.Core;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class JoinByCodeWindow : MonoBehaviour
    {
        public TMP_InputField CodeInput;
        public Button ConfirmButton;
        public Button CancelButton;

        [Inject] private LobbyViewModel _vm;

        private void Start()
        {
            CodeInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Room code";
            CodeInput.characterLimit = JoinCodes.Length;
            CodeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;

            _vm.IsJoinCodeWindowOpen
                .Subscribe(isOpen =>
                {
                    gameObject.SetActive(isOpen);

                    if (isOpen) CodeInput.text = string.Empty;
                })
                .AddTo(this);

            CodeInput.onValueChanged.AsObservable()
                .Subscribe(OnCodeChanged)
                .AddTo(this);

            CancelButton.OnClickAsObservable()
                .Subscribe(_ => _vm.CloseJoinCodeWindow())
                .AddTo(this);

            ConfirmButton.OnClickAsObservable()
                .Subscribe(_ => _vm.ConfirmJoinByCode(CodeInput.text))
                .AddTo(this);
        }

        private void OnCodeChanged(string code)
        {
            var upper = JoinCodes.Normalize(code);

            if (CodeInput.text != upper)
                CodeInput.text = upper;

            ConfirmButton.interactable = JoinCodes.IsWellFormed(upper);
        }
    }
}
