using System.Memory;

namespace System.Common.Tests.ObjectPool;

[TestClass]
public class RentShould
{
    [TestMethod]
    [DoNotParallelize]
    public void ReturnNewInstancesOnDemandIfPoolIsEmpty()
    {
        // Arrange
        var pool = new ObjectPool<MockObject>(2);
        MockObject.ResetCounter();

        // Act
        var actual = pool.Rent();

        // Assert
        Assert.IsNotNull(actual);
        Assert.AreEqual(1, MockObject.ConstructorInvocations);
    }

    [TestMethod]
    [DoNotParallelize]
    public void ReturnExistingInstanceIfPoolIsNotEmpty()
    {
        // Arrange
        var pool = new ObjectPool<MockObject>(2);
        var instance = new MockObject();
        MockObject.ResetCounter();
        pool.Return(instance);

        // Act
        var actual = pool.Rent();

        // Assert
        Assert.AreEqual(instance, actual);
        Assert.AreEqual(0, MockObject.ConstructorInvocations);
    }

    [TestMethod]
    [DoNotParallelize]
    public void ReturnExistingInstancesAndCreateExtraOnDemandInvokedInParallel()
    {
        // Arrange
        const int MaxCapacity = 8;
        var pool = new ObjectPool<MockObject>(MaxCapacity);
        var instances = new MockObject[MaxCapacity];
        for (var i = 0; i < instances.Length; i++) pool.Return(instances[i] = new());
        MockObject.ResetCounter();

        // Act
        var actual = new List<MockObject>();
        Parallel.For(0, 3 * MaxCapacity,
            static () => new List<MockObject>(),
            (_, _, acc) =>
            {
                acc.Add(pool.Rent());
                return acc;
            },
            acc =>
            {
                lock (actual)
                {
                    actual.AddRange(acc);
                }
            });

        // Assert
        Assert.AreEqual(MaxCapacity, actual.Intersect(instances).Count());
        Assert.AreEqual(2 * MaxCapacity, MockObject.ConstructorInvocations);
    }

    private sealed class MockObject
    {
        private static int constructorInvocations;

        public MockObject() => Interlocked.Increment(ref constructorInvocations);

        internal static int ConstructorInvocations => Volatile.Read(ref constructorInvocations);

        public static void ResetCounter() => Interlocked.Exchange(ref constructorInvocations, 0);
    }
}