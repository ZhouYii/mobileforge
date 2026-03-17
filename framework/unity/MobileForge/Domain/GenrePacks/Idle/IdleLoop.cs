using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Tick-based idle/incremental game loop.
    /// Handles resource accumulation, offline earnings, and prestige.
    /// </summary>
    public class IdleLoop : IGameLoop
    {
        private readonly Dictionary<string, double> _resources = new();
        private readonly List<Dictionary<string, object>> _generators = new();
        private readonly Action<string, Dictionary<string, object>> _emitEvent;
        private bool _isActive;
        private double _totalTime;
        private int _prestigeCount;
        private double _prestigeMultiplier = 1.0;

        public bool IsActive => _isActive;

        public IdleLoop(Action<string, Dictionary<string, object>> emitEvent = null)
        {
            _emitEvent = emitEvent;
        }

        public void Start(Dictionary<string, object> config)
        {
            _resources.Clear();
            if (config.TryGetValue("initial_resources", out var res) && res is Dictionary<string, object> initRes)
                foreach (var kvp in initRes)
                    _resources[kvp.Key] = Convert.ToDouble(kvp.Value);

            _generators.Clear();
            if (config.TryGetValue("generators", out var gens) && gens is List<Dictionary<string, object>> genList)
                foreach (var g in genList)
                    _generators.Add(new Dictionary<string, object>(g));

            _isActive = true;
            _totalTime = 0;

            if (config.TryGetValue("offline_seconds", out var off))
            {
                double offlineSec = Convert.ToDouble(off);
                if (offlineSec > 0) ApplyEarnings(offlineSec);
            }

            _emitEvent?.Invoke("idle_started", new Dictionary<string, object>());
        }

        public PhaseResult ProcessInput(Dictionary<string, object> input)
        {
            var action = input.TryGetValue("action", out var a) ? a as string ?? "" : "";
            var result = new PhaseResult { PhaseName = action, Completed = true };

            switch (action)
            {
                case "buy_generator":
                case "upgrade_generator":
                    var genId = input.TryGetValue("generator_id", out var gid) ? gid as string ?? "" : "";
                    result.Data = BuyGenerator(genId);
                    break;
                case "prestige":
                    result.Data = Prestige();
                    break;
                default:
                    result.Completed = false;
                    break;
            }
            return result;
        }

        public PhaseResult Tick(float delta)
        {
            if (!_isActive) return new PhaseResult { PhaseName = "idle", Completed = false };
            ApplyEarnings(delta);
            _totalTime += delta;
            return new PhaseResult
            {
                PhaseName = "tick",
                Completed = true,
                Data = new Dictionary<string, object> { ["resources"] = new Dictionary<string, double>(_resources) },
            };
        }

        public GameLoopState GetState()
        {
            return new GameLoopState
            {
                Phase = _isActive ? "running" : "inactive",
                IsActive = _isActive,
                Custom = new Dictionary<string, object>
                {
                    ["prestige_count"] = _prestigeCount,
                    ["prestige_multiplier"] = _prestigeMultiplier,
                    ["total_time"] = _totalTime,
                },
            };
        }

        private void ApplyEarnings(double seconds)
        {
            foreach (var gen in _generators)
            {
                var resource = gen.TryGetValue("resource", out var r) ? r as string ?? "" : "";
                var ratePer = gen.TryGetValue("rate_per_sec", out var rps) ? Convert.ToDouble(rps) : 0;
                var level = gen.TryGetValue("level", out var lv) ? Convert.ToInt32(lv) : 0;
                if (resource != "" && ratePer > 0 && level > 0)
                {
                    double earned = ratePer * level * seconds * _prestigeMultiplier;
                    _resources[resource] = (_resources.TryGetValue(resource, out var cur) ? cur : 0) + earned;
                }
            }
        }

        private Dictionary<string, object> BuyGenerator(string genId)
        {
            foreach (var gen in _generators)
            {
                var id = gen.TryGetValue("id", out var gid) ? gid as string ?? "" : "";
                if (id != genId) continue;
                var costBase = gen.TryGetValue("cost_base", out var cb) ? Convert.ToDouble(cb) : 10;
                var costGrowth = gen.TryGetValue("cost_growth", out var cg) ? Convert.ToDouble(cg) : 1.15;
                var level = gen.TryGetValue("level", out var lv) ? Convert.ToInt32(lv) : 0;
                var cost = costBase * Math.Pow(costGrowth, level);
                var currency = gen.TryGetValue("cost_resource", out var cr) ? cr as string ?? "gold" : "gold";
                var balance = _resources.TryGetValue(currency, out var bal) ? bal : 0;
                if (balance >= cost)
                {
                    _resources[currency] = balance - cost;
                    gen["level"] = level + 1;
                    return new Dictionary<string, object> { ["success"] = true, ["new_level"] = level + 1 };
                }
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "not_enough_" + currency };
            }
            return new Dictionary<string, object> { ["success"] = false, ["error"] = "generator_not_found" };
        }

        private Dictionary<string, object> Prestige()
        {
            _prestigeCount++;
            _prestigeMultiplier = 1.0 + _prestigeCount * 0.1;
            foreach (var key in new List<string>(_resources.Keys))
                _resources[key] = 0;
            foreach (var gen in _generators)
                gen["level"] = 0;
            _emitEvent?.Invoke("prestige", new Dictionary<string, object>
            {
                ["count"] = _prestigeCount, ["multiplier"] = _prestigeMultiplier,
            });
            return new Dictionary<string, object>
            {
                ["prestige_count"] = _prestigeCount, ["multiplier"] = _prestigeMultiplier,
            };
        }
    }
}
