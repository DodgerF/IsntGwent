using System;
using TMPro;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Localization
{
    [DisallowMultipleComponent]
    public class AutoLocalizeText : MonoBehaviour
    {
        private const float MinScale = 0.55f;

        private TMP_Text _target;
        private string _source;
        private string _applied;

        private bool _ownSizing;
        private float _fontSize;

        private IDisposable _subscription;

        private void Awake()
        {
            _target = GetComponent<TMP_Text>();
            _source = _target != null ? _target.text : string.Empty;

            if (_target != null && !_target.enableAutoSizing)
            {
                _ownSizing = true;
                _fontSize = _target.fontSize;
            }

            _subscription = Loc.Language.Subscribe(_ => Apply());
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }

        public void Adopt(string source)
        {
            _source = source;
            _applied = null;

            Apply();
        }

        private void Apply()
        {
            if (_target == null) return;

            var current = _target.text;

            if (!string.Equals(current, _applied))
                _source = Loc.SourceOf(current) ?? current;

            var value = Loc.T(_source);
            _applied = value;

            if (!string.Equals(current, value))
                _target.text = value;

            Fit(!string.Equals(value, _source));
        }

        private void Fit(bool translated)
        {
            if (!_ownSizing) return;

            _target.fontSize = _fontSize;

            if (!translated) return;

            var width = _target.rectTransform.rect.width;
            if (width <= 1f) return;

            if (_target.GetPreferredValues(_source, Mathf.Infinity, Mathf.Infinity).x > width) return;

            var preferred = _target.GetPreferredValues(_target.text, Mathf.Infinity, Mathf.Infinity).x;
            if (preferred <= width) return;

            _target.fontSize = Mathf.Max(_fontSize * MinScale, _fontSize * width / preferred);
        }
    }
}
