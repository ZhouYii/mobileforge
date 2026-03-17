using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Scrollable vertical list with cell recycling.
    /// Bridges the framework's VirtualList&lt;GameObject&gt; to a Unity ScrollRect.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFScrollList : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;

        public Action<object, int> OnItemSelected;

        private VirtualList<GameObject> _virtualList;
        private GameObject _cellPrefab;
        private Action<GameObject, object, int> _bindCallback;
        private RectTransform _content;
        private float _itemHeight;
        private bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (_scrollRect == null)
                _scrollRect = GetComponent<ScrollRect>();

            if (_scrollRect == null)
            {
                // Build fallback ScrollRect
                var layout = gameObject.AddComponent<LayoutElement>();
                layout.flexibleWidth = 1;
                layout.flexibleHeight = 1;

                _scrollRect = gameObject.AddComponent<ScrollRect>();
                _scrollRect.horizontal = false;
                _scrollRect.vertical = true;
                _scrollRect.movementType = ScrollRect.MovementType.Elastic;

                // Viewport (mask)
                var viewportGO = new GameObject("Viewport");
                viewportGO.transform.SetParent(transform, false);
                var vpRect = viewportGO.AddComponent<RectTransform>();
                vpRect.anchorMin = Vector2.zero;
                vpRect.anchorMax = Vector2.one;
                vpRect.offsetMin = Vector2.zero;
                vpRect.offsetMax = Vector2.zero;
                var mask = viewportGO.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                viewportGO.AddComponent<Image>().color = Color.clear;
                _scrollRect.viewport = vpRect;

                // Content
                var contentGO = new GameObject("Content");
                contentGO.transform.SetParent(viewportGO.transform, false);
                _content = contentGO.AddComponent<RectTransform>();
                _content.anchorMin = new Vector2(0, 1);
                _content.anchorMax = Vector2.one;
                _content.pivot = new Vector2(0.5f, 1);
                _content.offsetMin = Vector2.zero;
                _content.offsetMax = Vector2.zero;
                _scrollRect.content = _content;
            }
            else
            {
                _content = _scrollRect.content;
            }

            _virtualList = new VirtualList<GameObject>();
            _scrollRect.onValueChanged.AddListener(OnScroll);
        }

        /// <summary>
        /// Configure the list with a cell prefab, bind callback, and item height.
        /// </summary>
        public void Setup(GameObject cellPrefab, Action<GameObject, object, int> bindCallback, float itemHeight = 80f)
        {
            EnsureInitialized();
            _cellPrefab = cellPrefab;
            _bindCallback = bindCallback;
            _itemHeight = itemHeight;

            _virtualList.Setup(
                () =>
                {
                    var cell = _cellPrefab != null
                        ? Instantiate(_cellPrefab, _content)
                        : CreateFallbackCell();
                    // Add tap detection
                    var btn = cell.GetComponent<Button>();
                    if (btn == null) btn = cell.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    return cell;
                },
                (cell, data, index) =>
                {
                    cell.SetActive(true);
                    cell.transform.SetParent(_content, false);

                    // Position the cell
                    var cellRect = cell.GetComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0, 1);
                    cellRect.anchorMax = new Vector2(1, 1);
                    cellRect.pivot = new Vector2(0.5f, 1);
                    cellRect.anchoredPosition = new Vector2(0, -index * _itemHeight);
                    cellRect.sizeDelta = new Vector2(0, _itemHeight);

                    // Bind data
                    _bindCallback?.Invoke(cell, data, index);

                    // Wire tap
                    var btn = cell.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnItemSelected?.Invoke(data, index));
                    }
                },
                _itemHeight
            );
        }

        public void SetItems(List<object> items)
        {
            EnsureInitialized();
            _virtualList.SetData(items);

            // Size the content area
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, _virtualList.TotalHeight);

            // Initial population
            UpdateVisible();
        }

        private void OnScroll(Vector2 pos)
        {
            UpdateVisible();
        }

        private void UpdateVisible()
        {
            if (_scrollRect.viewport == null) return;
            float viewportHeight = _scrollRect.viewport.rect.height;
            float scrollOffset = _content.anchoredPosition.y;
            if (scrollOffset < 0) scrollOffset = 0;
            _virtualList.UpdateVisible(scrollOffset, viewportHeight);
        }

        private GameObject CreateFallbackCell()
        {
            var go = new GameObject("Cell");
            go.AddComponent<RectTransform>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lr = labelGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(10, 0);
            lr.offsetMax = new Vector2(-10, 0);
            var txt = labelGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 20;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            return go;
        }

        void OnDestroy()
        {
            _virtualList?.Clear();
        }
    }
}
