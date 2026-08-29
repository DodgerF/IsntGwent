using DG.Tweening;
using IsntGwent.Scripts.Match.Client;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class PassLabelUI : MonoBehaviour
    {
        public TextMeshProUGUI enemyPassedLabel;

        public float appearDuration = 0.18f;
        public float flareDelay = 0.06f;
        public float flareDuration = 0.26f;
        public float flareScale = 1.14f;
        public float appearScale = 0.84f;
        public Color flareColor = new(1f, 0.97f, 0.86f, 1f);

        [Inject] private readonly MatchState _matchState;

        private Color _baseColor;
        private Vector3 _baseScale;

        private void Awake()
        {
            if (enemyPassedLabel == null) return;

            _baseColor = enemyPassedLabel.color;
            _baseScale = enemyPassedLabel.transform.localScale;
        }

        private void Start()
        {
            if (enemyPassedLabel == null) return;

            Hide();

            _matchState.IsEnemyPassed
                .Subscribe(passed =>
                {
                    if (passed) Show();
                    else Hide();
                })
                .AddTo(this);

            _matchState.IsGameEnded
                .Where(ended => ended)
                .Subscribe(_ => Hide())
                .AddTo(this);
        }

        private void Show()
        {
            var label = enemyPassedLabel.transform;

            label.DOKill();
            enemyPassedLabel.DOKill();

            enemyPassedLabel.gameObject.SetActive(true);
            enemyPassedLabel.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
            label.localScale = _baseScale * appearScale;

            DOTween.Sequence()
                .SetTarget(label)
                .Append(enemyPassedLabel.DOFade(_baseColor.a, appearDuration))
                .Join(label.DOScale(_baseScale, appearDuration).SetEase(Ease.OutBack))
                .AppendInterval(flareDelay)
                .Append(enemyPassedLabel.DOColor(flareColor, flareDuration * 0.4f))
                .Join(label.DOScale(_baseScale * flareScale, flareDuration * 0.4f).SetEase(Ease.OutQuad))
                .Append(enemyPassedLabel.DOColor(_baseColor, flareDuration * 0.6f))
                .Join(label.DOScale(_baseScale, flareDuration * 0.6f).SetEase(Ease.InOutQuad))
                .SetLink(gameObject);
        }

        private void Hide()
        {
            var label = enemyPassedLabel.transform;

            label.DOKill();
            enemyPassedLabel.DOKill();

            label.localScale = _baseScale;
            enemyPassedLabel.color = _baseColor;
            enemyPassedLabel.gameObject.SetActive(false);
        }
    }
}
