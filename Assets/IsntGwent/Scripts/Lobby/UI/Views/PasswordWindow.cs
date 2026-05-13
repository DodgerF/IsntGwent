using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class PasswordWindow : MonoBehaviour
    {
        public TMP_InputField PasswordInput;
        public Button ConfirmButton;
        public Button CancelButton;
        
        [Inject] private LobbyViewModel _vm;

        private void Start()
        {
            PasswordInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Password";
            PasswordInput.contentType = TMP_InputField.ContentType.Password;
            
            _vm.IsPasswordWindowOpen
                .Subscribe(isOpen => gameObject.SetActive(isOpen))
                .AddTo(this);
            
            CancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    PasswordInput.text = "";
                    _vm.ClosePasswordWindow();
                })
                .AddTo(this);
            
            ConfirmButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _vm.ConfirmJoinWithPassword(PasswordInput.text);
                })
                .AddTo(this);
        }
    }
}