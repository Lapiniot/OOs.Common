using System.Collections.Immutable;

namespace OOs.CommandLine.Generators.Tests;

[TestClass]
public class OptionTypeContextTests
{
    [TestMethod]
    public void OptionTypeContext_Equals_SameInstance_ReturnsTrue()
    {
        var context = CreateSampleOptionTypeContext();
        var iequatable = (IEquatable<OptionTypeContext>)context;

        Assert.IsTrue(context.Equals(context));
        Assert.IsTrue(context.Equals((object)context));
        Assert.IsTrue(iequatable.Equals(context));
    }

    [TestMethod]
    public void OptionTypeContext_Equals_EqualInstances_ReturnsTrue()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext();
        var iequatable1 = (IEquatable<OptionTypeContext>)context1;
        var iequatable2 = (IEquatable<OptionTypeContext>)context2;

        Assert.IsTrue(context1.Equals(context2));
        Assert.IsTrue(context2.Equals(context1));
        Assert.IsTrue(context1.Equals((object)context2));
        Assert.IsTrue(context2.Equals((object)context1));
        Assert.IsTrue(iequatable1.Equals(context2));
        Assert.IsTrue(iequatable2.Equals(context1));
    }

    [TestMethod]
    public void OptionTypeContext_Equals_DifferentKnownType_ReturnsFalse()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext(knownType: WellKnownType.Boolean);
        var iequatable1 = (IEquatable<OptionTypeContext>)context1;
        var iequatable2 = (IEquatable<OptionTypeContext>)context2;

        Assert.IsFalse(context1.Equals(context2));
        Assert.IsFalse(context2.Equals(context1));
        Assert.IsFalse(context1.Equals((object)context2));
        Assert.IsFalse(context2.Equals((object)context1));
        Assert.IsFalse(iequatable1.Equals(context2));
        Assert.IsFalse(iequatable2.Equals(context1));
    }

    [TestMethod]
    public void OptionTypeContext_Equals_DifferentEnumValues_ReturnsFalse()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext(enumValues: ["Value1", "Value2", "Value3"]);
        var iequatable1 = (IEquatable<OptionTypeContext>)context1;
        var iequatable2 = (IEquatable<OptionTypeContext>)context2;

        Assert.IsFalse(context1.Equals(context2));
        Assert.IsFalse(context2.Equals(context1));
        Assert.IsFalse(context1.Equals((object)context2));
        Assert.IsFalse(context2.Equals((object)context1));
        Assert.IsFalse(iequatable1.Equals(context2));
        Assert.IsFalse(iequatable2.Equals(context1));
    }

    [TestMethod]
    public void OptionTypeContext_Equals_Null_ReturnsFalse()
    {
        var context = CreateSampleOptionTypeContext();
        Assert.IsFalse(context.Equals(null));
    }

    [TestMethod]
    public void OptionTypeContext_GetHashCode_EqualInstances_SameHashCode()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext();
        Assert.AreEqual(context1.GetHashCode(), context2.GetHashCode());
    }

    [TestMethod]
    public void OptionTypeContext_GetHashCode_DifferentKnownType_DifferentHashCode()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext(knownType: WellKnownType.Boolean);
        Assert.AreNotEqual(context1.GetHashCode(), context2.GetHashCode());
    }

    [TestMethod]
    public void OptionTypeContext_GetHashCode_DifferentEnumValues_DifferentHashCode()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext(enumValues: ["Value1", "Value2", "Value3"]);
        Assert.AreNotEqual(context1.GetHashCode(), context2.GetHashCode());
    }

    [TestMethod]
    public void OptionTypeContext_EqualityOperator_EqualInstances_ReturnsTrue()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext();
        Assert.IsTrue(context1 == context2);
    }

    [TestMethod]
    public void OptionTypeContext_EqualityOperator_DifferentKnownType_ReturnsFalse()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext(knownType: WellKnownType.Boolean);
        Assert.IsFalse(context1 == context2);
    }

    [TestMethod]
    public void OptionTypeContext_EqualityOperator_DifferentEnumValues_ReturnsFalse()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext(enumValues: ["Value1", "Value2", "Value3"]);
        Assert.IsFalse(context1 == context2);
    }

    [TestMethod]
    public void OptionTypeContext_InequalityOperator_DifferentInstances_ReturnsTrue()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext(knownType: WellKnownType.Boolean);
        Assert.IsTrue(context1 != context2);
    }

    [TestMethod]
    public void OptionTypeContext_InequalityOperator_EqualInstances_ReturnsFalse()
    {
        var context1 = CreateSampleOptionTypeContext();
        var context2 = CreateSampleOptionTypeContext();
        Assert.IsFalse(context1 != context2);
    }

    private static OptionTypeContext CreateSampleOptionTypeContext(
        WellKnownType knownType = WellKnownType.Enum,
        ImmutableArray<string>? enumValues = null)
    {
        enumValues ??= ["Value1", "Value2"];
        return new OptionTypeContext(knownType, enumValues.Value);
    }
}