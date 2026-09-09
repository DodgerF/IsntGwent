using System.Collections.Generic;
using IsntGwent.Scripts.Cards.UI;
using UnityEngine;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public static class TutorialAura
    {
        private const string Name = "TutorialAura";
        private const float Width = 44f;

        private static readonly Color Halftone = new(0.91f, 0.75f, 0.44f, 0.80f);
        private static readonly Color Ink = new(0.98f, 0.94f, 0.82f, 0.98f);

        private static HandAuraGraphic _aura;
        private static bool _hidden;
        private static readonly List<RectTransform> Targets = new();
        private static readonly List<CardView> Lifted = new();

        public static bool IsActive => Targets.Count > 0;

        public static void Apply(IReadOnlyList<RectTransform> targets)
        {
            Clear();

            if (targets == null) return;

            foreach (var target in targets)
            {
                if (target != null) Targets.Add(target);
            }

            if (Targets.Count == 0) return;

            var aura = Ensure();

            if (aura == null)
            {
                Targets.Clear();
                return;
            }

            aura.Bind(Targets);
            aura.gameObject.SetActive(!_hidden);

            Lift();
        }

        public static void Clear()
        {
            Drop();
            Targets.Clear();

            if (_aura != null) _aura.gameObject.SetActive(false);
        }

        public static void SetHidden(bool hidden)
        {
            _hidden = hidden;

            if (_aura != null) _aura.gameObject.SetActive(!hidden && Targets.Count > 0);
        }

        private static void Lift()
        {
            foreach (var target in Targets)
            {
                var view = target.GetComponentInParent<CardView>();

                if (view == null || view.mode != CardMode.InHand) continue;

                Lifted.Add(view);
                view.SetHovered(true);
            }
        }

        private static void Drop()
        {
            foreach (var view in Lifted)
            {
                if (view != null) view.SetHovered(false);
            }

            Lifted.Clear();
        }

        private static HandAuraGraphic Ensure()
        {
            if (_aura != null) return _aura;

            var canvas = TutorialOverlayFactory.RootCanvas();

            if (canvas == null) return null;

            var host = new GameObject(Name, typeof(RectTransform), typeof(Canvas));
            var hostRect = (RectTransform)host.transform;

            hostRect.SetParent(canvas.transform, false);
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.pivot = new Vector2(0.5f, 0.5f);
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;
            hostRect.SetAsLastSibling();

            var sorting = host.GetComponent<Canvas>();
            sorting.overrideSorting = true;
            sorting.sortingOrder = TutorialOverlayView.AuraSortingOrder;

            var go = new GameObject("Aura", typeof(RectTransform), typeof(CanvasRenderer), typeof(HandAuraGraphic));
            var rect = (RectTransform)go.transform;

            rect.SetParent(hostRect, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            _aura = go.GetComponent<HandAuraGraphic>();
            _aura.width = Width;
            _aura.cutout = 1f;
            _aura.halftoneColor = Halftone;
            _aura.inkColor = Ink;
            _aura.raycastTarget = false;

            return _aura;
        }
    }
}
