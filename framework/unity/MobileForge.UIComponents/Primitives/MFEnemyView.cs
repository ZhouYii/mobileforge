using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MobileForge.UIComponents;

namespace MobileForge.UIComponents.Primitives
{
    /// <summary>
    /// Enemy display panel with HP bar, countdown badge, element tint, and death state.
    /// Matches the Godot enemy_view.gd component.
    /// </summary>
    public class MFEnemyView : MonoBehaviour
    {
        private Image _background;
        private Text _nameLabel;
        private Image _hpFill;
        private Text _hpText;
        private Text _countdownText;
        private Image _countdownBg;
        private CanvasGroup _canvasGroup;
        private Text _deadMark;
        private LayoutElement _layoutElement;

        private int _element;
        private bool _isDead;
        private float _currentFillRatio = 1f;
        private float _targetFillRatio = 1f;
        private Coroutine _cdPulse;

        void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var rt = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _layoutElement = gameObject.AddComponent<LayoutElement>();
            _layoutElement.preferredWidth = 130;
            _layoutElement.preferredHeight = 140;

            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.spacing = 3;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(transform, false);
            _nameLabel = nameGo.AddComponent<Text>();
            _nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _nameLabel.fontSize = 13;
            _nameLabel.alignment = TextAnchor.MiddleCenter;
            _nameLabel.color = Color.white;
            var nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.preferredHeight = 20;

            var nameShadow = nameGo.AddComponent<Shadow>();
            nameShadow.effectColor = new Color(0, 0, 0, 0.6f);
            nameShadow.effectDistance = new Vector2(1, -1);

            // HP bar background
            var hpBarGo = new GameObject("HpBar");
            hpBarGo.transform.SetParent(transform, false);
            var hpBarBg = hpBarGo.AddComponent<Image>();
            hpBarBg.color = new Color(0.15f, 0.15f, 0.15f);
            var hpBarLe = hpBarGo.AddComponent<LayoutElement>();
            hpBarLe.preferredHeight = 14;

            // HP fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(hpBarGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0, 0.5f);
            _hpFill = fillGo.AddComponent<Image>();
            _hpFill.color = new Color(0.2f, 0.8f, 0.2f);

            // HP text
            var hpTextGo = new GameObject("HpText");
            hpTextGo.transform.SetParent(hpBarGo.transform, false);
            var hpTextRect = hpTextGo.AddComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;
            _hpText = hpTextGo.AddComponent<Text>();
            _hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hpText.fontSize = 10;
            _hpText.alignment = TextAnchor.MiddleCenter;
            _hpText.color = Color.white;

            // Countdown
            var cdGo = new GameObject("Countdown");
            cdGo.transform.SetParent(transform, false);
            _countdownBg = cdGo.AddComponent<Image>();
            _countdownBg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            var cdLe = cdGo.AddComponent<LayoutElement>();
            cdLe.preferredHeight = 22;

            _countdownText = new GameObject("CdText").AddComponent<Text>();
            _countdownText.transform.SetParent(cdGo.transform, false);
            var cdTextRect = _countdownText.GetComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.offsetMin = Vector2.zero;
            cdTextRect.offsetMax = Vector2.zero;
            _countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _countdownText.fontSize = 14;
            _countdownText.alignment = TextAnchor.MiddleCenter;
            _countdownText.color = Color.white;

            // Dead mark (hidden by default)
            var deadGo = new GameObject("DeadMark");
            deadGo.transform.SetParent(transform, false);
            _deadMark = deadGo.AddComponent<Text>();
            _deadMark.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _deadMark.fontSize = 36;
            _deadMark.alignment = TextAnchor.MiddleCenter;
            _deadMark.color = new Color(1, 0.2f, 0.2f, 0.8f);
            _deadMark.text = "\u2716"; // ✖
            deadGo.SetActive(false);
            var deadLe = deadGo.AddComponent<LayoutElement>();
            deadLe.flexibleHeight = 1;
        }

        /// <summary>
        /// Set enemy data.
        /// </summary>
        public void SetEnemy(string name, int hp, int maxHp, int element, int countdown)
        {
            _element = element;
            _isDead = hp <= 0;
            _nameLabel.text = name;
            _hpText.text = $"{hp}/{maxHp}";

            _targetFillRatio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0;

            var ec = TowerOfSaviors.TosTheme.ElementColor(element);
            _hpFill.color = ec;
            _background.color = TowerOfSaviors.TosTheme.ElementPanelBg(element);

            // Countdown
            _countdownText.text = $"CD: {countdown}";
            _countdownText.color = countdown <= 1 ? new Color(1, 0.3f, 0.3f) : Color.white;
            _countdownBg.color = countdown <= 1
                ? new Color(0.4f, 0.1f, 0.1f, 0.9f)
                : new Color(0.2f, 0.2f, 0.25f, 0.9f);

            // Pulse countdown badge when ≤ 1
            if (countdown <= 1 && !_isDead)
            {
                if (_cdPulse == null)
                    _cdPulse = StartCoroutine(MFUIAnim.Pulse(_countdownBg.transform, 1f, 1.15f, 0.6f));
            }
            else
            {
                if (_cdPulse != null) { StopCoroutine(_cdPulse); _cdPulse = null; }
                _countdownBg.transform.localScale = Vector3.one;
            }

            UpdateDeadState();
        }

        /// <summary>
        /// Mark enemy as dead.
        /// </summary>
        public void SetDead()
        {
            _isDead = true;
            _targetFillRatio = 0;
            UpdateDeadState();
        }

        private void UpdateDeadState()
        {
            _canvasGroup.alpha = _isDead ? 0.4f : 1f;
            _deadMark.gameObject.SetActive(_isDead);
        }

        void Update()
        {
            // Smooth HP bar animation
            if (Mathf.Abs(_currentFillRatio - _targetFillRatio) > 0.001f)
            {
                _currentFillRatio = Mathf.Lerp(_currentFillRatio, _targetFillRatio,
                    Time.deltaTime / TowerOfSaviors.TosTheme.AnimHpBar);
                UpdateFillBar();
            }
        }

        private void UpdateFillBar()
        {
            if (_hpFill == null) return;
            var rt = _hpFill.GetComponent<RectTransform>();
            rt.anchorMax = new Vector2(_currentFillRatio, 1);
        }
    }
}
