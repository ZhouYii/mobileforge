using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    /// <summary>
    /// Tests for MailManager — inbox management, reading, claiming, cleanup, persistence.
    /// </summary>
    [TestFixture]
    public class MailManagerTests
    {
        private long _currentTime;
        private MailManager _manager;

        // Event tracking
        private List<MailMessage> _receivedEvents;
        private List<string> _readEvents;
        private List<string> _claimedEvents;

        [SetUp]
        public void SetUp()
        {
            _currentTime = 1000;
            _manager = new MailManager(() => _currentTime);

            _receivedEvents = new List<MailMessage>();
            _readEvents = new List<string>();
            _claimedEvents = new List<string>();

            _manager.MailReceived += msg => _receivedEvents.Add(msg);
            _manager.MailRead += id => _readEvents.Add(id);
            _manager.MailClaimed += id => _claimedEvents.Add(id);
        }

        // ── Helpers ──

        private MailMessage MakeMessage(string id, long createdAt = 100,
            List<Dictionary<string, object>> attachments = null,
            List<string> tags = null, long expiresAt = 0)
        {
            return new MailMessage
            {
                Id = id,
                Sender = "system",
                Subject = $"Subject {id}",
                Body = $"Body {id}",
                Attachments = attachments ?? new List<Dictionary<string, object>>(),
                Tags = tags ?? new List<string>(),
                CreatedAt = createdAt,
                ExpiresAt = expiresAt,
            };
        }

        private List<Dictionary<string, object>> MakeRewards(string type, int count)
        {
            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["type"] = type, ["count"] = count },
            };
        }

        // ── AddMessage ──

        [Test]
        public void AddMessage_AddsToInbox_And_FiresEvent()
        {
            var msg = MakeMessage("m1");
            _manager.AddMessage(msg);

            Assert.AreEqual(1, _manager.GetAll().Count);
            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual("m1", _receivedEvents[0].Id);
        }

        // ── GetAll ──

        [Test]
        public void GetAll_ReturnsSortedByCreatedAtDescending()
        {
            _manager.AddMessage(MakeMessage("old", createdAt: 10));
            _manager.AddMessage(MakeMessage("mid", createdAt: 50));
            _manager.AddMessage(MakeMessage("new", createdAt: 90));

            var all = _manager.GetAll();
            Assert.AreEqual(3, all.Count);
            Assert.AreEqual("new", all[0].Id);
            Assert.AreEqual("mid", all[1].Id);
            Assert.AreEqual("old", all[2].Id);
        }

        // ── GetUnread ──

        [Test]
        public void GetUnread_ReturnsOnlyUnread()
        {
            _manager.AddMessage(MakeMessage("m1", createdAt: 10));
            _manager.AddMessage(MakeMessage("m2", createdAt: 20));
            _manager.MarkRead("m1");

            var unread = _manager.GetUnread();
            Assert.AreEqual(1, unread.Count);
            Assert.AreEqual("m2", unread[0].Id);
        }

        // ── GetByTag ──

        [Test]
        public void GetByTag_FiltersCorrectly()
        {
            _manager.AddMessage(MakeMessage("m1", tags: new List<string> { "promo", "vip" }));
            _manager.AddMessage(MakeMessage("m2", tags: new List<string> { "system" }));
            _manager.AddMessage(MakeMessage("m3", tags: new List<string> { "promo" }));

            var promo = _manager.GetByTag("promo");
            Assert.AreEqual(2, promo.Count);
            Assert.IsTrue(promo.Any(m => m.Id == "m1"));
            Assert.IsTrue(promo.Any(m => m.Id == "m3"));

            var system = _manager.GetByTag("system");
            Assert.AreEqual(1, system.Count);
            Assert.AreEqual("m2", system[0].Id);
        }

        // ── GetMessage ──

        [Test]
        public void GetMessage_ReturnsMessageOrNull()
        {
            _manager.AddMessage(MakeMessage("m1"));
            Assert.IsNotNull(_manager.GetMessage("m1"));
            Assert.IsNull(_manager.GetMessage("nonexistent"));
        }

        // ── MarkRead ──

        [Test]
        public void MarkRead_SetsReadAt_And_FiresEvent()
        {
            _manager.AddMessage(MakeMessage("m1"));
            _currentTime = 2000;

            var result = _manager.MarkRead("m1");

            Assert.IsTrue(result);
            Assert.AreEqual(2000, _manager.GetMessage("m1").ReadAt);
            Assert.IsTrue(_manager.GetMessage("m1").IsRead);
            Assert.AreEqual(1, _readEvents.Count);
            Assert.AreEqual("m1", _readEvents[0]);
        }

        [Test]
        public void MarkRead_ReturnsFalseForNonexistent()
        {
            Assert.IsFalse(_manager.MarkRead("nonexistent"));
        }

        [Test]
        public void MarkRead_AlreadyRead_ReturnsTrueWithoutFiringEvent()
        {
            _manager.AddMessage(MakeMessage("m1"));
            _manager.MarkRead("m1");
            _readEvents.Clear();

            var result = _manager.MarkRead("m1");

            Assert.IsTrue(result);
            Assert.AreEqual(0, _readEvents.Count);
        }

        // ── MarkAllRead ──

        [Test]
        public void MarkAllRead_MarksAllUnread()
        {
            _manager.AddMessage(MakeMessage("m1", createdAt: 10));
            _manager.AddMessage(MakeMessage("m2", createdAt: 20));
            _manager.AddMessage(MakeMessage("m3", createdAt: 30));
            _manager.MarkRead("m1");
            _readEvents.Clear();

            _currentTime = 3000;
            _manager.MarkAllRead();

            Assert.IsTrue(_manager.GetMessage("m2").IsRead);
            Assert.IsTrue(_manager.GetMessage("m3").IsRead);
            Assert.AreEqual(2, _readEvents.Count);
            Assert.AreEqual(0, _manager.UnreadCount);
        }

        // ── ClaimAttachments ──

        [Test]
        public void ClaimAttachments_ReturnsAttachments_And_SetsClaimedAt()
        {
            var rewards = MakeRewards("currency", 100);
            _manager.AddMessage(MakeMessage("m1", attachments: rewards));
            _currentTime = 5000;

            var result = _manager.ClaimAttachments("m1");

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("currency", result[0]["type"]);
            Assert.AreEqual(100, result[0]["count"]);
            Assert.AreEqual(5000, _manager.GetMessage("m1").ClaimedAt);
            Assert.IsTrue(_manager.GetMessage("m1").IsClaimed);
            Assert.AreEqual(1, _claimedEvents.Count);
            Assert.AreEqual("m1", _claimedEvents[0]);
        }

        [Test]
        public void ClaimAttachments_ReturnsNull_IfAlreadyClaimed()
        {
            var rewards = MakeRewards("currency", 100);
            _manager.AddMessage(MakeMessage("m1", attachments: rewards));
            _manager.ClaimAttachments("m1");

            var result = _manager.ClaimAttachments("m1");
            Assert.IsNull(result);
        }

        [Test]
        public void ClaimAttachments_ReturnsNull_IfNoAttachments()
        {
            _manager.AddMessage(MakeMessage("m1"));

            var result = _manager.ClaimAttachments("m1");
            Assert.IsNull(result);
        }

        [Test]
        public void ClaimAttachments_ReturnsNull_IfNonexistent()
        {
            var result = _manager.ClaimAttachments("nonexistent");
            Assert.IsNull(result);
        }

        // ── ClaimAll ──

        [Test]
        public void ClaimAll_ClaimsAllUnclaimed_ReturnsCombinedAttachments()
        {
            var r1 = MakeRewards("currency", 50);
            var r2 = MakeRewards("item", 1);
            _manager.AddMessage(MakeMessage("m1", attachments: r1, createdAt: 10));
            _manager.AddMessage(MakeMessage("m2", attachments: r2, createdAt: 20));
            _manager.AddMessage(MakeMessage("m3", createdAt: 30)); // no attachments
            _manager.ClaimAttachments("m1"); // claim m1 first
            _claimedEvents.Clear();

            var result = _manager.ClaimAll();

            Assert.AreEqual(1, result.Count); // only m2's attachment
            Assert.AreEqual("item", result[0]["type"]);
            Assert.IsTrue(_manager.GetMessage("m2").IsClaimed);
            Assert.AreEqual(1, _claimedEvents.Count);
            Assert.AreEqual("m2", _claimedEvents[0]);
        }

        // ── DeleteMessage ──

        [Test]
        public void DeleteMessage_RemovesAndReturnsTrue()
        {
            _manager.AddMessage(MakeMessage("m1"));

            Assert.IsTrue(_manager.DeleteMessage("m1"));
            Assert.IsNull(_manager.GetMessage("m1"));
            Assert.AreEqual(0, _manager.GetAll().Count);
        }

        [Test]
        public void DeleteMessage_ReturnsFalseForNonexistent()
        {
            Assert.IsFalse(_manager.DeleteMessage("nonexistent"));
        }

        // ── DeleteExpired ──

        [Test]
        public void DeleteExpired_RemovesOnlyExpired()
        {
            _manager.AddMessage(MakeMessage("expired", expiresAt: 500, createdAt: 10));
            _manager.AddMessage(MakeMessage("not_expired", expiresAt: 2000, createdAt: 20));
            _manager.AddMessage(MakeMessage("no_expiry", expiresAt: 0, createdAt: 30));

            _currentTime = 1000;
            var count = _manager.DeleteExpired();

            Assert.AreEqual(1, count);
            Assert.IsNull(_manager.GetMessage("expired"));
            Assert.IsNotNull(_manager.GetMessage("not_expired"));
            Assert.IsNotNull(_manager.GetMessage("no_expiry"));
        }

        // ── DeleteRead ──

        [Test]
        public void DeleteRead_RemovesReadWithNoUnclaimedAttachments()
        {
            // read, no attachments -> should be deleted
            _manager.AddMessage(MakeMessage("read_no_attach", createdAt: 10));
            _manager.MarkRead("read_no_attach");

            // read, attachments claimed -> should be deleted
            var rewards = MakeRewards("currency", 10);
            _manager.AddMessage(MakeMessage("read_claimed", attachments: rewards, createdAt: 20));
            _manager.MarkRead("read_claimed");
            _manager.ClaimAttachments("read_claimed");

            // read, attachments unclaimed -> should NOT be deleted
            var rewards2 = MakeRewards("item", 1);
            _manager.AddMessage(MakeMessage("read_unclaimed", attachments: rewards2, createdAt: 30));
            _manager.MarkRead("read_unclaimed");

            // unread -> should NOT be deleted
            _manager.AddMessage(MakeMessage("unread", createdAt: 40));

            var count = _manager.DeleteRead();

            Assert.AreEqual(2, count);
            Assert.IsNull(_manager.GetMessage("read_no_attach"));
            Assert.IsNull(_manager.GetMessage("read_claimed"));
            Assert.IsNotNull(_manager.GetMessage("read_unclaimed"));
            Assert.IsNotNull(_manager.GetMessage("unread"));
        }

        // ── UnreadCount / UnclaimedCount ──

        [Test]
        public void UnreadCount_IsAccurate()
        {
            _manager.AddMessage(MakeMessage("m1"));
            _manager.AddMessage(MakeMessage("m2"));
            _manager.AddMessage(MakeMessage("m3"));

            Assert.AreEqual(3, _manager.UnreadCount);

            _manager.MarkRead("m1");
            Assert.AreEqual(2, _manager.UnreadCount);

            _manager.MarkAllRead();
            Assert.AreEqual(0, _manager.UnreadCount);
        }

        [Test]
        public void UnclaimedCount_IsAccurate()
        {
            var r1 = MakeRewards("currency", 10);
            var r2 = MakeRewards("item", 1);
            _manager.AddMessage(MakeMessage("with_attach1", attachments: r1));
            _manager.AddMessage(MakeMessage("with_attach2", attachments: r2));
            _manager.AddMessage(MakeMessage("no_attach"));

            Assert.AreEqual(2, _manager.UnclaimedCount);

            _manager.ClaimAttachments("with_attach1");
            Assert.AreEqual(1, _manager.UnclaimedCount);

            _manager.ClaimAll();
            Assert.AreEqual(0, _manager.UnclaimedCount);
        }

        // ── Persistence Round-Trip ──

        [Test]
        public void ToSaveDict_FromSaveDict_RoundTrip_PreservesAllFields()
        {
            var attachments = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["type"] = "currency", ["id"] = "gems", ["count"] = 50 },
                new Dictionary<string, object> { ["type"] = "item", ["id"] = "sword_01", ["count"] = 1 },
            };
            var msg = new MailMessage
            {
                Id = "mail_1",
                Sender = "admin",
                Subject = "Welcome!",
                Body = "Thanks for playing.",
                Attachments = attachments,
                ReadAt = 500,
                ClaimedAt = 600,
                ExpiresAt = 9999,
                Tags = new List<string> { "welcome", "promo" },
                CreatedAt = 100,
            };

            _manager.AddMessage(msg);

            var saved = _manager.ToSaveDict();

            // Create a new manager and restore
            var manager2 = new MailManager(() => _currentTime);
            manager2.FromSaveDict(saved);

            var restored = manager2.GetMessage("mail_1");
            Assert.IsNotNull(restored);
            Assert.AreEqual("mail_1", restored.Id);
            Assert.AreEqual("admin", restored.Sender);
            Assert.AreEqual("Welcome!", restored.Subject);
            Assert.AreEqual("Thanks for playing.", restored.Body);
            Assert.AreEqual(500, restored.ReadAt);
            Assert.AreEqual(600, restored.ClaimedAt);
            Assert.AreEqual(9999, restored.ExpiresAt);
            Assert.AreEqual(100, restored.CreatedAt);
            Assert.IsTrue(restored.IsRead);
            Assert.IsTrue(restored.IsClaimed);

            Assert.AreEqual(2, restored.Tags.Count);
            Assert.Contains("welcome", restored.Tags);
            Assert.Contains("promo", restored.Tags);

            Assert.AreEqual(2, restored.Attachments.Count);
            Assert.AreEqual("currency", restored.Attachments[0]["type"]);
            Assert.AreEqual("gems", restored.Attachments[0]["id"]);
            Assert.AreEqual(50, restored.Attachments[0]["count"]);
            Assert.AreEqual("item", restored.Attachments[1]["type"]);
            Assert.AreEqual("sword_01", restored.Attachments[1]["id"]);
        }

        [Test]
        public void MailMessage_ToSaveDict_FromSaveDict_RoundTrip()
        {
            var original = new MailMessage
            {
                Id = "roundtrip",
                Sender = "bot",
                Subject = "Test",
                Body = "Body text",
                Attachments = MakeRewards("stamina", 20),
                ReadAt = 0,
                ClaimedAt = 0,
                ExpiresAt = 0,
                Tags = new List<string> { "daily" },
                CreatedAt = 777,
            };

            var dict = original.ToSaveDict();
            var restored = MailMessage.FromSaveDict(dict);

            Assert.AreEqual(original.Id, restored.Id);
            Assert.AreEqual(original.Sender, restored.Sender);
            Assert.AreEqual(original.Subject, restored.Subject);
            Assert.AreEqual(original.Body, restored.Body);
            Assert.AreEqual(original.ReadAt, restored.ReadAt);
            Assert.AreEqual(original.ClaimedAt, restored.ClaimedAt);
            Assert.AreEqual(original.ExpiresAt, restored.ExpiresAt);
            Assert.AreEqual(original.CreatedAt, restored.CreatedAt);
            Assert.IsFalse(restored.IsRead);
            Assert.IsFalse(restored.IsClaimed);
            Assert.IsTrue(restored.HasAttachments);
            Assert.AreEqual(1, restored.Attachments.Count);
            Assert.AreEqual("stamina", restored.Attachments[0]["type"]);
            Assert.AreEqual(1, restored.Tags.Count);
            Assert.AreEqual("daily", restored.Tags[0]);
        }
    }
}
