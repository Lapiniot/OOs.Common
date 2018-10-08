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
            var hashQueue = new HashQueue<int, string>((0, "Value 0"), (1, "Value 1"), (2, "Value 2"));

            hashQueue.TryAdd(3, "Value 3");
            hashQueue.TryAdd(4, "Value 4");

            using(var enumerator = hashQueue.GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(0, enumerator.Current.Key);
                Assert.AreEqual("Value 0", enumerator.Current.Value);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(1, enumerator.Current.Key);
                Assert.AreEqual("Value 1", enumerator.Current.Value);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(2, enumerator.Current.Key);
                Assert.AreEqual("Value 2", enumerator.Current.Value);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(3, enumerator.Current.Key);
                Assert.AreEqual("Value 3", enumerator.Current.Value);

                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(4, enumerator.Current.Key);
                Assert.AreEqual("Value 4", enumerator.Current.Value);

                Assert.IsFalse(enumerator.MoveNext());
            }
        }
    }
}