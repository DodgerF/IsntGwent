using UnityEngine;

namespace IsntGwent.Scripts.Vfx
{
    [RequireComponent(typeof(ParticleSystem))]
    public class VfxParticleRope : MonoBehaviour
    {
        public int count = 22;
        public float size = 0.13f;
        public float sizeJitter = 0.02f;
        public float sizeAtFrom = 1f;
        public float sizeAtTo = 1f;
        public bool alignToPath = true;
        public float spread = 0f;
        public float alphaJitter = 0f;
        public float wobble = 0.06f;
        public float wobbleSpeed = 7f;
        public Color tint = Color.white;

        private ParticleSystem _system;
        private ParticleSystem.Particle[] _buffer;
        private float _unitScale = 1f;

        private Transform _fromAnchor;
        private Transform _toAnchor;
        private Vector3 _fromOffset;
        private Vector3 _toOffset;
        private float _bend;
        private float _sag;
        private float _phase;
        private bool _bound;

        public float Tension { get; set; }
        public float Alpha { get; set; } = 1f;

        private void Awake()
        {
            _system = GetComponent<ParticleSystem>();

            var emission = _system.emission;
            emission.enabled = false;

            var main = _system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;
        }

        public void Bind(Transform from, Vector3 fromOffset, Transform to, Vector3 toOffset,
            float bend, float sag, float unitScale)
        {
            _fromAnchor = from;
            _toAnchor = to;
            _fromOffset = fromOffset;
            _toOffset = toOffset;
            _bend = bend;
            _sag = sag;
            _unitScale = Mathf.Approximately(unitScale, 0f) ? 1f : unitScale;
            _phase = Random.value * 10f;
            _bound = true;
            Tension = 0f;

            if (!_system.isPlaying) _system.Play(false);

            Rebuild();
        }

        public void Release()
        {
            _bound = false;
            _system.Clear(false);
        }

        private void LateUpdate()
        {
            if (!_bound) return;

            Rebuild();
        }

        public void Rebuild()
        {
            if (_fromAnchor == null || _toAnchor == null) return;

            var total = Mathf.Max(2, count);
            if (_buffer == null || _buffer.Length < total)
                _buffer = new ParticleSystem.Particle[total];

            var a = ToLocalUnits(_fromAnchor.TransformPoint(_fromOffset));
            var b = ToLocalUnits(_toAnchor.TransformPoint(_toOffset));

            var span = b - a;
            var length = span.magnitude;
            if (length < 0.001f) return;

            var dir = span / length;
            var normal = new Vector2(-dir.y, dir.x);
            var slack = Mathf.Clamp01(1f - Tension);
            var mid = (a + b) * 0.5f
                      + normal * (_bend * slack / _unitScale)
                      + Vector2.down * (_sag * slack / _unitScale);
            var control = mid * 2f - (a + b) * 0.5f;

            var time = Time.time * wobbleSpeed + _phase;
            var color = tint;
            color.a *= Alpha;

            for (var i = 0; i < total; i++)
            {
                var t = total == 1 ? 0f : (float)i / (total - 1);
                var inv = 1f - t;
                var point = inv * inv * a + 2f * inv * t * control + t * t * b;

                var swing = Mathf.Sin(time + i * 0.9f) * wobble * slack * Mathf.Sin(Mathf.PI * t);
                point += normal * (swing + (Hash(i * 3 + 1) - 0.5f) * 2f * spread / _unitScale);
                point += dir * ((Hash(i * 5 + 2) - 0.5f) * spread / _unitScale);

                var tangent = 2f * inv * (control - a) + 2f * t * (b - control);
                var angle = alignToPath
                    ? Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg
                    : i * 47f + time * 6f;

                var faded = color;
                faded.a *= 1f - alphaJitter * Hash(i * 7 + 3);

                var particle = _buffer[i];
                particle.position = point;
                particle.startSize = size * Mathf.Lerp(sizeAtFrom, sizeAtTo, t)
                                     + (Hash(i * 11 + 4) - 0.5f) * 2f * sizeJitter;
                particle.startColor = (Color32)faded;
                particle.rotation = -angle;
                particle.remainingLifetime = 10f;
                particle.startLifetime = 10f;
                particle.velocity = Vector3.zero;
                _buffer[i] = particle;
            }

            _system.SetParticles(_buffer, total);
        }

        private static float Hash(int value)
        {
            var x = Mathf.Sin(value * 127.1f + 311.7f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }

        private Vector2 ToLocalUnits(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            return new Vector2(local.x, local.y) / _unitScale;
        }
    }
}
