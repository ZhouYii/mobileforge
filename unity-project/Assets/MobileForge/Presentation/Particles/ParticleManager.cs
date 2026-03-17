using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Particle effect manager with pooling, deduplication, and LOD quality tiers.
    /// Pure C# — delegates spawning to engine layer.
    /// </summary>
    public class ParticleManager
    {
        private readonly Dictionary<string, Infrastructure.ObjectPool<object>> _pools = new();
        private readonly List<ActiveEffect> _active = new();
        private int _quality = 2;
        private int _maxActive = 32;

        private static readonly int[] MaxByQuality = { 0, 8, 32, 64 };

        /// <summary>Injected: show a particle effect. (effectId, x, y, instance) -> void</summary>
        public Action<string, float, float, object> OnSpawn { get; set; }

        /// <summary>Injected: return a particle to pool. (effectId, instance)</summary>
        public Action<string, object> OnDespawn { get; set; }

        public void SetQuality(int level)
        {
            _quality = Math.Clamp(level, 0, 3);
            _maxActive = MaxByQuality[_quality];
        }

        /// <summary>Register a particle effect factory.</summary>
        public void RegisterEffect(string effectId, Func<object> factory)
        {
            _pools[effectId] = new Infrastructure.ObjectPool<object>(factory, maxCapacity: 16);
        }

        /// <summary>Spawn a particle effect. Returns the instance or null.</summary>
        public object Spawn(string effectId, float x, float y, float duration = 1f)
        {
            if (_quality == 0 || _active.Count >= _maxActive) return null;
            if (!_pools.TryGetValue(effectId, out var pool)) return null;
            var instance = pool.Acquire();
            OnSpawn?.Invoke(effectId, x, y, instance);
            _active.Add(new ActiveEffect { EffectId = effectId, Instance = instance, Timer = duration });
            return instance;
        }

        /// <summary>Call each frame with delta.</summary>
        public void Update(float delta)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var tmp = _active[i];
                tmp.Timer -= delta;
                _active[i] = tmp;
                if (_active[i].Timer <= 0f)
                {
                    var entry = _active[i];
                    _active.RemoveAt(i);
                    if (_pools.TryGetValue(entry.EffectId, out var pool))
                        pool.Release(entry.Instance);
                    OnDespawn?.Invoke(entry.EffectId, entry.Instance);
                }
            }
        }

        private struct ActiveEffect
        {
            public string EffectId;
            public object Instance;
            public float Timer;
        }
    }
}
