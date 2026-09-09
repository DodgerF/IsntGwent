using System.Collections;
using IsntGwent.Scripts.Audio;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace IsntGwent.Scripts.Localization
{
    public class LocalizationBinder : MonoBehaviour
    {
        private static readonly float[] Delays = { 0.2f, 1f, 3f };

        [Inject] private readonly LocalizationService _localization;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            _localization.OnLoaded
                .Where(loaded => loaded)
                .Subscribe(_ => Rescan())
                .AddTo(this);

            Loc.Language
                .Subscribe(_ => Scan())
                .AddTo(this);

            Rescan();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Rescan();
        }

        private void Rescan()
        {
            if (!isActiveAndEnabled) return;

            StopAllCoroutines();
            StartCoroutine(RescanCoroutine());
        }

        private IEnumerator RescanCoroutine()
        {
            Scan();

            foreach (var delay in Delays)
            {
                yield return new WaitForSeconds(delay);
                Scan();
            }
        }

        private void Scan()
        {
            BindTexts();
            BindSelector();
        }

        private static void BindTexts()
        {
            var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var text in texts)
            {
                if (text == null) continue;
                if (text.GetComponent<AutoLocalizeText>() != null) continue;
                if (text.GetComponentInParent<LocalizationIgnore>(true) != null) continue;

                text.gameObject.AddComponent<AutoLocalizeText>();
            }
        }

        private static void BindSelector()
        {
            var windows = FindObjectsByType<SettingsWindowUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var window in windows)
            {
                if (window == null) continue;
                if (window.GetComponent<LanguageSelectorUI>() != null) continue;

                window.gameObject.AddComponent<LanguageSelectorUI>();
            }
        }
    }
}
