using OOs.Collections.Generic;

namespace OOs.Common.Tests.OrderedHashMap;

[TestClass]
public class TryGetValueShould
{
    [TestMethod]
    public void ReturnTrue_AndValue_GivenExistingKey()
    {
        var map = new OrderedHashMap<int, string>([new(1, "value 1")]);

        var actual = map.TryGetValue(1, out var value);

        Assert.IsTrue(actual);
        Assert.AreEqual("value 1", value);
    }

    [TestMethod]
    public void ReturnFalse_AndDefaultValue_GivenExistingKey()
    {
        var map = new OrderedHashMap<int, string>([new(1, "value 1")]);

        var actual = map.TryGetValue(2, out var value);

        Assert.IsFalse(actual);
        Assert.AreEqual(default, value);
    }
}
