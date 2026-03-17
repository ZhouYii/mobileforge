using System;
using System.Collections.Generic;
using MobileForge.Domain;
using MobileForge.Infrastructure;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Gacha pull screen. Shows available pools, handles pulls with pity tracking.
    /// Mirrors gacha_screen.gd.
    /// </summary>
    public class GachaScreen : IScreen
    {
        private GameData _gameData;
        private Economy _economy;
        private MonsterManager _monsterManager;
        private UIRouter _router;
        private readonly PityTracker _pityTracker = new();

        // Display state
        public int GemsBalance => _economy.GetBalance("gems");
        public List<GachaPoolDisplay> Pools { get; } = new();
        public List<GachaResultDisplay> LastResults { get; } = new();
        public string StatusMessage { get; private set; } = "";

        public void Setup(GameData gameData, Economy economy, MonsterManager monsterManager, UIRouter router)
        {
            _gameData = gameData;
            _economy = economy;
            _monsterManager = monsterManager;
            _router = router;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            RefreshPools();
        }

        public void OnPause() { }
        public void OnResume() { RefreshPools(); }
        public void OnExit() { }

        /// <summary>
        /// Rebuild the pool list from game data, computing displayed rates.
        /// </summary>
        public void RefreshPools()
        {
            Pools.Clear();
            var poolDefs = _gameData.GetAllDefinitions("gacha_pools");
            foreach (var def in poolDefs)
            {
                var poolData = def.Raw();
                var pool = new GachaPool(poolData);
                var rates = GachaRoller.GetDisplayedRates(pool);
                Pools.Add(new GachaPoolDisplay
                {
                    Pool = pool,
                    Rates = rates
                });
            }
        }

        /// <summary>
        /// Perform a gacha pull. Deducts currency, rolls results, creates monster instances.
        /// Returns true if the pull succeeded.
        /// </summary>
        public bool DoPull(GachaPool pool, int count)
        {
            int totalCost = pool.CostAmount * count;
            if (!_economy.CanAfford(pool.CostCurrency, totalCost))
            {
                StatusMessage = $"Not enough {pool.CostCurrency}! Need {totalCost}";
                return false;
            }

            _economy.Spend(pool.CostCurrency, totalCost);

            var rng = new Random();
            int pity = _pityTracker.GetPity(pool.Id);
            var results = GachaRoller.RollMulti(pool, count, pity, rng);

            LastResults.Clear();
            foreach (var result in results)
            {
                // Update pity
                if (result.IsPity || IsTopRarity(pool, result.Rarity))
                    _pityTracker.Reset(pool.Id);
                else
                    _pityTracker.Increment(pool.Id);

                // Create monster instance
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

            StatusMessage = $"Pulled {count}!";
            return true;
        }

        /// <summary>
        /// Navigate back to the previous screen.
        /// </summary>
        public void GoBack() => _router.Pop();

        private bool IsTopRarity(GachaPool pool, int rarity)
        {
            int max = 0;
            foreach (var e in pool.Entries)
            {
                if (e.Rarity > max)
                    max = e.Rarity;
            }
            return rarity == max;
        }
    }

    /// <summary>
    /// Display model for a gacha pool, including pre-computed rates.
    /// </summary>
    public class GachaPoolDisplay
    {
        public GachaPool Pool;
        public Dictionary<int, double> Rates;
    }

    /// <summary>
    /// Display model for a single gacha pull result.
    /// </summary>
    public class GachaResultDisplay
    {
        public string MonsterName;
        public int Rarity;
        public bool IsPity;
        public bool IsFeatured;
    }
}
