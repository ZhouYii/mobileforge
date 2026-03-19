using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Mail inbox page matching original Social_Mail_Box_List_View.
    /// Shows mail list with read/unread indicators, reward icons, and claim actions.
    /// </summary>
    public class MailPage : PageBase<MailScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Transform _mailListContent;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Text _countLabel;

        protected override void OnBind(MailScreen screen)
        {
            if (!IsPrefabPage)
            {
                BuildSkeleton(screen);
            }
            else
            {
                if (_backButton != null)
                    _backButton.onClick.AddListener(() => screen.GoBack());
                if (_titleLabel != null)
                    _titleLabel.text = "Mail";
                if (_countLabel != null)
                {
                    _countLabel.text = $"{screen.UnreadCount} new";
                    _countLabel.color = screen.UnreadCount > 0 ? new Color(1f, 0.4f, 0.4f) : TosTheme.TextMuted;
                }
            }

            InitContent(screen);
        }

        private void BuildSkeleton(MailScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            var h = TosPageHelper.BuildHeader(this, transform,
                "Mail", TosTheme.MailAccent, () => screen.GoBack());

            // Unread count badge in header
            var countGo = new GameObject("Count");
            countGo.transform.SetParent(h.Header, false);
            countGo.AddComponent<LayoutElement>().preferredWidth = 56;
            _countLabel = countGo.AddComponent<Text>();
            _countLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _countLabel.fontSize = TosTheme.FontSmall;
            _countLabel.color = screen.UnreadCount > 0 ? new Color(1f, 0.4f, 0.4f) : TosTheme.TextMuted;
            _countLabel.alignment = TextAnchor.MiddleRight;
            _countLabel.text = $"{screen.UnreadCount} new";

            // ── Mail List (scrollable) ──
            var listArea = CreateRegion("MailList", MFAnchor.Fill,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset + 42, TosTheme.ContentPadH, TosTheme.ContentBottomNav + 8));
            _mailListContent = TosPageHelper.BuildScrollContent(listArea, spacing: 4f);

            // Status label
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(transform, false);
            var sRect = statusGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.1f, 0.06f);
            sRect.anchorMax = new Vector2(0.9f, 0.09f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            _statusLabel = statusGo.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusLabel.fontSize = TosTheme.FontBody;
            _statusLabel.color = TosTheme.TextGold;
            _statusLabel.alignment = TextAnchor.MiddleCenter;
        }

        private void InitContent(MailScreen screen)
        {
            // ── Claim All Button ──
            var claimAllArea = CreateRegion("ClaimAll", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH, TosTheme.ContentTopOffset, TosTheme.ContentPadH, 0));
            claimAllArea.sizeDelta = new Vector2(0, 36);

            var claimAllBtn = SpawnPrimitive<MFButton>(claimAllArea);
            claimAllBtn.SetLabel("Claim All Rewards");
            claimAllBtn.SetColor(new Color(0.5f, 0.35f, 0.1f), TosTheme.TextGold);
            claimAllBtn.OnClick = () =>
            {
                string result = screen.ClaimAll();
                if (_statusLabel != null) _statusLabel.text = result;
                UIServices.Toasts?.ShowToast(result);
                RebuildMailList();
            };

            // ── Mail List ──
            if (_mailListContent != null)
                BuildMailList();
        }

        private void BuildMailList()
        {
            var canvasGroups = new List<CanvasGroup>();
            foreach (var mail in _screen.Mails)
            {
                var entry = CreateMailEntry(_mailListContent, mail);
                var cg = entry.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                canvasGroups.Add(cg);
            }

            if (_screen.Mails.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(_mailListContent, false);
                emptyGo.AddComponent<LayoutElement>().preferredHeight = 60;
                var emptyText = emptyGo.AddComponent<Text>();
                emptyText.text = "No mail";
                emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                emptyText.fontSize = TosTheme.FontBody;
                emptyText.color = TosTheme.TextMuted;
                emptyText.alignment = TextAnchor.MiddleCenter;
            }

            if (canvasGroups.Count > 0)
                StartCoroutine(MFUIAnim.StaggerFadeIn(canvasGroups, 0.25f, 0.06f));
        }

        private void RebuildMailList()
        {
            for (int i = _mailListContent.childCount - 1; i >= 0; i--)
                Destroy(_mailListContent.GetChild(i).gameObject);
            BuildMailList();
            UpdateCountLabel();
        }

        private void UpdateCountLabel()
        {
            int unread = _screen.UnreadCount;
            _countLabel.text = $"{unread} new";
            _countLabel.color = unread > 0 ? new Color(1f, 0.4f, 0.4f) : TosTheme.TextMuted;
        }

        private GameObject CreateMailEntry(Transform parent, MailScreen.MailEntry mail)
        {
            var go = new GameObject($"Mail_{mail.Id}");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 72;

            Color entryBg = mail.IsRead
                ? TosTheme.BgPanel
                : new Color(0.12f, 0.12f, 0.18f, 0.95f);
            go.AddComponent<Image>().color = entryBg;

            var hbox = go.AddComponent<HorizontalLayoutGroup>();
            hbox.padding = new RectOffset(10, 10, 6, 6);
            hbox.spacing = 8;
            hbox.childAlignment = TextAnchor.MiddleLeft;
            hbox.childForceExpandWidth = false;
            hbox.childForceExpandHeight = true;

            // Unread indicator / sender icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredWidth = 30;
            var iconText = iconGo.AddComponent<Text>();
            iconText.text = mail.IsFromGM ? "\u2709" : "\u263A"; // ✉ or ☺
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 20;
            iconText.color = mail.IsRead ? TosTheme.TextMuted :
                (mail.IsFromGM ? TosTheme.TextGold : new Color(0.4f, 0.8f, 1f));
            iconText.alignment = TextAnchor.MiddleCenter;

            // Content column
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(go.transform, false);
            contentGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var vbox = contentGo.AddComponent<VerticalLayoutGroup>();
            vbox.spacing = 2;
            vbox.childForceExpandHeight = false;

            // Subject
            var subGo = new GameObject("Subject");
            subGo.transform.SetParent(contentGo.transform, false);
            subGo.AddComponent<LayoutElement>().preferredHeight = 20;
            var subText = subGo.AddComponent<Text>();
            subText.text = mail.Subject;
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = TosTheme.FontBody;
            subText.color = mail.IsRead ? TosTheme.TextMuted : Color.white;
            subText.fontStyle = mail.IsRead ? FontStyle.Normal : FontStyle.Bold;

            // Sender + date
            var metaGo = new GameObject("Meta");
            metaGo.transform.SetParent(contentGo.transform, false);
            metaGo.AddComponent<LayoutElement>().preferredHeight = 16;
            var metaText = metaGo.AddComponent<Text>();
            metaText.text = $"From: {mail.Sender}  |  {mail.DateStr}";
            metaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            metaText.fontSize = 10;
            metaText.color = TosTheme.TextMuted;

            // Reward preview
            if (mail.HasReward)
            {
                var rewardGo = new GameObject("Reward");
                rewardGo.transform.SetParent(contentGo.transform, false);
                rewardGo.AddComponent<LayoutElement>().preferredHeight = 16;
                var rewardText = rewardGo.AddComponent<Text>();
                rewardText.text = mail.RewardClaimed
                    ? $"\u2713 {mail.RewardType} x{mail.RewardAmount} (claimed)"
                    : $"\u2666 {mail.RewardType} x{mail.RewardAmount}";
                rewardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                rewardText.fontSize = 10;
                rewardText.color = mail.RewardClaimed ? TosTheme.TextMuted : TosTheme.TextGold;
            }

            // Claim button (if has unclaimed reward)
            if (mail.HasReward && !mail.RewardClaimed)
            {
                var claimGo = new GameObject("ClaimBtn");
                claimGo.transform.SetParent(go.transform, false);
                claimGo.AddComponent<Image>().color = new Color(0.5f, 0.35f, 0.1f);
                var claimLe = claimGo.AddComponent<LayoutElement>();
                claimLe.preferredWidth = 56;

                var claimLbl = new GameObject("Label");
                claimLbl.transform.SetParent(claimGo.transform, false);
                var clRect = claimLbl.AddComponent<RectTransform>();
                clRect.anchorMin = Vector2.zero; clRect.anchorMax = Vector2.one;
                clRect.offsetMin = Vector2.zero; clRect.offsetMax = Vector2.zero;
                var clText = claimLbl.AddComponent<Text>();
                clText.text = "Claim";
                clText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                clText.fontSize = 11;
                clText.color = Color.white;
                clText.alignment = TextAnchor.MiddleCenter;

                int mailId = mail.Id;
                var btn = claimGo.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    string result = _screen.ClaimReward(mailId);
                    _statusLabel.text = result;
                    UIServices.Toasts?.ShowToast(result);
                    RebuildMailList();
                });
            }

            // Tap to read
            int readId = mail.Id;
            var readBtn = go.AddComponent<Button>();
            readBtn.onClick.AddListener(() =>
            {
                _screen.ReadMail(readId);
                ShowMailDetail(mail);
            });

            return go;
        }

        private void ShowMailDetail(MailScreen.MailEntry mail)
        {
            UIServices.Popups?.ShowCustomDialog($"mail_{mail.Id}", dialog =>
            {
                dialog.SetTitle(mail.Subject);
                string body = mail.Body;
                if (mail.HasReward)
                    body += $"\n\nReward: {mail.RewardAmount} {mail.RewardType}" +
                            (mail.RewardClaimed ? " (claimed)" : " (unclaimed)");
                dialog.SetMessage(body);

                if (mail.HasReward && !mail.RewardClaimed)
                {
                    int id = mail.Id;
                    dialog.AddButton("Claim", () =>
                    {
                        string result = _screen.ClaimReward(id);
                        _statusLabel.text = result;
                        UIServices.Toasts?.ShowToast(result);
                        RebuildMailList();
                        dialog.Dismiss();
                    }, new Color(0.5f, 0.35f, 0.1f));
                }
                dialog.AddButton("Close", () =>
                {
                    RebuildMailList();
                    dialog.Dismiss();
                }, new Color(0.3f, 0.3f, 0.35f));
            });
        }
    }
}
