using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Monster detail popup matching original General_CardInformation_View.
    /// Shows full monster info: element, stats, skills, leader skill, evolution.
    /// Can be invoked from inventory, team select, gacha results, etc.
    /// </summary>
    public class MonsterDetailPopup : MonoBehaviour
    {
        public Action OnDismissed;

        private CanvasGroup _canvasGroup;
        private RectTransform _panel;

        /// <summary>
        /// Show monster detail popup for the given monster entry.
        /// </summary>
        public static MonsterDetailPopup Show(Transform parent, MonsterBoxEntry monster)
        {
            var go = new GameObject("MonsterDetailPopup");
            go.transform.SetParent(parent, false);
            var popup = go.AddComponent<MonsterDetailPopup>();
            popup.Build(monster);
            return popup;
        }

        private void Build(MonsterBoxEntry monster)
        {
            // Full screen
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            // Backdrop
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(transform, false);
            SetFullStretch(backdrop);
            backdrop.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            backdrop.AddComponent<Button>().onClick.AddListener(Dismiss);

            // Panel (centered, larger than standard dialog)
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.05f, 0.1f);
            _panel.anchorMax = new Vector2(0.95f, 0.9f);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;

            int element = monster.Def?.Element ?? 0;
            panelGo.AddComponent<Image>().color = new Color(
                TosTheme.ElementColor(element).r * 0.08f + 0.06f,
                TosTheme.ElementColor(element).g * 0.08f + 0.06f,
                TosTheme.ElementColor(element).b * 0.08f + 0.06f, 0.97f);

            // Scrollable content
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            SetFullStretch(scrollGo);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            SetFullStretch(viewport);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            var cLayout = content.AddComponent<VerticalLayoutGroup>();
            cLayout.spacing = 6;
            cLayout.padding = new RectOffset(14, 14, 12, 12);
            cLayout.childForceExpandWidth = true;
            cLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRect;

            // ── Monster Header ──
            int rarity = monster.Def?.Rarity ?? 1;
            string stars = TosTheme.RarityStars(rarity);
            string elemIcon = TosTheme.ElementIcon(element);
            string elemName = TosTheme.ElementName(element);

            MakeLabel(content.transform, $"{elemIcon} {monster.DisplayName}", TosTheme.FontHeader,
                TosTheme.ElementColor(element), 32);
            MakeLabel(content.transform, $"{stars}  {elemName}  Lv.{monster.Instance.Level}",
                TosTheme.FontBody, TosTheme.RarityColor(rarity), 22);

            // Divider
            MakeDivider(content.transform);

            // ── Large Element Icon Area (simulated card art) ──
            var artGo = new GameObject("CardArt");
            artGo.transform.SetParent(content.transform, false);
            artGo.AddComponent<Image>().color = TosTheme.ElementPanelBg(element);
            artGo.AddComponent<LayoutElement>().preferredHeight = 120;
            var artVBox = artGo.AddComponent<VerticalLayoutGroup>();
            artVBox.childAlignment = TextAnchor.MiddleCenter;

            var bigIcon = new GameObject("BigIcon");
            bigIcon.transform.SetParent(artGo.transform, false);
            bigIcon.AddComponent<LayoutElement>().preferredHeight = 80;
            var bigIconText = bigIcon.AddComponent<Text>();
            bigIconText.text = elemIcon;
            bigIconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bigIconText.fontSize = 64;
            bigIconText.color = TosTheme.ElementColor(element);
            bigIconText.alignment = TextAnchor.MiddleCenter;

            var idLabel = new GameObject("ID");
            idLabel.transform.SetParent(artGo.transform, false);
            idLabel.AddComponent<LayoutElement>().preferredHeight = 16;
            var idText = idLabel.AddComponent<Text>();
            idText.text = $"No. {monster.Instance.DefId:D4}";
            idText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            idText.fontSize = TosTheme.FontSmall;
            idText.color = TosTheme.TextMuted;
            idText.alignment = TextAnchor.MiddleCenter;

            MakeDivider(content.transform);

            // ── Stats Section ──
            MakeLabel(content.transform, "Stats", TosTheme.FontSubheader, Color.white, 24);

            if (monster.Stats != null)
            {
                MakeStatRow(content.transform, "HP", monster.Stats.Hp, monster.Instance.PlusHp,
                    new Color(0.3f, 0.9f, 0.3f));
                MakeStatRow(content.transform, "ATK", monster.Stats.Atk, monster.Instance.PlusAtk,
                    new Color(0.9f, 0.3f, 0.3f));
                MakeStatRow(content.transform, "REC", monster.Stats.Rec, monster.Instance.PlusRec,
                    new Color(0.3f, 0.6f, 0.9f));

                int total = monster.Stats.Hp + monster.Stats.Atk + monster.Stats.Rec;
                MakeLabel(content.transform, $"Total: {total}", TosTheme.FontSmall, TosTheme.TextMuted, 18);
            }

            MakeDivider(content.transform);

            // ── Skill Info ──
            MakeLabel(content.transform, "Active Skill", TosTheme.FontSubheader, Color.white, 24);

            if (monster.Def != null)
            {
                string skillInfo = monster.Def.ActiveSkillId > 0
                    ? $"Skill #{monster.Def.ActiveSkillId} (Lv.{monster.Instance.SkillLevel})"
                    : "None";
                MakeLabel(content.transform, $"  {skillInfo}",
                    TosTheme.FontBody, new Color(0.8f, 0.8f, 0.5f), 20);
            }

            // Inherited skill
            if (monster.Instance.InheritedSkillId >= 0)
            {
                MakeLabel(content.transform, $"  Inherited: Skill #{monster.Instance.InheritedSkillId}",
                    TosTheme.FontSmall, new Color(0.6f, 0.4f, 0.8f), 18);
            }

            MakeDivider(content.transform);

            // ── Leader Skill ──
            MakeLabel(content.transform, "Leader Skill", TosTheme.FontSubheader, Color.white, 24);
            string leaderInfo = monster.Def?.LeaderSkillId > 0
                ? $"Leader Skill #{monster.Def.LeaderSkillId}"
                : "None";
            MakeLabel(content.transform, $"  {leaderInfo}", TosTheme.FontBody, TosTheme.TextGold, 20);

            MakeDivider(content.transform);

            // ── Additional Info ──
            MakeLabel(content.transform, "Details", TosTheme.FontSubheader, Color.white, 24);

            int awakenings = 0;
            if (monster.Instance.Awakenings != null)
                foreach (var a in monster.Instance.Awakenings)
                    if (a) awakenings++;

            string details = $"  Awakenings: {awakenings}/{monster.Instance.Awakenings?.Count ?? 0}\n" +
                             $"  Limit Break: +{monster.Instance.LimitBreakLevel}\n" +
                             $"  Plus Stats: HP+{monster.Instance.PlusHp} ATK+{monster.Instance.PlusAtk} REC+{monster.Instance.PlusRec}\n" +
                             $"  Favorite: {(monster.Instance.IsFavorite ? "Yes" : "No")}";
            MakeLabel(content.transform, details, TosTheme.FontSmall, TosTheme.TextMuted, 60);

            // ── Evolution Chain ──
            if (monster.Def?.EvolveTo > 0)
            {
                MakeDivider(content.transform);
                MakeLabel(content.transform, "Evolution", TosTheme.FontSubheader, Color.white, 24);
                MakeLabel(content.transform, $"  Evolves to: No.{monster.Def.EvolveTo:D4}",
                    TosTheme.FontSmall, new Color(0.8f, 0.6f, 0.3f), 22);
            }

            // ── Close Button ──
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(content.transform, false);
            closeGo.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);
            closeGo.AddComponent<LayoutElement>().preferredHeight = 44;
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(Dismiss);

            var closeLbl = new GameObject("Label");
            closeLbl.transform.SetParent(closeGo.transform, false);
            SetFullStretch(closeLbl);
            var closeText = closeLbl.AddComponent<Text>();
            closeText.text = "Close";
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = TosTheme.FontBody + 2;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;

            // Animate entrance
            StartCoroutine(AnimateOpen());
        }

        private void MakeStatRow(Transform parent, string stat, int baseVal, int plusVal, Color color)
        {
            var go = new GameObject($"Stat_{stat}");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 22;
            var hbox = go.AddComponent<HorizontalLayoutGroup>();
            hbox.spacing = 4;
            hbox.padding = new RectOffset(16, 16, 0, 0);
            hbox.childForceExpandHeight = true;

            // Stat name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            nameGo.AddComponent<LayoutElement>().preferredWidth = 50;
            var nameText = nameGo.AddComponent<Text>();
            nameText.text = stat;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = TosTheme.FontBody;
            nameText.color = color;
            nameText.fontStyle = FontStyle.Bold;

            // Base value
            var valGo = new GameObject("Value");
            valGo.transform.SetParent(go.transform, false);
            valGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var valText = valGo.AddComponent<Text>();
            string plusStr = plusVal > 0 ? $" (+{plusVal})" : "";
            valText.text = $"{baseVal}{plusStr}";
            valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valText.fontSize = TosTheme.FontBody;
            valText.color = Color.white;

            // Visual bar
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(go.transform, false);
            barGo.AddComponent<LayoutElement>().preferredWidth = 100;
            barGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(barGo.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            float ratio = Mathf.Clamp01(baseVal / 5000f); // approximate max stat
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(ratio, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(color.r, color.g, color.b, 0.7f);
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

        private void MakeDivider(Transform parent)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 1;
            go.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 0.6f);
        }

        private IEnumerator AnimateOpen()
        {
            _panel.localScale = Vector3.one * 0.85f;
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.3f);
                float ease = 1 - Mathf.Pow(1 - p, 3);
                _canvasGroup.alpha = ease;
                _panel.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, ease);
                yield return null;
            }
            _canvasGroup.alpha = 1;
            _panel.localScale = Vector3.one;
        }

        public void Dismiss()
        {
            StartCoroutine(AnimateClose());
        }

        private IEnumerator AnimateClose()
        {
            float t = 0;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.2f);
                _canvasGroup.alpha = 1 - p;
                yield return null;
            }
            OnDismissed?.Invoke();
            Destroy(gameObject);
        }

        private static void SetFullStretch(GameObject go)
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
