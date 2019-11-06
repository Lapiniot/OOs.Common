using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class HashQueue_TryGet_Should
    {
        [TestMethod]
        public void ReturnTrue_AndValue_GivenExistingKey()
        {
            var hq = new HashQueueCollection<int, string>(
                (1, "value 1"),
                (2, "value 2"),
                (3, "value 3"));

            var actual = hq.TryGet(1, out var value);
            Assert.IsTrue(actual);
            Assert.AreEqual("value 1", value);

            actual = hq.TryGet(2, out value);
            Assert.IsTrue(actual);
            Assert.AreEqual("value 2", value);

            actual = hq.TryGet(3, out value);
            Assert.IsTrue(actual);
            Assert.AreEqual("value 3", value);
        }

        [TestMethod]
        public void ReturnFalse_AndDefaultValue_GivenNonExistingKey()
        {
            var stringHashQueue = new HashQueueCollection<int, string>(
                (1, "value 1"),
                (2, "value 2"),
                (3, "value 3"));

            var actual = stringHashQueue.TryGet(5, out var strValue);
            Assert.IsFalse(actual);
            Assert.AreEqual(default, strValue);

            var intHashQueue = new HashQueueCollection<string, int>(
                ("1", 1),
                ("2", 2),
                ("3", 3));

            actual = intHashQueue.TryGet("5", out var intValue);
            Assert.IsFalse(actual);
            Assert.AreEqual(default, intValue);
        }

        [TestMethod]
        public void Throw_ArgumentNullException_GivenKey_Null()
        {
            var hq = new HashQueueCollection<string, string>();
            Assert.ThrowsException<ArgumentNullException>(() => hq.TryGet(null, out _));
        }
    }
}