using IsntGwent.Scripts.Messages;
using TMPro;
using UnityEngine;

namespace IsntGwent.Scripts.Accounts.UI
{
    public class LeaderboardEntryView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI recordText;
        [SerializeField] private GameObject highlight;

        public void Setup(LeaderboardEntry entry, int rank, bool isMe)
        {
            rankText.text = rank > 0 ? rank.ToString() : "—";
            nicknameText.text = entry.Nickname;
            pointsText.text = entry.Points.ToString();

            if (recordText != null)
                recordText.text = entry.Wins + " / " + entry.Losses + " / " + entry.Ties;

            if (highlight != null)
                highlight.SetActive(isMe);
        }
    }
}
