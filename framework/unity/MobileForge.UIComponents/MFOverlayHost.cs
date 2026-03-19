using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.Presentation;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// MonoBehaviour bridge for the pure C# OverlayManager.
    /// Manages persistent overlay GameObjects (loading spinners, combo display, etc.).
    /// </summary>
    public class MFOverlayHost : MonoBehaviour
    {
        private OverlayManager _overlayManager;
        private Transform _root;
        private readonly Dictionary<string, GameObject> _overlayObjects = new();

        public OverlayManager OverlayManager => _overlayManager;

        public void Setup(Transform root)
        {
            _root = root;
            _overlayManager = new OverlayManager();
        }

        /// <summary>
        /// Show an overlay with a factory that creates the overlay content.
        /// </summary>
        public GameObject ShowOverlay(string overlayId, System.Func<Transform, GameObject> factory)
        {
            if (_overlayObjects.ContainsKey(overlayId))
                return _overlayObjects[overlayId];

            var go = factory(_root);
            _overlayObjects[overlayId] = go;
            _overlayManager.ShowOverlay(overlayId, () => go);
            return go;
        }

        /// <summary>
        /// Show a simple loading overlay with a message.
        /// </summary>
        public GameObject ShowLoadingOverlay(string message = "Loading...")
        {
            return ShowOverlay("loading", root =>
            {
                var go = new GameObject("LoadingOverlay");
                go.transform.SetParent(root, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var bg = go.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.7f);

                var textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.2f, 0.4f);
                textRect.anchorMax = new Vector2(0.8f, 0.6f);
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                var txt = textGo.AddComponent<Text>();
                txt.text = message;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 18;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;

                return go;
            });
        }

        /// <summary>
        /// Hide a specific overlay.
        /// </summary>
        public void HideOverlay(string overlayId)
        {
            if (_overlayObjects.TryGetValue(overlayId, out var go) && go != null)
                Destroy(go);
            _overlayObjects.Remove(overlayId);
            _overlayManager.HideOverlay(overlayId);
        }

        /// <summary>
        /// Hide all overlays.
        /// </summary>
        public void HideAll()
        {
            foreach (var kvp in _overlayObjects)
                if (kvp.Value != null) Destroy(kvp.Value);
            _overlayObjects.Clear();
            _overlayManager.HideAll();
        }

        public bool IsShowing(string overlayId) => _overlayObjects.ContainsKey(overlayId);

        void OnDestroy()
        {
            HideAll();
        }
    }
}
