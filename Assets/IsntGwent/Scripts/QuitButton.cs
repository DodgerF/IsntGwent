using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts
{
    [RequireComponent(typeof(Button))]
    public class QuitButton : MonoBehaviour
    {
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
            #if UNITY_EDITOR
                Debug.Log("Quit");
                if (NetworkClient.active)
                    NetworkClient.Disconnect();
            #else
                Application.Quit();
            #endif
        }
    }
}