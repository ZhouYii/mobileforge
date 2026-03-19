using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// Anchor presets for layout regions within a page.
    /// </summary>
    public enum MFAnchor
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight,
        Fill
    }

    /// <summary>
    /// Padding values (left, top, right, bottom) in pixels for layout regions.
    /// </summary>
    public struct MFPadding
    {
        public float Left, Top, Right, Bottom;

        public MFPadding(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static MFPadding Zero => new(0, 0, 0, 0);
    }

    /// <summary>
    /// Base class for all page compositions. Pages extend this to build complete
    /// screens by instantiating and arranging primitives.
    /// </summary>
    public abstract class PageBase<TScreen> : MonoBehaviour, IUIComponent<TScreen>
        where TScreen : class
    {
        protected TScreen _screen;
        public TScreen Screen => _screen;

        /// <summary>
        /// True if this page was instantiated from a prefab (vs. built in code).
        /// Set automatically by prefab-based factories. Subclasses can check this
        /// to skip code-based UI construction in OnBind when a prefab layout exists.
        /// </summary>
        protected bool IsPrefabPage { get; set; }

        private RectTransform _safeArea;

        // ── IUIComponent implementation ──

        public void Bind(TScreen screen)
        {
            _screen = screen;
            // Auto-detect: prefabs have children, code-built GameObjects start empty
            IsPrefabPage = transform.childCount > 0;
            OnBind(screen);
        }

        public void BindUntyped(object screen) => Bind(screen as TScreen);

        public void Mount(Transform parent)
        {
            transform.SetParent(parent, false);

            // Stretch to fill parent
            var rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            OnMount();
        }

        public void Unmount()
        {
            OnUnmount();
            Destroy(gameObject);
        }

        public void Refresh()
        {
            if (_screen != null) OnRefresh();
        }

        // ── Override points ──

        protected abstract void OnBind(TScreen screen);
        protected virtual void OnMount() { }
        protected virtual void OnRefresh() { }
        protected virtual void OnUnmount() { }

        /// <summary>
        /// Called when the canvas/screen resolution changes.
        /// Override to implement custom responsive behavior.
        /// </summary>
        protected virtual void OnLayoutChanged(float width, float height) { }

        // ── Primitive spawning ──

        protected T SpawnPrimitive<T>(Transform parent = null) where T : MonoBehaviour
        {
            return MFPrimitiveLibrary.Spawn<T>(parent ?? transform);
        }

        // ── Layout helpers ──

        /// <summary>
        /// Safe area RectTransform that insets for device notch/cutout.
        /// </summary>
        protected RectTransform SafeArea
        {
            get
            {
                if (_safeArea == null)
                    _safeArea = CreateSafeArea();
                return _safeArea;
            }
        }

        /// <summary>
        /// Create an anchor-based layout region.
        /// </summary>
        protected RectTransform CreateRegion(string name, MFAnchor anchor, MFPadding padding = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor, padding);
            return rect;
        }

        /// <summary>
        /// Create a vertical layout container.
        /// </summary>
        protected RectTransform CreateVBox(string name, Transform parent, float spacing = 8f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rect;
        }

        /// <summary>
        /// Create a horizontal layout container.
        /// </summary>
        protected RectTransform CreateHBox(string name, Transform parent, float spacing = 8f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            return rect;
        }

        /// <summary>
        /// Create a grid layout container.
        /// </summary>
        protected RectTransform CreateGrid(string name, Transform parent, int columns, float cellSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var grid = go.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.spacing = new Vector2(4, 4);
            grid.childAlignment = TextAnchor.UpperCenter;

            return rect;
        }

        /// <summary>
        /// Create a simple container GameObject with a stretched RectTransform.
        /// </summary>
        protected GameObject CreateContainer(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        // ── Responsive layout ──

        protected virtual void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy) return;
            var rect = GetComponent<RectTransform>();
            if (rect != null)
                OnLayoutChanged(rect.rect.width, rect.rect.height);
        }

        // ── Internal helpers ──

        private RectTransform CreateSafeArea()
        {
            var go = new GameObject("SafeArea");
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Apply device safe area insets
            ApplySafeAreaInsets(rect);
            return rect;
        }

        private void ApplySafeAreaInsets(RectTransform rect)
        {
            var safeArea = UnityEngine.Screen.safeArea;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var canvasRect = canvas.GetComponent<RectTransform>();
            float scaleX = canvasRect.rect.width / UnityEngine.Screen.width;
            float scaleY = canvasRect.rect.height / UnityEngine.Screen.height;

            rect.offsetMin = new Vector2(safeArea.x * scaleX, safeArea.y * scaleY);
            rect.offsetMax = new Vector2(
                -(UnityEngine.Screen.width - safeArea.xMax) * scaleX,
                -(UnityEngine.Screen.height - safeArea.yMax) * scaleY);
        }

        private static void ApplyAnchor(RectTransform rect, MFAnchor anchor, MFPadding padding)
        {
            switch (anchor)
            {
                case MFAnchor.Top:
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
                case MFAnchor.Bottom:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;
                case MFAnchor.Left:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 0.5f);
                    break;
                case MFAnchor.Right:
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(1, 0.5f);
                    break;
                case MFAnchor.Center:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case MFAnchor.TopLeft:
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    break;
                case MFAnchor.TopRight:
                    rect.anchorMin = Vector2.one;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = Vector2.one;
                    break;
                case MFAnchor.BottomLeft:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = Vector2.zero;
                    break;
                case MFAnchor.BottomRight:
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    break;
                case MFAnchor.Fill:
                default:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    break;
            }

            rect.offsetMin = new Vector2(padding.Left, padding.Bottom);
            rect.offsetMax = new Vector2(-padding.Right, -padding.Top);
        }
    }
}
