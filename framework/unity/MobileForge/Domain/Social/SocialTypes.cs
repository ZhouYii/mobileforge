using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Constants for friend relationship status.
    /// </summary>
    public static class FriendStatus
    {
        public const string Active = "active";
        public const string Pending = "pending";
        public const string Blocked = "blocked";
    }

    /// <summary>
    /// Represents a single friend in the player's friend list.
    /// </summary>
    public class FriendEntry
    {
        public string PlayerId { get; set; }
        public string DisplayName { get; set; }
        public int PlayerLevel { get; set; }
        public long LastLogin { get; set; }
        public Dictionary<string, object> SupportUnit { get; set; }
        public string Status { get; set; }

        public FriendEntry(string playerId = "", string displayName = "",
            int playerLevel = 1, long lastLogin = 0,
            Dictionary<string, object> supportUnit = null,
            string status = FriendStatus.Active)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            PlayerLevel = playerLevel;
            LastLogin = lastLogin;
            SupportUnit = supportUnit ?? new Dictionary<string, object>();
            Status = status;
        }
    }

    /// <summary>
    /// Configuration for the social/support system.
    /// </summary>
    public class SupportConfig
    {
        public int MaxFriendSlots { get; set; }
        public int MaxSupportSlots { get; set; }
        public string FriendPointCurrency { get; set; }
        public int PointsPerUse { get; set; }
        public int PointsPerUseNonFriend { get; set; }

        public SupportConfig(int maxFriendSlots = 50, int maxSupportSlots = 5,
            string friendPointCurrency = "friend_points",
            int pointsPerUse = 10, int pointsPerUseNonFriend = 5)
        {
            MaxFriendSlots = maxFriendSlots;
            MaxSupportSlots = maxSupportSlots;
            FriendPointCurrency = friendPointCurrency;
            PointsPerUse = pointsPerUse;
            PointsPerUseNonFriend = pointsPerUseNonFriend;
        }
    }
}
