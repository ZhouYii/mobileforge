using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Team selection page. Shows 5 team slots at top and a monster grid for selection.
    /// </summary>
    public class TeamSelectPage : PageBase<TeamSelectScreen>
    {
        private MFScrollGrid _monsterGrid;
        private readonly List<MFButton> _slotButtons = new();
        private MFButton _enterBtn;
        private MobileForge.Domain.GameData _gameData;

        public void SetGameData(MobileForge.Domain.GameData gameData) => _gameData = gameData;

        protected override void OnBind(TeamSelectScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Header
            var header = CreateRegion("Header", MFAnchor.Top,
                new MFPadding(0, 0, 0, 0));
            header.sizeDelta = new Vector2(0, 50);
            var headerHBox = CreateHBox("HeaderContent", header, spacing: 8f);

            var backBtn = SpawnPrimitive<MFButton>(headerHBox);
            backBtn.gameObject.name = "btn_back";
            backBtn.SetLabel("Back");
            var backLayout = backBtn.gameObject.GetComponent<LayoutElement>();
            if (backLayout != null) { backLayout.preferredWidth = 120; backLayout.flexibleWidth = 0; }
            backBtn.OnClick = () => screen.OnBack();

            var headerLabel = new GameObject("HeaderLabel");
            headerLabel.transform.SetParent(headerHBox, false);
            headerLabel.AddComponent<RectTransform>();
            headerLabel.AddComponent<LayoutElement>().flexibleWidth = 1;
            var hlText = headerLabel.AddComponent<Text>();
            hlText.text = "Select Team";
            hlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hlText.fontSize = 36;
            hlText.color = Color.white;
            hlText.alignment = TextAnchor.MiddleCenter;

            // Team slots
            var slotArea = CreateRegion("Slots", MFAnchor.Top,
                new MFPadding(10, 60, 10, 0));
            slotArea.sizeDelta = new Vector2(0, 70);
            var slotHBox = CreateHBox("SlotRow", slotArea, spacing: 8f);

            for (int i = 0; i < TeamSelectScreen.MaxTeamSize; i++)
            {
                int slotIndex = i;
                var slotBtn = SpawnPrimitive<MFButton>(slotHBox);
                slotBtn.gameObject.name = $"btn_slot_{i}";
                var slotData = screen.GetSlotData(i);
                string label = slotData != null && slotData.ContainsKey("name")
                    ? (string)slotData["name"] : $"Slot {i + 1}";
                slotBtn.SetLabel(label);
                var sl = slotBtn.gameObject.GetComponent<LayoutElement>();
                if (sl != null) { sl.preferredWidth = 90; sl.flexibleWidth = 0; }
                _slotButtons.Add(slotBtn);
            }

            // Monster grid
            var gridArea = CreateRegion("GridArea", MFAnchor.Fill,
                new MFPadding(10, 140, 10, 80));
            _monsterGrid = SpawnPrimitive<MFScrollGrid>(gridArea);
            _monsterGrid.Setup(null, BindMonsterCell, columns: 5, cellSize: 90f);

            // Build monster list from game data
            var monsters = new List<object>();
            if (_gameData != null)
            {
                var allDefs = _gameData.GetAllDefinitions("monsters");
                int shown = 0;
                foreach (var def in allDefs)
                {
                    if (shown >= 20) break;
                    monsters.Add(new Dictionary<string, object>
                    {
                        { "id", def.Id },
                        { "name", def.GetString("name", $"Mon#{def.Id}") }
                    });
                    shown++;
                }
            }
            _monsterGrid.SetItems(monsters);

            // Enter Dungeon button at bottom
            var bottomBar = CreateRegion("BottomBar", MFAnchor.Bottom,
                new MFPadding(40, 0, 40, 10));
            bottomBar.sizeDelta = new Vector2(0, 70);
            _enterBtn = SpawnPrimitive<MFButton>(bottomBar);
            _enterBtn.gameObject.name = "btn_enter_dungeon";
            _enterBtn.SetLabel("Enter Dungeon");
            _enterBtn.SetEnabled(screen.CanStart);
            _enterBtn.SetColor(
                screen.CanStart ? new Color(0.2f, 0.7f, 0.3f) : Color.gray,
                Color.white);
            _enterBtn.OnClick = () =>
            {
                if (_screen.CanStart) _screen.OnStartBattle();
            };
        }

        private void BindMonsterCell(GameObject cell, object data, int index)
        {
            var dict = data as Dictionary<string, object>;
            if (dict == null) return;

            int monsterId = (int)dict["id"];
            string name = (string)dict["name"];

            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = name;
                label.fontSize = 12;
            }

            var btn = cell.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _screen.SelectMonster(monsterId);
                    RefreshSlots();
                });
            }
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < TeamSelectScreen.MaxTeamSize; i++)
            {
                var slotData = _screen.GetSlotData(i);
                string label = slotData != null && slotData.ContainsKey("name")
                    ? (string)slotData["name"] : $"Slot {i + 1}";
                if (i < _slotButtons.Count)
                    _slotButtons[i].SetLabel(label);
            }

            _enterBtn.SetEnabled(_screen.CanStart);
            _enterBtn.SetColor(
                _screen.CanStart ? new Color(0.2f, 0.7f, 0.3f) : Color.gray,
                Color.white);
        }

        protected override void OnRefresh()
        {
            RefreshSlots();
        }
    }
}
