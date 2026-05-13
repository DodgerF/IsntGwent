using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class CreateLobbyWindow : MonoBehaviour
    {
        public TMP_InputField NameInput;
        public TMP_InputField PasswordInput;
        public Button CreateButton;
        public Button CancelButton;
        
        [Inject] private LobbyViewModel _vm;

        private void Start()
        {
            NameInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Lobby name";
            PasswordInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Password";
            PasswordInput.contentType = TMP_InputField.ContentType.Password;
            
            Reset();

            CancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Reset();
                })
                .AddTo(this);
            
            CreateButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _vm.CreateLobby(NameInput.text, PasswordInput.text);
                    Reset();
                })
                .AddTo(this);;
            
            _vm.IsCreateLobbyWindowOpen
                .Subscribe(isOpen =>
                {
                    gameObject.SetActive(isOpen);
                })
                .AddTo(this);
        }

        private void Reset()
        {
            NameInput.text = "Test";
            PasswordInput.text = "";
                    
            _vm.IsCreateLobbyWindowOpen.Value = false;
        }
    }
}