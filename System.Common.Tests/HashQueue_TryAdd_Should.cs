using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class HashQueue_TryAdd_Should
    {
        [TestMethod]
        public void ReturnTrue_InitializeReferences_GivenFirstKey()
        {
            using var hq = new HashQueueCollection<int, string>();

            var actual = hq.TryAdd(1, "value 1");

            Assert.IsTrue(actual);

            var node = hq.Map[1];

            Assert.IsNull(node.Next);
            Assert.IsNull(node.Prev);
            Assert.AreEqual(node, hq.Head);
            Assert.AreEqual(node, hq.Tail);
        }

        [TestMethod]
        public void ReturnTrue_AddNode_GivenNonExistingKey()
        {
            const string key1 = "1";
            const string value1 = "value 1";
            const string key2 = "2";
            const string value2 = "value 2";

            using var hq = new HashQueueCollection<string, string>();

            var actual = hq.TryAdd(key1, value1);

            Assert.IsTrue(actual);
            Assert.AreEqual(1, hq.Map.Count);
            Assert.AreSame(key1, hq.Map[key1].Key);
            Assert.AreSame(value1, hq.Map[key1].Value);

            actual = hq.TryAdd(key2, value2);

            Assert.IsTrue(actual);
            Assert.AreEqual(2, hq.Map.Count);
            Assert.AreSame(key2, hq.Map[key2].Key);
            Assert.AreSame(value2, hq.Map[key2].Value);
        }

        [TestMethod]
        public void ReturnFalse_DoNotChangeNode_GivenExistingKey()
        {
            using var hq = new HashQueueCollection<int, string>();
            const string expectedValue = "old value";
            hq.TryAdd(1, expectedValue);

            var actual = hq.TryAdd(1, "new value");

            Assert.IsFalse(actual);
            Assert.AreEqual(1, hq.Map.Count);
            Assert.AreEqual(expectedValue, hq.Map[1].Value);
        }

        [TestMethod]
        public void ReturnTrue_UpdateHeadAndTail_GivenNotFirstKeys()
        {
            using var hq = new HashQueueCollection<int, string>();
            hq.TryAdd(1, "value 1");
            var node1 = hq.Map[1];

            var actual = hq.TryAdd(2, "value 2");

            Assert.IsTrue(actual);

            var node2 = hq.Map[2];

            Assert.AreEqual(node1, hq.Head);
            Assert.AreEqual(node2, hq.Tail);

            actual = hq.TryAdd(3, "value 3");

            Assert.IsTrue(actual);

            var node3 = hq.Map[3];

            Assert.AreEqual(node1, hq.Head);
            Assert.AreEqual(node3, hq.Tail);
        }

        [TestMethod]
        public void ReturnFalse_DoNotUpdateHeadAndTail_GivenExistingKeys()
        {
            using var hq = new HashQueueCollection<int, string>();
            hq.TryAdd(1, "value 1");
            var node1 = hq.Map[1];

            var actual = hq.TryAdd(1, "value 2");

            Assert.IsFalse(actual);
            Assert.AreEqual(node1, hq.Head);
            Assert.AreEqual(node1, hq.Tail);
        }

        [TestMethod]
        public void ReturnTrue_UpdateNodeReferences_GivenNotFirstKeys()
        {
            using var hq = new HashQueueCollection<int, string>();
            hq.TryAdd(1, "value 1");
            var node1 = hq.Map[1];

            var actual = hq.TryAdd(2, "value 2");
            var node2 = hq.Map[2];

            Assert.IsTrue(actual);
            // Null <== node1 <==> node2 ==> Null
            Assert.IsNull(node1.Prev);
            Assert.AreEqual(node2, node1.Next);
            Assert.AreEqual(node1, node2.Prev);
            Assert.IsNull(node2.Next);

            actual = hq.TryAdd(3, "value 3");
            var node3 = hq.Map[3];

            Assert.IsTrue(actual);
            // Null <== node1 <==> node2 <==> node3 ==> Null
            Assert.IsNull(node1.Prev);
            Assert.AreEqual(node2, node1.Next);
            Assert.AreEqual(node1, node2.Prev);
            Assert.AreEqual(node3, node2.Next);
            Assert.AreEqual(node2, node3.Prev);
            Assert.IsNull(node3.Next);
        }

        [TestMethod]
        public void ReturnFalse_DoNotUpdateNodeReferences_GivenExistingKeys()
        {
            using var hq = new HashQueueCollection<int, string>();

            hq.TryAdd(1, "value 1");
            var node1 = hq.Map[1];
            hq.TryAdd(2, "value 2");
            var node2 = hq.Map[2];

            var actual = hq.TryAdd(2, "value 3");
            Assert.IsFalse(actual);
            // Null <== node1 <==> node2 ==> Null
            Assert.IsNull(node1.Prev);
            Assert.AreEqual(node2, node1.Next);
            Assert.AreEqual(node1, node2.Prev);
            Assert.IsNull(node2.Next);
        }

        [TestMethod]
        public void ReturnTrue_DoNotAffectExistingSlots_GivenNewPairs()
        {
            using var hq = new HashQueueCollection<string, string>();

            const string value1 = "value 1";
            const string key1 = "1";
            hq.TryAdd(key1, value1);
            var node1 = hq.Map[key1];

            const string value2 = "value 2";
            const string key2 = "2";
            hq.TryAdd(key2, value2);
            var node2 = hq.Map[key2];

            var actual = hq.TryAdd("3", "value 3");
            Assert.IsTrue(actual);
            Assert.AreSame(node1, hq.Map[key1]);
            Assert.AreSame(value1, hq.Map[key1].Value);
            Assert.AreSame(key1, hq.Map[key1].Key);
            Assert.AreSame(node2, hq.Map[key2]);
            Assert.AreSame(value2, hq.Map[key2].Value);
            Assert.AreSame(key2, hq.Map[key2].Key);
        }

        [TestMethod]
        public void ReturnFalse_DoNotAffectExistingSlots_GivenExistingKeys()
        {
            using var hq = new HashQueueCollection<string, string>();

            const string value1 = "value 1";
            const string key1 = "1";
            hq.TryAdd(key1, value1);
            var node1 = hq.Map[key1];

            const string value2 = "value 2";
            const string key2 = "2";
            hq.TryAdd(key2, value2);
            var node2 = hq.Map[key2];

            var actual = hq.TryAdd(key2, "value 3");
            Assert.IsFalse(actual);
            Assert.AreSame(node1, hq.Map[key1]);
            Assert.AreSame(value1, hq.Map[key1].Value);
            Assert.AreSame(key1, hq.Map[key1].Key);
            Assert.AreSame(node2, hq.Map[key2]);
            Assert.AreSame(value2, hq.Map[key2].Value);
            Assert.AreSame(key2, hq.Map[key2].Key);
        }

        [TestMethod]
        public void Throw_ArgumentNullException_GivenKey_Null()
        {
            using var hq = new HashQueueCollection<string, string>();
            Assert.ThrowsException<ArgumentNullException>(() => hq.TryAdd(null, "test value"));
        }
    }
}