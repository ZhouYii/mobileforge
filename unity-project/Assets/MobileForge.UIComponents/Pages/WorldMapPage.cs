using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// World Map page matching original Worldmap_WorldMap_View / Worldmap_Zone_View.
    /// Shows zone cards with element theming and navigation to dungeon select.
    /// </summary>
    public class WorldMapPage : PageBase<WorldMapScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Transform _scrollContent;

        protected override void OnBind(WorldMapScreen screen)
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
                    _titleLabel.text = "World Map";
            }

            InitContent(screen);
        }

        private void BuildSkeleton(WorldMapScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Standardized Header ──
            TosPageHelper.BuildHeader(this, transform,
                "World Map", TosTheme.ElementColors[1], () => screen.GoBack());

            // ── Zone List (scrollable) ──
            var listArea = TosPageHelper.BuildContentArea(transform, "ZoneList");
            _scrollContent = TosPageHelper.BuildScrollContent(listArea, spacing: 10f);
        }

        private void InitContent(WorldMapScreen screen)
        {
            Transform target = _scrollContent ?? transform;
            var canvasGroups = new List<CanvasGroup>();
            foreach (var zone in screen.Zones)
            {
                var card = CreateZoneCard(target, zone, screen);
                var cg = card.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                canvasGroups.Add(cg);
            }
            if (canvasGroups.Count > 0)
                StartCoroutine(MFUIAnim.StaggerFadeIn(canvasGroups, 0.3f, 0.08f));
        }

        private GameObject CreateZoneCard(Transform parent, WorldMapScreen.WorldZone zone, WorldMapScreen screen)
        {
            var go = new GameObject($"Zone_{zone.Id}");
            go.transform.SetParent(parent, false);

            // Element-themed background
            Color elemColor = TosTheme.ElementColor(zone.ElementTheme);
            go.AddComponent<Image>().color = new Color(
                elemColor.r * 0.1f + 0.05f,
                elemColor.g * 0.1f + 0.05f,
                elemColor.b * 0.1f + 0.05f, 0.95f);
            go.AddComponent<LayoutElement>().preferredHeight = 90;

            var hbox = go.AddComponent<HorizontalLayoutGroup>();
            hbox.padding = new RectOffset(12, 12, 8, 8);
            hbox.spacing = 12;
            hbox.childAlignment = TextAnchor.MiddleLeft;
            hbox.childForceExpandWidth = false;
            hbox.childForceExpandHeight = true;

            // Element icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<LayoutElement>().preferredWidth = 50;
            var iconText = iconGo.AddComponent<Text>();
            iconText.text = TosTheme.ElementIcon(zone.ElementTheme);
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 36;
            iconText.color = elemColor;
            iconText.alignment = TextAnchor.MiddleCenter;

            // Content column
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(go.transform, false);
            contentGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var vbox = contentGo.AddComponent<VerticalLayoutGroup>();
            vbox.spacing = 3;
            vbox.childForceExpandHeight = false;

            // Zone name + badges
            var nameRow = new GameObject("NameRow");
            nameRow.transform.SetParent(contentGo.transform, false);
            nameRow.AddComponent<LayoutElement>().preferredHeight = 24;
            var nameHBox = nameRow.AddComponent<HorizontalLayoutGroup>();
            nameHBox.spacing = 6;
            nameHBox.childForceExpandWidth = false;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(nameRow.transform, false);
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var nameText = nameGo.AddComponent<Text>();
            nameText.text = zone.Name;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = TosTheme.FontBody + 2;
            nameText.color = Color.white;
            nameText.fontStyle = FontStyle.Bold;

            // NEW badge
            if (zone.IsNew)
            {
                var badgeGo = new GameObject("NewBadge");
                badgeGo.transform.SetParent(nameRow.transform, false);
                badgeGo.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
                badgeGo.AddComponent<LayoutElement>().preferredWidth = 36;
                var badgeLabel = new GameObject("Label");
                badgeLabel.transform.SetParent(badgeGo.transform, false);
                SetupFullStretch(badgeLabel);
                var bText = badgeLabel.AddComponent<Text>();
                bText.text = "NEW";
                bText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bText.fontSize = 9;
                bText.color = Color.white;
                bText.alignment = TextAnchor.MiddleCenter;
                bText.fontStyle = FontStyle.Bold;
            }

            // Event badge
            if (zone.IsEvent)
            {
                var eventGo = new GameObject("EventBadge");
                eventGo.transform.SetParent(nameRow.transform, false);
                eventGo.AddComponent<Image>().color = new Color(0.6f, 0.4f, 0.1f);
                eventGo.AddComponent<LayoutElement>().preferredWidth = 48;
                var eventLabel = new GameObject("Label");
                eventLabel.transform.SetParent(eventGo.transform, false);
                SetupFullStretch(eventLabel);
                var eText = eventLabel.AddComponent<Text>();
                eText.text = "EVENT";
                eText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                eText.fontSize = 9;
                eText.color = TosTheme.TextGold;
                eText.alignment = TextAnchor.MiddleCenter;
                eText.fontStyle = FontStyle.Bold;
            }

            // Description
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(contentGo.transform, false);
            descGo.AddComponent<LayoutElement>().preferredHeight = 16;
            var descText = descGo.AddComponent<Text>();
            descText.text = zone.Description;
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = TosTheme.FontSmall;
            descText.color = TosTheme.TextMuted;

            // Stage count + difficulty
            var metaGo = new GameObject("Meta");
            metaGo.transform.SetParent(contentGo.transform, false);
            metaGo.AddComponent<LayoutElement>().preferredHeight = 16;
            var metaText = metaGo.AddComponent<Text>();
            string diff = !string.IsNullOrEmpty(zone.Difficulty)
                ? $"  |  {char.ToUpper(zone.Difficulty[0])}{zone.Difficulty.Substring(1)}"
                : "";
            metaText.text = $"{zone.StageCount} stages{diff}";
            metaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            metaText.fontSize = 10;
            metaText.color = !string.IsNullOrEmpty(zone.Difficulty)
                ? TosTheme.DifficultyColor(zone.Difficulty) : TosTheme.TextMuted;

            // Enter arrow
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(go.transform, false);
            arrowGo.AddComponent<LayoutElement>().preferredWidth = 24;
            var arrowText = arrowGo.AddComponent<Text>();
            arrowText.text = "\u25B6"; // ▶
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            arrowText.fontSize = 18;
            arrowText.color = elemColor;
            arrowText.alignment = TextAnchor.MiddleCenter;

            // Click to enter zone
            string zoneId = zone.Id;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => screen.EnterZone(zoneId));

            TosPageHelper.AddPressFeedback(go);

            return go;
        }

        private static void SetupFullStretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
