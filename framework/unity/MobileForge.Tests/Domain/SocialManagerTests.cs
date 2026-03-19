using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    /// <summary>
    /// Tests for SocialManager -- friend list, support units, helper usage, and persistence.
    /// Creates a fresh SocialManager per test for isolation.
    /// </summary>
    [TestFixture]
    public class SocialManagerTests
    {
        private SocialManager _social;
        private SupportConfig _config;

        // Event tracking
        private List<FriendEntry> _addedEvents;
        private List<FriendEntry> _removedEvents;
        private List<string> _supportUsedEvents;

        [SetUp]
        public void SetUp()
        {
            _config = new SupportConfig(maxFriendSlots: 3, pointsPerUse: 10,
                pointsPerUseNonFriend: 5);
            _social = new SocialManager(_config);

            _addedEvents = new List<FriendEntry>();
            _removedEvents = new List<FriendEntry>();
            _supportUsedEvents = new List<string>();

            _social.FriendAdded += entry => _addedEvents.Add(entry);
            _social.FriendRemoved += entry => _removedEvents.Add(entry);
            _social.SupportUsed += id => _supportUsedEvents.Add(id);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private FriendEntry MakeFriend(string id, string name = "Player",
            int level = 10, Dictionary<string, object> supportUnit = null)
        {
            return new FriendEntry(
                playerId: id,
                displayName: name,
                playerLevel: level,
                lastLogin: 1000L,
                supportUnit: supportUnit
            );
        }

        // -----------------------------------------------------------------
        // AddFriend
        // -----------------------------------------------------------------

        [Test]
        public void AddFriend_Succeeds_And_Fires_Event()
        {
            var friend = MakeFriend("p1", "Alice");
            bool result = _social.AddFriend(friend);

            Assert.IsTrue(result, "AddFriend should return true");
            Assert.AreEqual(1, _social.GetFriendCount(),
                "Friend count should be 1 after adding one friend");
            Assert.AreEqual(1, _addedEvents.Count,
                "FriendAdded event should fire once");
            Assert.AreEqual("p1", _addedEvents[0].PlayerId,
                "Event should contain the added friend");
        }

        [Test]
        public void AddFriend_Fails_At_Max_Slots()
        {
            _social.AddFriend(MakeFriend("p1"));
            _social.AddFriend(MakeFriend("p2"));
            _social.AddFriend(MakeFriend("p3"));

            bool result = _social.AddFriend(MakeFriend("p4"));

            Assert.IsFalse(result, "AddFriend should return false when at max slots");
            Assert.AreEqual(3, _social.GetFriendCount(),
                "Friend count should remain at max");
        }

        [Test]
        public void AddFriend_Fails_For_Duplicate()
        {
            _social.AddFriend(MakeFriend("p1", "Alice"));

            bool result = _social.AddFriend(MakeFriend("p1", "Alice Duplicate"));

            Assert.IsFalse(result, "AddFriend should return false for duplicate player ID");
            Assert.AreEqual(1, _social.GetFriendCount(),
                "Friend count should remain at 1");
            Assert.AreEqual(1, _addedEvents.Count,
                "FriendAdded event should only fire for the first add");
        }

        // -----------------------------------------------------------------
        // RemoveFriend
        // -----------------------------------------------------------------

        [Test]
        public void RemoveFriend_Succeeds_And_Fires_Event()
        {
            _social.AddFriend(MakeFriend("p1", "Alice"));

            bool result = _social.RemoveFriend("p1");

            Assert.IsTrue(result, "RemoveFriend should return true for existing friend");
            Assert.AreEqual(0, _social.GetFriendCount(),
                "Friend count should be 0 after removal");
            Assert.AreEqual(1, _removedEvents.Count,
                "FriendRemoved event should fire once");
            Assert.AreEqual("p1", _removedEvents[0].PlayerId,
                "Event should contain the removed friend");
        }

        [Test]
        public void RemoveFriend_Returns_False_For_Nonexistent()
        {
            bool result = _social.RemoveFriend("unknown");

            Assert.IsFalse(result, "RemoveFriend should return false for nonexistent player");
            Assert.AreEqual(0, _removedEvents.Count,
                "FriendRemoved event should not fire");
        }

        // -----------------------------------------------------------------
        // GetFriends / IsFriend
        // -----------------------------------------------------------------

        [Test]
        public void GetFriends_Returns_All_Active()
        {
            _social.AddFriend(MakeFriend("p1", "Alice"));
            _social.AddFriend(new FriendEntry("p2", "Bob", status: FriendStatus.Blocked));
            _social.AddFriend(MakeFriend("p3", "Charlie"));

            var active = _social.GetFriends();

            Assert.AreEqual(2, active.Count,
                "GetFriends should return only active friends (excluding blocked)");
        }

        [Test]
        public void IsFriend_Works_Correctly()
        {
            _social.AddFriend(MakeFriend("p1"));

            Assert.IsTrue(_social.IsFriend("p1"),
                "IsFriend should return true for existing friend");
            Assert.IsFalse(_social.IsFriend("unknown"),
                "IsFriend should return false for unknown player");
        }

        // -----------------------------------------------------------------
        // Support Units
        // -----------------------------------------------------------------

        [Test]
        public void SetSupportUnit_GetSupportUnit_RoundTrip()
        {
            var unit = new Dictionary<string, object>
            {
                { "monster_id", 42 },
                { "level", 99 }
            };

            _social.SetSupportUnit(0, unit);
            var retrieved = _social.GetSupportUnit(0);

            Assert.IsNotNull(retrieved, "GetSupportUnit should return the set unit");
            Assert.AreEqual(42, retrieved["monster_id"],
                "Unit data should be preserved");
        }

        [Test]
        public void GetSupportUnit_Returns_Null_For_Empty_Slot()
        {
            var result = _social.GetSupportUnit(0);

            Assert.IsNull(result, "GetSupportUnit should return null for unset slot");
        }

        [Test]
        public void GetSupportUnits_Returns_All_Set_Units()
        {
            _social.SetSupportUnit(0, new Dictionary<string, object> { { "id", 1 } });
            _social.SetSupportUnit(2, new Dictionary<string, object> { { "id", 2 } });

            var units = _social.GetSupportUnits();

            Assert.AreEqual(2, units.Count,
                "GetSupportUnits should return all set support units");
        }

        // -----------------------------------------------------------------
        // Helper System
        // -----------------------------------------------------------------

        [Test]
        public void GetAvailableHelpers_Returns_Friend_Support_Units()
        {
            var support = new Dictionary<string, object> { { "monster_id", 99 } };
            _social.AddFriend(MakeFriend("p1", supportUnit: support));
            _social.AddFriend(MakeFriend("p2")); // empty support unit

            var helpers = _social.GetAvailableHelpers();

            Assert.AreEqual(1, helpers.Count,
                "Only friends with non-empty support units should appear as helpers");
            Assert.AreEqual("p1", helpers[0].Key,
                "Helper should be the friend with a support unit");
        }

        [Test]
        public void UseHelper_Returns_Friend_Points_For_Friend()
        {
            _social.AddFriend(MakeFriend("p1"));

            int points = _social.UseHelper("p1");

            Assert.AreEqual(10, points,
                "UseHelper should return PointsPerUse for a friend");
            Assert.AreEqual(1, _supportUsedEvents.Count,
                "SupportUsed event should fire");
            Assert.AreEqual("p1", _supportUsedEvents[0],
                "SupportUsed event should contain the helper's player ID");
        }

        [Test]
        public void UseHelper_Returns_NonFriend_Points_For_Unknown()
        {
            int points = _social.UseHelper("stranger");

            Assert.AreEqual(5, points,
                "UseHelper should return PointsPerUseNonFriend for non-friend");
        }

        [Test]
        public void UseHelper_Can_Only_Be_Used_Once_Per_Day()
        {
            _social.AddFriend(MakeFriend("p1"));

            int first = _social.UseHelper("p1");
            int second = _social.UseHelper("p1");

            Assert.AreEqual(10, first,
                "First use should return points");
            Assert.AreEqual(0, second,
                "Second use same day should return 0");
            Assert.AreEqual(1, _supportUsedEvents.Count,
                "SupportUsed event should only fire once");
        }

        [Test]
        public void ResetDailyHelperUsage_Allows_Reuse()
        {
            _social.AddFriend(MakeFriend("p1"));

            _social.UseHelper("p1");
            _social.ResetDailyHelperUsage();
            int points = _social.UseHelper("p1");

            Assert.AreEqual(10, points,
                "After reset, helper should be usable again for full points");
            Assert.AreEqual(2, _supportUsedEvents.Count,
                "SupportUsed event should fire again after reset");
        }

        // -----------------------------------------------------------------
        // SetMaxFriendSlots
        // -----------------------------------------------------------------

        [Test]
        public void SetMaxFriendSlots_Dynamically_Increases_Capacity()
        {
            _social.AddFriend(MakeFriend("p1"));
            _social.AddFriend(MakeFriend("p2"));
            _social.AddFriend(MakeFriend("p3"));

            // At max (3), adding should fail
            Assert.IsFalse(_social.AddFriend(MakeFriend("p4")),
                "Should fail at original max");

            // Increase capacity
            _social.SetMaxFriendSlots(5);

            bool result = _social.AddFriend(MakeFriend("p4"));
            Assert.IsTrue(result,
                "Should succeed after increasing max slots");
            Assert.AreEqual(4, _social.GetFriendCount(),
                "Friend count should reflect the new addition");
        }

        // -----------------------------------------------------------------
        // Persistence
        // -----------------------------------------------------------------

        [Test]
        public void ToSaveDict_FromSaveDict_RoundTrip()
        {
            // Set up state
            var support = new Dictionary<string, object> { { "monster_id", 77 } };
            _social.AddFriend(new FriendEntry("p1", "Alice", 25, 5000L, support));
            _social.AddFriend(MakeFriend("p2", "Bob", 30));
            _social.SetSupportUnit(0, new Dictionary<string, object> { { "id", 1 } });
            _social.SetSupportUnit(1, new Dictionary<string, object> { { "id", 2 } });
            _social.UseHelper("p1");

            // Save
            var saveData = _social.ToSaveDict();

            // Load into fresh manager
            var restored = new SocialManager(_config);
            restored.FromSaveDict(saveData);

            // Verify friends
            Assert.AreEqual(2, restored.GetFriendCount(),
                "Restored manager should have 2 friends");
            var alice = restored.GetFriend("p1");
            Assert.IsNotNull(alice, "Alice should be restored");
            Assert.AreEqual("Alice", alice.DisplayName,
                "Display name should be preserved");
            Assert.AreEqual(25, alice.PlayerLevel,
                "Player level should be preserved");
            Assert.AreEqual(5000L, alice.LastLogin,
                "Last login should be preserved");
            Assert.AreEqual(77, Convert.ToInt32(alice.SupportUnit["monster_id"]),
                "Support unit data should be preserved");

            // Verify own support units
            var unit0 = restored.GetSupportUnit(0);
            Assert.IsNotNull(unit0, "Support unit slot 0 should be restored");
            Assert.AreEqual(1, Convert.ToInt32(unit0["id"]),
                "Support unit data should match");

            // Verify used helpers
            Assert.AreEqual(0, restored.UseHelper("p1"),
                "Used helper state should be preserved — p1 already used");
            Assert.AreEqual(10, restored.UseHelper("p2"),
                "p2 should still be usable after restore");
        }
    }
}
