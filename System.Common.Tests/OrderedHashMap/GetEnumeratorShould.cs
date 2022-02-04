namespace System.Common.Tests.OrderedHashMap;

[TestClass]
public class GetEnumeratorShould
{
    [TestMethod]
    public void ReturnItemsInAddedOrder()
    {
        var hashQueue = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(4, "Value 4"), new(1, "Value 1"), new(0, "Value 0") });

        hashQueue.AddOrUpdate(3, "Value 3", "Value 3");
        hashQueue.AddOrUpdate(2, "Value 2", "Value 2");
        hashQueue.AddOrUpdate(4, "Value 4", "Value 4");

        using var enumerator = hashQueue.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("Value 4", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("Value 1", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("Value 0", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("Value 3", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("Value 2", enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }
}