using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TowerOfSaviors;
using MobileForge.UIComponents.Primitives;

namespace MobileForge.UIComponents.Pages
{
    /// <summary>
    /// Monster box/inventory page. Scrollable grid of owned monsters with detail view.
    /// </summary>
    public class InventoryPage : PageBase<MonsterBoxScreen>
    {
        private MFScrollGrid _monsterGrid;
        private Text _detailLabel;

        protected override void OnBind(MonsterBoxScreen screen)
        {
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Header
            var header = CreateRegion("Header", MFAnchor.Top,
                new MFPadding(0, 0, 0, 0));
            header.sizeDelta = new Vector2(0, 60);
            var hbox = CreateHBox("HeaderContent", header, spacing: 8f);

            var backBtn = SpawnPrimitive<MFButton>(hbox);
            backBtn.gameObject.name = "btn_back";
            backBtn.SetLabel("Back");
            var backLayout = backBtn.gameObject.GetComponent<LayoutElement>();
            if (backLayout != null) { backLayout.preferredWidth = 120; backLayout.flexibleWidth = 0; }
            backBtn.OnClick = () => screen.GoBack();

            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(hbox, false);
            titleGO.AddComponent<RectTransform>();
            titleGO.AddComponent<LayoutElement>().flexibleWidth = 1;
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = $"Monster Box ({screen.Monsters.Count})";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 30;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Monster grid
            var gridArea = CreateRegion("GridArea", MFAnchor.Fill,
                new MFPadding(10, 70, 10, 100));
            _monsterGrid = SpawnPrimitive<MFScrollGrid>(gridArea);
            _monsterGrid.Setup(null, BindMonsterCell, columns: 5, cellSize: 95f);
            _monsterGrid.SetItems(screen.Monsters.Cast<object>().ToList());
            _monsterGrid.OnItemSelected = (data, index) =>
            {
                screen.SelectMonster(index);
                UpdateDetail();
            };

            // Detail panel at bottom
            var detailArea = CreateRegion("DetailArea", MFAnchor.Bottom,
                new MFPadding(10, 0, 10, 10));
            detailArea.sizeDelta = new Vector2(0, 90);
            var detailBg = detailArea.gameObject.AddComponent<Image>();
            detailBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var detailGO = new GameObject("DetailLabel");
            detailGO.transform.SetParent(detailArea, false);
            var dRect = detailGO.AddComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = new Vector2(10, 5);
            dRect.offsetMax = new Vector2(-10, -5);
            _detailLabel = detailGO.AddComponent<Text>();
            _detailLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _detailLabel.fontSize = 18;
            _detailLabel.color = Color.white;
            _detailLabel.alignment = TextAnchor.MiddleLeft;
            _detailLabel.text = "Tap a monster to see details";
        }

        private void BindMonsterCell(GameObject cell, object data, int index)
        {
            var entry = data as MonsterBoxEntry;
            if (entry == null) return;

            var label = cell.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"{entry.DisplayName}\nLv.{entry.Instance.Level}";
                label.fontSize = 11;
            }

            // Tint by element
            var img = cell.GetComponent<Image>();
            if (img != null && entry.Def != null)
            {
                int elem = entry.Def.Element;
                Color elemColor = elem switch
                {
                    1 => new Color(0.15f, 0.3f, 0.5f),
                    2 => new Color(0.5f, 0.15f, 0.1f),
                    3 => new Color(0.1f, 0.4f, 0.15f),
                    4 => new Color(0.5f, 0.45f, 0.15f),
                    5 => new Color(0.3f, 0.1f, 0.4f),
                    _ => new Color(0.25f, 0.25f, 0.3f)
                };
                img.color = elemColor;
            }
        }

        private void UpdateDetail()
        {
            var sel = _screen.SelectedMonster;
            if (sel == null)
            {
                _detailLabel.text = "Tap a monster to see details";
                return;
            }

            string stats = "";
            if (sel.Stats != null)
                stats = $" | HP:{sel.Stats.Hp} ATK:{sel.Stats.Atk} REC:{sel.Stats.Rec}";

            _detailLabel.text = $"{sel.DisplayName} (Lv.{sel.Instance.Level}) [{sel.ElementName}]{stats}";
        }
    }
}
