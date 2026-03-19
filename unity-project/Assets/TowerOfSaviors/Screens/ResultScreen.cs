using System;
using System.Collections.Generic;
using MobileForge.Domain;
using MobileForge.Infrastructure;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Battle result screen. Shows victory/defeat status and rewards.
    /// On victory, distributes rewards (coins, exp, gems) to PlayerState when continuing.
    /// </summary>
    public class ResultScreen : IScreen
    {
        private UIRouter _router;
        private Economy _economy;
        private Dictionary<string, object> _params;
        private bool _rewardsCollected;

        public bool Won { get; private set; }
        public string ResultText => Won ? "VICTORY!" : "DEFEATED...";
        public List<RewardEntry> Rewards { get; private set; } = new List<RewardEntry>();

        public class RewardEntry
        {
            public string Type { get; set; }
            public int Count { get; set; }
        }

        public void Setup(UIRouter router, Dictionary<string, object> parameters, Economy economy = null)
        {
            _router = router;
            _params = parameters ?? new Dictionary<string, object>();
            _economy = economy;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            _rewardsCollected = false;
            Won = false;
            if (_params.TryGetValue("won", out var wonObj))
            {
                try { Won = Convert.ToBoolean(wonObj); }
                catch { Won = false; }
            }

            Rewards.Clear();
            if (Won && _params.TryGetValue("rewards", out var rewardsObj))
            {
                if (rewardsObj is List<object> rewardList)
                {
                    foreach (var item in rewardList)
                    {
                        if (item is Dictionary<string, object> rewardDict)
                        {
                            Rewards.Add(new RewardEntry
                            {
                                Type = rewardDict.TryGetValue("type", out var typeObj)
                                    ? Convert.ToString(typeObj) : "",
                                Count = rewardDict.TryGetValue("count", out var countObj)
                                    ? Convert.ToInt32(countObj) : 0
                            });
                        }
                    }
                }
                else if (rewardsObj is Dictionary<string, object> rewardsDict)
                {
                    foreach (var kv in rewardsDict)
                    {
                        Rewards.Add(new RewardEntry
                        {
                            Type = kv.Key,
                            Count = Convert.ToInt32(kv.Value)
                        });
                    }
                }
            }

            // If no explicit rewards but won, add default rewards
            if (Won && Rewards.Count == 0)
            {
                int staminaCost = _params.TryGetValue("stamina_cost", out var sc) ? Convert.ToInt32(sc) : 10;
                Rewards.Add(new RewardEntry { Type = "coins", Count = staminaCost * 100 });
                Rewards.Add(new RewardEntry { Type = "player_exp", Count = staminaCost * 50 });
            }
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Called when the player presses "Continue".
        /// Distributes rewards to economy and navigates to title.
        /// </summary>
        public void OnContinue(UIRouter router = null)
        {
            // Apply rewards to player state
            if (Won && !_rewardsCollected && _economy != null)
            {
                foreach (var reward in Rewards)
                {
                    if (!string.IsNullOrEmpty(reward.Type) && reward.Count > 0)
                        _economy.Earn(reward.Type, reward.Count);
                }
                _rewardsCollected = true;
            }

            var r = router ?? _router;
            r?.Navigate("title");
        }
    }
}
