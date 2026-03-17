using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileForge.UIComponents
{
    /// <summary>
    /// ScriptableObject registry that maps primitive MonoBehaviour types to prefab references.
    /// Pages call MFPrimitiveLibrary.Spawn&lt;MFButton&gt;(parent) and the library resolves the prefab.
    ///
    /// Loaded from Resources/MFPrimitiveLibrary.asset. Game devs can override by providing
    /// their own library asset — swap prefabs to change the look of all pages.
    /// </summary>
    [CreateAssetMenu(fileName = "MFPrimitiveLibrary", menuName = "MobileForge/Primitive Library")]
    public class MFPrimitiveLibrary : ScriptableObject
    {
        [Header("Primitives")]
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private GameObject overlayPrefab;
        [SerializeField] private GameObject progressBarPrefab;
        [SerializeField] private GameObject scrollListPrefab;
        [SerializeField] private GameObject scrollGridPrefab;
        [SerializeField] private GameObject gemBoardPrefab;
        [SerializeField] private GameObject monsterCardPrefab;
        [SerializeField] private GameObject currencyDisplayPrefab;

        private static MFPrimitiveLibrary _instance;

        /// <summary>
        /// The active library instance. Loaded from Resources on first access.
        /// Can be overridden via SetInstance() for testing or custom themes.
        /// </summary>
        public static MFPrimitiveLibrary Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<MFPrimitiveLibrary>("MFPrimitiveLibrary");
                return _instance;
            }
        }

        /// <summary>
        /// Override the active library instance (e.g., for custom themes or testing).
        /// </summary>
        public static void SetInstance(MFPrimitiveLibrary library)
        {
            _instance = library;
        }

        private Dictionary<Type, GameObject> _prefabMap;

        private Dictionary<Type, GameObject> PrefabMap
        {
            get
            {
                if (_prefabMap == null)
                {
                    _prefabMap = new Dictionary<Type, GameObject>();
                    Register<Primitives.MFButton>(buttonPrefab);
                    Register<Primitives.MFOverlay>(overlayPrefab);
                    Register<Primitives.MFProgressBar>(progressBarPrefab);
                    Register<Primitives.MFScrollList>(scrollListPrefab);
                    Register<Primitives.MFScrollGrid>(scrollGridPrefab);
                    Register<Primitives.MFGemBoard>(gemBoardPrefab);
                    Register<Primitives.MFMonsterCard>(monsterCardPrefab);
                    Register<Primitives.MFCurrencyDisplay>(currencyDisplayPrefab);
                }
                return _prefabMap;
            }
        }

        private void Register<T>(GameObject prefab) where T : MonoBehaviour
        {
            if (prefab != null)
                _prefabMap[typeof(T)] = prefab;
        }

        /// <summary>
        /// Get the prefab for a primitive type.
        /// </summary>
        public GameObject GetPrefab<T>() where T : MonoBehaviour
        {
            PrefabMap.TryGetValue(typeof(T), out var prefab);
            return prefab;
        }

        /// <summary>
        /// Instantiate a primitive under the given parent.
        /// If no prefab is registered, creates a fallback with just the component.
        /// </summary>
        public static T Spawn<T>(Transform parent) where T : MonoBehaviour
        {
            var lib = Instance;
            GameObject prefab = lib != null ? lib.GetPrefab<T>() : null;

            if (prefab != null)
            {
                var go = Instantiate(prefab, parent);
                var comp = go.GetComponent<T>();
                if (comp == null)
                    comp = go.AddComponent<T>();
                return comp;
            }

            // Fallback: create a bare GameObject with the component
            // This allows code-first development before prefabs are set up
            var fallback = new GameObject(typeof(T).Name);
            fallback.transform.SetParent(parent, false);
            var rect = fallback.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return fallback.AddComponent<T>();
        }

        private void OnEnable()
        {
            // Reset map when the asset is reloaded in the Editor
            _prefabMap = null;
        }
    }
}
