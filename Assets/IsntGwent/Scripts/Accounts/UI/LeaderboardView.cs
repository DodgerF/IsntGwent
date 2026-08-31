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
        [SerializeField] private LeaderboardEntryView ownEntry;
        [SerializeField] private GameObject ownEntryGroup;

        private readonly List<LeaderboardEntryView> _rows = new();

        private void Start()
        {
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

            if (ownEntryGroup != null)
                ownEntryGroup.SetActive(false);
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

            LayoutRebuilder.ForceRebuildLayoutImmediate(topParent as RectTransform);

            if (ownEntryGroup == null || ownEntry == null) return;

            var isInTop = msg.MyRank > 0 && msg.MyRank <= top.Length;
            var hasAccount = !string.IsNullOrEmpty(msg.Me.Nickname);

            ownEntryGroup.SetActive(hasAccount && !isInTop);

            if (hasAccount && !isInTop)
                ownEntry.Setup(msg.Me, msg.MyRank, true);
        }
    }
}
