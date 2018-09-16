using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class HashQueue_Constructor_Should
    {
        [TestMethod]
        public void InitializeMap_HeadNull_TailNull()
        {
            var hq = new HashQueue<int, string>();
            Assert.IsNull(hq.Head);
            Assert.IsNull(hq.Tail);
            Assert.IsNotNull(hq.Map);
            Assert.AreEqual(0, hq.Map.Count);
        }
    }
}