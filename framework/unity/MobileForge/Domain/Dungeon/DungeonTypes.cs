using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Immutable dungeon definition loaded from JSON.
    /// </summary>
    public class DungeonDef
    {
        public int Id { get; }
        public string Name { get; }
        public int StaminaCost { get; }
        public List<WaveDef> Waves { get; }
        public Dictionary<string, object> Rewards { get; }

        public DungeonDef(Dictionary<string, object> data)
        {
            Id = GetInt(data, "id", 0);
            Name = GetString(data, "name", "");
            StaminaCost = GetInt(data, "stamina_cost", 0);

            Waves = new List<WaveDef>();
            if (data.TryGetValue("waves", out var wavesObj) && wavesObj is List<object> wavesList)
            {
                foreach (var waveData in wavesList)
                {
                    if (waveData is Dictionary<string, object> waveDict)
                        Waves.Add(new WaveDef(waveDict));
                }
            }

            Rewards = new Dictionary<string, object>();
            if (data.TryGetValue("rewards", out var rewardsObj) && rewardsObj is Dictionary<string, object> rewardsDict)
            {
                foreach (var kv in rewardsDict)
                    Rewards[kv.Key] = kv.Value;
            }
        }

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToInt32(val) : defaultValue;
        }

        private static string GetString(Dictionary<string, object> data, string key, string defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToString(val) : defaultValue;
        }
    }

    /// <summary>
    /// One wave of enemies in a dungeon.
    /// </summary>
    public class WaveDef
    {
        public List<Dictionary<string, object>> Enemies { get; }

        public WaveDef(Dictionary<string, object> data)
        {
            Enemies = new List<Dictionary<string, object>>();
            if (data.TryGetValue("enemies", out var enemiesObj) && enemiesObj is List<object> enemiesList)
            {
                foreach (var e in enemiesList)
                {
                    if (e is Dictionary<string, object> eDict)
                        Enemies.Add(eDict);
                }
            }
        }
    }

    /// <summary>
    /// Result of a single player turn.
    /// </summary>
    public class TurnResult
    {
        public Dictionary<int, int> DamagePerEnemy { get; set; }
        public int Healing { get; set; }
        public List<int> EnemiesKilled { get; set; }
        public List<Dictionary<string, object>> EnemyAttacks { get; set; }
        public bool WaveCleared { get; set; }
        public bool BattleEnded { get; set; }
        public bool BattleWon { get; set; }
        public Dictionary<string, object> Rewards { get; set; }

        public TurnResult()
        {
            DamagePerEnemy = new Dictionary<int, int>();
            Healing = 0;
            EnemiesKilled = new List<int>();
            EnemyAttacks = new List<Dictionary<string, object>>();
            WaveCleared = false;
            BattleEnded = false;
            BattleWon = false;
            Rewards = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Current dungeon state snapshot.
    /// </summary>
    public class DungeonState
    {
        public DungeonDef DungeonDef { get; set; }
        public int CurrentWaveIndex { get; set; }
        public List<EnemyState> Enemies { get; set; }
        public int TeamHp { get; set; }
        public int MaxHp { get; set; }
        public int TurnNumber { get; set; }
        public int TotalCombos { get; set; }
        public bool IsActive { get; set; }

        public DungeonState()
        {
            Enemies = new List<EnemyState>();
            IsActive = false;
        }
    }
}
