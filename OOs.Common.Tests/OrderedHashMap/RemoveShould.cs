using OOs.Collections.Generic;

namespace OOs.Common.Tests.OrderedHashMap;

[TestClass]
public class RemoveShould
{
    [TestMethod]
    public void ReturnTrueAndValueGivenExistingKey()
    {
        var map = new OrderedHashMap<int, string>([new(1, "value 1"), new(2, "value 2"), new(3, "value 3")]);

        var actual = map.Remove(1, out var value);

        Assert.IsTrue(actual);
        Assert.AreEqual("value 1", value);
    }

    [TestMethod]
    public void ReturnFalseAndDefaultValueGivenNonExistingKey()
    {
        var stringMap = new OrderedHashMap<int, string>([new(1, "value 1"), new(2, "value 2"), new(3, "value 3")]);

        var actual = stringMap.Remove(5, out var strValue);
        Assert.IsFalse(actual);
        Assert.AreEqual(default, strValue);

        var intMap = new OrderedHashMap<string, int>([new("1", 1), new("2", 2), new("3", 3)]);

        actual = intMap.Remove("5", out var intValue);
        Assert.IsFalse(actual);
        Assert.AreEqual(default, intValue);
    }

    [TestMethod]
    public void RemoveFromMapGivenExistingKey()
    {
        var map = new OrderedHashMap<int, string>([new(1, "value 1"), new(2, "value 2"), new(3, "value 3")]);

        map.Remove(2, out _);

        Assert.IsFalse(map.Any(p => p.Value == "value 2"));
    }

    [TestMethod]
    public void RemoveItemAndRetainOrderGivenFirstItemKey()
    {
        var map = new OrderedHashMap<int, string>([new(1, "value 1"), new(2, "value 2"), new(3, "value 3")]);

        map.Remove(1, out _);
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 2", enumerator.Current.Value);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 3", enumerator.Current.Value);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void RemoveItemAndRetainOrderGivenLastItemKey()
    {
        var map = new OrderedHashMap<int, string>([new(1, "value 1"), new(2, "value 2"), new(3, "value 3")]);

        map.Remove(3, out _);
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 1", enumerator.Current.Value);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 2", enumerator.Current.Value);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void RemoveItemAndRetainOrder()
    {
        var map = new OrderedHashMap<int, string>([new(1, "value 1"), new(2, "value 2"), new(3, "value 3"), new(4, "value 4")]);

        map.Remove(2, out _);
        map.Remove(3, out _);
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 1", enumerator.Current.Value);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value 4", enumerator.Current.Value);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenKeyNull() =>
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            var map = new OrderedHashMap<string, string>();
            return map.Remove(null, out _);
        });
}