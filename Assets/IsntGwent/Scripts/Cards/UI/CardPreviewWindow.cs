using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public class CardPreviewWindow : MonoBehaviour
    {
        public CardView cardView;
        public TextMeshProUGUI description;
        public GameObject descriptionBackground;
        public GameObject preview;
        public KeywordListView keywords;

        [SerializeField] private Button dimmer;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI stats;
        [SerializeField] private ScrollRect descriptionScroll;

        [Inject] private CardPreviewService _previewService;
        [Inject] private KeywordDatabase _keywords;

        private void Start()
        {
            cardView.hoverSfx = false;

            _previewService.ShowCard
                .Subscribe(Show)
                .AddTo(this);

            _previewService.HideCard
                .Subscribe(_ => Hide())
                .AddTo(this);

            if (dimmer != null)
                dimmer.onClick.AddListener(_previewService.Hide);

            Hide();
        }

        private void Show(PreviewRequest request)
        {
            preview.SetActive(true);

            if (dimmer != null)
                dimmer.gameObject.SetActive(request.Modal);

            Setup(request.Card);
        }

        public void Setup(CardInstance card)
        {
            cardView.Setup(card);

            var text = card.Definition.Description;

            description.text = _keywords.Format(text);
            descriptionBackground.SetActive(!string.IsNullOrWhiteSpace(text));

            if (keywords != null)
                keywords.Show(text);

            if (descriptionScroll != null && descriptionScroll.gameObject.activeInHierarchy)
            {
                Canvas.ForceUpdateCanvases();
                descriptionScroll.verticalNormalizedPosition = 1f;
            }

            if (title != null)
                title.text = card.Definition.Name;

            if (stats != null)
            {
                var unit = card as UnitInstance;
                var changed = unit != null && unit.CurrentPower.Value != unit.BasePower.Value;

                stats.gameObject.SetActive(changed);

                if (changed)
                    stats.text = unit.BasePower.Value.ToString();
            }
        }

        private void Hide()
        {
            preview.SetActive(false);

            if (keywords != null)
                keywords.Hide();
        }
    }
}
