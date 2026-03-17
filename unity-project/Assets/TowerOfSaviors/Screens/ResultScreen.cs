using System;
using System.Collections.Generic;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Battle result screen. Shows victory/defeat status and rewards.
    /// Mirrors result_screen.gd.
    /// </summary>
    public class ResultScreen : IScreen
    {
        private Dictionary<string, object> _params;

        /// <summary>
        /// Whether the player won the battle.
        /// </summary>
        public bool Won { get; private set; }

        /// <summary>
        /// Result display text ("VICTORY!" or "DEFEATED...").
        /// </summary>
        public string ResultText => Won ? "VICTORY!" : "DEFEATED...";

        /// <summary>
        /// Reward entries parsed from parameters (only populated on victory).
        /// </summary>
        public List<RewardEntry> Rewards { get; private set; } = new List<RewardEntry>();

        public class RewardEntry
        {
            public string Type { get; set; }
            public int Count { get; set; }
        }

        public void Setup(Dictionary<string, object> parameters)
        {
            _params = parameters ?? new Dictionary<string, object>();
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
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
                    // Rewards as key-value pairs (e.g., {"coins": 500, "exp": 100})
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
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Called when the player presses "Continue".
        /// Navigate back to the title screen.
        /// Requires a UIRouter reference — caller should invoke via TosGame.Router.
        /// </summary>
        public void OnContinue(UIRouter router)
        {
            router.Navigate("title");
        }
    }
}
