using System;
using System.Collections.Generic;
using MobileForge.Domain;
using MobileForge.Infrastructure;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Shop screen with General tab (stamina/coins/debug) and Event tab
    /// (items loaded from event_shops.json). Mirrors shop_screen.gd.
    /// </summary>
    public class ShopScreen : IScreen
    {
        private Economy _economy;
        private UIRouter _router;
        private GameData _gameData;
        private PlayerState _playerState;

        public int GemsBalance => _economy.GetBalance("gems");
        public int CoinsBalance => _economy.GetBalance("coins");
        public int StaminaBalance => _economy.GetBalance("stamina");
        public int EventTokenBalance => _economy.GetBalance("event_tokens");
        public string StatusMessage { get; private set; } = "";

        /// <summary>Event shops loaded from event_shops.json.</summary>
        public List<EventShopInfo> EventShops { get; } = new();

        public class EventShopInfo
        {
            public int Id;
            public string Name;
            public string Currency;
            public List<EventShopItem> Items = new();
        }

        public class EventShopItem
        {
            public int Id;
            public string Name;
            public string Type;
            public int ItemId;
            public int Count;
            public string CostCurrency;
            public int CostAmount;
            public int BuyLimit; // 0 = unlimited
            public int TimesBought;
            public string CurrencyReward;

            public bool IsSoldOut => BuyLimit > 0 && TimesBought >= BuyLimit;
        }

        public void Setup(Economy economy, UIRouter router,
            GameData gameData = null, PlayerState playerState = null)
        {
            _economy = economy;
            _router = router;
            _gameData = gameData;
            _playerState = playerState;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            StatusMessage = "";
            LoadEventShops();
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        public bool RefillStamina()
        {
            if (_economy.Spend("gems", 1))
            {
                _economy.Earn("stamina", 100);
                StatusMessage = "Stamina refilled! +100";
                return true;
            }
            StatusMessage = "Not enough gems!";
            return false;
        }

        public bool BuyCoinPack()
        {
            if (_economy.Spend("gems", 2))
            {
                _economy.Earn("coins", 10000);
                StatusMessage = "Purchased 10,000 coins!";
                return true;
            }
            StatusMessage = "Not enough gems!";
            return false;
        }

        public void AddFreeGems(int amount = 50)
        {
            _economy.Earn("gems", amount);
            StatusMessage = $"Received {amount} free gems!";
        }

        public void AddFreeEventTokens(int amount = 100)
        {
            _economy.Earn("event_tokens", amount);
            StatusMessage = $"Received {amount} event tokens!";
        }

        /// <summary>
        /// Purchase an event shop item.
        /// </summary>
        public bool BuyEventItem(int shopId, int itemId)
        {
            EventShopItem item = null;
            foreach (var shop in EventShops)
            {
                if (shop.Id != shopId) continue;
                foreach (var i in shop.Items)
                {
                    if (i.Id == itemId) { item = i; break; }
                }
            }

            if (item == null) { StatusMessage = "Item not found"; return false; }
            if (item.IsSoldOut) { StatusMessage = "Sold out!"; return false; }

            if (!_economy.Spend(item.CostCurrency, item.CostAmount))
            {
                StatusMessage = $"Not enough {item.CostCurrency}!";
                return false;
            }

            // Grant reward
            if (item.Type == "currency" && !string.IsNullOrEmpty(item.CurrencyReward))
            {
                _economy.Earn(item.CurrencyReward, item.Count);
            }
            // Monster rewards would need MonsterManager — skip for now

            item.TimesBought++;
            string limitStr = item.BuyLimit > 0 ? $" ({item.TimesBought}/{item.BuyLimit})" : "";
            StatusMessage = $"Purchased {item.Name}!{limitStr}";
            return true;
        }

        public void GoBack() => _router.Pop();

        private void LoadEventShops()
        {
            EventShops.Clear();
            if (_gameData == null) return;

            var defs = _gameData.GetAllDefinitions("event_shops");
            foreach (var def in defs)
            {
                var shop = new EventShopInfo
                {
                    Id = def.Id,
                    Name = def.GetString("name", "Event Shop"),
                    Currency = def.GetString("currency", "event_tokens"),
                };

                var items = def.GetArray("items");
                if (items != null)
                {
                    foreach (var itemObj in items)
                    {
                        if (itemObj is Dictionary<string, object> itemDict)
                        {
                            shop.Items.Add(new EventShopItem
                            {
                                Id = Convert.ToInt32(itemDict.GetValueOrDefault("id", 0)),
                                Name = itemDict.GetValueOrDefault("name", "Item")?.ToString(),
                                Type = itemDict.GetValueOrDefault("type", "currency")?.ToString(),
                                ItemId = Convert.ToInt32(itemDict.GetValueOrDefault("item_id", 0)),
                                Count = Convert.ToInt32(itemDict.GetValueOrDefault("count", 1)),
                                CostCurrency = itemDict.GetValueOrDefault("cost_currency", "event_tokens")?.ToString(),
                                CostAmount = Convert.ToInt32(itemDict.GetValueOrDefault("cost_amount", 0)),
                                BuyLimit = Convert.ToInt32(itemDict.GetValueOrDefault("buy_limit", 0)),
                                CurrencyReward = itemDict.GetValueOrDefault("currency_reward", "")?.ToString(),
                                TimesBought = 0,
                            });
                        }
                    }
                }

                EventShops.Add(shop);
            }
        }
    }
}
