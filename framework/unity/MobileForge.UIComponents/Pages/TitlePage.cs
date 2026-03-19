using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Home/title page matching original TOS main menu (Worldmap_WorldMap_View).
    /// Shows player info card, currency bar, feature grid, announcements,
    /// and triggers daily login popup. The bottom nav bar is persistent externally.
    /// </summary>
    public class TitlePage : PageBase<TitleScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Text _titleLabel;
        [SerializeField] private RectTransform _playerInfoCard;
        [SerializeField] private Transform _featureGrid;
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _playerRankText;
        [SerializeField] private Text _gemsText;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _staminaText;

        protected override void OnBind(TitleScreen screen)
        {
            if (!IsPrefabPage)
            {
                BuildSkeleton(screen);
            }
            else
            {
                // Wire static labels
                if (_titleLabel != null)
                    _titleLabel.text = screen.Title;
                if (_playerNameText != null)
                    _playerNameText.text = $"\u263A {screen.PlayerName}";
                if (_playerRankText != null)
                    _playerRankText.text = $"Rank {screen.PlayerRank}";
                if (_gemsText != null)
                    _gemsText.text = $"\u2666 {screen.Gems} Gems";
                if (_coinsText != null)
                    _coinsText.text = $"\u25C9 {screen.Coins} Coins";
                if (_staminaText != null)
                    _staminaText.text = $"\u26A1 {screen.CurrentStamina}/{screen.MaxStamina} ST";
            }

            InitContent(screen);
        }

        private void InitContent(TitleScreen screen)
        {
            // Feature grid
            if (_featureGrid != null)
            {
                var grid = _featureGrid.gameObject.AddComponent<GridLayoutGroup>();
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.cellSize = new Vector2(155, 100);
                grid.spacing = new Vector2(10, 10);
                grid.childAlignment = TextAnchor.MiddleCenter;

                var featureCgs = new List<CanvasGroup>();
                foreach (var feature in screen.Features)
                {
                    var featureGo = CreateFeatureButton(_featureGrid, feature, screen);
                    var cg = featureGo.AddComponent<CanvasGroup>();
                    cg.alpha = 0;
                    featureCgs.Add(cg);
                }
                if (featureCgs.Count > 0)
                    StartCoroutine(MFUIAnim.StaggerFadeIn(featureCgs, 0.3f, TosTheme.StaggerButton));
            }

            // Play button at bottom
            var playArea = CreateRegion("PlayArea", MFAnchor.Bottom, new MFPadding(40, 0, 40, 68));
            playArea.sizeDelta = new Vector2(0, 56);
            var playBtn = SpawnPrimitive<MFButton>(playArea);
            playBtn.gameObject.name = "btn_play";
            playBtn.SetLabel("\u25B6  PLAY");
            playBtn.SetColor(new Color(0.8f, 0.3f, 0.1f), Color.white);
            playBtn.OnClick = () => screen.OnStartPressed();
            TosPageHelper.AddPressFeedback(playBtn.gameObject);
            var playCg = playBtn.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.DelayedFadeIn(playCg, 0.4f, 0.3f));

            // Daily login check
            ShowDailyLoginIfNeeded();

            // Announcements
            if (screen.Announcements.Count > 0)
            {
                var annoArea = CreateRegion("Announcements", MFAnchor.Top, new MFPadding(14, 148, 14, 0));
                annoArea.sizeDelta = new Vector2(0, 32);
                annoArea.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.1f, 0.2f, 0.9f);

                var annoText = new GameObject("AnnoText");
                annoText.transform.SetParent(annoArea, false);
                var atRect = annoText.AddComponent<RectTransform>();
                atRect.anchorMin = Vector2.zero; atRect.anchorMax = Vector2.one;
                atRect.offsetMin = new Vector2(8, 0); atRect.offsetMax = new Vector2(-8, 0);
                var at = annoText.AddComponent<Text>();
                at.text = $"\u26A0 {screen.Announcements[0]}";
                at.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                at.fontSize = TosTheme.FontSmall;
                at.color = new Color(1f, 0.8f, 0.4f);
                at.alignment = TextAnchor.MiddleLeft;
                StartCoroutine(RotateAnnouncements(at, screen.Announcements));

                var annoBtn = annoArea.gameObject.AddComponent<Button>();
                annoBtn.onClick.AddListener(() => ShowAnnouncementPopup(screen));
            }
        }

        private void BuildSkeleton(TitleScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = TosTheme.BgDark;

            // ── Golden Title with shadow (entrance animation) ──
            var titleRegion = CreateRegion("TitleArea", MFAnchor.Top, new MFPadding(20, 16, 20, 0));
            titleRegion.sizeDelta = new Vector2(0, 56);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(titleRegion, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleGo.AddComponent<Text>();
            titleText.text = screen.Title;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = TosTheme.FontTitle;
            titleText.color = TosTheme.TextGold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleGo.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.6f);
            titleGo.GetComponent<Shadow>().effectDistance = new Vector2(2, -2);

            var titleCg = titleGo.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(titleCg, 0.5f));

            // ── Player Info + Currency Bar ──
            var infoCard = CreateRegion("PlayerInfo", MFAnchor.Top, new MFPadding(14, 76, 14, 0));
            infoCard.sizeDelta = new Vector2(0, 64);
            infoCard.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.13f, 0.95f);

            var infoHBox = CreateHBox("InfoContent", infoCard, spacing: 12f);
            var infoLayout = infoHBox.GetComponent<HorizontalLayoutGroup>();
            if (infoLayout != null)
            {
                infoLayout.padding = new RectOffset(12, 12, 8, 8);
                infoLayout.childAlignment = TextAnchor.MiddleLeft;
            }

            // Player name + rank
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(infoHBox, false);
            playerGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var playerVBox = playerGo.AddComponent<VerticalLayoutGroup>();
            playerVBox.spacing = 2;
            playerVBox.childForceExpandHeight = false;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(playerGo.transform, false);
            nameGo.AddComponent<LayoutElement>().preferredHeight = 22;
            var nameText = nameGo.AddComponent<Text>();
            nameText.text = $"\u263A {screen.PlayerName}";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = TosTheme.FontSubheader;
            nameText.color = Color.white;

            var rankGo = new GameObject("Rank");
            rankGo.transform.SetParent(playerGo.transform, false);
            rankGo.AddComponent<LayoutElement>().preferredHeight = 16;
            var rankText = rankGo.AddComponent<Text>();
            rankText.text = $"Rank {screen.PlayerRank}";
            rankText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rankText.fontSize = TosTheme.FontSmall;
            rankText.color = TosTheme.TextMuted;

            // Currencies
            var currGo = new GameObject("Currencies");
            currGo.transform.SetParent(infoHBox, false);
            currGo.AddComponent<LayoutElement>().preferredWidth = 160;
            var currVBox = currGo.AddComponent<VerticalLayoutGroup>();
            currVBox.spacing = 1;
            currVBox.childForceExpandHeight = false;
            currVBox.childAlignment = TextAnchor.MiddleRight;

            MakeCurrLabel(currGo.transform, $"\u2666 {screen.Gems} Gems", TosTheme.TextGold);
            MakeCurrLabel(currGo.transform, $"\u25C9 {screen.Coins} Coins", new Color(0.9f, 0.8f, 0.3f));
            MakeCurrLabel(currGo.transform, $"\u26A1 {screen.CurrentStamina}/{screen.MaxStamina} ST", new Color(0.3f, 0.9f, 0.3f));

            var infoCg = infoCard.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(MFUIAnim.FadeIn(infoCg, 0.6f));

            // ── Feature Grid container (2x3) ──
            var gridArea = CreateRegion("Features", MFAnchor.Fill, new MFPadding(14, 190, 14, 80));
            _featureGrid = CreateGrid("FeatureGrid", gridArea, 3, 140);
        }

        private GameObject CreateFeatureButton(Transform parent, TitleScreen.FeatureButton feature, TitleScreen screen)
        {
            var go = new GameObject($"Feature_{feature.ScreenId}");
            go.transform.SetParent(parent, false);

            // Tinted background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(feature.AccentColor.r * 0.15f, feature.AccentColor.g * 0.15f,
                feature.AccentColor.b * 0.15f, 0.92f);

            var vbox = go.AddComponent<VerticalLayoutGroup>();
            vbox.padding = new RectOffset(6, 6, 10, 8);
            vbox.spacing = 4;
            vbox.childAlignment = TextAnchor.MiddleCenter;
            vbox.childForceExpandWidth = true;
            vbox.childForceExpandHeight = false;

            // Icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredHeight = 36;
            var iconText = iconGo.AddComponent<Text>();
            iconText.text = feature.Icon;
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 30;
            iconText.color = feature.AccentColor;
            iconText.alignment = TextAnchor.MiddleCenter;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.AddComponent<LayoutElement>().preferredHeight = 20;
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = feature.Label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = TosTheme.FontBody;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            // Accent underline
            var line = new GameObject("Underline");
            line.transform.SetParent(go.transform, false);
            line.AddComponent<LayoutElement>().preferredHeight = 2;
            line.AddComponent<Image>().color = new Color(feature.AccentColor.r, feature.AccentColor.g,
                feature.AccentColor.b, 0.5f);

            // Click handler with scale feedback
            string targetId = feature.ScreenId;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => screen.OnNavSelected(targetId));

            TosPageHelper.AddPressFeedback(go);

            return go;
        }

        private IEnumerator RotateAnnouncements(Text textComponent, List<string> announcements)
        {
            int index = 0;
            while (textComponent != null)
            {
                yield return new WaitForSeconds(4f);
                index = (index + 1) % announcements.Count;
                if (textComponent != null)
                {
                    // Fade transition
                    var cg = textComponent.GetComponent<CanvasGroup>();
                    if (cg == null) cg = textComponent.gameObject.AddComponent<CanvasGroup>();
                    // Fade out
                    float t = 0;
                    while (t < 0.2f && textComponent != null)
                    {
                        t += Time.deltaTime;
                        cg.alpha = 1 - (t / 0.2f);
                        yield return null;
                    }
                    if (textComponent != null)
                    {
                        textComponent.text = $"\u26A0 {announcements[index]}";
                        // Fade in
                        t = 0;
                        while (t < 0.2f && textComponent != null)
                        {
                            t += Time.deltaTime;
                            cg.alpha = t / 0.2f;
                            yield return null;
                        }
                        if (cg != null) cg.alpha = 1;
                    }
                }
            }
        }

        private void ShowAnnouncementPopup(TitleScreen screen)
        {
            UIServices.Popups?.ShowCustomDialog("announcements", dialog =>
            {
                dialog.SetTitle("Announcements");
                string body = "";
                for (int i = 0; i < screen.Announcements.Count; i++)
                    body += $"\u2022 {screen.Announcements[i]}\n\n";
                dialog.SetMessage(body.TrimEnd());
                dialog.AddButton("Close", () => dialog.Dismiss(), new Color(0.3f, 0.3f, 0.35f));
            });
        }

        private void ShowDailyLoginIfNeeded()
        {
            var playerState = MobileForge.Infrastructure.PlayerState.Instance;
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            string lastLogin = playerState?.GetValue("progress", "last_login_date", "")?.ToString() ?? "";

            if (lastLogin == today) return;

            // Mark as claimed
            playerState?.SetValue("progress", "last_login_date", today);
            int streak = System.Convert.ToInt32(playerState?.GetValue("progress", "login_streak", 0) ?? 0);
            streak++;
            playerState?.SetValue("progress", "login_streak", streak);

            int dayInCycle = ((streak - 1) % 7) + 1;
            int reward = dayInCycle;

            var popups = UIServices.Popups;
            if (popups != null)
            {
                popups.ShowCustomDialog("daily_login", dialog =>
                {
                    dialog.SetTitle("Daily Login Bonus!");
                    dialog.SetMessage(
                        $"Login Streak: {streak} days\n" +
                        $"Day {dayInCycle}/7\n\n" +
                        $"Reward: +{reward} Gem(s)");
                    dialog.AddButton("Claim!", () =>
                    {
                        int gems = System.Convert.ToInt32(
                            playerState?.GetValue("currencies", "gems", 0) ?? 0);
                        playerState?.SetValue("currencies", "gems", gems + reward);
                        dialog.Dismiss();
                        UIServices.Toasts?.ShowToast($"+{reward} Gems claimed!");
                    }, new Color(0.2f, 0.6f, 0.3f));
                });
            }
        }

        private void MakeCurrLabel(Transform parent, string text, Color color)
        {
            var go = new GameObject("Curr");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 16;
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = TosTheme.FontSmall;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleRight;
        }
    }
}
