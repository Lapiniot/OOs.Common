﻿using System.Memory;

namespace System.Common.Tests;

[TestClass]
public class ObjectPoolRentShould
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
        const int maxCapacity = 8;
        var pool = new ObjectPool<MockObject>(maxCapacity);
        var instances = new MockObject[maxCapacity];
        for(var i = 0; i < instances.Length; i++) { pool.Return(instances[i] = new MockObject()); }
        MockObject.ResetCounter();

        // Act
        var actual = new List<MockObject>();
        Parallel.For(0, 3 * maxCapacity,
            static () => new List<MockObject>(),
            (_, _, acc) => { acc.Add(pool.Rent()); return acc; },
            acc => { lock(actual) actual.AddRange(acc); });

        // Assert
        Assert.AreEqual(maxCapacity, actual.Intersect(instances).Count());
        Assert.AreEqual(2 * maxCapacity, MockObject.ConstructorInvocations);
    }

    private class MockObject
    {
        private static int constructorInvocations;

        public MockObject()
        {
            Interlocked.Increment(ref constructorInvocations);
        }

        public static void ResetCounter()
        {
            Interlocked.Exchange(ref constructorInvocations, 0);
        }

        internal static int ConstructorInvocations => Volatile.Read(ref constructorInvocations);
    }
}
