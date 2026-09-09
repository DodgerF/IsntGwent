using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public class TutorialAnchors : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public RectTransform target;
        }

        [SerializeField] private List<Entry> anchors = new();

        private static readonly Dictionary<string, string> Defaults = new()
        {
            { "own_hp", "OwnHp" },
            { "enemy_hp", "EnemyHp" },
            { "own_power", "OwnTotalPower" },
            { "enemy_power", "EnemyTotalPower" },
            { "own_card_counter", "OwnCardCounter" },
            { "pass_button", "PassButton" },
            { "own_hand", "OwnHandRow" },
            { "own_deck", "OwnDeckPile" },
            { "own_graveyard", "OwnGraveyard" },
            { "nickname_window", "NicknameWindow" },
            { "deck_list", "Decks" },
            { "deck_field", "Cards" },
            { "create_deck_button", "CreateDeckButton" },
            { "play_button", "PlayButton" },
            { "leaderboard", "Leaderboard" },
        };

        public static RectTransform Resolve(TutorialAnchors anchors, string key)
        {
            var wired = anchors == null ? null : anchors.Find(key);

            if (wired != null) return wired;

            return Defaults.TryGetValue(key, out var name) ? ByName(name) : null;
        }

        private static RectTransform ByName(string name)
        {
            var all = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var rect in all)
            {
                if (rect.name == name) return rect;
            }

            return null;
        }

        public RectTransform Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            foreach (var entry in anchors)
            {
                if (entry == null || entry.target == null) continue;
                if (entry.key == key) return entry.target;
            }

            return null;
        }
    }
}
