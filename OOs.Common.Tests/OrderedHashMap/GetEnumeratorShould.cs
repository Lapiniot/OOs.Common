using OOs.Collections.Generic;

namespace OOs.Common.Tests.OrderedHashMap;

[TestClass]
public class GetEnumeratorShould
{
    [TestMethod]
    public void ReturnItemsInAddedOrder()
    {
        var map = new OrderedHashMap<int, string>([new(4, "Value 4"), new(1, "Value 1"), new(0, "Value 0")]);

        map.AddOrUpdate(3, "Value 3");
        map.AddOrUpdate(2, "Value 2");
        map.AddOrUpdate(4, "Value 4");

        using var enumerator = map.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(new(4, "Value 4"), enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(new(1, "Value 1"), enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(new(0, "Value 0"), enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(new(3, "Value 3"), enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(new(2, "Value 2"), enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void ReturnNoItemsForEmptyMap()
    {
        var map = new OrderedHashMap<int, string>();

        using var enumerator = map.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }
}