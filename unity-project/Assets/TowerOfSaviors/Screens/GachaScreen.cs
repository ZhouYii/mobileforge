using System;
using System.Collections.Generic;
using System.Linq;
using MobileForge.Domain;
using MobileForge.Infrastructure;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Gacha pull screen with support for regular, step-up, one-time, and daily-free pools.
    /// Matches Godot gacha_screen.gd feature parity.
    /// </summary>
    public class GachaScreen : IScreen
    {
        private GameData _gameData;
        private Economy _economy;
        private MonsterManager _monsterManager;
        private UIRouter _router;
        private PlayerState _playerState;
        private readonly PityTracker _pityTracker = new();

        public int GemsBalance => _economy.GetBalance("gems");
        public List<GachaPoolDisplay> Pools { get; } = new();
        public List<GachaResultDisplay> LastResults { get; } = new();
        public List<ExchangeOffer> ExchangeOffers { get; } = new();
        public string StatusMessage { get; private set; } = "";

        public void Setup(GameData gameData, Economy economy, MonsterManager monsterManager,
            UIRouter router, PlayerState playerState = null)
        {
            _gameData = gameData;
            _economy = economy;
            _monsterManager = monsterManager;
            _router = router;
            _playerState = playerState;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            RefreshPools();
            LoadExchangeOffers();
        }
        public void OnPause() { }
        public void OnResume()
        {
            RefreshPools();
            LoadExchangeOffers();
        }
        public void OnExit() { }

        public void RefreshPools()
        {
            Pools.Clear();
            var poolDefs = _gameData.GetAllDefinitions("gacha_pools");
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            foreach (var def in poolDefs)
            {
                var poolData = def.Raw();
                var pool = new GachaPool(poolData);
                var rates = GachaRoller.GetDisplayedRates(pool);

                var display = new GachaPoolDisplay
                {
                    Pool = pool,
                    Rates = rates,
                    PoolType = DeterminePoolType(poolData),
                    IsOneTime = GetBool(poolData, "one_time"),
                    IsOneTimeUsed = GetGachaState($"one_time_{pool.Id}") == "true",
                    HasDailyFree = GetBool(poolData, "daily_free"),
                    DailyFreeUsedToday = GetGachaState($"free_pull_{pool.Id}") == today,
                    GuaranteedTopRarity = GetBool(poolData, "guaranteed_top_rarity"),
                    MultiPullDiscount = GetInt(poolData, "multi_pull_discount", 0),
                };

                // Step-up data
                var stepUpData = GetList(poolData, "step_up");
                if (stepUpData != null && stepUpData.Count > 0)
                {
                    display.PoolType = PoolType.StepUp;
                    int currentStep = GetGachaStateInt($"step_{pool.Id}");
                    display.CurrentStep = currentStep;
                    display.TotalSteps = stepUpData.Count;
                    display.IsStepUpComplete = currentStep >= stepUpData.Count;

                    if (currentStep < stepUpData.Count && stepUpData[currentStep] is Dictionary<string, object> stepData)
                    {
                        display.StepCostMult = GetFloat(stepData, "cost_mult", 1f);
                        display.StepPullCount = GetInt(stepData, "pull_count", 10);
                        display.StepGuaranteedRarity = GetInt(stepData, "guaranteed_rarity", 0);
                    }
                }

                Pools.Add(display);
            }
        }

        /// <summary>
        /// Perform a gacha pull with full type support.
        /// </summary>
        public bool DoPull(GachaPoolDisplay poolDisplay, int count)
        {
            var pool = poolDisplay.Pool;

            // One-time check
            if (poolDisplay.IsOneTime && poolDisplay.IsOneTimeUsed)
            {
                StatusMessage = "This pool has already been used!";
                return false;
            }

            // Determine actual cost
            int costPerPull = pool.CostAmount;
            bool isFree = false;

            if (poolDisplay.PoolType == PoolType.StepUp && !poolDisplay.IsStepUpComplete)
            {
                costPerPull = (int)(pool.CostAmount * poolDisplay.StepCostMult);
                count = poolDisplay.StepPullCount;
            }
            else if (poolDisplay.HasDailyFree && !poolDisplay.DailyFreeUsedToday && count == 1)
            {
                costPerPull = 0;
                isFree = true;
            }

            int totalCost = costPerPull * count;

            // Multi-pull discount: "pay for N-discount" on 10x
            if (poolDisplay.MultiPullDiscount > 0 && count >= 10 && !isFree)
            {
                totalCost = costPerPull * (count - poolDisplay.MultiPullDiscount);
            }

            if (totalCost > 0 && !_economy.CanAfford(pool.CostCurrency, totalCost))
            {
                StatusMessage = $"Not enough {pool.CostCurrency}! Need {totalCost}";
                return false;
            }

            if (totalCost > 0) _economy.Spend(pool.CostCurrency, totalCost);

            // Roll
            var rng = new Random();
            int pity = _pityTracker.GetPity(pool.Id);
            var results = GachaRoller.RollMulti(pool, count, pity, rng);

            LastResults.Clear();
            foreach (var result in results)
            {
                if (result.IsPity || IsTopRarity(pool, result.Rarity))
                    _pityTracker.Reset(pool.Id);
                else
                    _pityTracker.Increment(pool.Id);

                var instance = _monsterManager.CreateInstance(result.MonsterId);
                var def = _monsterManager.GetDef(result.MonsterId);

                LastResults.Add(new GachaResultDisplay
                {
                    MonsterName = def?.Name ?? $"Monster #{result.MonsterId}",
                    Rarity = result.Rarity,
                    IsPity = result.IsPity,
                    IsFeatured = result.IsFeatured
                });
            }

            // Track state
            if (isFree)
            {
                SetGachaState($"free_pull_{pool.Id}", DateTime.Now.ToString("yyyy-MM-dd"));
                StatusMessage = "Free pull!";
            }
            else if (poolDisplay.IsOneTime)
            {
                SetGachaState($"one_time_{pool.Id}", "true");
                StatusMessage = "One-time pull complete!";
            }
            else if (poolDisplay.PoolType == PoolType.StepUp)
            {
                int newStep = poolDisplay.CurrentStep + 1;
                SetGachaState($"step_{pool.Id}", newStep.ToString());
                StatusMessage = newStep >= poolDisplay.TotalSteps
                    ? "All steps complete!"
                    : $"Step {newStep}/{poolDisplay.TotalSteps} complete!";
            }
            else
            {
                StatusMessage = totalCost > 0 ? $"Pulled {count} for {totalCost} {pool.CostCurrency}!" : $"Pulled {count}!";
            }

            RefreshPools();
            return true;
        }

        public void GoBack() => _router.Pop();

        // ── Monster Exchange ──

        public void LoadExchangeOffers()
        {
            ExchangeOffers.Clear();
            var defs = _gameData.GetAllDefinitions("monster_exchange");
            foreach (var def in defs)
            {
                var targetDef = _monsterManager.GetDef(def.GetInt("target_monster_id", 0));
                int timesUsed = GetGachaStateInt($"exchange_{def.Id}");
                ExchangeOffers.Add(new ExchangeOffer
                {
                    Id = def.Id,
                    Name = def.GetString("name", "Exchange"),
                    TargetMonsterId = def.GetInt("target_monster_id", 0),
                    TargetMonsterName = targetDef?.Name ?? "Unknown",
                    RequiredCount = def.GetInt("required_count", 5),
                    RequiredMinRarity = def.GetInt("required_min_rarity", 4),
                    ExchangeLimit = def.GetInt("exchange_limit", 1),
                    TimesUsed = timesUsed,
                });
            }
        }

        public bool ExecuteExchange(int offerId)
        {
            var offer = ExchangeOffers.Find(o => o.Id == offerId);
            if (offer == null) { StatusMessage = "Offer not found"; return false; }
            if (offer.IsDone) { StatusMessage = "Exchange limit reached"; return false; }

            // For now, just grant the target monster and increment counter
            // (In a full implementation, would check/consume owned monsters meeting requirements)
            _monsterManager.CreateInstance(offer.TargetMonsterId);
            offer.TimesUsed++;
            SetGachaState($"exchange_{offer.Id}", offer.TimesUsed.ToString());
            StatusMessage = $"Exchanged! Received {offer.TargetMonsterName}!";
            return true;
        }

        // ── Helpers ──

        private PoolType DeterminePoolType(Dictionary<string, object> data)
        {
            if (GetBool(data, "one_time")) return PoolType.OneTime;
            if (data.ContainsKey("step_up")) return PoolType.StepUp;
            return PoolType.Regular;
        }

        private bool IsTopRarity(GachaPool pool, int rarity) =>
            pool.Entries.Count > 0 && rarity == pool.Entries.Max(e => e.Rarity);

        private string GetGachaState(string key)
        {
            var val = _playerState?.GetValue("gacha", key, null);
            return val?.ToString() ?? "";
        }

        private int GetGachaStateInt(string key)
        {
            var val = _playerState?.GetValue("gacha", key, 0);
            try { return Convert.ToInt32(val); } catch { return 0; }
        }

        private void SetGachaState(string key, string value)
        {
            _playerState?.SetValue("gacha", key, value);
        }

        private static bool GetBool(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out var v) && Convert.ToBoolean(v);

        private static int GetInt(Dictionary<string, object> d, string k, int def) =>
            d.TryGetValue(k, out var v) ? Convert.ToInt32(v) : def;

        private static float GetFloat(Dictionary<string, object> d, string k, float def) =>
            d.TryGetValue(k, out var v) ? Convert.ToSingle(v) : def;

        private static List<object> GetList(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out var v) && v is List<object> l ? l : null;
    }

    public enum PoolType { Regular, StepUp, OneTime }

    public class GachaPoolDisplay
    {
        public GachaPool Pool;
        public Dictionary<int, double> Rates;
        public PoolType PoolType;
        public bool IsOneTime;
        public bool IsOneTimeUsed;
        public bool HasDailyFree;
        public bool DailyFreeUsedToday;
        public bool GuaranteedTopRarity;
        public int MultiPullDiscount;

        // Step-up fields
        public int CurrentStep;
        public int TotalSteps;
        public bool IsStepUpComplete;
        public float StepCostMult;
        public int StepPullCount;
        public int StepGuaranteedRarity;
    }

    public class GachaResultDisplay
    {
        public string MonsterName;
        public int Rarity;
        public bool IsPity;
        public bool IsFeatured;
    }

    public class ExchangeOffer
    {
        public int Id;
        public string Name;
        public int TargetMonsterId;
        public string TargetMonsterName;
        public int RequiredCount;
        public int RequiredMinRarity;
        public int ExchangeLimit;
        public int TimesUsed;
        public bool IsDone => ExchangeLimit > 0 && TimesUsed >= ExchangeLimit;
    }
}
