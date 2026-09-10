using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Cards.UI
{
    [DefaultExecutionOrder(200)]
    public class HandAuraGraphic : MaskableGraphic
    {
        public const int Capacity = 16;

        private const string MaterialPath = "Shaders/M_UI_HandAura";
        private const string ShaderName = "IsntGwent/UI/HandAura";

        private static readonly int RectsId = Shader.PropertyToID("_AuraRects");
        private static readonly int RotationsId = Shader.PropertyToID("_AuraRot");
        private static readonly int CountId = Shader.PropertyToID("_AuraCount");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int LineColorId = Shader.PropertyToID("_LineColor");
        private static readonly int WidthId = Shader.PropertyToID("_Width");
        private static readonly int CornerId = Shader.PropertyToID("_Corner");
        private static readonly int DotCellId = Shader.PropertyToID("_DotCell");
        private static readonly int LineOffsetId = Shader.PropertyToID("_LineOffset");
        private static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
        private static readonly int WobbleId = Shader.PropertyToID("_Wobble");
        private static readonly int UnderId = Shader.PropertyToID("_Under");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int DashPhaseId = Shader.PropertyToID("_DashPhase");
        private static readonly int DashCountId = Shader.PropertyToID("_DashCount");
        private static readonly int HalfSizeId = Shader.PropertyToID("_HalfSize");
        private static readonly int CutoutId = Shader.PropertyToID("_Cutout");

        public float width = 56f;
        public float corner = 9f;
        public float dotCell = 7f;
        public float lineOffset = 7f;
        public float lineWidth = 3.6f;
        public float wobble = 3.4f;
        public float underlight = 0.85f;
        public float cutout;
        public float pulseAmount = 0.07f;
        public float pulseSpeed = 1.6f;
        public float dashSpeed = 0.4f;
        public float dashCount = 16f;
        public Color halftoneColor = new(0.88f, 0.70f, 0.38f, 0.72f);
        public Color inkColor = new(0.97f, 0.93f, 0.80f, 0.95f);

        private readonly List<HandCardQuad> _quads = new();
        private readonly Vector4[] _rects = new Vector4[Capacity];
        private readonly Vector4[] _rotations = new Vector4[Capacity];

        private CardLaneView _lane;
        private IReadOnlyList<RectTransform> _targets;
        private Material _instance;
        private bool _warmup;

        public IReadOnlyList<HandCardQuad> Quads => _quads;

        public float Corner => corner;

        public override Texture mainTexture => Texture2D.whiteTexture;

        public void Bind(CardLaneView lane)
        {
            _lane = lane;
            _targets = null;
        }

        public void Bind(IReadOnlyList<RectTransform> targets)
        {
            _targets = targets;
            _lane = null;
        }

        public void SetWarmup(bool warmup)
        {
            if (_warmup == warmup) return;

            _warmup = warmup;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();

            raycastTarget = false;
            color = Color.white;

            var source = Resources.Load<Material>(MaterialPath);
            var shader = source != null ? source.shader : Shader.Find(ShaderName);

            if (shader == null) return;

            _instance = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material = _instance;
        }

        private void LateUpdate()
        {
            if (_instance == null) return;

            if (_lane != null)
                HandAuraGeometry.Collect(_lane.Cards, _quads);
            else if (_targets != null)
                HandAuraGeometry.Collect(_targets, rectTransform.parent as RectTransform, _quads);
            else
                return;

            if (_quads.Count == 0)
            {
                SetVerticesDirty();
                return;
            }

            var bounds = HandAuraGeometry.Bounds(_quads, width + lineOffset + wobble * 3f);
            var count = Mathf.Min(_quads.Count, Capacity);

            rectTransform.anchoredPosition = bounds.center;
            rectTransform.sizeDelta = bounds.size;

            for (var i = 0; i < count; i++)
            {
                var quad = _quads[i];
                var center = quad.Center - bounds.center;

                _rects[i] = new Vector4(center.x, center.y, quad.Half.x, quad.Half.y);
                _rotations[i] = new Vector4(quad.Cos, quad.Sin, 0f, 0f);
            }

            for (var i = count; i < Capacity; i++)
            {
                _rects[i] = Vector4.zero;
                _rotations[i] = new Vector4(1f, 0f, 0f, 0f);
            }

            _instance.SetVectorArray(RectsId, _rects);
            _instance.SetVectorArray(RotationsId, _rotations);
            _instance.SetInteger(CountId, count);
            _instance.SetColor(ColorId, halftoneColor);
            _instance.SetColor(LineColorId, inkColor);
            _instance.SetFloat(WidthId, width);
            _instance.SetFloat(CornerId, corner);
            _instance.SetFloat(DotCellId, dotCell);
            _instance.SetFloat(LineOffsetId, lineOffset);
            _instance.SetFloat(LineWidthId, lineWidth);
            _instance.SetFloat(WobbleId, wobble);
            _instance.SetFloat(UnderId, underlight);
            _instance.SetFloat(CutoutId, cutout);
            _instance.SetFloat(PulseId, Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount);
            _instance.SetFloat(DashPhaseId, Time.unscaledTime * dashSpeed);
            _instance.SetFloat(DashCountId, Mathf.Round(dashCount));
            _instance.SetVector(HalfSizeId, new Vector4(bounds.width * 0.5f, bounds.height * 0.5f, 0f, 0f));

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_quads.Count == 0 && !_warmup) return;

            var rect = _quads.Count > 0 ? GetPixelAdjustedRect() : new Rect(-1f, -1f, 2f, 2f);
            var tint = color;

            AddCorner(vh, new Vector2(rect.xMin, rect.yMin), tint);
            AddCorner(vh, new Vector2(rect.xMin, rect.yMax), tint);
            AddCorner(vh, new Vector2(rect.xMax, rect.yMax), tint);
            AddCorner(vh, new Vector2(rect.xMax, rect.yMin), tint);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private static void AddCorner(VertexHelper vh, Vector2 position, Color tint)
            => vh.AddVert(position, tint, position);

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_instance != null)
                DestroyImmediate(_instance);
        }
    }
}
