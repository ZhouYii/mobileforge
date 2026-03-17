using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Presentation;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class VirtualListTests
    {
        private VirtualList<TestItem> _list;
        private List<string> _bindLog;

        [SetUp]
        public void SetUp()
        {
            _list = new VirtualList<TestItem>();
            _bindLog = new List<string>();
        }

        private TestItem CreateItem()
        {
            return new TestItem();
        }

        private void BindItem(TestItem item, object data, int index)
        {
            _bindLog.Add($"bind:{index}");
        }

        [Test]
        public void SetData_PopulatesVisibleItems()
        {
            _list.Setup(CreateItem, BindItem, 50f);

            var data = new List<object> { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
            _list.SetData(data);

            _list.UpdateVisible(0f, 200f);

            Assert.AreEqual(10, _list.DataCount, "DataCount should be 10");
            Assert.AreEqual(5, _list.VisibleCount, "Should have 5 visible items (200/50)");
            Assert.AreEqual(5, _bindLog.Count, "Should have 5 bind calls");
        }

        [Test]
        public void SetData_RecyclesPoolItems()
        {
            int createCount = 0;
            _list.Setup(
                () => { createCount++; return new TestItem(); },
                BindItem,
                50f
            );

            var data = new List<object>();
            for (int i = 0; i < 20; i++) data.Add($"item{i}");

            _list.SetData(data);
            _list.UpdateVisible(0f, 100f);
            int firstCreateCount = createCount;

            _list.UpdateVisible(400f, 100f);

            Assert.LessOrEqual(createCount, firstCreateCount + 2,
                "Should reuse pooled items rather than create new ones");
        }

        [Test]
        public void VisibleRange_CalculatesCorrectly()
        {
            _list.Setup(CreateItem, BindItem, 50f);

            var data = new List<object>();
            for (int i = 0; i < 100; i++) data.Add(i);

            _list.SetData(data);

            var (first, last) = _list.GetVisibleRange(0f, 200f);
            Assert.AreEqual(0, first, "First visible should be 0");
            Assert.AreEqual(4, last, "Last visible should be 4 (5 items * 50 = 250 covers 0-200)");

            (first, last) = _list.GetVisibleRange(250f, 200f);
            Assert.AreEqual(5, first, "First visible at scroll 250 should be 5");
            Assert.AreEqual(9, last, "Last visible should be 9");

            (first, last) = _list.GetVisibleRange(4950f, 100f);
            Assert.AreEqual(99, last, "Should not exceed data count - 1");
        }

        [Test]
        public void EmptyData_ShowsNothing()
        {
            _list.Setup(CreateItem, BindItem, 50f);

            _list.SetData(new List<object>());
            _list.UpdateVisible(0f, 200f);

            Assert.AreEqual(0, _list.VisibleCount, "No items should be visible with empty data");
        }

        [Test]
        public void UpdateData_RefreshesExistingItems()
        {
            _bindLog.Clear();
            _list.Setup(CreateItem, BindItem, 50f);

            var data = new List<object> { "a", "b", "c" };
            _list.SetData(data);
            _list.UpdateVisible(0f, 200f);

            int firstBindCount = _bindLog.Count;

            _list.SetData(new List<object> { "x", "y", "z" });
            _list.UpdateVisible(0f, 200f);

            Assert.AreEqual(firstBindCount * 2, _bindLog.Count, "Should rebind on new data");
        }

        [Test]
        public void ItemPool_ReusesInstances()
        {
            var createdItems = new List<TestItem>();
            int createCount = 0;

            _list.Setup(
                () =>
                {
                    var item = new TestItem();
                    createdItems.Add(item);
                    createCount++;
                    return item;
                },
                BindItem,
                50f
            );

            var data = new List<object>();
            for (int i = 0; i < 20; i++) data.Add(i);

            _list.SetData(data);
            _list.UpdateVisible(0f, 100f);
            int initialCreateCount = createCount;

            _list.UpdateVisible(500f, 100f);
            _list.UpdateVisible(0f, 100f);

            Assert.AreEqual(initialCreateCount, createCount,
                "Pool should reuse items, not create new ones");
        }

        [Test]
        public void TotalHeight_CalculatesCorrectly()
        {
            _list.Setup(CreateItem, BindItem, 50f);

            var data = new List<object>();
            for (int i = 0; i < 10; i++) data.Add(i);

            _list.SetData(data);

            Assert.AreEqual(500f, _list.TotalHeight, "TotalHeight should be 10 * 50 = 500");
        }

        [Test]
        public void ReleaseAll_ClearsVisibleItems()
        {
            _list.Setup(CreateItem, BindItem, 50f);

            var data = new List<object> { "a", "b", "c", "d", "e" };
            _list.SetData(data);
            _list.UpdateVisible(0f, 200f);

            Assert.Greater(_list.VisibleCount, 0);

            _list.ReleaseAll();

            Assert.AreEqual(0, _list.VisibleCount, "VisibleCount should be 0 after ReleaseAll");
        }

        [Test]
        public void Clear_ResetsEverything()
        {
            _list.Setup(CreateItem, BindItem, 50f);

            var data = new List<object> { "a", "b", "c" };
            _list.SetData(data);
            _list.UpdateVisible(0f, 200f);

            _list.Clear();

            Assert.AreEqual(0, _list.DataCount, "DataCount should be 0 after Clear");
            Assert.AreEqual(0, _list.VisibleCount, "VisibleCount should be 0 after Clear");
        }

        [Test]
        public void GetVisibleItem_ReturnsCorrectItem()
        {
            var boundIndices = new Dictionary<int, TestItem>();
            _list.Setup(CreateItem, (item, data, index) => { boundIndices[index] = item; }, 50f);

            var data = new List<object> { "a", "b", "c", "d", "e" };
            _list.SetData(data);
            _list.UpdateVisible(0f, 200f);

            var item0 = _list.GetVisibleItem(0);
            Assert.IsNotNull(item0, "Item 0 should be visible");

            var item10 = _list.GetVisibleItem(10);
            Assert.IsNull(item10, "Item 10 should not be visible");
        }

        private class TestItem
        {
            public string Data { get; set; }
        }
    }
}
