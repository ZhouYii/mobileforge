using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Generalized shop system. Multi-section, rotating inventory, timed bundles.
    /// </summary>
    public class Shop
    {
        private readonly RewardPipeline _rewardPipeline;
        private readonly Action<string, Dictionary<string, object>> _emitEvent;
        private readonly Dictionary<string, int> _purchaseCounts = new();

        /// <summary>Injected: check if player can afford. (currencyId, amount) -> bool</summary>
        public Func<string, int, bool> CanAfford { get; set; }

        /// <summary>Injected: spend currency. (currencyId, amount)</summary>
        public Action<string, int> Spend { get; set; }

        /// <summary>Injected: get current unix timestamp.</summary>
        public Func<long> GetCurrentTime { get; set; }

        public Shop(RewardPipeline rewardPipeline = null,
            Action<string, Dictionary<string, object>> emitEvent = null)
        {
            _rewardPipeline = rewardPipeline;
            _emitEvent = emitEvent;
        }

        /// <summary>Purchase an item from a section.</summary>
        public Dictionary<string, object> Purchase(ShopSection section, string itemId)
        {
            ShopItem item = null;
            foreach (var si in section.Items)
                if (si.Id == itemId) { item = si; break; }
            if (item == null)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "item_not_found" };

            if (item.AvailableUntil > 0 && GetCurrentTime != null && GetCurrentTime() > item.AvailableUntil)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "item_expired" };

            if (item.BuyLimit > 0)
            {
                var key = $"{section.Id}_{itemId}";
                var bought = _purchaseCounts.TryGetValue(key, out var c) ? c : 0;
                if (bought >= item.BuyLimit)
                    return new Dictionary<string, object> { ["success"] = false, ["error"] = "limit_reached" };
            }

            if (item.CostType == "currency")
            {
                if (CanAfford == null || !CanAfford(item.CostCurrency, item.CostAmount))
                    return new Dictionary<string, object> { ["success"] = false, ["error"] = "insufficient_funds" };
                Spend?.Invoke(item.CostCurrency, item.CostAmount);
            }
            else if (item.CostType == "iap")
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false, ["error"] = "iap_requires_platform",
                    ["iap_product_id"] = item.IapProductId,
                };
            }

            List<Dictionary<string, object>> granted = null;
            if (_rewardPipeline != null && item.Rewards.Count > 0)
                granted = _rewardPipeline.Grant(item.Rewards, "shop:" + section.Id);

            var countKey = $"{section.Id}_{itemId}";
            _purchaseCounts[countKey] = (_purchaseCounts.TryGetValue(countKey, out var cnt) ? cnt : 0) + 1;

            _emitEvent?.Invoke("shop_purchase", new Dictionary<string, object>
            {
                ["section"] = section.Id, ["item"] = itemId,
            });
            return new Dictionary<string, object> { ["success"] = true, ["rewards_granted"] = granted };
        }

        public int GetPurchaseCount(string sectionId, string itemId)
        {
            return _purchaseCounts.TryGetValue($"{sectionId}_{itemId}", out var c) ? c : 0;
        }
    }

    public class ShopSection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ShopItem> Items { get; set; } = new();
        public int RefreshInterval { get; set; }
    }

    public class ShopItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CostType { get; set; } = "currency";
        public string CostCurrency { get; set; } = "gems";
        public int CostAmount { get; set; }
        public string IapProductId { get; set; }
        public List<Dictionary<string, object>> Rewards { get; set; } = new();
        public int BuyLimit { get; set; }
        public long AvailableUntil { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
