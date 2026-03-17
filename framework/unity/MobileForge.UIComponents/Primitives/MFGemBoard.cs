using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Puzzle gem board with drag-to-swap interaction.
    /// Renders a rows x cols grid of colored gem cells with touch/mouse drag support.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MFGemBoard : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup _gridLayout;

        /// <summary>Fired when the player drags a gem from one cell to another.</summary>
        public Action<int, int> OnGemSwapped;

        // Element ID -> color mapping (ToS standard: 1=water, 2=fire, 3=grass, 4=light, 5=dark, 6=heart)
        private static readonly Dictionary<int, Color> DefaultColorMap = new()
        {
            { 1, new Color(0.2f, 0.5f, 1f) },    // Water
            { 2, new Color(1f, 0.3f, 0.2f) },     // Fire
            { 3, new Color(0.2f, 0.8f, 0.3f) },   // Grass
            { 4, new Color(1f, 0.9f, 0.3f) },     // Light
            { 5, new Color(0.6f, 0.2f, 0.8f) },   // Dark
            { 6, new Color(1f, 0.5f, 0.7f) },     // Heart
        };

        private Dictionary<int, Color> _colorMap;
        private int _rows;
        private int _cols;
        private int[] _elements;
        private readonly List<Image> _gemImages = new();
        private int _dragStartIndex = -1;
        private bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _colorMap = DefaultColorMap;

            if (_gridLayout == null)
                _gridLayout = GetComponent<GridLayoutGroup>();

            if (_gridLayout == null)
            {
                _gridLayout = gameObject.AddComponent<GridLayoutGroup>();

                // Add aspect ratio fitter to ensure board fits parent without overflow
                var fitter = gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1.2f; // 6:5 = 1.2

                var bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            }
        }

        public void SetColorMap(Dictionary<int, Color> colorMap)
        {
            _colorMap = colorMap ?? DefaultColorMap;
        }

        public void SetElements(int[] elements, int rows, int cols)
        {
            EnsureInitialized();
            _rows = rows;
            _cols = cols;
            _elements = elements;

            // Configure grid layout
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = cols;
            _gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // Calculate cell size based on available space
            var boardRect = GetComponent<RectTransform>();
            float availableWidth = boardRect.rect.width;
            float availableHeight = boardRect.rect.height;

            // If rect is not resolved yet, use reasonable defaults
            if (availableWidth <= 0) availableWidth = 450;
            if (availableHeight <= 0) availableHeight = 375;

            float spacing = 4f;
            float cellW = (availableWidth - spacing * (cols - 1)) / cols;
            float cellH = (availableHeight - spacing * (rows - 1)) / rows;
            float cellSize = Mathf.Min(cellW, cellH);

            _gridLayout.cellSize = new Vector2(cellSize, cellSize);
            _gridLayout.spacing = new Vector2(spacing, spacing);

            RebuildCells();
        }

        private void RebuildCells()
        {
            // Clear existing
            foreach (var img in _gemImages)
                if (img != null) Destroy(img.gameObject);
            _gemImages.Clear();

            if (_elements == null) return;

            for (int i = 0; i < _rows * _cols && i < _elements.Length; i++)
            {
                int elem = _elements[i];
                var cellGO = new GameObject($"gem_{i / _cols}_{i % _cols}");
                cellGO.transform.SetParent(transform, false);

                var img = cellGO.AddComponent<Image>();
                img.color = _colorMap.GetValueOrDefault(elem, Color.gray);
                _gemImages.Add(img);

                // Drag support via EventTrigger
                int cellIndex = i;
                var trigger = cellGO.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerDown, () => _dragStartIndex = cellIndex);
                AddTrigger(trigger, EventTriggerType.PointerUp, () =>
                {
                    if (_dragStartIndex >= 0 && _dragStartIndex != cellIndex)
                    {
                        OnGemSwapped?.Invoke(_dragStartIndex, cellIndex);
                    }
                    _dragStartIndex = -1;
                });
                AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
                {
                    if (_dragStartIndex >= 0 && _dragStartIndex != cellIndex)
                    {
                        OnGemSwapped?.Invoke(_dragStartIndex, cellIndex);
                        _dragStartIndex = cellIndex;
                    }
                });
            }
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }
    }
}
