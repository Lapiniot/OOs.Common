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
        foreach(var (key, value) in collection)
        {
            _ = AddNodeInternal(key, value);
        }
    }

    public OrderedHashMap(int capacity)
    {
        map = new Dictionary<TKey, Node>(capacity);
    }

    public OrderedHashMap()
    {
        map = new Dictionary<TKey, Node>();
    }

    // TODO: implement struct based enumerator (with Reset() for optional reuse)
    public IEnumerator<TValue> GetEnumerator()
    {
        lock(syncLock)
        {
            var node = head;
            while(node != null)
            {
                yield return node.Value;
                node = node.Next;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public TValue AddOrUpdate(TKey key, TValue addValue, TValue updateValue)
    {
        lock(syncLock)
        {
            return map.TryGetValue(key, out var node)
                ? node.Value = updateValue
                : AddNodeInternal(key, addValue).Value;
        }
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        lock(syncLock)
        {
            if(map.Remove(key, out var node))
            {
                if(node.Next != null) node.Next.Prev = node.Prev;

                if(node.Prev != null) node.Prev.Next = node.Next;

                if(head == node) head = node.Next;

                if(tail == node) tail = node.Prev;

                value = node.Value;

                return true;
            }

            value = default;

            return false;
        }
    }

    private Node AddNodeInternal(TKey key, TValue value)
    {
        var node = new Node(value, tail, null);
        head ??= node;
        if(tail != null) tail.Next = node;
        tail = node;
        map.Add(key, node);
        return node;
    }

    internal sealed class Node
    {
        public Node(TValue value, Node prev, Node next)
        {
            Value = value;
            Prev = prev;
            Next = next;
        }

        public TValue Value { get; set; }
        public Node Prev { get; set; }
        public Node Next { get; set; }
    }
}