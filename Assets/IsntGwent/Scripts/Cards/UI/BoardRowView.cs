using System.Collections.Generic;
using DG.Tweening;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Vfx;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public enum RowHighlight { None, Choosable, Placement }

    [RequireComponent(typeof(Image))]
    public class BoardRowView : CardLaneView
    {
        [InjectOptional] private CardSelectionService _selectionService;
        [InjectOptional] private VfxService _vfx;

        private static readonly Color RowIdle = Color.clear;

        private const float RowGlowAlpha = 0.5f;
        private const float RowPlacementGlowAlpha = 0.85f;
        private const float RowPulseFloor = 0.55f;

        [SerializeField] private Vector2 cardSize = new(105.3136f, 156.434f);
        [SerializeField] private float slotOversize = 1.06f;
        [SerializeField] private float slotGap = 12f;
        [SerializeField] private float rowPadding = 6f;
        [SerializeField] private float slotIconScale = 0.45f;

        [SerializeField] private WeatherBadgeView weatherBadge;

        private Image _image;
        private Image _glow;

        private SlotView[] _slots;
        private readonly Transform[] _slotCards = new Transform[BoardConfig.SlotsPerRow];
        private readonly HashSet<int> _zoneSlots = new();
        private readonly HashSet<int> _choosableSlots = new();
        private readonly HashSet<int> _damageSlots = new();
        private readonly HashSet<int> _supportSlots = new();
        private int _placementSlot = -1;
        private bool _freeSlotsHighlighted;
        private RowHighlight _tone;
        private bool _hovered;

        private int _pendingSlot = -1;

        private GameObject _weatherVfx;
        private string _weatherVfxId;
        private Image _weatherTint;

        public bool OwnSide { get; private set; }

        public RowType BoardRow { get; private set; }

        public bool HasSlots => _slots != null;

        public void MarkAsBoardRow(bool ownSide, RowType boardRow)
        {
            OwnSide = ownSide;
            BoardRow = boardRow;
            BuildSlots();
            RefreshLayout();
        }

        protected override void Awake()
        {
            base.Awake();

            _image = GetComponent<Image>();
            _image.color = RowIdle;

            _glow = GlowSprite.CreateInner(transform, "RowGlow");
            GlowSprite.SetWidth(_glow, HighlightPalette.RowGlowWidth);

            var glowRt = (RectTransform)_glow.transform;
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.offsetMin = new Vector2(-HighlightPalette.RowGlowBleed, -HighlightPalette.RowGlowBleed);
            glowRt.offsetMax = new Vector2(HighlightPalette.RowGlowBleed, HighlightPalette.RowGlowBleed);
            glowRt.SetAsFirstSibling();
        }

        private void Start()
        {
            _selectionService?.HighlightRowTargets
                .Subscribe(scope => SetTone(IsInScope(scope) ? RowHighlight.Choosable : RowHighlight.None))
                .AddTo(this);

            _selectionService?.HighlightRowHover
                .Subscribe(row => SetHovered(row == this))
                .AddTo(this);

            _selectionService?.HighlightFreeSlots
                .Subscribe(side => HighlightFreeSlots(OwnSide == side))
                .AddTo(this);

            _selectionService?.HighlightCells
                .Subscribe(HighlightCells)
                .AddTo(this);

            _selectionService?.HighlightPlacementCell
                .Subscribe(HighlightPlacementCell)
                .AddTo(this);

            _selectionService?.HighlightTargets
                .Subscribe(pool =>
                {
                    ClearTargetSlots();
                    MarkSlots(_choosableSlots, pool);
                    RepaintSlots();
                })
                .AddTo(this);

            _selectionService?.HighlightPredicted
                .Subscribe(prediction =>
                {
                    _damageSlots.Clear();
                    _supportSlots.Clear();
                    MarkSlots(_damageSlots, prediction.Hostile);
                    MarkSlots(_supportSlots, prediction.Friendly);
                    RepaintSlots();
                })
                .AddTo(this);

            _selectionService?.ClearHighlights
                .Subscribe(_ =>
                {
                    _zoneSlots.Clear();
                    _choosableSlots.Clear();
                    _damageSlots.Clear();
                    _supportSlots.Clear();
                    _placementSlot = -1;
                    _hovered = false;
                    SetTone(RowHighlight.None);
                    HighlightFreeSlots(false);
                })
                .AddTo(this);
        }

        public void ShowWeather(string cardId)
        {
            var active = !string.IsNullOrEmpty(cardId);

            if (weatherBadge != null) weatherBadge.Show(cardId);

            ShowWeatherVfx(active ? cardId : null);
        }

        private void ShowWeatherVfx(string cardId)
        {
            if (_vfx == null || !Application.isPlaying) return;

            var id = cardId != null ? WeatherVfx.Row(_vfx, cardId) : null;
            if (id == _weatherVfxId) return;

            if (_weatherVfx != null)
            {
                _vfx.StopAndDespawn(_weatherVfx, CardAnimConfig.WeatherFadeDuration);
                _weatherVfx = null;
            }

            _weatherVfxId = id;

            Color? tint = null;
            if (id != null && _vfx.TryTint(id, out var color)) tint = color;
            PaintWeather(tint);

            if (id == null) return;

            _weatherVfx = _vfx.SpawnFitted(id, (RectTransform)transform, transform);
            if (_weatherVfx == null)
            {
                _weatherVfxId = null;
                return;
            }

            _weatherVfx.transform.SetSiblingIndex(WeatherSiblingIndex);
        }

        private int WeatherSiblingIndex => (_slots?.Length ?? 0) + (_weatherTint != null ? 1 : 0);

        private void PaintWeather(Color? tint)
        {
            if (tint == null)
            {
                if (_weatherTint == null) return;

                _weatherTint.DOKill();
                _weatherTint
                    .DOFade(0f, CardAnimConfig.WeatherTintDuration)
                    .OnComplete(() => _weatherTint.gameObject.SetActive(false));
                return;
            }

            if (_weatherTint == null) _weatherTint = BuildWeatherTint();

            var color = tint.Value;

            _weatherTint.DOKill();
            _weatherTint.gameObject.SetActive(true);
            _weatherTint.color = new Color(color.r, color.g, color.b, _weatherTint.color.a);
            _weatherTint.DOFade(CardAnimConfig.WeatherTintAlpha, CardAnimConfig.WeatherTintDuration);
        }

        private Image BuildWeatherTint()
        {
            var go = new GameObject("WeatherTint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.clear;

            return image;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_weatherVfx != null) _vfx?.Fit(_weatherVfx, (RectTransform)transform);
        }

        private void OnDisable()
        {
            if (_weatherTint != null)
            {
                _weatherTint.DOKill();
                _weatherTint.color = Color.clear;
                _weatherTint.gameObject.SetActive(false);
            }

            if (_weatherVfx == null) return;

            _vfx.StopAndDespawn(_weatherVfx, 0f);
            _weatherVfx = null;
            _weatherVfxId = null;
        }

        public void PlaceCard(GameObject card, int slotIndex)
        {
            _pendingSlot = _slots != null && BoardConfig.IsValidSlot(slotIndex) ? slotIndex : -1;

            AddCard(card);

            _pendingSlot = -1;
        }

        public RectTransform SlotTransform(int index)
        {
            if (_slots == null || !BoardConfig.IsValidSlot(index)) return null;
            if (index >= _slots.Length) return null;

            return (RectTransform)_slots[index].transform;
        }

        public bool IsSlotFree(int index)
        {
            if (!BoardConfig.IsValidSlot(index)) return false;

            PruneSlots();
            return _slotCards[index] == null;
        }

        protected override bool IsLayoutChild(Transform child)
            => child.GetComponent<SlotView>() == null
               && child.gameObject != _weatherVfx
               && (weatherBadge == null || child != weatherBadge.transform)
               && (_weatherTint == null || child != _weatherTint.transform);

        protected override void PlaceAttached(Transform card)
        {
            if (_pendingSlot < 0) return;

            ForgetPlacement(card);
            _slotCards[_pendingSlot] = card;
        }

        protected override void ForgetPlacement(Transform card)
        {
            for (var i = 0; i < _slotCards.Length; i++)
                if (_slotCards[i] == card)
                    _slotCards[i] = null;
        }

        protected override void ClearPlacements()
        {
            for (var i = 0; i < _slotCards.Length; i++)
                _slotCards[i] = null;
        }

        protected override void LayoutCards()
        {
            if (_slots == null)
            {
                base.LayoutCards();
                return;
            }

            PruneSlots();

            if (_rt == null)
                _rt = (RectTransform)transform;

            var frameSize = cardSize * Mathf.Max(slotOversize, 1f);
            var gap = Mathf.Max(slotGap, 0f);
            var spacing = SlotSpacing(frameSize.x + gap);
            var startX = -((_slots.Length - 1) * spacing) / 2f;
            var slotSize = new Vector2(spacing, Mathf.Max(_rt.rect.height, 1f));

            frameSize = Vector2.Min(frameSize, new Vector2(spacing - gap, slotSize.y - gap));

            FitWidth((_slots.Length - 1) * spacing + frameSize.x);

            for (var i = 0; i < _slots.Length; i++)
            {
                var position = new Vector3(startX + i * spacing, 0f, 0f);

                _slots[i].SetSize(slotSize, frameSize, slotIconScale);

                var slotRt = (RectTransform)_slots[i].transform;
                slotRt.anchoredPosition = position;

                var card = _slotCards[i];
                if (card == null) continue;

                var isFlight = card == _flightChild;

                MoveTo(
                    card,
                    position,
                    isFlight ? _flightDuration : CardAnimConfig.RowLayoutDuration,
                    isFlight ? CardAnimConfig.FlightEase : CardAnimConfig.RowLayoutEase);
            }
        }

        private void BuildSlots()
        {
            if (_slots != null) return;

            _slots = new SlotView[BoardConfig.SlotsPerRow];

            for (var i = 0; i < _slots.Length; i++)
            {
                var go = new GameObject($"Slot{i}", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.SetAsFirstSibling();

                var slot = go.AddComponent<SlotView>();
                slot.Setup(this, i, BoardRow);
                _slots[i] = slot;
            }
        }

        private void FitWidth(float span)
        {
            var width = span + Mathf.Max(rowPadding, 0f) * 2f;

            if (Mathf.Abs(_rt.rect.width - width) < 0.01f) return;

            _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private float SlotSpacing(float minSpacing)
        {
            var limit = maxWidth > 0f ? maxWidth : AvailableWidth();
            var spacing = Mathf.Max(cardSpacing, minSpacing);

            if (_slots.Length > 1 && (_slots.Length - 1) * spacing > limit)
                spacing = limit / (_slots.Length - 1);

            return spacing;
        }

        private bool IsInScope(CardSelectionService.RowScope scope)
        {
            if (scope == CardSelectionService.RowScope.Any) return true;

            return OwnSide == (scope == CardSelectionService.RowScope.Own);
        }

        private void SetTone(RowHighlight tone)
        {
            _tone = tone;

            if (tone == RowHighlight.None)
                _hovered = false;

            RepaintRow();
        }

        private void SetHovered(bool hovered)
        {
            if (_hovered == hovered) return;

            _hovered = hovered;
            RepaintRow();
        }

        private void RepaintRow()
        {
            var state = _hovered && _tone != RowHighlight.None ? RowHighlight.Placement : _tone;

            var light = state switch
            {
                RowHighlight.Choosable => HighlightPalette.LightChoosable,
                RowHighlight.Placement => HighlightPalette.LightPlacement,
                _ => HighlightPalette.Hidden,
            };

            var alpha = state switch
            {
                RowHighlight.Choosable => RowGlowAlpha,
                RowHighlight.Placement => RowPlacementGlowAlpha,
                _ => 0f,
            };

            PaintRow(HighlightPalette.WithAlpha(light, alpha), alpha);

            RepaintSlots();
        }

        private void PaintRow(Color target, float alpha)
        {
            if (_glow == null) return;

            _glow.DOKill();

            if (!Application.isPlaying)
            {
                _glow.color = target;
                return;
            }

            _glow.DOColor(target, CardAnimConfig.SlotHighlightDuration);

            if (alpha <= 0f) return;

            _glow
                .DOFade(alpha * RowPulseFloor, CardAnimConfig.SlotGlowPulseDuration)
                .SetDelay(CardAnimConfig.SlotHighlightDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void HighlightFreeSlots(bool active)
        {
            _freeSlotsHighlighted = active;
            RepaintSlots();
        }

        private void HighlightCells(IReadOnlyList<BoardCell> cells)
        {
            _zoneSlots.Clear();

            if (cells != null)
                foreach (var cell in cells)
                    if (cell.OwnSide == OwnSide && cell.Row == BoardRow && BoardConfig.IsValidSlot(cell.Index))
                        _zoneSlots.Add(cell.Index);

            RepaintSlots();
        }

        private void HighlightPlacementCell(BoardCell? cell)
        {
            _placementSlot = cell.HasValue
                             && cell.Value.OwnSide == OwnSide
                             && cell.Value.Row == BoardRow
                             && BoardConfig.IsValidSlot(cell.Value.Index)
                ? cell.Value.Index
                : -1;

            RepaintSlots();
        }

        private void RepaintSlots()
        {
            if (_slots == null) return;

            PruneSlots();

            for (var i = 0; i < _slots.Length; i++)
                _slots[i].SetHighlight(SlotState(i));
        }

        private SlotHighlight SlotState(int index)
        {
            if (_placementSlot == index)
                return SlotHighlight.Placement;

            if (_damageSlots.Contains(index))
                return SlotHighlight.Damage;

            if (_supportSlots.Contains(index))
                return SlotHighlight.Support;

            if (_choosableSlots.Contains(index) || _zoneSlots.Contains(index))
                return SlotHighlight.Zone;

            if (_freeSlotsHighlighted && _slotCards[index] == null)
                return SlotHighlight.Free;

            return SlotHighlight.None;
        }

        private void ClearTargetSlots()
        {
            _choosableSlots.Clear();
            _damageSlots.Clear();
            _supportSlots.Clear();
        }

        private void MarkSlots(HashSet<int> slots, IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0 || _slots == null) return;

            PruneSlots();

            for (var i = 0; i < _slotCards.Length; i++)
            {
                var id = CardIdAt(i);

                if (id != null && Contains(ids, id))
                    slots.Add(i);
            }
        }

        private string CardIdAt(int index)
        {
            var card = _slotCards[index];

            if (card == null) return null;
            if (!card.TryGetComponent<CardView>(out var view)) return null;
            if (view.Instance == null) return null;

            return view.Instance.Id.ToString();
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            for (var i = 0; i < ids.Count; i++)
                if (ids[i] == id)
                    return true;

            return false;
        }

        private void PruneSlots()
        {
            for (var i = 0; i < _slotCards.Length; i++)
                if (_slotCards[i] == null || _slotCards[i].parent != transform)
                    _slotCards[i] = null;
        }
    }
}
