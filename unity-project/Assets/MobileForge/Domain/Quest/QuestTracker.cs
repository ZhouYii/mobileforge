using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Tracks quest progress via event matching.
    /// Pure C# — no MonoBehaviour dependency.
    /// Supports daily/weekly auto-reset and progressive achievement tiers.
    /// </summary>
    public class QuestTracker
    {
        private readonly Dictionary<string, QuestDef> _defs = new();
        private readonly Dictionary<string, QuestState> _active = new();
        private readonly HashSet<string> _completedUnclaimed = new();
        private readonly HashSet<string> _claimed = new();
        private readonly Func<DateTime> _dateProvider;

        // Reset tracking: category -> last reset date string "yyyy-MM-dd" (or "yyyy-Www" for weekly)
        private readonly Dictionary<string, string> _lastResetKeys = new();

        public event Action<string> QuestActivated;
        public event Action<string> QuestCompleted;
        public event Action<string, int, int, int> QuestProgress; // questId, objectiveIndex, current, target
        public event Action<string> QuestClaimed;

        public QuestTracker(Func<DateTime> dateProvider = null)
        {
            _dateProvider = dateProvider ?? (() => DateTime.UtcNow);
        }

        /// <summary>Load quest definitions. Call once at startup.</summary>
        public void LoadQuestDefs(IEnumerable<QuestDef> defs)
        {
            foreach (var def in defs)
                _defs[def.Id] = def;
        }

        /// <summary>Activate a quest by ID.</summary>
        public bool ActivateQuest(string id)
        {
            if (!_defs.TryGetValue(id, out var def)) return false;
            if (_active.ContainsKey(id)) return false;
            foreach (var prereq in def.Prerequisites)
                if (!_claimed.Contains(prereq)) return false;
            if (!def.Repeatable && _claimed.Contains(id)) return false;

            var state = new QuestState();
            for (int i = 0; i < def.Objectives.Count; i++)
                state.ObjectiveProgress.Add(0);
            _active[id] = state;
            QuestActivated?.Invoke(id);
            return true;
        }

        /// <summary>
        /// Process an event. Returns list of quest IDs newly completed.
        /// </summary>
        public List<string> OnEvent(string eventName, Dictionary<string, object> payload = null)
        {
            payload ??= new Dictionary<string, object>();
            var newlyCompleted = new List<string>();

            foreach (var questId in _active.Keys.ToList())
            {
                var def = _defs[questId];
                var state = _active[questId];
                if (state.Status != QuestStatus.Active) continue;

                for (int i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];
                    if (obj.EventName != eventName) continue;
                    if (state.ObjectiveProgress[i] >= obj.TargetCount) continue;
                    if (!FilterMatches(obj.Filter, payload)) continue;

                    state.ObjectiveProgress[i]++;
                    QuestProgress?.Invoke(questId, i, state.ObjectiveProgress[i], obj.TargetCount);
                }

                bool allDone = true;
                for (int i = 0; i < def.Objectives.Count; i++)
                {
                    if (state.ObjectiveProgress[i] < def.Objectives[i].TargetCount)
                    {
                        allDone = false;
                        break;
                    }
                }

                if (allDone && state.Status == QuestStatus.Active)
                {
                    state.Status = QuestStatus.Completed;
                    _completedUnclaimed.Add(questId);
                    newlyCompleted.Add(questId);
                    QuestCompleted?.Invoke(questId);
                }
            }

            return newlyCompleted;
        }

        /// <summary>Get all currently active quest IDs.</summary>
        public List<string> GetActiveQuests() =>
            _active.Where(kv => kv.Value.Status == QuestStatus.Active).Select(kv => kv.Key).ToList();

        /// <summary>Get all completed-but-unclaimed quest IDs.</summary>
        public List<string> GetCompletedUnclaimed() => new(_completedUnclaimed);

        /// <summary>Get progress for a quest.</summary>
        public QuestState GetQuestState(string id) =>
            _active.TryGetValue(id, out var state) ? state : null;

        /// <summary>Claim a completed quest. Returns its rewards, or null if not claimable.</summary>
        public List<Dictionary<string, object>> ClaimQuest(string id)
        {
            if (!_completedUnclaimed.Remove(id)) return null;
            _active.Remove(id);
            _claimed.Add(id);
            QuestClaimed?.Invoke(id);
            return _defs.TryGetValue(id, out var def) ? def.Rewards : new();
        }

        /// <summary>Whether there are any completed-but-unclaimed quests.</summary>
        public bool HasClaimable() => _completedUnclaimed.Count > 0;

        /// <summary>Number of claimable quests.</summary>
        public int ClaimableCount => _completedUnclaimed.Count;

        // ── Daily/Weekly Reset ──

        /// <summary>
        /// Check if daily/weekly quests need resetting and reactivate them.
        /// Uses the same date-provider pattern as DailyLoginTracker.
        /// </summary>
        public int CheckAndResetCycle(MissionResetConfig config)
        {
            var now = _dateProvider();
            string currentKey = GetResetKey(config, now);
            string category = config.ResetCategory;

            if (_lastResetKeys.TryGetValue(category, out var lastKey) && lastKey == currentKey)
                return 0; // Already reset this cycle

            _lastResetKeys[category] = currentKey;

            // Remove all active/completed quests in this category and re-activate them
            var toReset = _defs.Values
                .Where(d => d.Category == category)
                .Select(d => d.Id)
                .ToList();

            int reactivated = 0;
            foreach (var id in toReset)
            {
                _active.Remove(id);
                _completedUnclaimed.Remove(id);
                _claimed.Remove(id);
                if (ActivateQuest(id))
                    reactivated++;
            }

            return reactivated;
        }

        /// <summary>
        /// After claiming an achievement, auto-activate the next tier if one exists.
        /// Returns the quest ID of the next tier, or null.
        /// </summary>
        public string ActivateNextTier(string achievementBaseId)
        {
            // Find the highest claimed tier for this achievement
            int highestClaimed = -1;
            foreach (var def in _defs.Values)
            {
                if (def.AchievementBaseId == achievementBaseId && _claimed.Contains(def.Id))
                {
                    if (def.AchievementTier > highestClaimed)
                        highestClaimed = def.AchievementTier;
                }
            }

            // Find the next tier
            var nextTier = _defs.Values
                .Where(d => d.AchievementBaseId == achievementBaseId && d.AchievementTier == highestClaimed + 1)
                .FirstOrDefault();

            if (nextTier != null && ActivateQuest(nextTier.Id))
                return nextTier.Id;

            return null;
        }

        /// <summary>Bulk-activate all quests in a category.</summary>
        public int ActivateCategory(string category)
        {
            int count = 0;
            foreach (var def in _defs.Values)
            {
                if (def.Category == category && ActivateQuest(def.Id))
                    count++;
            }
            return count;
        }

        /// <summary>Get all quest IDs in a specific category (active or not).</summary>
        public List<string> GetQuestsByCategory(string category) =>
            _defs.Values.Where(d => d.Category == category).Select(d => d.Id).ToList();

        /// <summary>Get quest IDs in a category that are active.</summary>
        public List<string> GetActiveByCategory(string category) =>
            _active.Where(kv => _defs.TryGetValue(kv.Key, out var def) && def.Category == category
                && kv.Value.Status == QuestStatus.Active)
                .Select(kv => kv.Key).ToList();

        /// <summary>Get claimable count for a specific category (useful for badges).</summary>
        public int GetClaimableCountByCategory(string category) =>
            _completedUnclaimed.Count(id => _defs.TryGetValue(id, out var def) && def.Category == category);

        private static string GetResetKey(MissionResetConfig config, DateTime now)
        {
            if (config.ResetCategory == QuestCategory.Weekly)
            {
                // Weekly: use ISO week number
                var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
                int week = cal.GetWeekOfYear(now, System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                    (DayOfWeek)config.ResetDayOfWeek);
                return $"{now.Year}-W{week:D2}";
            }

            // Daily: use date, but shift by reset hour
            var shifted = now.AddHours(-config.ResetHourUtc);
            return shifted.ToString("yyyy-MM-dd");
        }

        /// <summary>Serialize for save system.</summary>
        public Dictionary<string, object> ToSaveDict()
        {
            var activeData = new Dictionary<string, object>();
            foreach (var kv in _active)
            {
                activeData[kv.Key] = new Dictionary<string, object>
                {
                    ["objective_progress"] = new List<object>(kv.Value.ObjectiveProgress.Select(x => (object)x)),
                    ["status"] = kv.Value.Status,
                };
            }
            var resetKeysData = new Dictionary<string, object>();
            foreach (var kv in _lastResetKeys)
                resetKeysData[kv.Key] = kv.Value;

            return new Dictionary<string, object>
            {
                ["active"] = activeData,
                ["completed_unclaimed"] = new List<object>(_completedUnclaimed.Select(x => (object)x)),
                ["claimed"] = new List<object>(_claimed.Select(x => (object)x)),
                ["last_reset_keys"] = resetKeysData,
            };
        }

        /// <summary>Deserialize from save data.</summary>
        public void FromSaveDict(Dictionary<string, object> data)
        {
            _active.Clear();
            _completedUnclaimed.Clear();
            _claimed.Clear();

            if (data.TryGetValue("active", out var activeObj) && activeObj is Dictionary<string, object> activeData)
            {
                foreach (var kv in activeData)
                {
                    if (kv.Value is Dictionary<string, object> stateData)
                    {
                        var state = new QuestState { Status = stateData.GetValueOrDefault("status", QuestStatus.Active) as string };
                        if (stateData.TryGetValue("objective_progress", out var progObj) && progObj is List<object> progList)
                            state.ObjectiveProgress = progList.Select(x => Convert.ToInt32(x)).ToList();
                        _active[kv.Key] = state;
                    }
                }
            }

            if (data.TryGetValue("completed_unclaimed", out var uncObj) && uncObj is List<object> uncList)
                foreach (var id in uncList) _completedUnclaimed.Add(id.ToString());

            if (data.TryGetValue("claimed", out var clObj) && clObj is List<object> clList)
                foreach (var id in clList) _claimed.Add(id.ToString());

            _lastResetKeys.Clear();
            if (data.TryGetValue("last_reset_keys", out var rkObj) && rkObj is Dictionary<string, object> rkData)
                foreach (var kv in rkData) _lastResetKeys[kv.Key] = kv.Value?.ToString() ?? "";
        }

        private static bool FilterMatches(Dictionary<string, object> filter, Dictionary<string, object> payload)
        {
            if (filter == null || filter.Count == 0) return true;
            foreach (var kv in filter)
            {
                if (!payload.TryGetValue(kv.Key, out var val)) return false;
                if (!Equals(val, kv.Value)) return false;
            }
            return true;
        }
    }
}
