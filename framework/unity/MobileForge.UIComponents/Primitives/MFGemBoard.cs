using System;
using System.Collections;
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

        /// <summary>Fired when a drag gesture ends (finger/mouse released or timer expired).</summary>
        public Action OnDragEnded;

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
        private bool _isDragging;
        private float _dragTimer;
        private float _dragMaxTime = 5f;
        private Image _timeBarFill;
        private GameObject _timeBarGo;
        private readonly List<int> _dragPath = new();
        private readonly List<GameObject> _trailMarkers = new();
        private GameObject _dragIndicator;

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
            BuildTimeBar();
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
                AddTrigger(trigger, EventTriggerType.PointerDown, () =>
                {
                    _dragStartIndex = cellIndex;
                    StartDrag();
                });
                AddTrigger(trigger, EventTriggerType.PointerUp, () =>
                {
                    if (_dragStartIndex >= 0 && _dragStartIndex != cellIndex)
                    {
                        OnGemSwapped?.Invoke(_dragStartIndex, cellIndex);
                    }
                    EndDrag();
                });
                AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
                {
                    if (_dragStartIndex >= 0 && _dragStartIndex != cellIndex)
                    {
                        OnGemSwapped?.Invoke(_dragStartIndex, cellIndex);
                        AddToTrail(cellIndex);
                        _dragStartIndex = cellIndex;
                        UpdateDragIndicatorColor();
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

        /// <summary>
        /// Swap the visual colors of two cells without rebuilding the grid.
        /// Use during drag to avoid destroying cells mid-interaction.
        /// </summary>
        public void SwapCellColors(int indexA, int indexB)
        {
            if (indexA >= 0 && indexA < _gemImages.Count && indexB >= 0 && indexB < _gemImages.Count
                && _gemImages[indexA] != null && _gemImages[indexB] != null)
            {
                var colorA = _gemImages[indexA].color;
                _gemImages[indexA].color = _gemImages[indexB].color;
                _gemImages[indexB].color = colorA;
            }
        }

        /// <summary>
        /// Update all cell colors from the current elements array without rebuilding.
        /// </summary>
        public void RefreshColors(int[] elements)
        {
            if (elements == null) return;
            int count = Mathf.Min(elements.Length, _gemImages.Count);
            for (int i = 0; i < count; i++)
            {
                if (_gemImages[i] != null)
                    _gemImages[i].color = _colorMap.GetValueOrDefault(elements[i], Color.gray);
            }
        }

        /// <summary>
        /// Flash specific gem cells to indicate a match. Gems scale up + turn white briefly.
        /// </summary>
        public void FlashGems(List<int> indices, float duration = 0.33f)
        {
            foreach (int idx in indices)
            {
                if (idx >= 0 && idx < _gemImages.Count && _gemImages[idx] != null)
                    StartCoroutine(FlashGem(_gemImages[idx], duration));
            }
        }

        private IEnumerator FlashGem(Image gem, float duration)
        {
            if (gem == null) yield break;
            Color original = gem.color;
            var rt = gem.GetComponent<RectTransform>();
            Vector3 origScale = rt.localScale;

            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);

                // Scale up then down
                float scale = p < 0.3f
                    ? Mathf.Lerp(1f, 1.3f, p / 0.3f)
                    : Mathf.Lerp(1.3f, 0f, (p - 0.3f) / 0.7f);
                if (rt != null) rt.localScale = origScale * Mathf.Max(scale, 0.01f);

                // White flash then fade
                if (gem != null)
                    gem.color = Color.Lerp(Color.white, original, p);

                yield return null;
            }

            // Reset
            if (rt != null) rt.localScale = origScale;
            if (gem != null) gem.color = original;
        }

        private void StartDrag()
        {
            _isDragging = true;
            _dragTimer = _dragMaxTime;
            _dragPath.Clear();
            if (_dragStartIndex >= 0) _dragPath.Add(_dragStartIndex);
            if (_timeBarGo != null)
                _timeBarGo.SetActive(true);

            // Dim the source gem
            if (_dragStartIndex >= 0 && _dragStartIndex < _gemImages.Count)
            {
                var srcImg = _gemImages[_dragStartIndex];
                if (srcImg != null)
                {
                    var c = srcImg.color;
                    srcImg.color = new Color(c.r, c.g, c.b, TowerOfSaviors.TosTheme.GemDragAlpha);
                }
            }

            // Create drag indicator
            if (_dragIndicator == null)
            {
                _dragIndicator = new GameObject("DragIndicator");
                _dragIndicator.transform.SetParent(transform.parent, false);
                var indImg = _dragIndicator.AddComponent<Image>();
                indImg.color = new Color(1, 1, 1, TowerOfSaviors.TosTheme.GemDragAlpha);
                indImg.raycastTarget = false;
                var rt = _dragIndicator.GetComponent<RectTransform>();
                float cellSize = _gridLayout != null ? _gridLayout.cellSize.x : 50;
                rt.sizeDelta = new Vector2(cellSize, cellSize) * TowerOfSaviors.TosTheme.GemDragScale;
            }
            UpdateDragIndicatorColor();
            _dragIndicator.SetActive(true);
        }

        private void EndDrag()
        {
            _isDragging = false;
            _dragStartIndex = -1;
            _dragPath.Clear();
            ClearTrailMarkers();
            if (_timeBarGo != null)
                _timeBarGo.SetActive(false);
            if (_dragIndicator != null)
                _dragIndicator.SetActive(false);

            // Restore all gem alphas
            foreach (var img in _gemImages)
                if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);

            // Notify listeners that drag is complete (triggers cascade resolution)
            OnDragEnded?.Invoke();
        }

        private void AddToTrail(int cellIndex)
        {
            if (_dragPath.Contains(cellIndex)) return;
            _dragPath.Add(cellIndex);

            // Create trail ring marker
            if (cellIndex >= 0 && cellIndex < _gemImages.Count && _gemImages[cellIndex] != null)
            {
                var gemRt = _gemImages[cellIndex].GetComponent<RectTransform>();
                var marker = new GameObject($"Trail_{cellIndex}");
                marker.transform.SetParent(transform, false);
                var markerRt = marker.AddComponent<RectTransform>();
                markerRt.anchoredPosition = gemRt.anchoredPosition;
                float cellSize = _gridLayout != null ? _gridLayout.cellSize.x : 50;
                markerRt.sizeDelta = new Vector2(cellSize, cellSize) * 0.9f;
                var ring = marker.AddComponent<Image>();
                ring.color = new Color(1, 1, 1, 0.25f);
                ring.raycastTarget = false;
                _trailMarkers.Add(marker);
            }
        }

        private void ClearTrailMarkers()
        {
            foreach (var m in _trailMarkers)
                if (m != null) Destroy(m);
            _trailMarkers.Clear();
        }

        private void UpdateDragIndicatorColor()
        {
            if (_dragIndicator == null || _dragStartIndex < 0 || _dragStartIndex >= _gemImages.Count) return;
            var srcImg = _gemImages[_dragStartIndex];
            if (srcImg != null)
            {
                var indImg = _dragIndicator.GetComponent<Image>();
                if (indImg != null)
                    indImg.color = new Color(srcImg.color.r, srcImg.color.g, srcImg.color.b, TowerOfSaviors.TosTheme.GemDragAlpha);
            }
        }

        void Update()
        {
            if (_isDragging)
            {
                _dragTimer -= Time.deltaTime;
                if (_dragTimer <= 0)
                {
                    EndDrag();
                    return;
                }
                UpdateTimeBar();
            }
        }

        private void BuildTimeBar()
        {
            if (_timeBarGo != null) Destroy(_timeBarGo);

            _timeBarGo = new GameObject("DragTimeBar");
            _timeBarGo.transform.SetParent(transform.parent, false);
            var barRect = _timeBarGo.AddComponent<RectTransform>();
            // Position below the board
            barRect.anchorMin = new Vector2(0.05f, 0);
            barRect.anchorMax = new Vector2(0.95f, 0);
            barRect.offsetMin = new Vector2(0, -10);
            barRect.offsetMax = new Vector2(0, -4);

            var barBg = _timeBarGo.AddComponent<Image>();
            barBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_timeBarGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0, 0.5f);
            _timeBarFill = fillGo.AddComponent<Image>();
            _timeBarFill.color = new Color(0.2f, 0.8f, 0.2f);

            _timeBarGo.SetActive(false);
        }

        private void UpdateTimeBar()
        {
            if (_timeBarFill == null) return;
            float ratio = Mathf.Clamp01(_dragTimer / _dragMaxTime);
            var rt = _timeBarFill.GetComponent<RectTransform>();
            rt.anchorMax = new Vector2(ratio, 1);

            // Color: green → yellow → red
            if (ratio > 0.5f)
                _timeBarFill.color = Color.Lerp(new Color(1f, 0.9f, 0.1f), new Color(0.2f, 0.8f, 0.2f), (ratio - 0.5f) * 2f);
            else
                _timeBarFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(1f, 0.9f, 0.1f), ratio * 2f);
        }
    }
}
