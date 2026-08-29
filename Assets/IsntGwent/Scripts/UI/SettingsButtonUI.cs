using IsntGwent.Scripts.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.UI
{
    [RequireComponent(typeof(Button))]
    public class SettingsButtonUI : MonoBehaviour
    {
        private MenuSettings _menu;

        private void Start()
        {
            GetComponent<Button>()
                .OnClickAsObservable()
                .Subscribe(_ => Open())
                .AddTo(this);
        }

        private void Open()
        {
            if (_menu == null)
                _menu = FindAnyObjectByType<MenuSettings>(FindObjectsInactive.Include);

            if (_menu != null)
                _menu.ShowMenu();
        }
    }
}
