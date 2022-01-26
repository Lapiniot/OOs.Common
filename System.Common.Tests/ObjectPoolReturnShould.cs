using System.Memory;

namespace System.Common.Tests;

[TestClass]
public class ObjectPoolReturnShould
{
    [TestMethod]
    public void ThrowArgumentExceptionGivenNullValue()
    {
        var pool = new ObjectPool<object>(2);
        Assert.ThrowsException<ArgumentNullException>(() => pool.Return(null));
    }

    [TestMethod]
    public void AddToStoreIfCapacityIsAboveZero()
    {
        // Arrange
        const int maxCapacity = 2;
        var instance1 = new object();
        var instance2 = new object();
        var pool = new ObjectPool<object>(maxCapacity);

        // Act
        pool.Return(instance1);
        pool.Return(instance2);

        // Assert
        var instances = new[] { pool.Rent(), pool.Rent() };
        Assert.IsTrue(instances.Contains(instance1));
        Assert.IsTrue(instances.Contains(instance2));
    }

    [TestMethod]
    public void DiscardValueIfCapacityIsAlreadyZero()
    {
        // Arrange
        const int maxCapacity = 2;
        var pool = new ObjectPool<object>(maxCapacity);
        var instance1 = new object();
        var instance2 = new object();
        var instance3 = new object();
        pool.Return(instance1);
        pool.Return(instance2);

        // Act
        pool.Return(instance3);

        // Assert
        var instances = new[] { pool.Rent(), pool.Rent(), pool.Rent() };
        Assert.IsFalse(instances.Contains(instance3));
    }

    [TestMethod]
    public void AddToStoreNoMoreThanCapacityAndDiscardExcessInvokedInParallel()
    {
        // Arrange
        const int maxCapacity = 3;
        var instances = new object[] { new(), new(), new(), new(), new() };
        var pool = new ObjectPool<object>(maxCapacity);

        // Act
        Parallel.ForEach(instances, instance => pool.Return(instance));

        // Assert
        var rented = new[] { pool.Rent(), pool.Rent(), pool.Rent(), pool.Rent(), pool.Rent() };
        Assert.AreEqual(maxCapacity, instances.Intersect(rented).Count());
    }
}