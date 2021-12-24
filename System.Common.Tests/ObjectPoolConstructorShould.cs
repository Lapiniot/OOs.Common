﻿using System.Memory;

namespace System.Common.Tests;

[TestClass]
public class ObjectPoolConstructorShould
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-10)]
    public void ThrowArgumentOutOfRangeExceptionGivenZeroOrNegativeCapacitiy(int capacity)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ObjectPool<object>(capacity));
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    public void NotThrowButSetInitialCapacityGivenPositiveValue(int capacity)
    {
        // Act
        var pool = new ObjectPool<object>(capacity);

        // Assert: verify effective capacity equals to the value passed via constructor
        var instances = new object[2 * capacity];
        for(int i = 0; i < instances.Length; i++) { pool.Return(instances[i] = new object()); }
        var rented = new object[2 * capacity];
        for(int i = 0; i < rented.Length; i++) { rented[i] = pool.Rent(); }

        Assert.AreEqual(capacity, instances.Intersect(rented).Count());
    }
}