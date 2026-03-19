using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Reward from a single dungeon clear — distributed after battle won.
    /// </summary>
    public class DungeonClearReward
    {
        public int PlayerExp { get; set; }
        public int Coins { get; set; }
        public int TeamMonsterExp { get; set; }
        public List<int> DroppedMonsterIds { get; set; } = new();
        public List<Dictionary<string, object>> ItemDrops { get; set; } = new();
        public bool FirstClearBonus { get; set; }
    }

    /// <summary>
    /// Calculates and distributes rewards after completing a dungeon.
    /// Connects DungeonRunner results to Economy and MonsterManager.
    ///
    /// ToS reward structure:
    /// - Base EXP/coins from dungeon def
    /// - Per-enemy EXP based on enemy level/HP
    /// - Monster drops: each enemy has a drop chance + drop monster ID
    /// - First-clear bonus (gems)
    /// - Combo bonus: extra coins for high combo counts
    /// </summary>
    public static class DungeonRewardCalculator
    {
        /// <summary>
        /// Calculate all rewards for a dungeon clear.
        /// </summary>
        public static DungeonClearReward Calculate(DungeonDef dungeon, DungeonState finalState,
            bool isFirstClear, Random rng = null)
        {
            rng ??= new Random();
            var reward = new DungeonClearReward();

            // Base rewards from dungeon definition
            if (dungeon.Rewards.TryGetValue("player_exp", out var pExp))
                reward.PlayerExp = Convert.ToInt32(pExp);
            if (dungeon.Rewards.TryGetValue("coins", out var coins))
                reward.Coins = Convert.ToInt32(coins);
            if (dungeon.Rewards.TryGetValue("team_exp", out var tExp))
                reward.TeamMonsterExp = Convert.ToInt32(tExp);

            // Per-enemy rewards: EXP contribution + monster drops
            if (finalState.Enemies != null)
            {
                foreach (var enemy in finalState.Enemies)
                {
                    if (!enemy.IsAlive) // Only reward killed enemies
                    {
                        // Enemy EXP contribution: atk + hp/10 (rough ToS formula)
                        reward.TeamMonsterExp += enemy.Atk + enemy.MaxHp / 10;

                        // Monster drop check
                        foreach (var se in enemy.StatusEffects)
                        {
                            // Convention: enemies define drops via "drop" status entries
                            if (se.TryGetValue("type", out var t) && Convert.ToString(t) == "drop")
                            {
                                float dropRate = se.TryGetValue("rate", out var r) ? Convert.ToSingle(r) : 0f;
                                int dropMonsterId = se.TryGetValue("monster_id", out var mid) ? Convert.ToInt32(mid) : -1;

                                if (dropMonsterId >= 0 && rng.NextDouble() < dropRate)
                                    reward.DroppedMonsterIds.Add(dropMonsterId);
                            }
                        }
                    }
                }
            }

            // Combo bonus: +10% coins per 5 combos above 10
            if (finalState.TotalCombos > 10)
            {
                int bonusStacks = (finalState.TotalCombos - 10) / 5;
                reward.Coins += (int)(reward.Coins * bonusStacks * 0.1f);
            }

            // First-clear bonus
            if (isFirstClear)
            {
                reward.FirstClearBonus = true;
                // Standard ToS: 1 gem for first clear
                if (!dungeon.Rewards.ContainsKey("first_clear_gems"))
                    reward.ItemDrops.Add(new Dictionary<string, object>
                    {
                        { "type", "currency" }, { "currency", "gems" }, { "amount", 1 },
                    });
            }

            // Item drops from dungeon definition
            if (dungeon.Rewards.TryGetValue("item_drops", out var dropsObj) && dropsObj is List<object> dropsList)
            {
                foreach (var dropObj in dropsList)
                {
                    if (dropObj is Dictionary<string, object> dropDict)
                    {
                        float rate = dropDict.TryGetValue("rate", out var dr) ? Convert.ToSingle(dr) : 1f;
                        if (rng.NextDouble() < rate)
                            reward.ItemDrops.Add(dropDict);
                    }
                }
            }

            return reward;
        }

        /// <summary>
        /// Distribute calculated rewards to the game systems.
        /// </summary>
        public static void Distribute(DungeonClearReward reward, Economy economy,
            MonsterManager monsterManager, List<MonsterInstance> team)
        {
            // Coins
            if (reward.Coins > 0)
                economy?.Earn("coins", reward.Coins);

            // Player EXP (stored as currency for simplicity)
            if (reward.PlayerExp > 0)
                economy?.Earn("player_exp", reward.PlayerExp);

            // Team monster EXP: split evenly among team members
            if (reward.TeamMonsterExp > 0 && team != null && team.Count > 0 && monsterManager != null)
            {
                int expPerMember = reward.TeamMonsterExp / team.Count;
                foreach (var member in team)
                    monsterManager.AddExp(member, expPerMember);
            }

            // Monster drops: create new instances
            if (monsterManager != null)
            {
                foreach (int monsterId in reward.DroppedMonsterIds)
                    monsterManager.CreateInstance(monsterId);
            }

            // Currency item drops (gems, etc.)
            if (economy != null)
            {
                foreach (var item in reward.ItemDrops)
                {
                    if (item.TryGetValue("type", out var type) && Convert.ToString(type) == "currency")
                    {
                        string currency = item.TryGetValue("currency", out var c) ? Convert.ToString(c) : "";
                        int amount = item.TryGetValue("amount", out var a) ? Convert.ToInt32(a) : 0;
                        if (!string.IsNullOrEmpty(currency) && amount > 0)
                            economy.Earn(currency, amount);
                    }
                }
            }
        }
    }
}
