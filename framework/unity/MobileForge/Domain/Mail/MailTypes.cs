using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// A single mail/inbox message with optional reward attachments.
    /// </summary>
    public class MailMessage
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<Dictionary<string, object>> Attachments { get; set; } = new();
        public long ReadAt { get; set; }
        public long ClaimedAt { get; set; }
        public long ExpiresAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public long CreatedAt { get; set; }

        // ── Computed Properties ──

        public bool IsRead => ReadAt > 0;
        public bool IsClaimed => ClaimedAt > 0;
        public bool HasAttachments => Attachments?.Count > 0;

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["sender"] = Sender,
                ["subject"] = Subject,
                ["body"] = Body,
                ["attachments"] = Attachments?.Select(a => (object)new Dictionary<string, object>(a)).ToList()
                    ?? new List<object>(),
                ["read_at"] = ReadAt,
                ["claimed_at"] = ClaimedAt,
                ["expires_at"] = ExpiresAt,
                ["tags"] = Tags?.Select(t => (object)t).ToList() ?? new List<object>(),
                ["created_at"] = CreatedAt,
            };
        }

        public static MailMessage FromSaveDict(Dictionary<string, object> data)
        {
            var msg = new MailMessage
            {
                Id = (string)data["id"],
                Sender = (string)data["sender"],
                Subject = (string)data["subject"],
                Body = (string)data["body"],
                ReadAt = Convert.ToInt64(data["read_at"]),
                ClaimedAt = Convert.ToInt64(data["claimed_at"]),
                ExpiresAt = Convert.ToInt64(data["expires_at"]),
                CreatedAt = Convert.ToInt64(data["created_at"]),
            };

            if (data.TryGetValue("attachments", out var attObj) && attObj is List<object> attList)
            {
                msg.Attachments = attList
                    .OfType<Dictionary<string, object>>()
                    .Select(d => new Dictionary<string, object>(d))
                    .ToList();
            }

            if (data.TryGetValue("tags", out var tagsObj) && tagsObj is List<object> tagsList)
            {
                msg.Tags = tagsList.Select(t => t.ToString()).ToList();
            }

            return msg;
        }
    }
}
