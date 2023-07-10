namespace System.Common.Tests.OrderedHashMap;

[TestClass]
public class UpdateShould
{
    [TestMethod]
    public void ReturnTrue_AndUpdateValue_GivenExistingKey()
    {
        var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1") });

        var actual = map.Update(1, "updated value");

        Assert.IsTrue(actual);
        Assert.IsTrue(map.TryGetValue(1, out var value));
        Assert.AreEqual("updated value", value);
    }

    [TestMethod]
    public void ReturnFalse_AndDoNotAddValue_GivenExistingKey()
    {
        var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(1, "value 1") });

        var actual = map.Update(2, "updated value");

        Assert.IsFalse(actual);
        Assert.IsFalse(map.TryGetValue(2, out _));
    }
}