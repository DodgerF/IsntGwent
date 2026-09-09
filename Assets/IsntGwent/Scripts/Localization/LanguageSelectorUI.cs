using IsntGwent.Scripts.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Localization
{
    [DisallowMultipleComponent]
    public class LanguageSelectorUI : MonoBehaviour
    {
        public const string Caption = "Language";

        private const string LabelTemplateName = "UiText";
        private const string ButtonTemplateName = "Button";
        private const float RowY = -176f;
        private const float ButtonX = 143f;
        private const float ButtonWidth = 300f;
        private const float ButtonHeight = 44f;
        private const float ButtonFontSize = 30f;
        private const float ButtonFontSizeMin = 16f;

        private TMP_Text _value;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            if (transform.Find(ButtonTemplateName) == null) return;

            var labelTemplate = transform.Find(LabelTemplateName) as RectTransform;
            var buttonTemplate = transform.Find(ButtonTemplateName) as RectTransform;

            if (labelTemplate == null || buttonTemplate == null) return;

            BuildCaption(labelTemplate);
            BuildButton(buttonTemplate);

            Loc.Language
                .Subscribe(_ => Refresh())
                .AddTo(this);
        }

        private void BuildCaption(RectTransform template)
        {
            var caption = Instantiate(template.gameObject, transform);
            caption.name = "LanguageText";

            var rect = Copy(template, (RectTransform)caption.transform);
            rect.sizeDelta = template.sizeDelta;
            rect.anchoredPosition = new Vector2(template.anchoredPosition.x, RowY);

            var text = caption.GetComponent<TMP_Text>();
            if (text == null) return;

            text.text = Caption;

            var auto = text.GetComponent<AutoLocalizeText>();

            if (auto == null)
                text.gameObject.AddComponent<AutoLocalizeText>();
            else
                auto.Adopt(Caption);
        }

        private void BuildButton(RectTransform template)
        {
            var button = Instantiate(template.gameObject, transform);
            button.name = "LanguageButton";
            button.SetActive(true);

            var quit = button.GetComponent<QuitButton>();
            if (quit != null) Destroy(quit);

            var rect = Copy(template, (RectTransform)button.transform);
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            rect.anchoredPosition = new Vector2(ButtonX, RowY);

            _value = button.GetComponentInChildren<TMP_Text>(true);

            if (_value != null)
            {
                var auto = _value.GetComponent<AutoLocalizeText>();
                if (auto != null) Destroy(auto);

                if (_value.GetComponent<LocalizationIgnore>() == null)
                    _value.gameObject.AddComponent<LocalizationIgnore>();

                _value.enableAutoSizing = true;
                _value.fontSizeMax = ButtonFontSize;
                _value.fontSizeMin = ButtonFontSizeMin;
            }

            var click = button.GetComponent<Button>();
            if (click == null) return;

            click.onClick.RemoveAllListeners();
            click.onClick.AddListener(OnClick);
        }

        private static RectTransform Copy(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.localScale = from.localScale;
            to.localRotation = from.localRotation;

            return to;
        }

        private void OnClick()
        {
            Loc.Service?.Next();
        }

        private void Refresh()
        {
            if (_value == null) return;

            var locale = Loc.Service?.Active();

            _value.text = locale == null || string.IsNullOrWhiteSpace(locale.name)
                ? Loc.Language.Value
                : locale.name;
        }
    }
}
