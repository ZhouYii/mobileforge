using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Social hub page matching original Social_Menu_View.
    /// Shows player ID/rank header and menu list with badge counts.
    /// Uses TosPageHelper for consistent header/layout.
    /// </summary>
    public class SocialPage : PageBase<SocialScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private RectTransform _playerInfoCard;
        [SerializeField] private Text _playerNameLabel;
        [SerializeField] private Text _playerIdLabel;
        [SerializeField] private Transform _scrollContent;

        protected override void OnBind(SocialScreen screen)
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
                    _titleLabel.text = "Social";
                if (_playerNameLabel != null)
                    _playerNameLabel.text = $"\u263A {screen.PlayerName}";
                if (_playerIdLabel != null)
                    _playerIdLabel.text = $"ID: {screen.PlayerId}  |  Rank: {screen.PlayerRank}";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(SocialScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            var h = TosPageHelper.BuildHeader(this, transform,
                "Social", TosTheme.SocialAccent, () => screen.GoBack());

            // ── Player Info Card ──
            var infoCard = CreateRegion("PlayerInfo", MFAnchor.Top,
                new MFPadding(TosTheme.ContentPadH + 4, TosTheme.ContentTopOffset, TosTheme.ContentPadH + 4, 0));
            infoCard.sizeDelta = new Vector2(0, 64);
            infoCard.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.95f);

            var infoVBox = CreateVBox("InfoContent", infoCard, spacing: 4f);
            var infoLayout = infoVBox.GetComponent<VerticalLayoutGroup>();
            if (infoLayout != null) infoLayout.padding = new RectOffset(14, 14, 8, 8);

            TosPageHelper.MakeLabel(infoVBox, $"\u263A {screen.PlayerName}",
                TosTheme.FontSubheader, Color.white, 22);
            TosPageHelper.MakeLabel(infoVBox, $"ID: {screen.PlayerId}  |  Rank: {screen.PlayerRank}",
                TosTheme.FontSmall, TosTheme.TextMuted, 16);

            // ── Menu List ──
            var listArea = TosPageHelper.BuildContentArea(transform, "MenuList",
                extraTopPad: 70f);
            _scrollContent = TosPageHelper.BuildScrollContent(listArea);
        }

        private void InitContent(SocialScreen screen)
        {
            Transform target = _scrollContent ?? transform;
            var canvasGroups = new List<CanvasGroup>();
            foreach (var entry in screen.MenuEntries)
            {
                var entryObj = CreateMenuEntry(target, entry, screen);
                var cg = entryObj.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                canvasGroups.Add(cg);
            }
            if (canvasGroups.Count > 0)
                StartCoroutine(MFUIAnim.StaggerFadeIn(canvasGroups, 0.25f, 0.05f));
        }

        private GameObject CreateMenuEntry(Transform parent, SocialScreen.SocialMenuEntry entry, SocialScreen screen)
        {
            var go = new GameObject($"Entry_{entry.ScreenId}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = TosTheme.BgPanel;
            go.AddComponent<LayoutElement>().preferredHeight = 54;

            var hbox = go.AddComponent<HorizontalLayoutGroup>();
            hbox.padding = new RectOffset(12, 12, 6, 6);
            hbox.spacing = 10;
            hbox.childAlignment = TextAnchor.MiddleLeft;
            hbox.childForceExpandWidth = false;
            hbox.childForceExpandHeight = true;

            // Icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredWidth = 30;
            var iconText = iconGo.AddComponent<Text>();
            iconText.text = entry.Icon;
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 20;
            iconText.color = TosTheme.SocialAccent;
            iconText.alignment = TextAnchor.MiddleCenter;

            // Label
            TosPageHelper.MakeLabel(go.transform, entry.Label,
                TosTheme.FontBody + 1, Color.white, 0)
                .gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;

            // Badge count
            if (entry.BadgeCount > 0)
            {
                var badgeGo = new GameObject("Badge");
                badgeGo.transform.SetParent(go.transform, false);
                badgeGo.AddComponent<Image>().color = TosTheme.BadgeRed;
                var badgeLe = badgeGo.AddComponent<LayoutElement>();
                badgeLe.preferredWidth = 26;
                badgeLe.preferredHeight = 20;

                var badgeLbl = new GameObject("Count");
                badgeLbl.transform.SetParent(badgeGo.transform, false);
                var blRect = badgeLbl.AddComponent<RectTransform>();
                blRect.anchorMin = Vector2.zero; blRect.anchorMax = Vector2.one;
                blRect.offsetMin = Vector2.zero; blRect.offsetMax = Vector2.zero;
                var blText = badgeLbl.AddComponent<Text>();
                blText.text = entry.BadgeCount.ToString();
                blText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                blText.fontSize = 10;
                blText.color = Color.white;
                blText.alignment = TextAnchor.MiddleCenter;
                blText.fontStyle = FontStyle.Bold;
            }

            // Arrow
            TosPageHelper.MakeLabel(go.transform, "\u25B6", TosTheme.FontSmall,
                TosTheme.TextMuted, 0, TextAnchor.MiddleCenter)
                .gameObject.GetComponent<LayoutElement>().preferredWidth = 18;

            // Click handler + press feedback
            string targetId = entry.ScreenId;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => screen.Navigate(targetId));
            TosPageHelper.AddPressFeedback(go);

            return go;
        }
    }
}
