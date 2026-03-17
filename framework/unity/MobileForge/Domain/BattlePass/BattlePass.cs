using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Tiered free + premium battle/season pass.
    /// Tracks XP, tier progress, and reward claims.
    /// </summary>
    public class BattlePass
    {
        private readonly RewardPipeline _rewardPipeline;
        private readonly Action<string, Dictionary<string, object>> _emitEvent;

        private int _xp;
        private bool _isPremium;
        private readonly HashSet<string> _claimed = new();

        public int Xp => _xp;
        public bool IsPremium => _isPremium;

        public BattlePass(RewardPipeline rewardPipeline = null,
            Action<string, Dictionary<string, object>> emitEvent = null)
        {
            _rewardPipeline = rewardPipeline;
            _emitEvent = emitEvent;
        }

        /// <summary>Add XP. Returns number of tiers gained.</summary>
        public int AddXp(BattlePassDef def, int xp)
        {
            int oldTier = _xp / def.XpPerTier;
            _xp += xp;
            int newTier = _xp / def.XpPerTier;
            if (newTier > oldTier)
                _emitEvent?.Invoke("battle_pass_tier_up", new Dictionary<string, object>
                {
                    ["old_tier"] = oldTier, ["new_tier"] = newTier,
                });
            return newTier - oldTier;
        }

        public int GetTier(BattlePassDef def) => Math.Min(_xp / def.XpPerTier, def.Tiers.Count - 1);

        /// <summary>Claim rewards for a tier.</summary>
        public Dictionary<string, object> Claim(BattlePassDef def, int tierIndex, string track = "free")
        {
            if (tierIndex > GetTier(def))
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "tier_not_reached" };
            var key = $"{track}_{tierIndex}";
            if (_claimed.Contains(key))
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "already_claimed" };
            if (track == "premium" && !_isPremium)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "premium_required" };
            if (tierIndex >= def.Tiers.Count)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "invalid_tier" };

            var rewards = track == "free" ? def.Tiers[tierIndex].FreeRewards : def.Tiers[tierIndex].PremiumRewards;
            List<Dictionary<string, object>> granted = null;
            if (_rewardPipeline != null && rewards.Count > 0)
                granted = _rewardPipeline.Grant(rewards, "battle_pass:" + track);

            _claimed.Add(key);
            _emitEvent?.Invoke("battle_pass_claimed", new Dictionary<string, object>
            {
                ["tier"] = tierIndex, ["track"] = track,
            });
            return new Dictionary<string, object> { ["success"] = true, ["rewards_granted"] = granted };
        }

        public void ActivatePremium()
        {
            _isPremium = true;
            _emitEvent?.Invoke("battle_pass_premium_activated", new Dictionary<string, object>());
        }

        /// <summary>Load state from saved data.</summary>
        public void LoadState(int xp, bool isPremium, List<string> claimedKeys)
        {
            _xp = xp;
            _isPremium = isPremium;
            _claimed.Clear();
            foreach (var k in claimedKeys) _claimed.Add(k);
        }
    }

    public class BattlePassDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int XpPerTier { get; set; } = 100;
        public long EndTime { get; set; }
        public List<BattlePassTier> Tiers { get; set; } = new();
    }

    public class BattlePassTier
    {
        public int Tier { get; set; }
        public List<Dictionary<string, object>> FreeRewards { get; set; } = new();
        public List<Dictionary<string, object>> PremiumRewards { get; set; } = new();
    }
}
