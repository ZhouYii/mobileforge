using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Daily check-in page matching original Social_DailyCheckIn_V13_View.
    /// Shows 7-day reward calendar grid with streak progress and milestones.
    /// </summary>
    public class DailyCheckInPage : PageBase<DailyCheckInScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _streakLabel;
        [SerializeField] private Text _statusLabel;
        private MFButton _claimBtn;
        private readonly List<GameObject> _dayCells = new();
        [SerializeField] private RectTransform _calendarArea;
        [SerializeField] private Transform _calendarGrid;

        protected override void OnBind(DailyCheckInScreen screen)
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
                    _titleLabel.text = "Daily Check-In";
                if (_streakLabel != null)
                    _streakLabel.text = $"\u2605 Login Streak: {screen.LoginStreak} days";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(DailyCheckInScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            TosPageHelper.BuildHeader(this, transform,
                "Daily Check-In", TosTheme.DailyCheckInAccent, () => screen.GoBack());

            // ── Streak Info ──
            var streakArea = CreateRegion("Streak", MFAnchor.Top,
                new MFPadding(20, TosTheme.ContentTopOffset + 4, 20, 0));
            streakArea.sizeDelta = new Vector2(0, 50);
            streakArea.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.15f, 0.95f);

            var streakVBox = CreateVBox("StreakContent", streakArea, spacing: 2f);
            var sLayout = streakVBox.GetComponent<VerticalLayoutGroup>();
            if (sLayout != null) sLayout.padding = new RectOffset(14, 14, 6, 6);

            var streakGo = new GameObject("StreakLabel");
            streakGo.transform.SetParent(streakVBox, false);
            streakGo.AddComponent<LayoutElement>().preferredHeight = 22;
            _streakLabel = streakGo.AddComponent<Text>();
            _streakLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _streakLabel.fontSize = TosTheme.FontSubheader;
            _streakLabel.color = TosTheme.TextGold;
            _streakLabel.alignment = TextAnchor.MiddleCenter;
            _streakLabel.text = $"\u2605 Login Streak: {screen.LoginStreak} days";

            var dayLabel = new GameObject("DayLabel");
            dayLabel.transform.SetParent(streakVBox, false);
            dayLabel.AddComponent<LayoutElement>().preferredHeight = 16;
            var dayText = dayLabel.AddComponent<Text>();
            dayText.text = $"Day {screen.DayInCycle} of 7";
            dayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dayText.fontSize = TosTheme.FontSmall;
            dayText.color = TosTheme.TextMuted;
            dayText.alignment = TextAnchor.MiddleCenter;

            // ── Calendar Grid container ──
            _calendarArea = CreateRegion("Calendar", MFAnchor.Fill, new MFPadding(12, 120, 12, 250));
            _calendarGrid = CreateGrid("DayGrid", _calendarArea, 7, 68);
            var gridLayout = _calendarGrid.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.spacing = new Vector2(6, 6);
                gridLayout.cellSize = new Vector2(68, 90);
                gridLayout.childAlignment = TextAnchor.MiddleCenter;
            }

            // ── Milestone Rewards ──
            var milestoneArea = CreateRegion("Milestones", MFAnchor.Bottom, new MFPadding(12, 0, 12, 130));
            milestoneArea.sizeDelta = new Vector2(0, 110);
            milestoneArea.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            var mVBox = CreateVBox("MilestoneContent", milestoneArea, spacing: 3f);
            var mLayout = mVBox.GetComponent<VerticalLayoutGroup>();
            if (mLayout != null) mLayout.padding = new RectOffset(10, 10, 6, 6);

            MakeLabel(mVBox, "Cumulative Milestones", TosTheme.FontBody, new Color(0.8f, 0.6f, 1f), 20);
            foreach (var ms in screen.Milestones)
            {
                string status = ms.Claimed ? "\u2713" : (screen.LoginStreak >= ms.RequiredDays ? "\u2605" : "\u25cb");
                Color color = ms.Claimed ? TosTheme.TextMuted : (screen.LoginStreak >= ms.RequiredDays ? TosTheme.TextGold : Color.white);
                MakeLabel(mVBox, $"  {status}  {ms.RequiredDays} Days: +{ms.Amount} {ms.RewardType}", TosTheme.FontSmall, color, 18);
            }

            // Status label
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(transform, false);
            var sRect = statusGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.1f, 0.07f);
            sRect.anchorMax = new Vector2(0.9f, 0.1f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            _statusLabel = statusGo.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusLabel.fontSize = TosTheme.FontBody;
            _statusLabel.color = TosTheme.TextGold;
            _statusLabel.alignment = TextAnchor.MiddleCenter;
        }

        private void InitContent(DailyCheckInScreen screen)
        {
            // ── Calendar Day Cells ──
            Transform gridTarget = _calendarGrid;
            if (gridTarget == null)
            {
                var gridArea = CreateRegion("Calendar", MFAnchor.Fill, new MFPadding(12, 120, 12, 250));
                gridTarget = CreateGrid("DayGrid", gridArea, 7, 68);
                var gl = gridTarget.GetComponent<GridLayoutGroup>();
                if (gl != null)
                {
                    gl.spacing = new Vector2(6, 6);
                    gl.cellSize = new Vector2(68, 90);
                    gl.childAlignment = TextAnchor.MiddleCenter;
                }
            }

            var canvasGroups = new List<CanvasGroup>();
            foreach (var reward in screen.Rewards)
            {
                var cell = CreateDayCell(gridTarget, reward);
                _dayCells.Add(cell);
                var cg = cell.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                canvasGroups.Add(cg);
            }
            if (canvasGroups.Count > 0)
                StartCoroutine(MFUIAnim.StaggerFadeIn(canvasGroups, 0.35f, 0.08f));

            // ── Claim Button ──
            var btnArea = CreateRegion("ClaimArea", MFAnchor.Bottom, new MFPadding(40, 0, 40, 72));
            btnArea.sizeDelta = new Vector2(0, 50);
            _claimBtn = SpawnPrimitive<MFButton>(btnArea);
            UpdateClaimButton(screen);
            _claimBtn.OnClick = () =>
            {
                string result = screen.ClaimToday();
                if (_statusLabel != null) _statusLabel.text = result;
                UpdateClaimButton(screen);
                if (_streakLabel != null)
                    _streakLabel.text = $"\u2605 Login Streak: {screen.LoginStreak} days";
                RefreshDayCells(screen);
                UIServices.Toasts?.ShowToast(result);
            };
        }

        private GameObject CreateDayCell(Transform parent, DailyCheckInScreen.DayReward reward)
        {
            var go = new GameObject($"Day_{reward.Day}");
            go.transform.SetParent(parent, false);

            Color cellBg;
            if (reward.Claimed)
                cellBg = new Color(0.15f, 0.25f, 0.15f, 0.9f); // green tint for claimed
            else if (reward.IsToday)
                cellBg = new Color(0.25f, 0.2f, 0.1f, 0.95f); // golden for today
            else
                cellBg = TosTheme.BgPanel;

            go.AddComponent<Image>().color = cellBg;

            var vbox = go.AddComponent<VerticalLayoutGroup>();
            vbox.padding = new RectOffset(4, 4, 4, 4);
            vbox.spacing = 2;
            vbox.childAlignment = TextAnchor.MiddleCenter;
            vbox.childForceExpandWidth = true;
            vbox.childForceExpandHeight = false;

            // Day number
            var dayGo = new GameObject("Day");
            dayGo.transform.SetParent(go.transform, false);
            dayGo.AddComponent<LayoutElement>().preferredHeight = 18;
            var dayText = dayGo.AddComponent<Text>();
            dayText.text = $"Day {reward.Day}";
            dayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dayText.fontSize = 10;
            dayText.color = reward.IsToday ? TosTheme.TextGold : Color.white;
            dayText.alignment = TextAnchor.MiddleCenter;
            dayText.fontStyle = reward.IsToday ? FontStyle.Bold : FontStyle.Normal;

            // Reward icon
            string icon = reward.RewardType == "gems" ? "\u2666" : "\u25C9"; // ◆ or ◉
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredHeight = 26;
            var iconText = iconGo.AddComponent<Text>();
            iconText.text = icon;
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 22;
            iconText.color = reward.Claimed ? TosTheme.TextMuted :
                (reward.RewardType == "gems" ? TosTheme.TextGold : new Color(0.9f, 0.8f, 0.3f));
            iconText.alignment = TextAnchor.MiddleCenter;

            // Amount
            var amtGo = new GameObject("Amount");
            amtGo.transform.SetParent(go.transform, false);
            amtGo.AddComponent<LayoutElement>().preferredHeight = 14;
            var amtText = amtGo.AddComponent<Text>();
            amtText.text = $"+{reward.Amount}";
            amtText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            amtText.fontSize = 10;
            amtText.color = reward.Claimed ? TosTheme.TextMuted : Color.white;
            amtText.alignment = TextAnchor.MiddleCenter;

            // Status
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(go.transform, false);
            statusGo.AddComponent<LayoutElement>().preferredHeight = 14;
            var statusText = statusGo.AddComponent<Text>();
            statusText.text = reward.Claimed ? "\u2713" : (reward.IsToday ? "TODAY" : "");
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 9;
            statusText.color = reward.Claimed ? new Color(0.3f, 0.8f, 0.3f) : TosTheme.TextGold;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.fontStyle = FontStyle.Bold;

            return go;
        }

        private void RefreshDayCells(DailyCheckInScreen screen)
        {
            for (int i = 0; i < _dayCells.Count && i < screen.Rewards.Count; i++)
            {
                var reward = screen.Rewards[i];
                var cell = _dayCells[i];
                var cellBg = cell.GetComponent<Image>();
                if (cellBg != null)
                {
                    cellBg.color = reward.Claimed
                        ? new Color(0.15f, 0.25f, 0.15f, 0.9f)
                        : (reward.IsToday ? new Color(0.25f, 0.2f, 0.1f, 0.95f) : TosTheme.BgPanel);
                }
            }
        }

        private void UpdateClaimButton(DailyCheckInScreen screen)
        {
            if (screen.ClaimedToday)
            {
                _claimBtn.SetLabel("\u2713 Already Claimed Today");
                _claimBtn.SetColor(new Color(0.2f, 0.2f, 0.2f), TosTheme.TextMuted);
                _claimBtn.SetEnabled(false);
            }
            else
            {
                _claimBtn.SetLabel("Claim Today's Reward!");
                _claimBtn.SetColor(new Color(0.6f, 0.4f, 0.1f), TosTheme.TextGold);
                _claimBtn.SetEnabled(true);
            }
        }

        private void MakeLabel(Transform parent, string text, int fontSize, Color color, float height)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
        }
    }
}
