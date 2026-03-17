using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Tracks quest progress via event matching.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class QuestTracker
    {
        private readonly Dictionary<string, QuestDef> _defs = new();
        private readonly Dictionary<string, QuestState> _active = new();
        private readonly HashSet<string> _completedUnclaimed = new();
        private readonly HashSet<string> _claimed = new();

        public event Action<string> QuestActivated;
        public event Action<string> QuestCompleted;
        public event Action<string, int, int, int> QuestProgress; // questId, objectiveIndex, current, target
        public event Action<string> QuestClaimed;

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
            return new Dictionary<string, object>
            {
                ["active"] = activeData,
                ["completed_unclaimed"] = new List<object>(_completedUnclaimed.Select(x => (object)x)),
                ["claimed"] = new List<object>(_claimed.Select(x => (object)x)),
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
