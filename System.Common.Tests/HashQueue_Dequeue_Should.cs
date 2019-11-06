using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class HashQueue_Dequeue_Should
    {
        internal static HashQueueCollection<string, string> CreateSampleHashQueue()
        {
            return new HashQueueCollection<string, string>(("key1", "value 1"), ("key2", "value 2"), ("key3", "value 3"));
        }

        [TestMethod]
        public void ReturnFalseAndDefaultValue_GivenEmptyQueue()
        {
            var hq = new HashQueueCollection<string, string>();

            var actual = hq.Dequeue(out _);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ReturnTrueAndValue_GivenNotEmptyQueue()
        {
            var hq = CreateSampleHashQueue();

            var actual = hq.Dequeue(out var value);

            Assert.IsTrue(actual);
            Assert.AreEqual("value 1", value);
        }

        [TestMethod]
        public void RemoveFirstItemFromMap_GivenNonEmptyQueue()
        {
            const string key2 = "key2";
            const string key3 = "key3";
            const string value2 = "value 2";
            const string value3 = "value 3";

            var hashQueue = CreateSampleHashQueue();

            hashQueue.Dequeue(out _);

            Assert.AreEqual(2, hashQueue.Map.Count);

            var node2 = hashQueue.Map[key2];
            var node3 = hashQueue.Map[key3];

            Assert.AreEqual(key2, node2.Key);
            Assert.AreEqual(key3, node3.Key);

            Assert.AreEqual(value2, node2.Value);
            Assert.AreEqual(value3, node3.Value);
        }

        [TestMethod]
        public void UpdateReferences_GivenNotEmptyQueue()
        {
            var hashQueue = CreateSampleHashQueue();

            hashQueue.Dequeue(out _);

            var node2 = hashQueue.Map["key2"];
            var node3 = hashQueue.Map["key3"];

            Assert.IsNull(node2.Prev);
            Assert.AreSame(node3, node2.Next);

            Assert.AreSame(node2, node3.Prev);
            Assert.IsNull(node3.Next);

            Assert.AreSame(node2, hashQueue.Head);
            Assert.AreSame(node3, hashQueue.Tail);
        }
    }
}