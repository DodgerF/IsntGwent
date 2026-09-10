using System.Collections.Generic;
using UnityEngine;

namespace IsntGwent.Scripts.Cards.UI
{
    public struct HandCardQuad
    {
        public Vector2 Center;
        public Vector2 Half;
        public float Cos;
        public float Sin;
    }

    public static class HandAuraGeometry
    {
        public static void Collect(IReadOnlyList<Transform> cards, List<HandCardQuad> result)
        {
            result.Clear();

            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i] is not RectTransform card) continue;
                if (!card.gameObject.activeInHierarchy) continue;

                var visual = card.TryGetComponent<CardView>(out var view) ? view.VisualRect : card;
                var scale = card.localScale;
                var degrees = card.localEulerAngles.z;
                var center = (Vector2)card.localPosition;

                if (visual != card)
                {
                    center += (Vector2)(card.localRotation * Vector3.Scale(visual.localPosition, scale));
                    degrees += visual.localEulerAngles.z;
                    scale = Vector3.Scale(scale, visual.localScale);
                }

                var rect = visual.rect;
                var angle = degrees * Mathf.Deg2Rad;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                var pivot = new Vector2(rect.center.x * scale.x, rect.center.y * scale.y);

                result.Add(new HandCardQuad
                {
                    Center = center
                             + new Vector2(pivot.x * cos - pivot.y * sin, pivot.x * sin + pivot.y * cos),
                    Half = new Vector2(
                        Mathf.Abs(rect.width * 0.5f * scale.x),
                        Mathf.Abs(rect.height * 0.5f * scale.y)),
                    Cos = cos,
                    Sin = sin
                });
            }
        }

        public static void Collect(IReadOnlyList<RectTransform> targets, RectTransform space,
            List<HandCardQuad> result)
        {
            result.Clear();

            if (space == null) return;

            var corners = new Vector3[4];

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                if (target == null) continue;
                if (!target.gameObject.activeInHierarchy) continue;

                target.GetWorldCorners(corners);

                var bottomLeft = (Vector2)space.InverseTransformPoint(corners[0]);
                var topLeft = (Vector2)space.InverseTransformPoint(corners[1]);
                var bottomRight = (Vector2)space.InverseTransformPoint(corners[3]);

                var right = bottomRight - bottomLeft;
                var up = topLeft - bottomLeft;
                var length = right.magnitude;

                if (length <= Mathf.Epsilon) continue;

                var direction = right / length;

                result.Add(new HandCardQuad
                {
                    Center = bottomLeft + (right + up) * 0.5f,
                    Half = new Vector2(length * 0.5f, up.magnitude * 0.5f),
                    Cos = direction.x,
                    Sin = direction.y
                });
            }
        }

        public static float Distance(IReadOnlyList<HandCardQuad> quads, Vector2 point, float corner)
        {
            var best = float.MaxValue;

            for (var i = 0; i < quads.Count; i++)
            {
                var quad = quads[i];
                var local = point - quad.Center;
                var rotated = new Vector2(
                    local.x * quad.Cos + local.y * quad.Sin,
                    local.y * quad.Cos - local.x * quad.Sin);

                var ex = Mathf.Abs(rotated.x) - Mathf.Max(quad.Half.x - corner, 0f);
                var ey = Mathf.Abs(rotated.y) - Mathf.Max(quad.Half.y - corner, 0f);

                var outside = new Vector2(Mathf.Max(ex, 0f), Mathf.Max(ey, 0f));
                var distance = outside.magnitude + Mathf.Min(Mathf.Max(ex, ey), 0f) - corner;

                if (distance < best) best = distance;
            }

            return best;
        }

        public static Rect Bounds(IReadOnlyList<HandCardQuad> quads, float padding)
        {
            if (quads.Count == 0) return Rect.zero;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (var i = 0; i < quads.Count; i++)
            {
                var quad = quads[i];
                var extentX = Mathf.Abs(quad.Half.x * quad.Cos) + Mathf.Abs(quad.Half.y * quad.Sin);
                var extentY = Mathf.Abs(quad.Half.x * quad.Sin) + Mathf.Abs(quad.Half.y * quad.Cos);

                min = Vector2.Min(min, quad.Center - new Vector2(extentX, extentY));
                max = Vector2.Max(max, quad.Center + new Vector2(extentX, extentY));
            }

            min -= new Vector2(padding, padding);
            max += new Vector2(padding, padding);

            return new Rect(min, max - min);
        }
    }
}
