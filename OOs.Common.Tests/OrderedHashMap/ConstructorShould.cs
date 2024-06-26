using OOs.Collections.Generic;

namespace OOs.Common.Tests.OrderedHashMap;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentNullExceptionGivenNullCollection() =>
        Assert.ThrowsException<ArgumentNullException>(() => new OrderedHashMap<string, string>(null));

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenNegativeCapacity() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new OrderedHashMap<string, string>(-1));

    [TestMethod]
    public void AddItemsWithGivenCollectionOrder()
    {
        var map = new OrderedHashMap<int, string>([new(10, "10"), new(1, "1"), new(4, "4"), new(15, "15"), new(0, "0")]);
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("10", enumerator.Current.Value);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("1", enumerator.Current.Value);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("4", enumerator.Current.Value);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("15", enumerator.Current.Value);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("0", enumerator.Current.Value);

        Assert.IsFalse(enumerator.MoveNext());
    }
}