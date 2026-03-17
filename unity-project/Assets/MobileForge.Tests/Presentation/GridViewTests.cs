using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Presentation;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class GridViewTests
    {
        private GridView<TestCell> _grid;
        private List<string> _bindLog;

        [SetUp]
        public void SetUp()
        {
            _grid = new GridView<TestCell>();
            _bindLog = new List<string>();
        }

        private TestCell CreateCell()
        {
            return new TestCell();
        }

        private void BindCell(TestCell cell, object data, int index)
        {
            _bindLog.Add($"bind:{index}");
        }

        [Test]
        public void SetData_CalculatesColumnLayout()
        {
            _grid.Setup(CreateCell, BindCell, columns: 3, cellSize: 50f);

            var data = new List<object>();
            for (int i = 0; i < 9; i++) data.Add(i);

            _grid.SetData(data);

            Assert.AreEqual(3, _grid.Columns, "Should have 3 columns");
            Assert.AreEqual(3, _grid.RowCount, "9 items / 3 columns = 3 rows");
            Assert.AreEqual(150f, _grid.TotalHeight, "3 rows * 50 cellSize = 150");
        }

        [Test]
        public void VisibleRange_AccountsForColumns()
        {
            _grid.Setup(CreateCell, BindCell, columns: 3, cellSize: 50f);

            var data = new List<object>();
            for (int i = 0; i < 30; i++) data.Add(i);

            _grid.SetData(data);

            var (first, last) = _grid.GetVisibleRange(scrollOffset: 0f, viewportHeight: 100f);

            Assert.AreEqual(0, first, "First visible should be 0");
            Assert.AreEqual(8, last, "With 3 columns, 2 visible rows = indices 0-5, plus one extra row = 0-8");

            (first, last) = _grid.GetVisibleRange(scrollOffset: 100f, viewportHeight: 100f);

            Assert.AreEqual(6, first, "At scroll 100 (row 2), first index should be 6");
        }

        [Test]
        public void CellRecycling_ReusesOnScroll()
        {
            int createCount = 0;
            _grid.Setup(
                () => { createCount++; return new TestCell(); },
                BindCell,
                columns: 3,
                cellSize: 50f
            );

            var data = new List<object>();
            for (int i = 0; i < 30; i++) data.Add(i);

            _grid.SetData(data);
            _grid.UpdateVisible(0f, 100f);
            int initialCreateCount = createCount;

            _grid.UpdateVisible(200f, 100f);

            Assert.LessOrEqual(createCount, initialCreateCount + 3,
                "Should reuse pooled cells rather than create new ones");
        }

        [Test]
        public void EmptyData_ClearsGrid()
        {
            _grid.Setup(CreateCell, BindCell, columns: 3, cellSize: 50f);

            var data = new List<object> { "a", "b", "c" };
            _grid.SetData(data);
            _grid.UpdateVisible(0f, 200f);

            Assert.Greater(_grid.VisibleCount, 0);

            _grid.SetData(new List<object>());

            Assert.AreEqual(0, _grid.DataCount, "DataCount should be 0");
            Assert.AreEqual(0, _grid.VisibleCount, "VisibleCount should be 0");
        }

        [Test]
        public void SingleColumn_BehavesLikeList()
        {
            _grid.Setup(CreateCell, BindCell, columns: 1, cellSize: 50f);

            var data = new List<object>();
            for (int i = 0; i < 10; i++) data.Add(i);

            _grid.SetData(data);
            _grid.UpdateVisible(0f, 150f);

            Assert.AreEqual(10, _grid.DataCount, "Should have 10 items");
            Assert.AreEqual(10, _grid.RowCount, "With 1 column, 10 items = 10 rows");

            var (first, last) = _grid.GetVisibleRange(0f, 150f);
            Assert.AreEqual(4, last - first + 1, "Should have ~4 visible items (with buffer)");
        }

        [Test]
        public void GetGridPosition_ReturnsCorrectRowCol()
        {
            _grid.Setup(CreateCell, BindCell, columns: 4, cellSize: 50f);

            var (row, col) = _grid.GetGridPosition(0);
            Assert.AreEqual(0, row, "Index 0 = row 0");
            Assert.AreEqual(0, col, "Index 0 = col 0");

            (row, col) = _grid.GetGridPosition(3);
            Assert.AreEqual(0, row, "Index 3 = row 0");
            Assert.AreEqual(3, col, "Index 3 = col 3");

            (row, col) = _grid.GetGridPosition(4);
            Assert.AreEqual(1, row, "Index 4 = row 1");
            Assert.AreEqual(0, col, "Index 4 = col 0");

            (row, col) = _grid.GetGridPosition(9);
            Assert.AreEqual(2, row, "Index 9 = row 2");
            Assert.AreEqual(1, col, "Index 9 = col 1");
        }

        [Test]
        public void Clear_ResetsEverything()
        {
            _grid.Setup(CreateCell, BindCell, columns: 3, cellSize: 50f);

            var data = new List<object> { "a", "b", "c" };
            _grid.SetData(data);
            _grid.UpdateVisible(0f, 200f);

            _grid.Clear();

            Assert.AreEqual(0, _grid.DataCount);
            Assert.AreEqual(0, _grid.VisibleCount);
            Assert.AreEqual(0, _grid.RowCount);
        }

        private class TestCell
        {
            public object Data { get; set; }
        }
    }
}
