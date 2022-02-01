using System.Collections.Generic;

namespace System.Common.Tests;

[TestClass]
public class HashQueueConstructorShould
{
    [TestMethod]
    public void InitializeMapHeadNullTailNull()
    {
        using var hq = new HashQueueCollection<int, string>();
        Assert.IsNull(hq.Head);
        Assert.IsNull(hq.Tail);
        Assert.IsNotNull(hq.Map);
        Assert.AreEqual(0, hq.Map.Count);
    }

    [TestMethod]
    public void AddNewItemsMaintainingOrder()
    {
        const string key1 = "key1";
        const string key2 = "key2";
        const string key3 = "key3";
        const string value1 = "value 1";
        const string value2 = "value 2";
        const string value3 = "value 3";

        using var hashQueue = new HashQueueCollection<string, string>(new[] { (key1, value1), (key2, value2), (key3, value3) });

        Assert.AreEqual(3, hashQueue.Map.Count);

        var node1 = hashQueue.Map[key1];
        var node2 = hashQueue.Map[key2];
        var node3 = hashQueue.Map[key3];

        Assert.AreEqual(key1, node1.Key);
        Assert.AreEqual(key2, node2.Key);
        Assert.AreEqual(key3, node3.Key);

        Assert.AreEqual(value1, node1.Value);
        Assert.AreEqual(value2, node2.Value);
        Assert.AreEqual(value3, node3.Value);

        Assert.IsNull(node1.Prev);
        Assert.AreSame(node2, node1.Next);

        Assert.AreSame(node1, node2.Prev);
        Assert.AreSame(node3, node2.Next);

        Assert.AreSame(node2, node3.Prev);
        Assert.IsNull(node3.Next);

        Assert.AreSame(node1, hashQueue.Head);
        Assert.AreSame(node3, hashQueue.Tail);
    }

    [TestMethod]
    public void ThrowArgumentExceptionGivenKeyDuplicates()
    {
        _ = Assert.ThrowsException<ArgumentException>(() => new HashQueueCollection<string, string>(
            new[] { ("key1", "value 1"), ("key2", "value 2"), ("key2", ""), ("key3", "value 3") }));
    }
}