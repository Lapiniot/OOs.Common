﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class HashQueue_GetOrAdd_Should
    {
        internal static HashQueueCollection<string, string> CreateSampleHashQueue()
        {
            return new HashQueueCollection<string, string>(("key1", "value 1"), ("key2", "value 2"), ("key3", "value 3"));
        }

        [TestMethod]
        public void Throw_ArgumentNullException_GivenKey_Null()
        {
            var hq = new HashQueueCollection<string, string>();

            Assert.ThrowsException<ArgumentNullException>(() => hq.GetOrAdd(null, "value 1"));
        }

        [TestMethod]
        public void ReturnExistingValue_GivenExistingKey()
        {
            var hashQueue = CreateSampleHashQueue();

            var actual = hashQueue.GetOrAdd("key2", "value 2-2");

            Assert.AreEqual("value 2", actual);
        }

        [TestMethod]
        public void AddNewNodeToMap_GivenNewKey()
        {
            var hashQueue = CreateSampleHashQueue();

            const string key1 = "key1";
            const string key2 = "key2";
            const string key3 = "key3";
            const string key4 = "key4";
            const string value1 = "value 1";
            const string value2 = "value 2";
            const string value3 = "value 3";
            const string value4 = "value 4";

            var actual = hashQueue.GetOrAdd(key4, value4);

            Assert.AreEqual(value4, actual);

            Assert.AreEqual(4, hashQueue.Map.Count);

            var node1 = hashQueue.Map[key1];
            var node2 = hashQueue.Map[key2];
            var node3 = hashQueue.Map[key3];
            var node4 = hashQueue.Map[key4];

            Assert.AreEqual(key1, node1.Key);
            Assert.AreEqual(key2, node2.Key);
            Assert.AreEqual(key3, node3.Key);
            Assert.AreEqual(key4, node4.Key);

            Assert.AreEqual(value1, node1.Value);
            Assert.AreEqual(value2, node2.Value);
            Assert.AreEqual(value3, node3.Value);
            Assert.AreEqual(value4, node4.Value);
        }

        [TestMethod]
        public void UpdateReferences_GivenNewKey()
        {
            var hashQueue = CreateSampleHashQueue();

            const string key1 = "key1";
            const string key2 = "key2";
            const string key3 = "key3";
            const string key4 = "key4";
            const string value4 = "value 4";

            var actual = hashQueue.GetOrAdd(key4, value4);

            Assert.AreEqual(value4, actual);

            var node1 = hashQueue.Map[key1];
            var node2 = hashQueue.Map[key2];
            var node3 = hashQueue.Map[key3];
            var node4 = hashQueue.Map[key4];

            Assert.IsNull(node1.Prev);
            Assert.AreSame(node2, node1.Next);

            Assert.AreSame(node1, node2.Prev);
            Assert.AreSame(node3, node2.Next);

            Assert.AreSame(node2, node3.Prev);
            Assert.AreSame(node4, node3.Next);

            Assert.AreSame(node3, node4.Prev);
            Assert.IsNull(node4.Next);

            Assert.AreSame(node1, hashQueue.Head);
            Assert.AreSame(node4, hashQueue.Tail);
        }

        [TestMethod]
        public void NotAffectInternalData_GivenExistingKey()
        {
            var hashQueue = CreateSampleHashQueue();

            const string key1 = "key1";
            const string key2 = "key2";
            const string key3 = "key3";
            const string value1 = "value 1";
            const string value2 = "value 2";
            const string value3 = "value 3";

            var actual = hashQueue.GetOrAdd(key2, "value 2-2");

            Assert.AreEqual(value2, actual);

            Assert.AreEqual(3, hashQueue.Map.Count);

            var node1 = hashQueue.Map[key1];
            var node2 = hashQueue.Map[key2];
            var node3 = hashQueue.Map[key3];

            Assert.AreEqual(key1, node1.Key);
            Assert.AreEqual(key2, node2.Key);
            Assert.AreEqual(key3, node3.Key);

            Assert.AreEqual(value1, node1.Value);
            Assert.AreEqual(value2, node2.Value);
            Assert.AreEqual(value3, node3.Value);

            Assert.IsNull(node1.Prev);
            Assert.AreSame(node2, node1.Next);

            Assert.AreSame(node1, node2.Prev);
            Assert.AreSame(node3, node2.Next);

            Assert.AreSame(node2, node3.Prev);
            Assert.IsNull(node3.Next);

            Assert.AreSame(node1, hashQueue.Head);
            Assert.AreSame(node3, hashQueue.Tail);
        }

        [TestMethod]
        public void ReturnNewValue_GivenNewKey()
        {
            var hashQueue = CreateSampleHashQueue();

            var expected = "value 4";

            var actual = hashQueue.GetOrAdd("key4", expected);

            Assert.AreSame(expected, actual);
        }
    }
}