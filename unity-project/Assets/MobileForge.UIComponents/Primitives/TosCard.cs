using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Rich monster card display matching Godot's TosCard.
    /// Shows element icon, name, rarity stars, level, element-tinted frame,
    /// and optional plus-stat badge.
    /// </summary>
    public class TosCard : MonoBehaviour
    {
        public System.Action OnTapped;

        private Image _frame;
        private Image _elementStrip;
        private Text _nameLabel;
        private Text _starLabel;
        private Text _levelLabel;
        private Text _plusBadge;
        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;

        private int _element;
        private int _rarity;

        void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var rt = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _layoutElement = gameObject.AddComponent<LayoutElement>();
            _layoutElement.preferredWidth = TowerOfSaviors.TosTheme.CardSize.x;
            _layoutElement.preferredHeight = TowerOfSaviors.TosTheme.CardSize.y;

            // Frame background
            _frame = gameObject.AddComponent<Image>();
            _frame.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);

            // Tap handler
            var btn = gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => OnTapped?.Invoke());

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(3, 3, 2, 2);
            layout.spacing = 1;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Element color strip at top
            var stripGo = new GameObject("ElementStrip");
            stripGo.transform.SetParent(transform, false);
            _elementStrip = stripGo.AddComponent<Image>();
            _elementStrip.color = new Color(0.5f, 0.5f, 0.5f);
            var stripLe = stripGo.AddComponent<LayoutElement>();
            stripLe.preferredHeight = 3;

            // Element icon + name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(transform, false);
            _nameLabel = nameGo.AddComponent<Text>();
            _nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _nameLabel.fontSize = 10;
            _nameLabel.alignment = TextAnchor.MiddleCenter;
            _nameLabel.color = Color.white;
            _nameLabel.resizeTextForBestFit = true;
            _nameLabel.resizeTextMinSize = 8;
            _nameLabel.resizeTextMaxSize = 11;
            var nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.preferredHeight = 28;
            nameLe.flexibleHeight = 1;

            // Rarity stars
            var starGo = new GameObject("Stars");
            starGo.transform.SetParent(transform, false);
            _starLabel = starGo.AddComponent<Text>();
            _starLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _starLabel.fontSize = 9;
            _starLabel.alignment = TextAnchor.MiddleCenter;
            var starLe = starGo.AddComponent<LayoutElement>();
            starLe.preferredHeight = 14;

            // Level
            var lvlGo = new GameObject("Level");
            lvlGo.transform.SetParent(transform, false);
            _levelLabel = lvlGo.AddComponent<Text>();
            _levelLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _levelLabel.fontSize = 9;
            _levelLabel.alignment = TextAnchor.MiddleCenter;
            _levelLabel.color = new Color(0.7f, 0.7f, 0.7f);
            var lvlLe = lvlGo.AddComponent<LayoutElement>();
            lvlLe.preferredHeight = 14;

            // Plus badge (hidden by default)
            var plusGo = new GameObject("PlusBadge");
            plusGo.transform.SetParent(transform, false);
            _plusBadge = plusGo.AddComponent<Text>();
            _plusBadge.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _plusBadge.fontSize = 8;
            _plusBadge.alignment = TextAnchor.MiddleCenter;
            _plusBadge.color = new Color(0.3f, 1f, 0.3f);
            var plusLe = plusGo.AddComponent<LayoutElement>();
            plusLe.preferredHeight = 12;
            plusGo.SetActive(false);
        }

        /// <summary>
        /// Set monster data for display.
        /// </summary>
        public void SetMonster(string name, int element, int rarity, int level, int plusTotal = 0)
        {
            _element = element;
            _rarity = rarity;

            var ec = TowerOfSaviors.TosTheme.ElementColor(element);
            var rc = TowerOfSaviors.TosTheme.RarityColor(rarity);

            _nameLabel.text = $"{TowerOfSaviors.TosTheme.ElementIcon(element)} {name}";
            _nameLabel.color = ec;

            _starLabel.text = TowerOfSaviors.TosTheme.RarityStars(rarity);
            _starLabel.color = rc;

            _levelLabel.text = $"Lv.{level}";

            _elementStrip.color = ec;

            // Frame border color by rarity
            _frame.color = new Color(ec.r * 0.12f, ec.g * 0.12f, ec.b * 0.12f, 0.9f);

            // Plus badge
            if (plusTotal > 0)
            {
                _plusBadge.gameObject.SetActive(true);
                _plusBadge.text = $"+{plusTotal}";
            }
            else
            {
                _plusBadge.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Show empty slot appearance.
        /// </summary>
        public void SetEmpty(string placeholder = "+")
        {
            _nameLabel.text = placeholder;
            _nameLabel.color = TowerOfSaviors.TosTheme.TextMuted;
            _starLabel.text = "";
            _levelLabel.text = "";
            _elementStrip.color = new Color(0.3f, 0.3f, 0.3f);
            _frame.color = new Color(0.1f, 0.1f, 0.12f, 0.7f);
            _plusBadge.gameObject.SetActive(false);
            _canvasGroup.alpha = 0.7f;
        }

        /// <summary>
        /// Dim the card (for already-selected monsters in grid).
        /// </summary>
        public void SetDimmed(bool dimmed)
        {
            _canvasGroup.alpha = dimmed ? TowerOfSaviors.TosTheme.CardAlphaDisabled : 1f;
        }

        /// <summary>
        /// Highlight as selected (bright border).
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selected)
            {
                var rc = TowerOfSaviors.TosTheme.RarityColor(_rarity);
                _frame.color = new Color(rc.r * 0.3f, rc.g * 0.3f, rc.b * 0.3f, 0.95f);
            }
            else
            {
                var ec = TowerOfSaviors.TosTheme.ElementColor(_element);
                _frame.color = new Color(ec.r * 0.12f, ec.g * 0.12f, ec.b * 0.12f, 0.9f);
            }
        }
    }
}
