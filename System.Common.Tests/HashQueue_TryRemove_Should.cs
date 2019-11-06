using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class HashQueue_TryRemove_Should
    {
        [TestMethod]
        public void ReturnTrue_AndValue_GivenExistingKey()
        {
            var hq = new HashQueueCollection<int, string>(
                (1, "value 1"),
                (2, "value 2"),
                (3, "value 3"));

            var actual = hq.TryRemove(1, out var value);
            Assert.IsTrue(actual);
            Assert.AreEqual("value 1", value);
        }

        [TestMethod]
        public void ReturnFalse_AndDefaultValue_GivenNonExistingKey()
        {
            var stringHashQueue = new HashQueueCollection<int, string>(
                (1, "value 1"),
                (2, "value 2"),
                (3, "value 3"));

            var actual = stringHashQueue.TryRemove(5, out var strValue);
            Assert.IsFalse(actual);
            Assert.AreEqual(default, strValue);

            var intHashQueue = new HashQueueCollection<string, int>(
                ("1", 1),
                ("2", 2),
                ("3", 3));

            actual = intHashQueue.TryRemove("5", out var intValue);
            Assert.IsFalse(actual);
            Assert.AreEqual(default, intValue);
        }

        [TestMethod]
        public void RemoveFromMap_GivenExistingKey()
        {
            var hq = new HashQueueCollection<int, string>((1, "value 1"), (2, "value 2"), (3, "value 3"));

            var actual = hq.TryRemove(2, out _);
            Assert.IsTrue(actual);
            Assert.AreEqual(2, hq.Map.Count);
            Assert.IsFalse(hq.Map.TryGetValue(2, out _));
        }

        [TestMethod]
        public void UpdateHead_RemovingFirstItemByKey()
        {
            var hq = new HashQueueCollection<int, string>((1, "value 1"), (2, "value 2"), (3, "value 3"));

            Assert.AreSame(hq.Map[1], hq.Head);

            var actual = hq.TryRemove(1, out _);
            Assert.IsTrue(actual);
            Assert.AreSame(hq.Map[2], hq.Head);
        }

        [TestMethod]
        public void NotUpdateHead_RemovingNonFirstItemByKey()
        {
            var hq = new HashQueueCollection<int, string>((1, "value 1"), (2, "value 2"), (3, "value 3"));

            Assert.AreSame(hq.Map[1], hq.Head);

            var actual = hq.TryRemove(2, out _);
            Assert.IsTrue(actual);
            Assert.AreSame(hq.Map[1], hq.Head);
        }

        [TestMethod]
        public void UpdateTail_RemovingLastItemByKey()
        {
            var hq = new HashQueueCollection<int, string>((1, "value 1"), (2, "value 2"), (3, "value 3"));

            Assert.AreSame(hq.Map[3], hq.Tail);

            var actual = hq.TryRemove(3, out _);
            Assert.IsTrue(actual);
            Assert.AreSame(hq.Map[2], hq.Tail);
        }

        [TestMethod]
        public void NotUpdateTail_RemovingNonLastItemByKey()
        {
            var hq = new HashQueueCollection<int, string>((1, "value 1"), (2, "value 2"), (3, "value 3"));

            Assert.AreSame(hq.Map[3], hq.Tail);

            var actual = hq.TryRemove(2, out _);
            Assert.IsTrue(actual);
            Assert.AreSame(hq.Map[3], hq.Tail);
        }

        [TestMethod]
        public void UpdateReferences_RemovingItemByKey()
        {
            var hq = new HashQueueCollection<int, string>((1, "value 1"), (2, "value 2"), (3, "value 3"), (4, "value 4"), (5, "value 5"));

            var node1 = hq.Map[1];
            var node2 = hq.Map[2];
            var node4 = hq.Map[4];
            var node5 = hq.Map[5];

            var actual = hq.TryRemove(3, out _);
            Assert.IsTrue(actual);

            Assert.IsNull(node1.Prev);
            Assert.AreSame(node2, node1.Next);

            Assert.AreSame(node1, node2.Prev);
            Assert.AreSame(node4, node2.Next);

            Assert.AreSame(node2, node4.Prev);
            Assert.AreSame(node5, node4.Next);

            Assert.AreSame(node4, node5.Prev);
            Assert.IsNull(node5.Next);

            actual = hq.TryRemove(1, out _);
            Assert.IsTrue(actual);

            Assert.IsNull(node2.Prev);
            Assert.AreSame(node4, node2.Next);

            Assert.AreSame(node2, node4.Prev);
            Assert.AreSame(node5, node4.Next);

            Assert.AreSame(node4, node5.Prev);
            Assert.IsNull(node5.Next);

            actual = hq.TryRemove(5, out _);
            Assert.IsTrue(actual);

            Assert.IsNull(node2.Prev);
            Assert.AreSame(node4, node2.Next);

            Assert.AreSame(node2, node4.Prev);
            Assert.IsNull(node4.Next);
        }

        [TestMethod]
        public void NotUpdateReferences_RemovingNonExistingItemByKey()
        {
            var hq = new HashQueueCollection<int, string>((1, "value 1"), (2, "value 2"), (3, "value 3"));

            var node1 = hq.Map[1];
            var node2 = hq.Map[2];
            var node3 = hq.Map[3];

            var actual = hq.TryRemove(5, out _);
            Assert.IsFalse(actual);

            Assert.IsNull(node1.Prev);
            Assert.AreSame(node2, node1.Next);

            Assert.AreSame(node1, node2.Prev);
            Assert.AreSame(node3, node2.Next);

            Assert.AreSame(node2, node3.Prev);
            Assert.IsNull(node3.Next);
        }

        [TestMethod]
        public void Throw_ArgumentNullException_GivenKey_Null()
        {
            var hq = new HashQueueCollection<string, string>();
            Assert.ThrowsException<ArgumentNullException>(() => hq.TryRemove(null, out _));
        }
    }
}