using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class HashQueue_GetEnumerator_Should
    {
        [TestMethod]
        public void ReturnOrderedSequence()
        {
            var hashQueue = new HashQueueCollection<int, string>((0, "Value 0"), (1, "Value 1"), (2, "Value 2"));

            hashQueue.TryAdd(4, "Value 4");
            hashQueue.TryAdd(3, "Value 3");

            using(var enumerator = hashQueue.GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual("Value 0", enumerator.Current);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual("Value 1", enumerator.Current);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual("Value 2", enumerator.Current);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual("Value 4", enumerator.Current);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual("Value 3", enumerator.Current);

                Assert.IsFalse(enumerator.MoveNext());
            }
        }
    }
}