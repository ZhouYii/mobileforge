using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Manages the friend list, own support units, and daily helper usage.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class SocialManager
    {
        private readonly SupportConfig _config;
        private readonly Dictionary<string, FriendEntry> _friends = new();
        private readonly Dictionary<int, Dictionary<string, object>> _supportUnits = new();
        private readonly HashSet<string> _usedHelpers = new();

        /// <summary>Fired when a friend is added.</summary>
        public event Action<FriendEntry> FriendAdded;
        /// <summary>Fired when a friend is removed.</summary>
        public event Action<FriendEntry> FriendRemoved;
        /// <summary>Fired when a support helper is used (playerId of helper).</summary>
        public event Action<string> SupportUsed;

        public SocialManager(SupportConfig config = null)
        {
            _config = config ?? new SupportConfig();
        }

        // ── Friend List ──

        /// <summary>
        /// Add a friend entry. Returns false if at max slots or already exists.
        /// </summary>
        public bool AddFriend(FriendEntry entry)
        {
            if (entry == null) return false;
            if (_friends.Count >= _config.MaxFriendSlots) return false;
            if (_friends.ContainsKey(entry.PlayerId)) return false;

            _friends[entry.PlayerId] = entry;
            FriendAdded?.Invoke(entry);
            return true;
        }

        /// <summary>
        /// Remove a friend by player ID. Returns false if not found.
        /// </summary>
        public bool RemoveFriend(string playerId)
        {
            if (!_friends.TryGetValue(playerId, out var entry)) return false;
            _friends.Remove(playerId);
            FriendRemoved?.Invoke(entry);
            return true;
        }

        /// <summary>Get a friend entry by player ID, or null if not found.</summary>
        public FriendEntry GetFriend(string playerId)
        {
            return _friends.TryGetValue(playerId, out var entry) ? entry : null;
        }

        /// <summary>Get all active friends.</summary>
        public List<FriendEntry> GetFriends()
        {
            return _friends.Values.Where(f => f.Status == FriendStatus.Active).ToList();
        }

        /// <summary>Get the total friend count (all statuses).</summary>
        public int GetFriendCount() => _friends.Count;

        /// <summary>Check if a player ID is in the friend list.</summary>
        public bool IsFriend(string playerId) => _friends.ContainsKey(playerId);

        // ── Own Support Units ──

        /// <summary>Set your own support unit for a given slot index.</summary>
        public void SetSupportUnit(int slotIndex, Dictionary<string, object> unitData)
        {
            if (slotIndex < 0 || slotIndex >= _config.MaxSupportSlots) return;
            _supportUnits[slotIndex] = unitData;
        }

        /// <summary>Get your own support unit at a given slot index, or null.</summary>
        public Dictionary<string, object> GetSupportUnit(int slotIndex)
        {
            return _supportUnits.TryGetValue(slotIndex, out var data) ? data : null;
        }

        /// <summary>Get all own support units.</summary>
        public Dictionary<int, Dictionary<string, object>> GetSupportUnits()
        {
            return new Dictionary<int, Dictionary<string, object>>(_supportUnits);
        }

        // ── Helper System ──

        /// <summary>
        /// Get all friends' support units (available helpers for quests/battles).
        /// Returns a list of (playerId, supportUnit) pairs for friends with non-empty support units.
        /// </summary>
        public List<KeyValuePair<string, Dictionary<string, object>>> GetAvailableHelpers()
        {
            var helpers = new List<KeyValuePair<string, Dictionary<string, object>>>();
            foreach (var kvp in _friends)
            {
                if (kvp.Value.Status == FriendStatus.Active &&
                    kvp.Value.SupportUnit != null && kvp.Value.SupportUnit.Count > 0)
                {
                    helpers.Add(new KeyValuePair<string, Dictionary<string, object>>(
                        kvp.Key, kvp.Value.SupportUnit));
                }
            }
            return helpers;
        }

        /// <summary>
        /// Use a helper. Marks as used for the day. Returns friend points earned
        /// (PointsPerUse if friend, PointsPerUseNonFriend if not).
        /// Returns 0 if already used today.
        /// </summary>
        public int UseHelper(string playerId)
        {
            if (_usedHelpers.Contains(playerId)) return 0;
            _usedHelpers.Add(playerId);

            int points = _friends.ContainsKey(playerId)
                ? _config.PointsPerUse
                : _config.PointsPerUseNonFriend;

            SupportUsed?.Invoke(playerId);
            return points;
        }

        /// <summary>Reset daily helper usage, allowing all helpers to be used again.</summary>
        public void ResetDailyHelperUsage()
        {
            _usedHelpers.Clear();
        }

        /// <summary>Dynamically change the max friend slots (e.g. from player level).</summary>
        public void SetMaxFriendSlots(int slots)
        {
            _config.MaxFriendSlots = slots;
        }

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            var friendsData = new List<object>();
            foreach (var kvp in _friends)
            {
                var entry = kvp.Value;
                friendsData.Add(new Dictionary<string, object>
                {
                    ["player_id"] = entry.PlayerId,
                    ["display_name"] = entry.DisplayName,
                    ["player_level"] = entry.PlayerLevel,
                    ["last_login"] = entry.LastLogin,
                    ["support_unit"] = entry.SupportUnit,
                    ["status"] = entry.Status,
                });
            }

            var supportData = new Dictionary<string, object>();
            foreach (var kvp in _supportUnits)
            {
                supportData[kvp.Key.ToString()] = kvp.Value;
            }

            var usedHelpersList = new List<object>();
            foreach (var id in _usedHelpers)
            {
                usedHelpersList.Add(id);
            }

            return new Dictionary<string, object>
            {
                ["friends"] = friendsData,
                ["support_units"] = supportData,
                ["used_helpers"] = usedHelpersList,
            };
        }

        public void FromSaveDict(Dictionary<string, object> data)
        {
            _friends.Clear();
            _supportUnits.Clear();
            _usedHelpers.Clear();

            if (data.TryGetValue("friends", out var fObj) && fObj is List<object> fList)
            {
                foreach (var item in fList)
                {
                    if (item is Dictionary<string, object> entry)
                    {
                        var friend = new FriendEntry(
                            playerId: entry.GetValueOrDefault("player_id")?.ToString() ?? "",
                            displayName: entry.GetValueOrDefault("display_name")?.ToString() ?? "",
                            playerLevel: Convert.ToInt32(entry.GetValueOrDefault("player_level", 1)),
                            lastLogin: Convert.ToInt64(entry.GetValueOrDefault("last_login", 0L)),
                            supportUnit: entry.GetValueOrDefault("support_unit") as Dictionary<string, object>,
                            status: entry.GetValueOrDefault("status")?.ToString() ?? FriendStatus.Active
                        );
                        _friends[friend.PlayerId] = friend;
                    }
                }
            }

            if (data.TryGetValue("support_units", out var sObj) && sObj is Dictionary<string, object> sDict)
            {
                foreach (var kvp in sDict)
                {
                    if (int.TryParse(kvp.Key, out int slotIndex) &&
                        kvp.Value is Dictionary<string, object> unitData)
                    {
                        _supportUnits[slotIndex] = unitData;
                    }
                }
            }

            if (data.TryGetValue("used_helpers", out var uObj) && uObj is List<object> uList)
            {
                foreach (var item in uList)
                {
                    if (item != null)
                        _usedHelpers.Add(item.ToString());
                }
            }
        }
    }
}
