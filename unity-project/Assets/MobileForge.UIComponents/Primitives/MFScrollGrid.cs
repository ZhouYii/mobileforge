using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Scrollable grid with cell recycling.
    /// Bridges the framework's GridView&lt;GameObject&gt; to a Unity ScrollRect + GridLayoutGroup.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFScrollGrid : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;

        public Action<object, int> OnItemSelected;

        private GridView<GameObject> _gridView;
        private GameObject _cellPrefab;
        private Action<GameObject, object, int> _bindCallback;
        private RectTransform _content;
        private int _columns;
        private float _cellSize;
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
                var layout = gameObject.AddComponent<LayoutElement>();
                layout.flexibleWidth = 1;
                layout.flexibleHeight = 1;

                _scrollRect = gameObject.AddComponent<ScrollRect>();
                _scrollRect.horizontal = false;
                _scrollRect.vertical = true;
                _scrollRect.movementType = ScrollRect.MovementType.Elastic;

                // Viewport
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

            _gridView = new GridView<GameObject>();
            _scrollRect.onValueChanged.AddListener(OnScroll);
        }

        /// <summary>
        /// Configure the grid with a cell prefab, bind callback, column count, and cell size.
        /// </summary>
        public void Setup(GameObject cellPrefab, Action<GameObject, object, int> bindCallback, int columns = 5, float cellSize = 100f)
        {
            EnsureInitialized();
            _cellPrefab = cellPrefab;
            _bindCallback = bindCallback;
            _columns = columns;
            _cellSize = cellSize;

            _gridView.Setup(
                () =>
                {
                    var cell = _cellPrefab != null
                        ? Instantiate(_cellPrefab, _content)
                        : CreateFallbackCell();
                    return cell;
                },
                (cell, data, index) =>
                {
                    cell.SetActive(true);
                    cell.transform.SetParent(_content, false);

                    // Position in grid
                    var (row, col) = _gridView.GetGridPosition(index);
                    var cellRect = cell.GetComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0, 1);
                    cellRect.anchorMax = new Vector2(0, 1);
                    cellRect.pivot = new Vector2(0, 1);
                    float spacing = 4f;
                    cellRect.anchoredPosition = new Vector2(
                        col * (_cellSize + spacing),
                        -row * (_cellSize + spacing));
                    cellRect.sizeDelta = new Vector2(_cellSize, _cellSize);

                    // Bind data
                    _bindCallback?.Invoke(cell, data, index);

                    // Wire tap
                    var btn = cell.GetComponent<Button>();
                    if (btn == null) btn = cell.AddComponent<Button>();
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnItemSelected?.Invoke(data, index));
                },
                columns,
                cellSize
            );
        }

        public void SetItems(List<object> items)
        {
            EnsureInitialized();
            _gridView.SetData(items);

            // Size content
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, _gridView.TotalHeight);

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
            _gridView.UpdateVisible(scrollOffset, viewportHeight);
        }

        private GameObject CreateFallbackCell()
        {
            var go = new GameObject("GridCell");
            go.AddComponent<RectTransform>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lr = labelGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            var txt = labelGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        void OnDestroy()
        {
            _gridView?.Clear();
        }
    }
}
