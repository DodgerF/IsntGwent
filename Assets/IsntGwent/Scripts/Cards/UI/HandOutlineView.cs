using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace IsntGwent.Scripts.Cards.UI
{
    [RequireComponent(typeof(CardLaneView))]
    public class HandOutlineView : MonoBehaviour
    {
        public float auraWidth = 56f;
        public Color halftoneColor = new(0.88f, 0.70f, 0.38f, 0.72f);
        public Color inkColor = new(0.97f, 0.93f, 0.80f, 0.95f);
        public float fadeDuration = 0.25f;
        public bool showMotes = true;
        public int moteCount = 24;
        public Color moteColor = new(0.96f, 0.88f, 0.66f, 0.62f);
        public float warmupDuration = 1.5f;

        private CardLaneView _lane;
        private RectTransform _layer;
        private CanvasGroup _group;
        private HandAuraGraphic _aura;
        private HandAuraMotes _motes;
        private bool _shown;
        private bool _wanted;
        private bool _suppressed;

        private void Awake() => _lane = GetComponent<CardLaneView>();

        private void Start()
        {
            EnsureLayer();
            Configure();
            StartCoroutine(Warmup());
        }

        private IEnumerator Warmup()
        {
            if (_shown) yield break;

            _group.alpha = 0f;
            _layer.gameObject.SetActive(true);
            _aura.SetWarmup(true);

            var until = Time.unscaledTime + warmupDuration;

            while (Time.unscaledTime < until && !_shown)
                yield return null;

            _aura.SetWarmup(false);

            if (!_shown)
                _layer.gameObject.SetActive(false);
        }

        public void SetShown(bool shown)
        {
            _wanted = shown;

            Apply();
        }

        public void SetSuppressed(bool suppressed)
        {
            _suppressed = suppressed;

            Apply();
        }

        private void Apply()
        {
            var shown = _wanted && !_suppressed;

            if (_shown == shown) return;

            _shown = shown;

            EnsureLayer();
            Configure();

            _group.DOKill();

            if (shown)
            {
                _layer.gameObject.SetActive(true);
                _group.DOFade(1f, fadeDuration);
                return;
            }

            _group
                .DOFade(0f, fadeDuration)
                .OnComplete(() => _layer.gameObject.SetActive(false));
        }

        private void Configure()
        {
            _aura.width = auraWidth;
            _aura.halftoneColor = halftoneColor;
            _aura.inkColor = inkColor;

            _motes.gameObject.SetActive(showMotes);
            _motes.maxMotes = moteCount;
            _motes.moteColor = moteColor;
        }

        private void EnsureLayer()
        {
            if (_layer != null) return;

            var go = new GameObject("HandGlow", typeof(RectTransform), typeof(CanvasGroup), typeof(HandOutlineLayer));

            _layer = (RectTransform)go.transform;
            _layer.SetParent(transform, false);
            _layer.anchorMin = new Vector2(0.5f, 0.5f);
            _layer.anchorMax = new Vector2(0.5f, 0.5f);
            _layer.pivot = new Vector2(0.5f, 0.5f);
            _layer.sizeDelta = Vector2.zero;
            _layer.anchoredPosition = Vector2.zero;
            _layer.SetAsFirstSibling();

            _group = go.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _aura = Attach<HandAuraGraphic>("Aura", Vector2.zero);
            _aura.Bind(_lane);

            _motes = Attach<HandAuraMotes>("Motes", new Vector2(4000f, 2000f));
            _motes.Bind(_aura);

            go.SetActive(false);
        }

        private T Attach<T>(string name, Vector2 size) where T : MonoBehaviour
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
            var rt = (RectTransform)go.transform;

            rt.SetParent(_layer, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            return go.GetComponent<T>();
        }

        private void OnDestroy()
        {
            if (_group != null)
                _group.DOKill();
        }
    }
}
