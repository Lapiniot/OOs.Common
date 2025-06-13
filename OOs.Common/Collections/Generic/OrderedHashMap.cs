using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OOs.Collections.Generic;

#nullable enable

public sealed class OrderedHashMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private readonly Dictionary<TKey, Node> map;
    private Node? head;
    private Node? tail;

    public OrderedHashMap(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this()
    {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (var (key, value) in collection)
        {
            AddOrUpdateInternal(key, value);
        }
    }

    public OrderedHashMap(int capacity) => map = new(capacity);

    public OrderedHashMap() => map = [];

    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (map.TryGetValue(key, out var node))
        {
            value = node.Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public void AddOrUpdate(TKey key, TValue value) => AddOrUpdateInternal(key, value);

    public bool Remove(TKey key, out TValue? value)
    {
        if (map.Remove(key, out var node))
        {
            node.Next?.Prev = node.Prev;
            node.Prev?.Next = node.Next;
            if (head == node)
                head = node.Next;
            if (tail == node)
                tail = node.Prev;
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    public bool Update(TKey key, TValue value)
    {
        ref var node = ref CollectionsMarshal.GetValueRefOrNullRef(map, key);
        if (!Unsafe.IsNullRef(ref node))
        {
            node.Value = value;
            return true;
        }

        return false;
    }

    private void AddOrUpdateInternal(TKey key, TValue value)
    {
        ref var node = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out var exists);

        if (!exists)
        {
            node = new Node(key, value, tail, null);
            head ??= node;
            tail?.Next = node;
            tail = node;
        }
        else
        {
            node!.Value = value;
        }
    }

    public void TrimExcess() => map.TrimExcess();

    private sealed class Node(TKey key, TValue value, Node? prev, Node? next)
    {
        public TKey Key { get; } = key;
        public TValue Value { get; set; } = value;
        public Node? Prev { get; set; } = prev;
        public Node? Next { get; set; } = next;
    }

    #region Implementation of IEnumerable

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private const int Init = 0;
        private const int InProgress = 1;
        private const int Done = 2;
        private readonly OrderedHashMap<TKey, TValue> map;
        private Node? node;
        private int state;

        internal Enumerator(OrderedHashMap<TKey, TValue> map) => this.map = map;

        public readonly KeyValuePair<TKey, TValue> Current => new(node!.Key, node.Value);

        readonly object? IEnumerator.Current => node!.Value;

        public readonly void Dispose() { }

        public bool MoveNext()
        {
            switch (state)
            {
                case Init:
                    node = map.head;
                    if (node is not null)
                    {
                        state = InProgress;
                        return true;
                    }

                    state = Done;
                    return false;

                case InProgress:
                    node = node!.Next;
                    if (node is not null)
                        return true;
                    state = Done;
                    return false;

                default:
                    return false;
            }
        }

        public void Reset()
        {
            node = null;
            state = Init;
        }
    }

    #endregion
}