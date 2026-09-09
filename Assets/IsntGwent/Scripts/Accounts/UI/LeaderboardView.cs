using System.Collections.Generic;
using IsntGwent.Scripts.Accounts.Client;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Accounts.UI
{
    public class LeaderboardView : MonoBehaviour
    {
        [Inject] private AccountClientHandler _handler;
        [Inject] private PlayerAccount _account;
        [Inject] private DiContainer _container;
        [Inject] private ConnectionService _connection;

        [SerializeField] private Transform topParent;
        [SerializeField] private LeaderboardEntryView entryPrefab;
        [SerializeField] private float listBottomPadding = 46f;
        [SerializeField] private float minHeight = 220f;

        private readonly List<LeaderboardEntryView> _rows = new();

        private RectTransform _panel;
        private RectTransform _parent;
        private RectTransform _content;
        private RectTransform _viewport;
        private RectTransform _scrollArea;
        private ScrollRect _scroll;
        private float _topInset;
        private float _bottomInset;

        private void Awake()
        {
            _panel = (RectTransform)transform;
            _parent = _panel.parent as RectTransform;
            _content = (RectTransform)topParent;
            _viewport = (RectTransform)_content.parent;
            _scrollArea = (RectTransform)_viewport.parent;
            _scroll = _scrollArea.GetComponent<ScrollRect>();

            _topInset = -_panel.offsetMax.y;
            _bottomInset = _panel.offsetMin.y;

            _scrollArea.offsetMin = new Vector2(_scrollArea.offsetMin.x, listBottomPadding);

            _panel.anchorMin = new Vector2(_panel.anchorMin.x, 0f);
            _panel.anchorMax = new Vector2(_panel.anchorMax.x, 0f);
            _panel.pivot = new Vector2(_panel.pivot.x, 0f);
        }

        private void Start()
        {
            ResizeToContent();

            _handler.OnLeaderboard
                .Subscribe(Render)
                .AddTo(this);

            _account.IsLoggedIn
                .Where(isLoggedIn => isLoggedIn)
                .Subscribe(_ => _handler.RequestLeaderboard())
                .AddTo(this);

            _account.Nickname
                .Skip(1)
                .Where(_ => _account.IsLoggedIn.Value)
                .Subscribe(_ => _handler.RequestLeaderboard())
                .AddTo(this);

            _connection.IsConnected
                .Where(isConnected => isConnected && _account.IsLoggedIn.Value)
                .Subscribe(_ => _handler.RequestLeaderboard())
                .AddTo(this);
        }

        private void Render(LeaderboardResultMessage msg)
        {
            foreach (var row in _rows)
                Destroy(row.gameObject);

            _rows.Clear();

            var top = msg.Top ?? new LeaderboardEntry[0];

            for (var i = 0; i < top.Length; i++)
            {
                var row = _container.InstantiatePrefabForComponent<LeaderboardEntryView>(entryPrefab, topParent);
                row.Setup(top[i], i + 1, msg.MyRank == i + 1);

                _rows.Add(row);
            }

            ResizeToContent();
        }

        private void ResizeToContent()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

            var chrome = -_scrollArea.offsetMax.y + _scrollArea.offsetMin.y;
            var available = _parent != null ? _parent.rect.height - _topInset - _bottomInset : minHeight;
            var maxHeight = Mathf.Max(minHeight, available);
            var height = Mathf.Clamp(LayoutUtility.GetPreferredHeight(_content) + chrome, minHeight, maxHeight);

            _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, _bottomInset);
            _panel.sizeDelta = new Vector2(_panel.sizeDelta.x, height);

            if (_scroll != null)
                _scroll.verticalNormalizedPosition = 1f;
        }
    }
}
