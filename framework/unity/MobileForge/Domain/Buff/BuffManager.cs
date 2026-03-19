using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Per-entity buff manager. Tracks active buffs, handles stacking rules,
    /// tick-based expiry, and stat modifier aggregation.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class BuffManager
    {
        private readonly List<BuffInstance> _active = new();
        private readonly Dictionary<string, BuffDef> _defs = new();

        /// <summary>Fired when a new buff is applied.</summary>
        public event Action<BuffInstance> BuffApplied;
        /// <summary>Fired when a buff is removed (expired or manually).</summary>
        public event Action<BuffInstance> BuffRemoved;
        /// <summary>Fired when an existing buff gains stacks.</summary>
        public event Action<BuffInstance> BuffStacked;
        /// <summary>Fired when a buff is cleansed.</summary>
        public event Action<BuffInstance> BuffCleansed;

        /// <summary>Number of active buff instances.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>Load buff definitions for lookup.</summary>
        public void LoadDefs(IEnumerable<BuffDef> defs)
        {
            foreach (var def in defs)
                _defs[def.Id] = def;
        }

        /// <summary>
        /// Apply a buff using its definition. Stacking behaviour is determined by
        /// <see cref="BuffDef.StackRule"/>.
        /// </summary>
        public void ApplyBuff(BuffDef def, string sourceId, float value = 0f)
        {
            // Ensure the def is registered
            if (!_defs.ContainsKey(def.Id))
                _defs[def.Id] = def;

            var existing = _active.FirstOrDefault(b => b.DefId == def.Id);

            if (existing != null)
            {
                switch (def.StackRule)
                {
                    case BuffStackRule.Replace:
                        _active.Remove(existing);
                        BuffRemoved?.Invoke(existing);
                        AddNew(def, sourceId, value);
                        break;

                    case BuffStackRule.Extend:
                        existing.RemainingDuration += def.BaseDuration;
                        BuffStacked?.Invoke(existing);
                        break;

                    case BuffStackRule.Stack:
                        if (existing.Stacks < def.MaxStacks)
                        {
                            existing.Stacks++;
                            existing.RemainingDuration = def.BaseDuration;
                            BuffStacked?.Invoke(existing);
                        }
                        break;

                    case BuffStackRule.Highest:
                        if (value > existing.Value)
                        {
                            _active.Remove(existing);
                            BuffRemoved?.Invoke(existing);
                            AddNew(def, sourceId, value);
                        }
                        break;

                    case BuffStackRule.Refresh:
                        existing.RemainingDuration = def.BaseDuration;
                        BuffStacked?.Invoke(existing);
                        break;

                    default:
                        // Unknown rule — treat as replace
                        _active.Remove(existing);
                        BuffRemoved?.Invoke(existing);
                        AddNew(def, sourceId, value);
                        break;
                }
            }
            else
            {
                AddNew(def, sourceId, value);
            }
        }

        /// <summary>Remove the first instance of a buff by definition ID.</summary>
        public bool RemoveBuff(string defId)
        {
            var inst = _active.FirstOrDefault(b => b.DefId == defId);
            if (inst == null) return false;
            _active.Remove(inst);
            BuffRemoved?.Invoke(inst);
            return true;
        }

        /// <summary>Remove all instances of a buff by definition ID.</summary>
        public int RemoveAll(string defId)
        {
            var matches = _active.Where(b => b.DefId == defId).ToList();
            foreach (var inst in matches)
            {
                _active.Remove(inst);
                BuffRemoved?.Invoke(inst);
            }
            return matches.Count;
        }

        /// <summary>
        /// Cleanse buffs. If debuffsOnly is true, only removes buffs whose definition
        /// has IsDebuff set. Fires <see cref="BuffCleansed"/> for each removed buff.
        /// </summary>
        public int Cleanse(bool debuffsOnly = false)
        {
            List<BuffInstance> targets;

            if (debuffsOnly)
            {
                targets = _active.Where(b =>
                    _defs.TryGetValue(b.DefId, out var def) && def.IsDebuff).ToList();
            }
            else
            {
                targets = _active.Where(b =>
                    !_defs.TryGetValue(b.DefId, out var def) || def.Dispellable).ToList();
            }

            foreach (var inst in targets)
            {
                _active.Remove(inst);
                BuffCleansed?.Invoke(inst);
            }
            return targets.Count;
        }

        /// <summary>Tick at turn start — decrements duration on all active buffs and removes expired.</summary>
        public void TickTurnStart()
        {
            TickInternal();
        }

        /// <summary>Tick at turn end — decrements duration on all active buffs and removes expired.</summary>
        public void TickTurnEnd()
        {
            TickInternal();
        }

        /// <summary>
        /// Get the combined multiplicative stat modifier across all active buffs.
        /// Returns the product of all matching modifiers (1.0 = no change).
        /// </summary>
        public float GetStatModifier(string stat)
        {
            float product = 1f;
            foreach (var inst in _active)
            {
                if (_defs.TryGetValue(inst.DefId, out var def) &&
                    def.StatModifiers.TryGetValue(stat, out float mod))
                {
                    // Apply per-stack: multiply mod for each stack
                    for (int i = 0; i < inst.Stacks; i++)
                        product *= mod;
                }
            }
            return product;
        }

        /// <summary>
        /// Get the combined additive flat modifier across all active buffs.
        /// Returns the sum of all matching modifiers (0 = no change).
        /// </summary>
        public float GetFlatModifier(string stat)
        {
            float sum = 0f;
            foreach (var inst in _active)
            {
                if (_defs.TryGetValue(inst.DefId, out var def) &&
                    def.FlatModifiers.TryGetValue(stat, out float mod))
                {
                    sum += mod * inst.Stacks;
                }
            }
            return sum;
        }

        /// <summary>Check if a buff with the given definition ID is active.</summary>
        public bool HasBuff(string defId) => _active.Any(b => b.DefId == defId);

        /// <summary>Get all active buff instances matching a category.</summary>
        public List<BuffInstance> GetByCategory(string category)
        {
            return _active.Where(b =>
                _defs.TryGetValue(b.DefId, out var def) && def.Category == category).ToList();
        }

        /// <summary>Get all active buff instances.</summary>
        public List<BuffInstance> GetAllActive() => new(_active);

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            var buffsData = new List<object>();
            foreach (var inst in _active)
            {
                buffsData.Add(new Dictionary<string, object>
                {
                    ["def_id"] = inst.DefId,
                    ["source_id"] = inst.SourceId,
                    ["remaining_duration"] = inst.RemainingDuration,
                    ["stacks"] = inst.Stacks,
                    ["value"] = inst.Value,
                });
            }
            return new Dictionary<string, object>
            {
                ["buffs"] = buffsData,
            };
        }

        public void FromSaveDict(Dictionary<string, object> data)
        {
            _active.Clear();

            if (data.TryGetValue("buffs", out var bObj) && bObj is List<object> bList)
            {
                foreach (var item in bList)
                {
                    if (item is Dictionary<string, object> entry)
                    {
                        _active.Add(new BuffInstance
                        {
                            DefId = entry.GetValueOrDefault("def_id")?.ToString() ?? "",
                            SourceId = entry.GetValueOrDefault("source_id")?.ToString() ?? "",
                            RemainingDuration = Convert.ToInt32(entry.GetValueOrDefault("remaining_duration", 0)),
                            Stacks = Convert.ToInt32(entry.GetValueOrDefault("stacks", 1)),
                            Value = Convert.ToSingle(entry.GetValueOrDefault("value", 0f)),
                        });
                    }
                }
            }
        }

        // ── Private helpers ──

        private void AddNew(BuffDef def, string sourceId, float value)
        {
            var inst = new BuffInstance
            {
                DefId = def.Id,
                SourceId = sourceId,
                RemainingDuration = def.BaseDuration,
                Stacks = 1,
                Value = value,
            };
            _active.Add(inst);
            BuffApplied?.Invoke(inst);
        }

        private void TickInternal()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].RemainingDuration--;
                if (_active[i].IsExpired)
                {
                    var expired = _active[i];
                    _active.RemoveAt(i);
                    BuffRemoved?.Invoke(expired);
                }
            }
        }
    }
}
