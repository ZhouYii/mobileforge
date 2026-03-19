using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Settings/Preferences page matching original Preferences_MainMenu_View.
    /// Shows categorized settings with toggles and navigation items.
    /// Uses TosPageHelper for consistent header/layout.
    /// </summary>
    public class SettingsPage : PageBase<SettingsScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Transform _scrollContent;
        [SerializeField] private Text _playerNameLabel;

        protected override void OnBind(SettingsScreen screen)
        {
            // Wire info popup event (works for both code-built and prefab modes)
            screen.OnShowInfo += (title, message) =>
            {
                UIServices.Popups?.ShowDialog(
                    $"settings_{title.ToLower().Replace(" ", "_")}",
                    title, message, "OK");
            };

            if (!IsPrefabPage)
            {
                BuildSkeleton(screen);
            }
            else
            {
                if (_backButton != null)
                    _backButton.onClick.AddListener(() => screen.GoBack());
                if (_titleLabel != null)
                    _titleLabel.text = "Settings";
                if (_playerNameLabel != null)
                    _playerNameLabel.text = $"\u263A {screen.PlayerName}";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(SettingsScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            TosPageHelper.BuildHeader(this, transform,
                "Settings", TosTheme.SettingsAccent, () => screen.GoBack());

            // ── Scrollable content ──
            var contentArea = TosPageHelper.BuildContentArea(transform);
            _scrollContent = TosPageHelper.BuildScrollContent(contentArea, spacing: 4f);

            // Player info header
            var playerCard = new GameObject("PlayerCard");
            playerCard.transform.SetParent(_scrollContent, false);
            playerCard.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.95f);
            playerCard.AddComponent<LayoutElement>().preferredHeight = 44;
            var pcLayout = playerCard.AddComponent<HorizontalLayoutGroup>();
            pcLayout.padding = new RectOffset(14, 14, 8, 8);
            pcLayout.childAlignment = TextAnchor.MiddleLeft;
            pcLayout.childForceExpandHeight = true;

            TosPageHelper.MakeLabel(playerCard.transform, $"\u263A {screen.PlayerName}",
                TosTheme.FontSubheader, Color.white, 0)
                .gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        private void InitContent(SettingsScreen screen)
        {
            Transform target = _scrollContent ?? transform;
            string lastCategory = "";
            var canvasGroups = new List<CanvasGroup>();

            foreach (var entry in screen.Entries)
            {
                if (entry.Category != lastCategory)
                {
                    lastCategory = entry.Category;
                    var sepGo = new GameObject($"Cat_{entry.Category}");
                    sepGo.transform.SetParent(target, false);
                    sepGo.AddComponent<LayoutElement>().preferredHeight = 26;
                    var sepText = sepGo.AddComponent<Text>();
                    sepText.text = $"  {entry.Category}";
                    sepText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    sepText.fontSize = TosTheme.FontSmall;
                    sepText.color = new Color(0.5f, 0.5f, 0.6f);
                    sepText.alignment = TextAnchor.MiddleLeft;
                    sepText.fontStyle = FontStyle.Bold;
                }

                var entryObj = CreateSettingsEntry(target, entry, screen);
                var cg = entryObj.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                canvasGroups.Add(cg);
            }

            if (canvasGroups.Count > 0)
                StartCoroutine(MFUIAnim.StaggerFadeIn(canvasGroups, 0.2f, 0.04f));
        }

        private GameObject CreateSettingsEntry(Transform parent, SettingsScreen.SettingsEntry entry, SettingsScreen screen)
        {
            var go = new GameObject($"Setting_{entry.Label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = TosTheme.BgPanel;
            go.AddComponent<LayoutElement>().preferredHeight = 46;

            var hbox = go.AddComponent<HorizontalLayoutGroup>();
            hbox.padding = new RectOffset(14, 14, 6, 6);
            hbox.spacing = 8;
            hbox.childAlignment = TextAnchor.MiddleLeft;
            hbox.childForceExpandWidth = false;
            hbox.childForceExpandHeight = true;

            // Label
            TosPageHelper.MakeLabel(go.transform, entry.Label, TosTheme.FontBody,
                entry.Type == SettingsScreen.EntryType.Info ? TosTheme.TextMuted : Color.white, 0)
                .gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;

            if (entry.Type == SettingsScreen.EntryType.Toggle)
            {
                bool currentVal = entry.ToggleValue;
                var toggleGo = new GameObject("Toggle");
                toggleGo.transform.SetParent(go.transform, false);
                var toggleBg = toggleGo.AddComponent<Image>();
                toggleBg.color = currentVal ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.3f, 0.3f, 0.35f);
                var toggleLe = toggleGo.AddComponent<LayoutElement>();
                toggleLe.preferredWidth = 50;
                toggleLe.preferredHeight = 26;

                var tlText = TosPageHelper.MakeLabel(toggleGo.transform, currentVal ? "ON" : "OFF",
                    TosTheme.FontSmall, Color.white, 0, TextAnchor.MiddleCenter);
                tlText.fontStyle = FontStyle.Bold;
                tlText.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;

                var toggleBtn = toggleGo.AddComponent<Button>();
                toggleBtn.onClick.AddListener(() =>
                {
                    currentVal = !currentVal;
                    entry.OnToggle?.Invoke(currentVal);
                    entry.ToggleValue = currentVal;
                    toggleBg.color = currentVal ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.3f, 0.3f, 0.35f);
                    tlText.text = currentVal ? "ON" : "OFF";
                });
            }
            else if (entry.Type == SettingsScreen.EntryType.Navigation)
            {
                TosPageHelper.MakeLabel(go.transform, "\u25B6", TosTheme.FontSmall,
                    TosTheme.TextMuted, 0, TextAnchor.MiddleCenter)
                    .gameObject.GetComponent<LayoutElement>().preferredWidth = 18;

                if (entry.Label == "Change Name")
                {
                    var btn = go.AddComponent<Button>();
                    btn.onClick.AddListener(() => ShowChangeNameDialog(screen));
                }
                else
                {
                    var btn = go.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        UIServices.Popups?.ShowDialog($"settings_{entry.Label}",
                            entry.Label, "Coming soon!", "OK");
                    });
                }
                TosPageHelper.AddPressFeedback(go);
            }

            return go;
        }

        private void ShowChangeNameDialog(SettingsScreen screen)
        {
            UIServices.Popups?.ShowCustomDialog("change_name", dialog =>
            {
                dialog.SetTitle("Change Name");
                dialog.SetMessage($"Current: {screen.PlayerName}\n\nEnter new name:");
                dialog.AddButton("Change", () =>
                {
                    screen.ChangeName("Summoner_" + Random.Range(100, 999));
                    UIServices.Toasts?.ShowToast($"Name changed to {screen.PlayerName}");
                    dialog.Dismiss();
                }, new Color(0.2f, 0.5f, 0.8f));
                dialog.AddButton("Cancel", () => dialog.Dismiss(), new Color(0.3f, 0.3f, 0.35f));
            });
        }
    }
}
