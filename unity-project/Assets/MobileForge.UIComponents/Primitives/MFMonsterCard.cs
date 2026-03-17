using System;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Monster card display showing name, element, rarity stars, and level.
    /// Wraps the framework's CardView pure C# model.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFMonsterCard : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _elementIcon;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _rarityLabel;

        public Action OnTapped;

        // Element ID -> color for the card frame tint
        private static readonly Color[] ElementColors =
        {
            Color.gray,                            // 0: None
            new Color(0.2f, 0.5f, 1f),            // 1: Water
            new Color(1f, 0.3f, 0.2f),            // 2: Fire
            new Color(0.2f, 0.8f, 0.3f),          // 3: Grass
            new Color(1f, 0.9f, 0.3f),            // 4: Light
            new Color(0.6f, 0.2f, 0.8f),          // 5: Dark
            new Color(1f, 0.5f, 0.7f),            // 6: Heart
        };

        private bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (_frame == null) _frame = GetComponent<Image>();

            if (_frame == null)
            {
                // Build fallback card UI
                _frame = gameObject.AddComponent<Image>();
                _frame.color = new Color(0.2f, 0.2f, 0.25f, 1f);

                var layout = gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = 100;
                layout.preferredHeight = 130;

                var btn = gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnTapped?.Invoke());

                // Element icon (small colored square at top-left)
                var iconGO = new GameObject("ElementIcon");
                iconGO.transform.SetParent(transform, false);
                var iconRect = iconGO.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0, 1);
                iconRect.anchorMax = new Vector2(0, 1);
                iconRect.pivot = new Vector2(0, 1);
                iconRect.anchoredPosition = new Vector2(4, -4);
                iconRect.sizeDelta = new Vector2(20, 20);
                _elementIcon = iconGO.AddComponent<Image>();

                // Name label
                var nameGO = new GameObject("Name");
                nameGO.transform.SetParent(transform, false);
                var nameRect = nameGO.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.35f);
                nameRect.anchorMax = new Vector2(1, 0.65f);
                nameRect.offsetMin = new Vector2(2, 0);
                nameRect.offsetMax = new Vector2(-2, 0);
                _nameLabel = nameGO.AddComponent<Text>();
                _nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _nameLabel.fontSize = 12;
                _nameLabel.color = Color.white;
                _nameLabel.alignment = TextAnchor.MiddleCenter;
                _nameLabel.resizeTextForBestFit = true;
                _nameLabel.resizeTextMinSize = 8;
                _nameLabel.resizeTextMaxSize = 14;

                // Level label
                var lvlGO = new GameObject("Level");
                lvlGO.transform.SetParent(transform, false);
                var lvlRect = lvlGO.AddComponent<RectTransform>();
                lvlRect.anchorMin = new Vector2(0, 0);
                lvlRect.anchorMax = new Vector2(1, 0.2f);
                lvlRect.offsetMin = new Vector2(2, 2);
                lvlRect.offsetMax = new Vector2(-2, 0);
                _levelLabel = lvlGO.AddComponent<Text>();
                _levelLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _levelLabel.fontSize = 11;
                _levelLabel.color = Color.yellow;
                _levelLabel.alignment = TextAnchor.MiddleCenter;

                // Rarity label (stars at top)
                var rarityGO = new GameObject("Rarity");
                rarityGO.transform.SetParent(transform, false);
                var rarityRect = rarityGO.AddComponent<RectTransform>();
                rarityRect.anchorMin = new Vector2(0, 0.8f);
                rarityRect.anchorMax = new Vector2(1, 1);
                rarityRect.offsetMin = new Vector2(24, 0);
                rarityRect.offsetMax = new Vector2(-2, -2);
                _rarityLabel = rarityGO.AddComponent<Text>();
                _rarityLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _rarityLabel.fontSize = 10;
                _rarityLabel.color = Color.yellow;
                _rarityLabel.alignment = TextAnchor.MiddleRight;
            }
        }

        /// <summary>
        /// Bind to a CardView pure C# model.
        /// </summary>
        public void SetMonster(CardView cardView)
        {
            if (cardView == null) return;
            SetData(cardView.Name, cardView.Element, cardView.Rarity, cardView.Level);
        }

        /// <summary>
        /// Set card data directly.
        /// </summary>
        public void SetData(string name, int element, int rarity, int level)
        {
            EnsureInitialized();

            if (_nameLabel != null) _nameLabel.text = name;
            if (_levelLabel != null) _levelLabel.text = level > 0 ? $"Lv.{level}" : "";

            if (_rarityLabel != null)
            {
                string stars = "";
                for (int i = 0; i < rarity && i < 7; i++) stars += "*";
                _rarityLabel.text = stars;
            }

            if (_elementIcon != null)
            {
                Color elemColor = element >= 0 && element < ElementColors.Length
                    ? ElementColors[element] : Color.gray;
                _elementIcon.color = elemColor;
            }

            if (_frame != null)
            {
                // Tint frame border slightly with element color
                Color elemColor = element >= 0 && element < ElementColors.Length
                    ? ElementColors[element] : Color.gray;
                _frame.color = Color.Lerp(new Color(0.2f, 0.2f, 0.25f), elemColor, 0.2f);
            }
        }
    }
}
