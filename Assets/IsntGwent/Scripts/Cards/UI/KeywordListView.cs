using System.Text;
using TMPro;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public class KeywordListView : MonoBehaviour
    {
        public GameObject panel;
        public TextMeshProUGUI text;

        [Inject] private readonly KeywordDatabase _keywords;

        public bool Show(string description)
        {
            if (text == null) return false;

            var used = _keywords.Used(description);

            if (used.Count == 0)
            {
                Hide();
                return false;
            }

            var builder = new StringBuilder();

            for (var i = 0; i < used.Count; i++)
            {
                var entry = used[i];

                if (i > 0) builder.Append("\n\n");

                var colored = !string.IsNullOrWhiteSpace(entry.color);

                if (colored) builder.Append("<color=").Append(entry.color).Append('>');
                builder.Append("<b>").Append(entry.Title()).Append("</b>");
                if (colored) builder.Append("</color>");

                builder.Append('\n').Append(entry.description);
            }

            text.text = builder.ToString();

            if (panel != null) panel.SetActive(true);

            return true;
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
