using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Cards.UI
{
    public class HandAuraMotes : MaskableGraphic
    {
        private const int TextureSize = 32;
        private const int SpawnAttempts = 8;

        private static Texture2D _dot;

        public int maxMotes = 24;
        public float spawnInterval = 0.12f;
        public float lifeMin = 1.7f;
        public float lifeMax = 3.1f;
        public float sizeMin = 4.5f;
        public float sizeMax = 11f;
        public float rise = 30f;
        public float sway = 12f;
        public float swaySpeed = 1.3f;
        public Color moteColor = new(0.96f, 0.88f, 0.66f, 0.62f);

        private struct Mote
        {
            public Vector2 Position;
            public float Age;
            public float Life;
            public float Size;
            public float Rise;
            public float Phase;
        }

        private Mote[] _motes;
        private int _count;
        private float _spawnTimer;

        private HandAuraGraphic _aura;

        public static Texture2D Dot => _dot != null ? _dot : _dot = BuildDot();

        public override Texture mainTexture => Dot;

        public void Bind(HandAuraGraphic aura) => _aura = aura;

        protected override void Awake()
        {
            base.Awake();

            raycastTarget = false;
            _motes = new Mote[Mathf.Max(maxMotes, 1)];
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _count = 0;
            _spawnTimer = 0f;
        }

        private void LateUpdate()
        {
            if (_aura == null) return;

            var delta = Time.unscaledDeltaTime;

            Advance(delta);
            Spawn(delta);
            SetVerticesDirty();
        }

        private void Advance(float delta)
        {
            for (var i = _count - 1; i >= 0; i--)
            {
                var mote = _motes[i];

                mote.Age += delta;

                if (mote.Age >= mote.Life)
                {
                    _motes[i] = _motes[_count - 1];
                    _count--;
                    continue;
                }

                mote.Position.y += mote.Rise * delta;
                mote.Position.x += Mathf.Sin(mote.Phase + mote.Age * swaySpeed) * sway * delta;

                _motes[i] = mote;
            }
        }

        private void Spawn(float delta)
        {
            if (_motes.Length != Mathf.Max(maxMotes, 1))
                System.Array.Resize(ref _motes, Mathf.Max(maxMotes, 1));

            _spawnTimer -= delta;

            if (_spawnTimer > 0f || _count >= _motes.Length) return;

            _spawnTimer = spawnInterval * Random.Range(0.6f, 1.4f);

            if (!TryFindPoint(out var point)) return;

            _motes[_count++] = new Mote
            {
                Position = point,
                Age = 0f,
                Life = Random.Range(lifeMin, lifeMax),
                Size = Random.Range(sizeMin, sizeMax),
                Rise = rise * Random.Range(0.7f, 1.3f),
                Phase = Random.Range(0f, Mathf.PI * 2f)
            };
        }

        private bool TryFindPoint(out Vector2 point)
        {
            point = Vector2.zero;

            var quads = _aura.Quads;
            if (quads.Count == 0) return false;

            var band = _aura.width;
            var bounds = HandAuraGeometry.Bounds(quads, band);

            for (var attempt = 0; attempt < SpawnAttempts; attempt++)
            {
                var candidate = new Vector2(
                    Random.Range(bounds.xMin, bounds.xMax),
                    Random.Range(bounds.yMin, bounds.yMax));

                var distance = HandAuraGeometry.Distance(quads, candidate, _aura.Corner);

                if (distance < band * 0.15f || distance > band * 0.85f) continue;

                point = candidate;
                return true;
            }

            return false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            for (var i = 0; i < _count; i++)
            {
                var mote = _motes[i];
                var progress = Mathf.Clamp01(mote.Age / mote.Life);
                var fade = Mathf.Min(progress / 0.25f, (1f - progress) / 0.45f);

                var tint = moteColor;
                tint.a *= Mathf.Clamp01(fade);

                if (tint.a <= 0.001f) continue;

                var half = mote.Size * (1f - progress * 0.35f) * 0.5f;
                var origin = vh.currentVertCount;

                vh.AddVert(mote.Position + new Vector2(-half, -half), tint, new Vector2(0f, 0f));
                vh.AddVert(mote.Position + new Vector2(-half, half), tint, new Vector2(0f, 1f));
                vh.AddVert(mote.Position + new Vector2(half, half), tint, new Vector2(1f, 1f));
                vh.AddVert(mote.Position + new Vector2(half, -half), tint, new Vector2(1f, 0f));

                vh.AddTriangle(origin, origin + 1, origin + 2);
                vh.AddTriangle(origin + 2, origin + 3, origin);
            }
        }

        private static Texture2D BuildDot()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[TextureSize * TextureSize];
            var center = (TextureSize - 1) * 0.5f;

            for (var y = 0; y < TextureSize; y++)
            for (var x = 0; x < TextureSize; x++)
            {
                var offset = new Vector2(x - center, y - center) / center;
                var alpha = Mathf.Pow(Mathf.Clamp01(1f - offset.magnitude), 1.7f);

                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }
}
