using System.Runtime.CompilerServices;

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

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
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

    public struct Enumerator : IEnumerator<TValue>
    {
        private const int Init = 0;
        private const int InProgress = 1;
        private const int Done = 2;
        private readonly OrderedHashMap<TKey, TValue> map;
        private Node node;

        //0 - "init" state
        //1 - "in progress"
        //2 - "done" state
        private int state;
        private bool locked;

        internal Enumerator(OrderedHashMap<TKey, TValue> map)
        {
            node = null;
            state = Init;
            locked = false;
            this.map = map;
        }

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
            switch(state)
            {
                case Init:
                    Monitor.Enter(map.syncLock, ref locked);
                    node = map.head;
                    if(node is not null)
                    {
                        state = InProgress;
                        return true;
                    }
                    else
                    {
                        state = Done;
                        ReleaseLock();
                        return false;
                    }
                case InProgress:
                    node = node.Next;
                    if(node is null)
                    {
                        state = Done;
                        ReleaseLock();
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseLock()
        {
            if(locked)
            {
                locked = false;
                Monitor.Exit(map.syncLock);
            }
        }

        public void Reset()
        {
            node = null;
            state = Init;
            ReleaseLock();
        }
    }
}