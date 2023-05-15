﻿using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

public sealed class OrderedHashMap<TKey, TValue> : IEnumerable<TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, Node> map;
    private readonly object syncLock = new();
    private Node head;
    private Node tail;

    public OrderedHashMap(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this()
    {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (var (key, value) in collection)
        {
            AddNode(key, value);
        }
    }

    public OrderedHashMap(int capacity) => map = new(capacity);

    public OrderedHashMap() => map = new();

    public void AddOrUpdate(TKey key, TValue value)
    {
        lock (syncLock)
        {
            if (map.TryGetValue(key, out var node))
                node.Value = value;
            else
                AddNode(key, value);
        }
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        lock (syncLock)
        {
            if (map.Remove(key, out var node))
            {
                if (node.Next != null) node.Next.Prev = node.Prev;
                if (node.Prev != null) node.Prev.Next = node.Next;
                if (head == node) head = node.Next;
                if (tail == node) tail = node.Prev;
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }
    }

    public void TrimExcess()
    {
        lock (syncLock)
        {
            map.TrimExcess();
        }
    }

    private void AddNode(TKey key, TValue value)
    {
        var node = new Node { Value = value, Prev = tail };
        head ??= node;
        if (tail != null) tail.Next = node;
        tail = node;
        map.Add(key, node);
    }

    private sealed class Node
    {
        public TValue Value { get; set; }
        public Node Prev { get; set; }
        public Node Next { get; set; }
    }

    #region Implementation of IEnumerable

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<TValue>
    {
        private const int Init = 0;
        private const int InProgress = 1;
        private const int Done = 2;
        private readonly OrderedHashMap<TKey, TValue> map;
        private Node node;
        private int state;
        private bool locked;

        internal Enumerator(OrderedHashMap<TKey, TValue> map) => this.map = map;

        public TValue Current => node.Value;

        object IEnumerator.Current => node.Value;

        public void Dispose()
        {
            node = null;
            state = Done;
            ReleaseLock();
        }

        public bool MoveNext()
        {
            switch (state)
            {
                case Init:
                    Monitor.Enter(map.syncLock, ref locked);
                    node = map.head;

                    if (node is not null)
                    {
                        state = InProgress;
                        return true;
                    }

                    state = Done;
                    ReleaseLock();
                    return false;
                case InProgress:
                    node = node.Next;

                    if (node is not null) return true;

                    state = Done;
                    ReleaseLock();
                    return false;

                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseLock()
        {
            if (!locked) return;
            locked = false;
            Monitor.Exit(map.syncLock);
        }

        public void Reset()
        {
            node = null;
            state = Init;
            ReleaseLock();
        }
    }

    #endregion
}