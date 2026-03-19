using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.Domain;
using MobileForge.UIComponents.Primitives;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Battle page. Shows enemies with HP/countdown, team HP, gem board,
    /// combo display, wave/turn info, and action buttons.
    /// Matches Godot battle_screen.gd layout.
    /// </summary>
    public class BattlePage : PageBase<BattleScreen>
    {
        [Header("Prefab References")]
        [SerializeField] private Text _waveLabel;
        [SerializeField] private Text _turnLabel;
        [SerializeField] private RectTransform _enemyArea;
        [SerializeField] private RectTransform _boardArea;
        [SerializeField] private RectTransform _skillArea;
        [SerializeField] private RectTransform _actionArea;

        private readonly List<MFEnemyView> _enemyViews = new();
        private readonly List<MFButton> _skillButtons = new();
        private Coroutine _hpPulseCoroutine;
        private float _lastHpRatio = 1f;
        private MFProgressBar _teamHpBar;
        private Text _teamHpLabel;
        private MFGemBoard _gemBoard;
        private MFButton _resolveBtn;
        private MFButton _shuffleBtn;
        private MFFloatingText _floatingText;
        private Text _comboDisplay;
        private Transform _enemyContainer;
        private Transform _skillContainer;
        private BattleOverlayMenu _overlayMenu;
        private int _lastCombo;

        protected override void OnBind(BattleScreen screen)
        {
            if (!IsPrefabPage)
                BuildSkeleton(screen);
            else
            {
                if (_waveLabel != null) _waveLabel.text = "Wave 1";
                if (_turnLabel != null) _turnLabel.text = "Turn 1";
            }

            InitContent(screen);
        }

        /// <summary>
        /// Creates dynamic primitives (enemies, HP bar, gem board, skills, buttons)
        /// into the container references set by either BuildSkeleton or prefab SerializeFields.
        /// Shared between both paths — eliminates duplication.
        /// </summary>
        private void InitContent(BattleScreen screen)
        {
            // Enemy views into _enemyArea
            if (_enemyArea != null)
            {
                _enemyContainer = CreateHBox("EnemyRow", _enemyArea, spacing: 6f);
                var ecLayout = _enemyContainer.GetComponent<HorizontalLayoutGroup>();
                if (ecLayout != null) ecLayout.childAlignment = TextAnchor.MiddleCenter;
            }

            // Combo display overlay
            var comboGo = new GameObject("ComboDisplay");
            comboGo.transform.SetParent(transform, false);
            var comboRect = comboGo.AddComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.2f, 0.68f);
            comboRect.anchorMax = new Vector2(0.8f, 0.75f);
            comboRect.offsetMin = Vector2.zero;
            comboRect.offsetMax = Vector2.zero;
            _comboDisplay = comboGo.AddComponent<Text>();
            _comboDisplay.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _comboDisplay.fontSize = 24;
            _comboDisplay.alignment = TextAnchor.MiddleCenter;
            _comboDisplay.color = Color.clear;
            comboGo.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            // Team HP bar
            if (_enemyArea != null)
            {
                var hpGo = new GameObject("TeamHpArea");
                hpGo.transform.SetParent(transform, false);
                var hpRect = hpGo.AddComponent<RectTransform>();
                hpRect.anchorMin = new Vector2(0, 1);
                hpRect.anchorMax = Vector2.one;
                hpRect.pivot = new Vector2(0.5f, 1);
                hpRect.offsetMin = new Vector2(20, -198 - TosTheme.HpBarHeight - 4);
                hpRect.offsetMax = new Vector2(-20, -198);
                _teamHpBar = SpawnPrimitive<MFProgressBar>(hpRect);
                _teamHpBar.SetColor(new Color(0.2f, 0.8f, 0.2f));
            }

            // Gem board into _boardArea
            if (_boardArea != null)
            {
                _gemBoard = SpawnPrimitive<MFGemBoard>(_boardArea);
                _gemBoard.SetElements(screen.GetBoardElements(), screen.Board?.Config?.Rows ?? 5, screen.Board?.Config?.Cols ?? 6);
                _gemBoard.OnGemSwapped = (from, to) =>
                {
                    if (_screen.Board != null)
                    {
                        _screen.Board.SwapGems(from, to);
                        _gemBoard.SwapCellColors(from, to);
                    }
                };
                _gemBoard.OnDragEnded = () =>
                {
                    if (_resolveBtn != null)
                    {
                        _resolveBtn.SetEnabled(false);
                        _shuffleBtn?.SetEnabled(false);
                        var result = screen.ResolveTurn();
                        if (result != null && screen.LastCascadeSteps.Count > 0)
                            StartCoroutine(AnimateCascadeThenFinish(result, screen.LastCascadeSteps));
                        else
                        {
                            Refresh();
                            _resolveBtn.SetEnabled(true);
                            _shuffleBtn?.SetEnabled(true);
                        }
                    }
                };
            }

            // Floating text layer
            var floatLayer = new GameObject("FloatingTextLayer");
            floatLayer.transform.SetParent(transform, false);
            var floatRect = floatLayer.AddComponent<RectTransform>();
            floatRect.anchorMin = Vector2.zero;
            floatRect.anchorMax = Vector2.one;
            floatRect.offsetMin = Vector2.zero;
            floatRect.offsetMax = Vector2.zero;
            _floatingText = floatLayer.AddComponent<MFFloatingText>();

            // Skill buttons into _skillArea
            if (_skillArea != null)
            {
                _skillContainer = CreateHBox("SkillRow", _skillArea, spacing: 4f);
                var skillLayout = _skillContainer.GetComponent<HorizontalLayoutGroup>();
                if (skillLayout != null) skillLayout.childAlignment = TextAnchor.MiddleCenter;
                BuildSkillButtons();
            }

            // Action buttons into _actionArea
            if (_actionArea != null)
            {
                var actionVBox = CreateVBox("ActionVBox", _actionArea, spacing: 4f);

                _resolveBtn = SpawnPrimitive<MFButton>(actionVBox);
                _resolveBtn.gameObject.name = "btn_resolve";
                _resolveBtn.SetLabel("Resolve Turn");
                _resolveBtn.SetColor(new Color(0.8f, 0.3f, 0.1f), Color.white);
                _resolveBtn.OnClick = () =>
                {
                    _resolveBtn.SetEnabled(false);
                    _shuffleBtn.SetEnabled(false);
                    var result = screen.ResolveTurn();
                    if (result != null && screen.LastCascadeSteps.Count > 0)
                        StartCoroutine(AnimateCascadeThenFinish(result, screen.LastCascadeSteps));
                    else
                    {
                        ShowTurnResults(result);
                        Refresh();
                        _resolveBtn.SetEnabled(true);
                        _shuffleBtn.SetEnabled(true);
                    }
                };

                _shuffleBtn = SpawnPrimitive<MFButton>(actionVBox);
                _shuffleBtn.gameObject.name = "btn_shuffle";
                _shuffleBtn.SetLabel("Shuffle");
                _shuffleBtn.SetColor(new Color(0.25f, 0.25f, 0.3f), Color.white);
                _shuffleBtn.OnClick = () =>
                {
                    screen.Board?.InitBoard();
                    Refresh();
                };
                var shuffleLe = _shuffleBtn.gameObject.GetComponent<LayoutElement>();
                if (shuffleLe != null) shuffleLe.preferredHeight = 36;
            }

            OnRefresh();
        }

        /// <summary>
        /// Code-built fallback: creates the full page skeleton (background, header, containers).
        /// Container refs are assigned to instance fields so InitContent() can populate them.
        /// </summary>
        private void BuildSkeleton(BattleScreen screen)
        {
            gameObject.AddComponent<Image>().color = TosTheme.BgDark;

            // ── Header: Wave + Turn (slides in from top) ──
            var header = CreateRegion("Header", MFAnchor.Top, new MFPadding(0, 0, 0, 0));
            header.sizeDelta = new Vector2(0, 40);
            header.gameObject.AddComponent<Image>().color = TosTheme.BgPanel;
            StartCoroutine(MFUIAnim.SlideIn(header, new Vector2(0, 60), TosTheme.AnimPanel));

            var headerHBox = CreateHBox("HeaderRow", header, spacing: 8f);

            var pauseBtn = SpawnPrimitive<MFButton>(headerHBox);
            pauseBtn.gameObject.name = "btn_pause";
            pauseBtn.SetLabel("\u2630");
            pauseBtn.SetColor(new Color(0.2f, 0.2f, 0.25f), Color.white);
            var pauseLe = pauseBtn.gameObject.GetComponent<LayoutElement>();
            if (pauseLe != null) { pauseLe.preferredWidth = 40; pauseLe.flexibleWidth = 0; }
            pauseBtn.OnClick = () => ShowOverlayMenu();

            var waveGo = new GameObject("WaveLabel");
            waveGo.transform.SetParent(headerHBox, false);
            waveGo.AddComponent<RectTransform>();
            waveGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            _waveLabel = waveGo.AddComponent<Text>();
            _waveLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _waveLabel.fontSize = TosTheme.FontBody;
            _waveLabel.color = Color.white;
            _waveLabel.alignment = TextAnchor.MiddleCenter;

            var turnGo = new GameObject("TurnLabel");
            turnGo.transform.SetParent(headerHBox, false);
            turnGo.AddComponent<RectTransform>();
            turnGo.AddComponent<LayoutElement>().preferredWidth = 80;
            _turnLabel = turnGo.AddComponent<Text>();
            _turnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _turnLabel.fontSize = TosTheme.FontBody;
            _turnLabel.color = Color.white;
            _turnLabel.alignment = TextAnchor.MiddleCenter;

            // ── Container regions (content populated by InitContent) ──
            _enemyArea = CreateRegion("EnemyArea", MFAnchor.Top, new MFPadding(4, 44, 4, 0));
            _enemyArea.sizeDelta = new Vector2(0, 150);

            _boardArea = CreateRegion("BoardArea", MFAnchor.Fill, new MFPadding(8, 230, 8, 110));

            _skillArea = CreateRegion("SkillArea", MFAnchor.Bottom, new MFPadding(4, 0, 4, 112));
            _skillArea.sizeDelta = new Vector2(0, TosTheme.SkillBtnSize.y + 4);

            _actionArea = CreateRegion("Actions", MFAnchor.Bottom, new MFPadding(8, 0, 8, 8));
            _actionArea.sizeDelta = new Vector2(0, 100);
        }

        protected override void OnRefresh()
        {
            if (_screen?.State == null) return;
            var state = _screen.State;

            // Update wave/turn labels
            _waveLabel.text = $"Wave {state.CurrentWaveIndex + 1}";
            _turnLabel.text = $"Turn {state.TurnNumber}";

            // Update enemies
            RefreshEnemyViews();

            // Update team HP with color coding and pulse
            float hpRatio = state.MaxHp > 0 ? (float)state.TeamHp / state.MaxHp : 0;
            _teamHpBar.SetColor(TosTheme.HpBarColor(hpRatio));
            _teamHpBar.SetProgress(state.TeamHp, state.MaxHp);

            // Pulse when crossing critical threshold
            if (hpRatio < 0.2f && _lastHpRatio >= 0.2f)
            {
                if (_hpPulseCoroutine != null) StopCoroutine(_hpPulseCoroutine);
                _hpPulseCoroutine = StartCoroutine(PulseHpBar());
            }
            _lastHpRatio = hpRatio;

            // Update board
            _gemBoard.SetElements(_screen.GetBoardElements(), _screen.Board?.Config?.Rows ?? 5, _screen.Board?.Config?.Cols ?? 6);

            // Update skill buttons
            RefreshSkillButtons();
        }

        private IEnumerator PulseHpBar()
        {
            var rt = _teamHpBar.GetComponent<RectTransform>();
            if (rt == null) yield break;
            for (int i = 0; i < 3; i++)
            {
                rt.localScale = Vector3.one * 1.08f;
                yield return new WaitForSeconds(0.1f);
                rt.localScale = Vector3.one;
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void RefreshEnemyViews()
        {
            var state = _screen.State;
            if (state?.Enemies == null) return;

            // Create/update enemy views
            int enemyCount = state.Enemies.Count;

            // Remove excess views
            while (_enemyViews.Count > enemyCount)
            {
                int last = _enemyViews.Count - 1;
                if (_enemyViews[last] != null)
                    Destroy(_enemyViews[last].gameObject);
                _enemyViews.RemoveAt(last);
            }

            // Add missing views with entrance animation
            while (_enemyViews.Count < enemyCount)
            {
                var view = SpawnPrimitive<MFEnemyView>(_enemyContainer);
                _enemyViews.Add(view);

                // Slide in from top (1.0s)
                var rt = view.GetComponent<RectTransform>();
                if (rt != null)
                    StartCoroutine(MFUIAnim.SlideIn(rt, new Vector2(0, 150), TosTheme.AnimEnemyEnter));
            }

            // Update all views
            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = state.Enemies[i];
                _enemyViews[i].SetEnemy(
                    enemy.Name,
                    enemy.Hp,
                    enemy.MaxHp,
                    enemy.Element,
                    enemy.Countdown
                );
                if (!enemy.IsAlive)
                    _enemyViews[i].SetDead();
            }
        }

        private IEnumerator AnimateCascadeThenFinish(TurnResult result, List<CascadeStep> steps)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];

                // Flash matched positions
                var matchedPositions = new List<int>();
                foreach (var match in step.Matches)
                    matchedPositions.AddRange(match.Positions);

                if (matchedPositions.Count > 0)
                {
                    _gemBoard.FlashGems(matchedPositions.Distinct().ToList(), TosTheme.AnimGemMatch);

                    // Show combo number
                    int comboNum = i + 1;
                    _floatingText?.SpawnCombo(comboNum, new Vector2(0, 120));

                    yield return new WaitForSeconds(TosTheme.AnimGemMatch + 0.1f);
                }

                // Refresh board after this step
                _gemBoard.SetElements(_screen.GetBoardElements(), _screen.Board?.Config?.Rows ?? 5, _screen.Board?.Config?.Cols ?? 6);
                yield return new WaitForSeconds(0.15f);
            }

            // Show final results
            ShowTurnResults(result);
            Refresh();
            _resolveBtn.SetEnabled(true);
            _shuffleBtn.SetEnabled(true);
        }

        private void BuildSkillButtons()
        {
            _skillButtons.Clear();
            if (_screen?.SkillSlots == null) return;

            for (int i = 0; i < _screen.SkillSlots.Count; i++)
            {
                int slotIndex = i;
                var slot = _screen.SkillSlots[i];

                var btn = SpawnPrimitive<MFButton>(_skillContainer);
                btn.gameObject.name = $"btn_skill_{i}";
                var le = btn.gameObject.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.preferredWidth = TosTheme.SkillBtnSize.x;
                    le.preferredHeight = TosTheme.SkillBtnSize.y;
                    le.flexibleWidth = 0;
                }
                btn.OnClick = () =>
                {
                    _screen.ActivateSkill(slotIndex);
                    RefreshSkillButtons();
                    UIServices.Toasts?.ShowToast($"Skill activated: {slot.Name}");
                };
                _skillButtons.Add(btn);
            }
            RefreshSkillButtons();
        }

        private void RefreshSkillButtons()
        {
            if (_screen?.SkillSlots == null) return;

            for (int i = 0; i < _skillButtons.Count && i < _screen.SkillSlots.Count; i++)
            {
                var slot = _screen.SkillSlots[i];
                var btn = _skillButtons[i];

                if (slot.IsSealed)
                {
                    btn.SetLabel($"{slot.Name}\n(SEALED)");
                    btn.SetColor(new Color(0.3f, 0.1f, 0.1f), new Color(0.6f, 0.3f, 0.3f));
                    btn.SetEnabled(false);
                }
                else if (slot.CurrentCd > 0)
                {
                    btn.SetLabel($"{slot.Name}\n(CD:{slot.CurrentCd})");
                    btn.SetColor(new Color(0.2f, 0.2f, 0.2f), TosTheme.TextMuted);
                    btn.SetEnabled(false);
                }
                else
                {
                    btn.SetLabel(slot.Name);
                    btn.SetColor(TosTheme.ElementButtonBg(slot.Element), TosTheme.ElementColor(slot.Element));
                    btn.SetEnabled(true);
                }
            }
        }

        private void ShowOverlayMenu()
        {
            if (_overlayMenu != null) return;

            var overlayGo = new GameObject("BattleOverlay");
            overlayGo.transform.SetParent(transform, false);
            _overlayMenu = overlayGo.AddComponent<BattleOverlayMenu>();

            // Set stage context (matching original stageName_TM, floorName_TM, etc.)
            _overlayMenu.StageName = _screen?.StageName ?? "Stage";
            _overlayMenu.FloorName = _screen?.FloorName ?? "";
            _overlayMenu.BattleProgress = _screen != null
                ? $"Battle {_screen.CurrentWave}/{_screen.TotalWaves}" : "";
            _overlayMenu.RoundNumber = _screen?.RoundNumber ?? 0;

            _overlayMenu.Setup();
            _overlayMenu.OnResume = () => { _overlayMenu = null; };
            _overlayMenu.OnSurrender = () =>
            {
                _overlayMenu = null;
                UIServices.Toasts?.ShowToast("Surrendered — returning to title...");
            };
            _overlayMenu.OnSettings = () =>
            {
                UIServices.Popups?.ShowDialog("battle_settings",
                    "Battle Settings",
                    "Sound: ON\nBGM: ON\nAnimations: ON\n\n(Adjust in main Settings)",
                    "OK");
            };
            _overlayMenu.OnRewards = () =>
            {
                UIServices.Popups?.ShowDialog("battle_rewards",
                    "Floor Rewards",
                    "Rewards will be shown at the end of the dungeon.",
                    "OK");
            };
            _overlayMenu.OnTeamInfo = () =>
            {
                string teamInfo = "Team Members:\n\n";
                if (_screen?.SkillSlots != null)
                {
                    for (int i = 0; i < _screen.SkillSlots.Count; i++)
                    {
                        var slot = _screen.SkillSlots[i];
                        teamInfo += $"  {i + 1}. {slot.Name} ({TosTheme.ElementName(slot.Element)})\n";
                    }
                }
                UIServices.Popups?.ShowDialog("team_info", "Team Info", teamInfo, "Close");
            };
            _overlayMenu.Open();
        }

        private void ShowTurnResults(TurnResult result)
        {
            if (result == null) return;

            // Show total damage dealt to enemies
            int totalDamage = 0;
            foreach (var kvp in result.DamagePerEnemy)
                totalDamage += kvp.Value;

            if (totalDamage > 0)
            {
                _floatingText?.SpawnDamage(totalDamage, new Vector2(0, 50), 0);
            }

            // Show healing
            if (result.Healing > 0)
            {
                _floatingText?.SpawnHeal(result.Healing, new Vector2(0, -100));
            }

            // Show enemy attacks as damage to player
            int enemyDamage = 0;
            if (result.EnemyAttacks != null)
            {
                foreach (var atk in result.EnemyAttacks)
                {
                    if (atk.TryGetValue("damage", out var dmgObj))
                        enemyDamage += System.Convert.ToInt32(dmgObj);
                }
            }
            if (enemyDamage > 0)
            {
                _floatingText?.Spawn(
                    $"-{enemyDamage}",
                    new Vector2(0, -60),
                    new Color(1f, 0.3f, 0.3f),
                    0.5f, 20);
            }

            // Show kills
            if (result.EnemiesKilled?.Count > 0)
            {
                _floatingText?.Spawn(
                    $"{result.EnemiesKilled.Count} Killed!",
                    new Vector2(0, 80),
                    TosTheme.TextGold,
                    0.6f, 20);
            }
        }
    }
}
