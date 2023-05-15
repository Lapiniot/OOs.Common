namespace System.Common.Tests.OrderedHashMap;

[TestClass]
public class AddOrUpdateShould
{
    internal static OrderedHashMap<string, string> CreateSampleHashQueue() =>
        new(new KeyValuePair<string, string>[] { new("key1", "value 1"), new("key2", "value 2"), new("key3", "value 3") });

    [TestMethod]
    public void ThrowArgumentNullExceptionGivenKeyNull() =>
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            var map = new OrderedHashMap<string, string>();
            map.AddOrUpdate(null, "");
        });

    [TestMethod]
    public void AppendItemAndRetainOriginalOrderGivenNotExistingKey()
    {
        var map = new OrderedHashMap<string, string>(new KeyValuePair<string, string>[] { new("key2", "value2"), new("key3", "value3") });

        map.AddOrUpdate("key1", "add-value1");
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value2", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("value3", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("add-value1", enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void ReplaceItemAndRetainOriginalOrderGivenExistingKey()
    {
        var map = new OrderedHashMap<string, string>(new KeyValuePair<string, string>[] { new("key2", "value2"), new("key3", "value3") });

        map.AddOrUpdate("key2", "update-value2");
        map.AddOrUpdate("key3", "update-value3");
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("update-value2", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("update-value3", enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }
}