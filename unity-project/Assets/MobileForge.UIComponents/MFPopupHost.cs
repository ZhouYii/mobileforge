using System;
using System.Collections.Generic;
using UnityEngine;
using MobileForge.Presentation;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// MonoBehaviour bridge for the pure C# PopupStack.
    /// Creates/destroys popup GameObjects from PopupStack events.
    /// </summary>
    public class MFPopupHost : MonoBehaviour
    {
        private PopupStack _popupStack;
        private Transform _root;
        private readonly Dictionary<string, GameObject> _activePopups = new();

        public PopupStack PopupStack => _popupStack;

        public void Setup(Transform root)
        {
            _root = root;
            _popupStack = new PopupStack();
        }

        /// <summary>
        /// Show a dialog popup via the PopupStack.
        /// </summary>
        public MFDialog ShowDialog(string popupId, string title, string message,
            string okLabel = "OK", Action onOk = null, int priority = 0)
        {
            var dialog = MFDialog.CreateConfirm(_root, title, message, okLabel, () =>
            {
                onOk?.Invoke();
                RemovePopup(popupId);
            });
            dialog.OnDismissed += () => RemovePopup(popupId);
            dialog.Show();
            _activePopups[popupId] = dialog.gameObject;
            return dialog;
        }

        /// <summary>
        /// Show a custom dialog with full configuration.
        /// </summary>
        public MFDialog ShowCustomDialog(string popupId, Action<MFDialog> configure)
        {
            var go = new GameObject($"Popup_{popupId}");
            go.transform.SetParent(_root, false);
            var dialog = go.AddComponent<MFDialog>();
            configure?.Invoke(dialog);
            dialog.OnDismissed += () => RemovePopup(popupId);
            dialog.Show();
            _activePopups[popupId] = go;
            return dialog;
        }

        /// <summary>
        /// Dismiss a popup by ID.
        /// </summary>
        public void DismissPopup(string popupId)
        {
            if (_activePopups.TryGetValue(popupId, out var go) && go != null)
            {
                var dialog = go.GetComponent<MFDialog>();
                if (dialog != null)
                    dialog.Dismiss();
                else
                    Destroy(go);
            }
            RemovePopup(popupId);
        }

        public bool IsShowing(string popupId) => _activePopups.ContainsKey(popupId);
        public int PopupCount => _activePopups.Count;

        private void RemovePopup(string popupId)
        {
            _activePopups.Remove(popupId);
        }

        void OnDestroy()
        {
            foreach (var kvp in _activePopups)
                if (kvp.Value != null) Destroy(kvp.Value);
            _activePopups.Clear();
        }
    }
}
