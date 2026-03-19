using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Manages a player's mail inbox — receiving, reading, claiming attachments, cleanup.
    /// </summary>
    public class MailManager
    {
        private readonly Func<long> _getTime;
        private readonly Dictionary<string, MailMessage> _messages = new();

        // ── Events ──

        public event Action<MailMessage> MailReceived;
        public event Action<string> MailRead;
        public event Action<string> MailClaimed;

        // ── Computed Properties ──

        public int UnreadCount => _messages.Values.Count(m => !m.IsRead);

        public int UnclaimedCount => _messages.Values.Count(m => m.HasAttachments && !m.IsClaimed);

        // ── Constructor ──

        public MailManager(Func<long> timeProvider)
        {
            _getTime = timeProvider;
        }

        // ── Public API ──

        /// <summary>Add a message to the inbox and fire MailReceived.</summary>
        public void AddMessage(MailMessage message)
        {
            _messages[message.Id] = message;
            MailReceived?.Invoke(message);
        }

        /// <summary>Return all messages sorted by CreatedAt descending (newest first).</summary>
        public List<MailMessage> GetAll()
        {
            return _messages.Values
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
        }

        /// <summary>Return only unread messages.</summary>
        public List<MailMessage> GetUnread()
        {
            return _messages.Values
                .Where(m => !m.IsRead)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
        }

        /// <summary>Return messages with a specific tag.</summary>
        public List<MailMessage> GetByTag(string tag)
        {
            return _messages.Values
                .Where(m => m.Tags != null && m.Tags.Contains(tag))
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
        }

        /// <summary>Return a specific message by id, or null.</summary>
        public MailMessage GetMessage(string id)
        {
            return _messages.TryGetValue(id, out var msg) ? msg : null;
        }

        /// <summary>Mark a message as read. Returns true if the message was found and marked.</summary>
        public bool MarkRead(string id)
        {
            if (!_messages.TryGetValue(id, out var msg)) return false;
            if (msg.IsRead) return true;
            msg.ReadAt = _getTime();
            MailRead?.Invoke(id);
            return true;
        }

        /// <summary>Mark all unread messages as read.</summary>
        public void MarkAllRead()
        {
            var now = _getTime();
            foreach (var msg in _messages.Values)
            {
                if (!msg.IsRead)
                {
                    msg.ReadAt = now;
                    MailRead?.Invoke(msg.Id);
                }
            }
        }

        /// <summary>
        /// Claim attachments from a message. Returns the attachments list on success,
        /// or null if already claimed or no attachments.
        /// </summary>
        public List<Dictionary<string, object>> ClaimAttachments(string id)
        {
            if (!_messages.TryGetValue(id, out var msg)) return null;
            if (msg.IsClaimed) return null;
            if (!msg.HasAttachments) return null;

            msg.ClaimedAt = _getTime();
            MailClaimed?.Invoke(id);
            return msg.Attachments.Select(a => new Dictionary<string, object>(a)).ToList();
        }

        /// <summary>
        /// Claim all unclaimed messages with attachments. Returns combined attachments list.
        /// </summary>
        public List<Dictionary<string, object>> ClaimAll()
        {
            var all = new List<Dictionary<string, object>>();
            var now = _getTime();
            foreach (var msg in _messages.Values)
            {
                if (!msg.IsClaimed && msg.HasAttachments)
                {
                    msg.ClaimedAt = now;
                    all.AddRange(msg.Attachments.Select(a => new Dictionary<string, object>(a)));
                    MailClaimed?.Invoke(msg.Id);
                }
            }
            return all;
        }

        /// <summary>Delete a message. Returns true if it existed.</summary>
        public bool DeleteMessage(string id)
        {
            return _messages.Remove(id);
        }

        /// <summary>Remove all messages past their ExpiresAt. Returns count removed.</summary>
        public int DeleteExpired()
        {
            var now = _getTime();
            var expired = _messages.Values
                .Where(m => m.ExpiresAt > 0 && m.ExpiresAt <= now)
                .Select(m => m.Id)
                .ToList();

            foreach (var id in expired)
                _messages.Remove(id);

            return expired.Count;
        }

        /// <summary>
        /// Remove all read messages that have no unclaimed attachments. Returns count removed.
        /// </summary>
        public int DeleteRead()
        {
            var removable = _messages.Values
                .Where(m => m.IsRead && (!m.HasAttachments || m.IsClaimed))
                .Select(m => m.Id)
                .ToList();

            foreach (var id in removable)
                _messages.Remove(id);

            return removable.Count;
        }

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            var messagesList = _messages.Values
                .Select(m => (object)m.ToSaveDict())
                .ToList();

            return new Dictionary<string, object>
            {
                ["messages"] = messagesList,
            };
        }

        public void FromSaveDict(Dictionary<string, object> data)
        {
            _messages.Clear();
            if (data.TryGetValue("messages", out var msgsObj) && msgsObj is List<object> msgsList)
            {
                foreach (var obj in msgsList)
                {
                    if (obj is Dictionary<string, object> msgDict)
                    {
                        var msg = MailMessage.FromSaveDict(msgDict);
                        _messages[msg.Id] = msg;
                    }
                }
            }
        }
    }
}
