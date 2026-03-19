using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Horizontal tab bar with colored active tab indicator.
    /// Used for difficulty selection, shop tabs, etc.
    /// </summary>
    public class MFTabBar : MonoBehaviour
    {
        public Action<int, string> OnTabSelected;

        private readonly List<Button> _buttons = new();
        private readonly List<Text> _labels = new();
        private readonly List<Image> _backgrounds = new();
        private int _activeIndex;
        private HorizontalLayoutGroup _layout;
        private Color[] _tabColors;
        private string[] _tabLabels;

        void Awake()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            if (_layout != null) return;

            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            _layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            _layout.spacing = 4;
            _layout.childAlignment = TextAnchor.MiddleCenter;
            _layout.childForceExpandWidth = true;
            _layout.childForceExpandHeight = true;

            var le = gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.flexibleWidth = 1;
        }

        /// <summary>
        /// Configure tabs with labels and per-tab accent colors.
        /// </summary>
        public void SetTabs(string[] labels, Color[] colors = null)
        {
            BuildLayout();
            Clear();

            _tabLabels = labels;
            _tabColors = colors ?? new Color[labels.Length];
            if (colors == null)
            {
                for (int i = 0; i < labels.Length; i++)
                    _tabColors[i] = new Color(0.5f, 0.5f, 0.5f);
            }

            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var tabGo = new GameObject($"Tab_{labels[i]}");
                tabGo.transform.SetParent(transform, false);

                var tabRect = tabGo.AddComponent<RectTransform>();
                var bg = tabGo.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.2f);
                _backgrounds.Add(bg);

                var btn = tabGo.AddComponent<Button>();
                btn.onClick.AddListener(() => SetActive(index));
                _buttons.Add(btn);

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(tabGo.transform, false);
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var text = labelGo.AddComponent<Text>();
                text.text = labels[i];
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 13;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.7f, 0.7f, 0.7f);
                _labels.Add(text);

                var tabLe = tabGo.AddComponent<LayoutElement>();
                tabLe.flexibleWidth = 1;
                tabLe.preferredHeight = 34;
            }

            SetActive(0, silent: true);
        }

        /// <summary>
        /// Set active tab by index.
        /// </summary>
        public void SetActive(int index, bool silent = false)
        {
            if (index < 0 || index >= _buttons.Count) return;
            _activeIndex = index;

            for (int i = 0; i < _buttons.Count; i++)
            {
                bool active = i == index;
                _backgrounds[i].color = active
                    ? new Color(_tabColors[i].r * 0.3f, _tabColors[i].g * 0.3f, _tabColors[i].b * 0.3f, 0.95f)
                    : new Color(0.15f, 0.15f, 0.2f);
                _labels[i].color = active ? _tabColors[i] : new Color(0.5f, 0.5f, 0.5f);
                _labels[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            }

            if (!silent)
                OnTabSelected?.Invoke(index, _tabLabels[index]);
        }

        public int ActiveIndex => _activeIndex;

        private void Clear()
        {
            foreach (var btn in _buttons)
                if (btn != null) Destroy(btn.gameObject);
            _buttons.Clear();
            _labels.Clear();
            _backgrounds.Clear();
        }
    }
}
