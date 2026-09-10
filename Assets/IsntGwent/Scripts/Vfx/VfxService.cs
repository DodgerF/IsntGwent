using System.Collections.Generic;
using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;

namespace IsntGwent.Scripts.Vfx
{
    public class VfxService
    {
        private readonly VfxDatabase _database;
        private readonly VfxLayer _layer;

        private readonly Dictionary<string, Stack<GameObject>> _pool = new();
        private readonly Dictionary<GameObject, string> _origin = new();
        private readonly Dictionary<GameObject, ParticleSystem[]> _systems = new();

        public VfxService(VfxDatabase database, VfxLayer layer)
        {
            _database = database;
            _layer = layer;
        }

        public void Play(string id, Vector3 position)
        {
            var instance = Spawn(id);
            if (instance == null) return;

            instance.transform.position = position;
            Despawn(instance, _database.Get(id).Life);
        }

        public void PlayFitted(string id, RectTransform target)
        {
            var instance = SpawnFitted(id, target);
            if (instance == null) return;

            Despawn(instance, _database.Get(id).Life);
        }

        public GameObject SpawnFitted(string id, RectTransform target, Transform parent = null)
        {
            if (target == null) return null;

            var instance = Spawn(id, parent);
            if (instance == null) return null;

            Fit(instance, target);
            return instance;
        }

        public void Fit(GameObject instance, RectTransform target)
        {
            if (instance == null || target == null) return;

            instance.transform.position = target.position;
            Resize(instance, Vector2.Scale(target.rect.size, target.lossyScale));
        }

        public bool Has(string id) => _database.Get(id)?.LoadedPrefab != null;

        public bool TryTint(string id, out Color tint)
        {
            var entry = _database.Get(id);
            tint = entry != null ? entry.TintColor : Color.white;

            return entry is { HasTint: true };
        }

        public string Resolve(string preferred, string fallback) => Has(preferred) ? preferred : fallback;

        public void PlayBeam(string id, Vector3 from, Vector3 to)
        {
            var instance = Spawn(id);
            if (instance == null) return;

            var delta = to - from;

            instance.transform.position = from + delta * 0.5f;
            instance.transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            Stretch(instance, delta.magnitude);
            Despawn(instance, _database.Get(id).Life);
        }

        public GameObject Spawn(string id, Transform parent = null)
        {
            var entry = _database.Get(id);
            if (entry?.LoadedPrefab == null) return null;

            var root = parent != null ? parent : _layer.Root;

            GameObject instance;
            if (_pool.TryGetValue(id, out var stack) && stack.Count > 0)
            {
                instance = stack.Pop();
                instance.SetActive(true);
            }
            else
            {
                instance = Object.Instantiate(entry.LoadedPrefab, root);
            }

            if (instance.transform.parent != root)
                instance.transform.SetParent(root, false);

            _origin[instance] = id;
            Apply(instance, entry);
            return instance;
        }

        public void StopAndDespawn(GameObject instance, float tail)
        {
            if (instance == null) return;

            foreach (var system in SystemsOf(instance))
                system.Stop(false, ParticleSystemStopBehavior.StopEmitting);

            Despawn(instance, tail);
        }

        public void Despawn(GameObject instance, float delay)
        {
            if (instance == null) return;

            DOVirtual.DelayedCall(delay, () => Release(instance), false);
        }

        private void Release(GameObject instance)
        {
            if (instance == null) return;

            foreach (var system in SystemsOf(instance))
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            instance.SetActive(false);
            instance.transform.SetParent(_layer.Root, false);

            if (!_origin.TryGetValue(instance, out var id)) return;

            if (!_pool.TryGetValue(id, out var stack))
            {
                stack = new Stack<GameObject>();
                _pool[id] = stack;
            }

            stack.Push(instance);
        }

        private void Apply(GameObject instance, VfxEntryDefinition entry)
        {
            var particle = instance.GetComponent<UIParticle>();
            if (particle != null)
            {
                particle.RefreshParticles();
                particle.scale = entry.BaseScale * entry.Scale;
            }

            instance.transform.rotation = Quaternion.identity;

            var systems = SystemsOf(instance);

            for (var i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;

                if (entry.HasTint && entry.BaseColors != null && i < entry.BaseColors.Length)
                    main.startColor = Tinted(entry.BaseColors[i], entry.TintColor);

                if (entry.BaseShapes != null && i < entry.BaseShapes.Length)
                {
                    var shape = systems[i].shape;
                    if (shape.enabled) shape.scale = entry.BaseShapes[i];
                }
            }

            foreach (var system in systems)
            {
                system.Clear(false);
                system.Play(false);
            }
        }

        private void Resize(GameObject instance, Vector2 sizeInPixels)
        {
            var scale = ScaleOf(instance);
            if (scale <= 0f) return;

            foreach (var system in SystemsOf(instance))
            {
                var shape = system.shape;
                if (!shape.enabled || !IsBox(shape.shapeType)) continue;

                shape.scale = new Vector3(sizeInPixels.x / scale, sizeInPixels.y / scale, shape.scale.z);
            }
        }

        private void Stretch(GameObject instance, float lengthInPixels)
        {
            var scale = ScaleOf(instance);
            if (scale <= 0f) return;

            foreach (var system in SystemsOf(instance))
            {
                var shape = system.shape;
                if (!shape.enabled || !IsBox(shape.shapeType)) continue;

                var current = shape.scale;
                shape.scale = new Vector3(lengthInPixels / scale, current.y, current.z);
            }
        }

        private float ScaleOf(GameObject instance)
        {
            var particle = instance.GetComponent<UIParticle>();
            return particle != null ? particle.scale : 1f;
        }

        private static bool IsBox(ParticleSystemShapeType type)
        {
            return type == ParticleSystemShapeType.Box
                   || type == ParticleSystemShapeType.BoxEdge
                   || type == ParticleSystemShapeType.BoxShell;
        }

        private ParticleSystem[] SystemsOf(GameObject instance)
        {
            if (_systems.TryGetValue(instance, out var cached)) return cached;

            var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            _systems[instance] = systems;
            return systems;
        }

        private static ParticleSystem.MinMaxGradient Tinted(ParticleSystem.MinMaxGradient source, Color tint)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(source.color * tint);
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(source.colorMin * tint, source.colorMax * tint);
                default:
                    return source;
            }
        }
    }
}
