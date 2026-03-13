using System.Collections.Immutable;

namespace OOs.CommandLine.Generators.Tests;

[TestClass]
public class SourceGenerationContextTests
{
    [TestMethod]
    public void SourceGenerationContext_Equals_SameInstance_ReturnsTrue()
    {
        var context = CreateSampleSourceGenerationContext();
        IEquatable<SourceGenerationContext> iequatable = context;

        Assert.IsTrue(context.Equals(context));
        Assert.IsTrue(context.Equals((object)context));
        Assert.IsTrue(iequatable.Equals(context));
    }

    [TestMethod]
    public void SourceGenerationContext_Equals_EqualInstances_ReturnsTrue()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext();
        var iequatable1 = (IEquatable<SourceGenerationContext>)context1;
        var iequatable2 = (IEquatable<SourceGenerationContext>)context2;

        Assert.IsTrue(context1.Equals(context2));
        Assert.IsTrue(context2.Equals(context1));
        Assert.IsTrue(context1.Equals((object)context2));
        Assert.IsTrue(context2.Equals((object)context1));
        Assert.IsTrue(iequatable1.Equals(context2));
        Assert.IsTrue(iequatable2.Equals(context1));
    }

    [TestMethod]
    public void SourceGenerationContext_Equals_DifferentContext_ReturnsFalse()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext(new TypeGenerationContext("Different", null, TypeKind.Class, Accessibility.Public, default));
        var iequatable1 = (IEquatable<SourceGenerationContext>)context1;
        var iequatable2 = (IEquatable<SourceGenerationContext>)context2;

        Assert.IsFalse(context1.Equals(context2));
        Assert.IsFalse(context2.Equals(context1));
        Assert.IsFalse(context1.Equals((object)context2));
        Assert.IsFalse(context2.Equals((object)context1));
        Assert.IsFalse(iequatable1.Equals(context2));
        Assert.IsFalse(iequatable2.Equals(context1));
    }

    [TestMethod]
    public void SourceGenerationContext_Equals_DifferentOptions_ReturnsFalse()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext(options: ImmutableArray<OptionGenerationContext>.Empty);
        var iequatable1 = (IEquatable<SourceGenerationContext>)context1;
        var iequatable2 = (IEquatable<SourceGenerationContext>)context2;

        Assert.IsFalse(context1.Equals(context2));
        Assert.IsFalse(context2.Equals(context1));
        Assert.IsFalse(context1.Equals((object)context2));
        Assert.IsFalse(context2.Equals((object)context1));
        Assert.IsFalse(iequatable1.Equals(context2));
        Assert.IsFalse(iequatable2.Equals(context1));
    }

    [TestMethod]
    public void SourceGenerationContext_Equals_Null_ReturnsFalse()
    {
        var context = CreateSampleSourceGenerationContext();
        Assert.IsFalse(context.Equals(null));
    }

    [TestMethod]
    public void SourceGenerationContext_GetHashCode_EqualInstances_SameHashCode()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext();
        Assert.AreEqual(context1.GetHashCode(), context2.GetHashCode());
    }

    [TestMethod]
    public void SourceGenerationContext_GetHashCode_DifferentContext_DifferentHashCode()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext(context: new TypeGenerationContext("Different",
            null, TypeKind.Class, Accessibility.Public, default));
        Assert.AreNotEqual(context1.GetHashCode(), context2.GetHashCode());
    }

    [TestMethod]
    public void SourceGenerationContext_GetHashCode_DifferentOptions_DifferentHashCode()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext(options: [new OptionGenerationContext("option2", "alias2", 'b',
            new OptionTypeContext(WellKnownType.Enum, ["Value1", "Value2"]), "description", "hint")]);
        Assert.AreNotEqual(context1.GetHashCode(), context2.GetHashCode());
    }

    [TestMethod]
    public void SourceGenerationContext_EqualityOperator_EqualInstances_ReturnsTrue()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext();
        Assert.IsTrue(context1 == context2);
    }

    [TestMethod]
    public void SourceGenerationContext_EqualityOperator_DifferentContext_ReturnsFalse()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext(context: new TypeGenerationContext("Different", null, TypeKind.Class, Accessibility.Public, default));
        Assert.IsFalse(context1 == context2);
    }

    [TestMethod]
    public void SourceGenerationContext_EqualityOperator_DifferentOptions_ReturnsFalse()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext(options: ImmutableArray<OptionGenerationContext>.Empty);
        Assert.IsFalse(context1 == context2);
    }

    [TestMethod]
    public void SourceGenerationContext_InequalityOperator_DifferentInstances_ReturnsTrue()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext(context: new TypeGenerationContext("Different", null, TypeKind.Class, Accessibility.Public, default));
        Assert.IsTrue(context1 != context2);
    }

    [TestMethod]
    public void SourceGenerationContext_InequalityOperator_EqualInstances_ReturnsFalse()
    {
        var context1 = CreateSampleSourceGenerationContext();
        var context2 = CreateSampleSourceGenerationContext();
        Assert.IsFalse(context1 != context2);
    }

    private static SourceGenerationContext CreateSampleSourceGenerationContext(
        TypeGenerationContext? context = null,
        ImmutableArray<OptionGenerationContext>? options = null)
    {
        context ??= new TypeGenerationContext("TestType", "TestNamespace", TypeKind.Class, Accessibility.Public,
            new TypeGenerationOptions(true, true, UnknownOptionBehavior.Allow));
        options ??= [new OptionGenerationContext("option1", "alias1", 'a',
            new OptionTypeContext(WellKnownType.Enum, ["Value1", "Value2"]),
            "description", "hint")];
        return new SourceGenerationContext(context.Value, options.Value);
    }
}