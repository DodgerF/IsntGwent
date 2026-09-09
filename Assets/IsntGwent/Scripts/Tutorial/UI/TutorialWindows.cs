using System.Collections.Generic;
using IsntGwent.Scripts.Accounts.UI;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Lobby.UI.Views;
using IsntGwent.Scripts.Match.UI;
using UnityEngine;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public static class TutorialWindows
    {
        public static List<GameObject> Collect()
        {
            var roots = new List<GameObject>();

            foreach (var view in All<SettingsWindowUI>()) Add(roots, view.gameObject);
            foreach (var view in All<NicknameWindow>()) Add(roots, view.gameObject);
            foreach (var view in All<JoinByCodeWindow>()) Add(roots, view.gameObject);
            foreach (var view in All<PileWindowView>()) Add(roots, view.window);
            foreach (var view in All<ConfirmWindow>()) Add(roots, view.panel);
            foreach (var view in All<CardPreviewWindow>()) Add(roots, view.preview);

            return roots;
        }

        public static bool AnyOpen(List<GameObject> roots)
        {
            foreach (var root in roots)
            {
                if (root != null && root.activeInHierarchy) return true;
            }

            return false;
        }

        private static T[] All<T>() where T : Object
            => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private static void Add(List<GameObject> roots, GameObject root)
        {
            if (root != null && !roots.Contains(root)) roots.Add(root);
        }
    }
}
