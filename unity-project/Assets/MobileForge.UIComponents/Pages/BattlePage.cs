using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;
using MobileForge.Presentation;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Battle page. Shows enemy HP, team HP, gem board, and resolve button.
    /// </summary>
    public class BattlePage : PageBase<BattleScreen>
    {
        private MFProgressBar _enemyHpBar;
        private MFProgressBar _teamHpBar;
        private MFGemBoard _gemBoard;
        private MFButton _resolveBtn;
        private Text _waveLabel;

        protected override void OnBind(BattleScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Top: enemy info + HP bar
            var topRegion = CreateRegion("EnemyArea", MFAnchor.Top,
                new MFPadding(20, 20, 20, 0));
            topRegion.sizeDelta = new Vector2(0, 80);
            var topVBox = CreateVBox("EnemyVBox", topRegion, spacing: 4f);

            // Wave label
            var waveLabelGO = new GameObject("WaveLabel");
            waveLabelGO.transform.SetParent(topVBox, false);
            waveLabelGO.AddComponent<RectTransform>();
            waveLabelGO.AddComponent<LayoutElement>().preferredHeight = 30;
            _waveLabel = waveLabelGO.AddComponent<Text>();
            _waveLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _waveLabel.fontSize = 24;
            _waveLabel.color = Color.red;
            _waveLabel.alignment = TextAnchor.MiddleCenter;

            _enemyHpBar = SpawnPrimitive<MFProgressBar>(topVBox);
            _enemyHpBar.SetColor(Color.red);

            // Team HP bar
            var teamRegion = CreateRegion("TeamArea", MFAnchor.Top,
                new MFPadding(20, 100, 20, 0));
            teamRegion.sizeDelta = new Vector2(0, 40);
            _teamHpBar = SpawnPrimitive<MFProgressBar>(teamRegion);
            _teamHpBar.SetColor(Color.green);

            // Gem board in center
            var boardRegion = CreateRegion("BoardArea", MFAnchor.Fill,
                new MFPadding(10, 150, 10, 80));
            _gemBoard = SpawnPrimitive<MFGemBoard>(boardRegion);
            _gemBoard.SetElements(screen.GetBoardElements(), 5, 6);
            _gemBoard.OnGemSwapped = (from, to) =>
            {
                // Swap elements on the board
                var elements = _screen.GetBoardElements();
                if (from >= 0 && from < elements.Length && to >= 0 && to < elements.Length)
                {
                    (elements[from], elements[to]) = (elements[to], elements[from]);
                }
            };

            // Resolve button at bottom
            var bottomRegion = CreateRegion("Actions", MFAnchor.Bottom,
                new MFPadding(40, 0, 40, 10));
            bottomRegion.sizeDelta = new Vector2(0, 65);
            _resolveBtn = SpawnPrimitive<MFButton>(bottomRegion);
            _resolveBtn.gameObject.name = "btn_resolve";
            _resolveBtn.SetLabel("Resolve Turn");
            _resolveBtn.SetColor(new Color(0.8f, 0.3f, 0.1f), Color.white);
            _resolveBtn.OnClick = () =>
            {
                screen.ResolveTurn();
                Refresh();
            };

            OnRefresh();
        }

        protected override void OnRefresh()
        {
            if (_screen == null) return;

            // Update gem board
            _gemBoard.SetElements(_screen.GetBoardElements(), 5, 6);

            // Update HP bars
            if (_screen.State != null)
            {
                int enemyHp = 0;
                int maxEnemyHp = 0;
                if (_screen.State.Enemies != null)
                {
                    foreach (var e in _screen.State.Enemies)
                    {
                        if (e.IsAlive) enemyHp += e.Hp;
                        maxEnemyHp += e.MaxHp;
                    }
                }
                if (maxEnemyHp <= 0) maxEnemyHp = 1;
                _enemyHpBar.SetProgress(enemyHp, maxEnemyHp);

                _teamHpBar.SetProgress(_screen.State.TeamHp, _screen.State.MaxHp);

                _waveLabel.text = $"Wave {_screen.State.CurrentWaveIndex + 1} - HP: {enemyHp}";
            }
        }
    }
}
