using System;
using System.Collections.Generic;
using System.Linq;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace TowerOfSaviors
{
    /// <summary>
    /// Mail inbox screen matching original Social_Mail_Box_List_View.
    /// Shows received mail with rewards, read/unread state, claim functionality.
    /// </summary>
    public class MailScreen : IScreen
    {
        private UIRouter _router;
        private PlayerState _playerState;

        public List<MailEntry> Mails { get; } = new();
        public int UnreadCount => Mails.Count(m => !m.IsRead);
        public string StatusMessage { get; private set; } = "";

        public class MailEntry
        {
            public int Id { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
            public string Sender { get; set; }
            public bool IsRead { get; set; }
            public bool HasReward { get; set; }
            public string RewardType { get; set; }
            public int RewardAmount { get; set; }
            public bool RewardClaimed { get; set; }
            public string DateStr { get; set; }
            public bool IsFromGM { get; set; }
        }

        public MailScreen() { }

        public void Setup(UIRouter router, PlayerState playerState)
        {
            _router = router;
            _playerState = playerState;
            GenerateSampleMails();
        }

        private void GenerateSampleMails()
        {
            Mails.Clear();
            Mails.Add(new MailEntry
            {
                Id = 1,
                Subject = "Welcome to Tower of Saviors!",
                Body = "Welcome, Summoner! Begin your journey by entering a dungeon. Tap the Quest tab to get started.",
                Sender = "System",
                IsFromGM = true,
                IsRead = false,
                HasReward = true,
                RewardType = "gems",
                RewardAmount = 10,
                DateStr = DateTime.Now.AddDays(-1).ToString("MM/dd")
            });
            Mails.Add(new MailEntry
            {
                Id = 2,
                Subject = "Maintenance Compensation",
                Body = "Thank you for your patience during server maintenance. Please accept this gift as compensation.",
                Sender = "System",
                IsFromGM = true,
                IsRead = false,
                HasReward = true,
                RewardType = "gems",
                RewardAmount = 5,
                DateStr = DateTime.Now.ToString("MM/dd")
            });
            Mails.Add(new MailEntry
            {
                Id = 3,
                Subject = "Friend Request Accepted",
                Body = "Your friend request has been accepted! You can now select their monster as a helper.",
                Sender = "Player123",
                IsRead = false,
                HasReward = false,
                DateStr = DateTime.Now.ToString("MM/dd")
            });
        }

        public void ReadMail(int mailId)
        {
            var mail = Mails.Find(m => m.Id == mailId);
            if (mail != null)
                mail.IsRead = true;
        }

        public string ClaimReward(int mailId)
        {
            var mail = Mails.Find(m => m.Id == mailId);
            if (mail == null) return "Mail not found";
            if (!mail.HasReward) return "No reward to claim";
            if (mail.RewardClaimed) return "Already claimed";

            mail.RewardClaimed = true;
            mail.IsRead = true;
            int current = Convert.ToInt32(
                _playerState?.GetValue("currencies", mail.RewardType, 0) ?? 0);
            _playerState?.SetValue("currencies", mail.RewardType, current + mail.RewardAmount);

            StatusMessage = $"Claimed +{mail.RewardAmount} {mail.RewardType}!";
            return StatusMessage;
        }

        public string ClaimAll()
        {
            int totalClaimed = 0;
            foreach (var mail in Mails.Where(m => m.HasReward && !m.RewardClaimed))
            {
                mail.RewardClaimed = true;
                mail.IsRead = true;
                int current = Convert.ToInt32(
                    _playerState?.GetValue("currencies", mail.RewardType, 0) ?? 0);
                _playerState?.SetValue("currencies", mail.RewardType, current + mail.RewardAmount);
                totalClaimed++;
            }

            StatusMessage = totalClaimed > 0
                ? $"Claimed {totalClaimed} reward(s)!"
                : "No rewards to claim";
            return StatusMessage;
        }

        public void DeleteMail(int mailId)
        {
            Mails.RemoveAll(m => m.Id == mailId && (!m.HasReward || m.RewardClaimed));
        }

        public void OnEnter(Dictionary<string, object> parameters) { }
        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        public void GoBack()
        {
            _router?.Pop();
        }
    }
}
