using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Manages time-limited game events — scheduling, points, bonuses, milestones.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class GameEventManager
    {
        private readonly Func<long> _now;
        private readonly Dictionary<string, GameEventDef> _defs = new();
        private readonly Dictionary<string, GameEventState> _states = new();

        public event Action<string> EventStarted;
        public event Action<string> EventEnded;
        public event Action<string, int> EventPointsEarned;
        public event Action<string, int> EventMilestoneClaimed;

        public GameEventManager(Func<long> nowProvider)
        {
            _now = nowProvider ?? throw new ArgumentNullException(nameof(nowProvider));
        }

        /// <summary>Load event definitions. Call once at startup or when defs refresh.</summary>
        public void LoadEvents(IEnumerable<GameEventDef> defs)
        {
            foreach (var def in defs)
            {
                bool wasActive = _defs.ContainsKey(def.Id) && IsActive(_defs[def.Id]);
                _defs[def.Id] = def;
                if (!_states.ContainsKey(def.Id))
                    _states[def.Id] = new GameEventState { EventId = def.Id };

                bool isNowActive = IsActive(def);
                if (!wasActive && isNowActive)
                    EventStarted?.Invoke(def.Id);
            }
        }

        /// <summary>Returns events where current time is between StartTime and EndTime.</summary>
        public List<GameEventDef> GetActiveEvents()
        {
            var now = _now();
            return _defs.Values.Where(d => now >= d.StartTime && now < d.EndTime).ToList();
        }

        /// <summary>Returns the status string for an event based on current time.</summary>
        public string GetStatus(string eventId)
        {
            if (!_defs.TryGetValue(eventId, out var def))
                return EventStatus.Ended;

            var now = _now();
            if (now < def.StartTime) return EventStatus.Upcoming;
            if (now < def.EndTime) return EventStatus.Active;
            if (now < def.GraceEndTime) return EventStatus.GracePeriod;
            return EventStatus.Ended;
        }

        /// <summary>
        /// Add points to an event, applying bonus multipliers from team composition.
        /// </summary>
        public int AddPoints(string eventId, int basePoints, List<Dictionary<string, object>> teamMonsters = null)
        {
            if (!_defs.TryGetValue(eventId, out var def)) return 0;
            if (!_states.TryGetValue(eventId, out var state)) return 0;

            var status = GetStatus(eventId);
            if (status != EventStatus.Active) return 0;

            float multiplier = 1.0f;
            if (teamMonsters != null && teamMonsters.Count > 0)
                multiplier = CalculateBonus(eventId, teamMonsters);

            int earned = (int)(basePoints * multiplier);
            state.Points += earned;
            EventPointsEarned?.Invoke(eventId, state.Points);
            return earned;
        }

        /// <summary>
        /// Calculate total point multiplier from all matching bonuses against team monsters.
        /// </summary>
        public float CalculateBonus(string eventId, List<Dictionary<string, object>> teamMonsters)
        {
            if (!_defs.TryGetValue(eventId, out var def)) return 1.0f;
            if (teamMonsters == null || teamMonsters.Count == 0) return 1.0f;
            if (def.Bonuses == null || def.Bonuses.Count == 0) return 1.0f;

            float totalMultiplier = 1.0f;

            foreach (var bonus in def.Bonuses)
            {
                foreach (var monster in teamMonsters)
                {
                    if (MonsterMatchesBonus(monster, bonus))
                    {
                        totalMultiplier += (bonus.PointMultiplier - 1.0f);
                        break; // each bonus applies at most once per team
                    }
                }
            }

            return totalMultiplier;
        }

        /// <summary>Claim a milestone reward if requirements are met.</summary>
        public Dictionary<string, object> ClaimMilestone(string eventId, int milestoneIndex)
        {
            if (!_defs.TryGetValue(eventId, out var def))
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "event_not_found" };
            if (!_states.TryGetValue(eventId, out var state))
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "event_not_found" };

            var status = GetStatus(eventId);
            if (status != EventStatus.Active && status != EventStatus.GracePeriod)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "event_not_active" };

            if (milestoneIndex < 0 || milestoneIndex >= def.Milestones.Count)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "invalid_milestone" };

            var milestone = def.Milestones[milestoneIndex];

            if (milestone.IsLoop)
            {
                int requiredPoints = milestone.PointsRequired * (state.LoopCount + 1);
                if (state.Points < requiredPoints)
                    return new Dictionary<string, object> { ["success"] = false, ["error"] = "insufficient_points" };

                state.LoopCount++;
                EventMilestoneClaimed?.Invoke(eventId, milestoneIndex);
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["rewards"] = milestone.Rewards,
                    ["loop_count"] = state.LoopCount,
                };
            }

            if (state.ClaimedMilestoneIndices.Contains(milestoneIndex))
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "already_claimed" };

            if (state.Points < milestone.PointsRequired)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "insufficient_points" };

            state.ClaimedMilestoneIndices.Add(milestoneIndex);
            EventMilestoneClaimed?.Invoke(eventId, milestoneIndex);
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["rewards"] = milestone.Rewards,
            };
        }

        /// <summary>Get current points for an event.</summary>
        public int GetPoints(string eventId) =>
            _states.TryGetValue(eventId, out var state) ? state.Points : 0;

        /// <summary>Get the index of the next unclaimed non-loop milestone.</summary>
        public int GetNextMilestoneIndex(string eventId)
        {
            if (!_defs.TryGetValue(eventId, out var def)) return -1;
            if (!_states.TryGetValue(eventId, out var state)) return -1;

            for (int i = 0; i < def.Milestones.Count; i++)
            {
                if (def.Milestones[i].IsLoop) continue;
                if (!state.ClaimedMilestoneIndices.Contains(i))
                    return i;
            }

            return -1;
        }

        /// <summary>Get indices of all milestones that can currently be claimed.</summary>
        public List<int> GetClaimableMilestones(string eventId)
        {
            var result = new List<int>();
            if (!_defs.TryGetValue(eventId, out var def)) return result;
            if (!_states.TryGetValue(eventId, out var state)) return result;

            for (int i = 0; i < def.Milestones.Count; i++)
            {
                var milestone = def.Milestones[i];
                if (milestone.IsLoop)
                {
                    int requiredPoints = milestone.PointsRequired * (state.LoopCount + 1);
                    if (state.Points >= requiredPoints)
                        result.Add(i);
                }
                else
                {
                    if (!state.ClaimedMilestoneIndices.Contains(i) && state.Points >= milestone.PointsRequired)
                        result.Add(i);
                }
            }

            return result;
        }

        /// <summary>Serialize all event states for the save system.</summary>
        public Dictionary<string, object> ToSaveDict()
        {
            var statesData = new Dictionary<string, object>();
            foreach (var kv in _states)
            {
                statesData[kv.Key] = new Dictionary<string, object>
                {
                    ["event_id"] = kv.Value.EventId,
                    ["points"] = kv.Value.Points,
                    ["claimed_milestone_indices"] = new List<object>(kv.Value.ClaimedMilestoneIndices.Select(x => (object)x)),
                    ["loop_count"] = kv.Value.LoopCount,
                };
            }

            return new Dictionary<string, object>
            {
                ["states"] = statesData,
            };
        }

        /// <summary>Deserialize event states from save data.</summary>
        public void FromSaveDict(Dictionary<string, object> data)
        {
            _states.Clear();

            if (data.TryGetValue("states", out var statesObj) && statesObj is Dictionary<string, object> statesData)
            {
                foreach (var kv in statesData)
                {
                    if (kv.Value is Dictionary<string, object> stateData)
                    {
                        var state = new GameEventState
                        {
                            EventId = stateData.GetValueOrDefault("event_id", kv.Key) as string ?? kv.Key,
                            Points = Convert.ToInt32(stateData.GetValueOrDefault("points", 0)),
                            LoopCount = Convert.ToInt32(stateData.GetValueOrDefault("loop_count", 0)),
                        };

                        if (stateData.TryGetValue("claimed_milestone_indices", out var indicesObj) && indicesObj is List<object> indicesList)
                        {
                            foreach (var idx in indicesList)
                                state.ClaimedMilestoneIndices.Add(Convert.ToInt32(idx));
                        }

                        _states[kv.Key] = state;
                    }
                }
            }
        }

        private bool IsActive(GameEventDef def)
        {
            var now = _now();
            return now >= def.StartTime && now < def.EndTime;
        }

        private static bool MonsterMatchesBonus(Dictionary<string, object> monster, EventBonusDef bonus)
        {
            switch (bonus.MatchType)
            {
                case EventBonusMatchType.MonsterId:
                    return monster.TryGetValue("id", out var id) && id?.ToString() == bonus.MatchValue;

                case EventBonusMatchType.Rarity:
                    return monster.TryGetValue("rarity", out var rarity) && rarity?.ToString() == bonus.MatchValue;

                case EventBonusMatchType.Element:
                    return monster.TryGetValue("element", out var element) && element?.ToString() == bonus.MatchValue;

                case EventBonusMatchType.Tag:
                    if (monster.TryGetValue("tags", out var tagsObj))
                    {
                        if (tagsObj is List<object> tagList)
                            return tagList.Any(t => t?.ToString() == bonus.MatchValue);
                        if (tagsObj is List<string> tagStrList)
                            return tagStrList.Contains(bonus.MatchValue);
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}
