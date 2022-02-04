namespace System.Common.Tests.OrderedHashMap;

[TestClass]
public class TryRemoveShould
{
    [TestMethod]
    public void ReturnTrueAndValueGivenExistingKey()
    {
        using var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1"), new(2, "value 2"), new(3, "value 3") });

        var actual = map.TryRemove(1, out var value);

        Assert.IsTrue(actual);
        Assert.AreEqual("value 1", value);
    }

    [TestMethod]
    public void ReturnFalseAndDefaultValueGivenNonExistingKey()
    {
        using var stringMap = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1"), new(2, "value 2"), new(3, "value 3") });

        var actual = stringMap.TryRemove(5, out var strValue);
        Assert.IsFalse(actual);
        Assert.AreEqual(default, strValue);

        using var intMap = new OrderedHashMap<string, int>(new KeyValuePair<string, int>[] { new("1", 1), new("2", 2), new("3", 3) });

        actual = intMap.TryRemove("5", out var intValue);
        Assert.IsFalse(actual);
        Assert.AreEqual(default, intValue);
    }

    [TestMethod]
    public void RemoveFromMapGivenExistingKey()
    {
        using var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1"), new(2, "value 2"), new(3, "value 3") });

        map.TryRemove(2, out _);

        Assert.IsFalse(map.Contains("value 2"));
    }

    [TestMethod]
    public void RemoveItemAndRetainOrderGivenFirstItemKey()
    {
        using var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1"), new(2, "value 2"), new(3, "value 3") });

        map.TryRemove(1, out _);
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 2", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 3", enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void RemoveItemAndRetainOrderGivenLastItemKey()
    {
        using var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1"), new(2, "value 2"), new(3, "value 3") });

        map.TryRemove(3, out _);
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 1", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 2", enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void RemoveItemAndRetainOrder()
    {
        using var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1"), new(2, "value 2"), new(3, "value 3"), new(4, "value 4") });

        map.TryRemove(2, out _);
        map.TryRemove(3, out _);
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 1", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 4", enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenKeyNull()
    {
        _ = Assert.ThrowsException<ArgumentNullException>(() =>
        {
            using var map = new OrderedHashMap<string, string>();
            return map.TryRemove(null, out _);
        });
    }
}