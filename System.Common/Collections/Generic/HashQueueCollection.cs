using static System.Threading.LockRecursionPolicy;

namespace System.Collections.Generic;

public sealed class HashQueueCollection<TKey, TValue> : IEnumerable<TValue>, IDisposable
{
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly Dictionary<TKey, Node> map;
    private Node head;
    private Node tail;

    public HashQueueCollection(params (TKey key, TValue value)[] items) : this()
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach(var (key, value) in items)
        {
            AddNodeInternal(key, value);
        }
    }

    public HashQueueCollection()
    {
        map = new Dictionary<TKey, Node>();
        lockSlim = new ReaderWriterLockSlim(NoRecursion);
    }

    internal Node Head
    {
        get => head;
        set => head = value;
    }

    internal Node Tail
    {
        get => tail;
        set => tail = value;
    }

    internal Dictionary<TKey, Node> Map => map;

    #region Implementation of IDisposable

    public void Dispose()
    {
        lockSlim.Dispose();
    }

    #endregion

    public IEnumerator<TValue> GetEnumerator()
    {
        using(lockSlim.WithReadLock())
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

    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
    {
        ArgumentNullException.ThrowIfNull(updateValueFactory);

        using(lockSlim.WithWriteLock())
        {
            return map.TryGetValue(key, out var node)
                ? node.Value = updateValueFactory(key, node.Value)
                : AddNodeInternal(key, addValue).Value;
        }
    }

    public TValue AddOrUpdate(TKey key, TValue addValue, TValue updateValue)
    {
        using(lockSlim.WithWriteLock())
        {
            return map.TryGetValue(key, out var node)
                ? node.Value = updateValue
                : AddNodeInternal(key, addValue).Value;
        }
    }

    public TValue GetOrAdd(TKey key, TValue value)
    {
        using(lockSlim.WithUpgradeableReadLock())
        {
            if(map.TryGetValue(key, out var node))
            {
                return node.Value;
            }

            using(lockSlim.WithWriteLock())
            {
                return AddNodeInternal(key, value).Value;
            }
        }
    }

    public bool TryAdd(TKey key, TValue value)
    {
        using(lockSlim.WithUpgradeableReadLock())
        {
            if(map.ContainsKey(key)) return false;

            using(lockSlim.WithWriteLock())
            {
                AddNodeInternal(key, value);
            }
        }

        return true;
    }

    public bool TryGet(TKey key, out TValue value)
    {
        using(lockSlim.WithReadLock())
        {
            if(!map.TryGetValue(key, out var node))
            {
                value = default;
                return false;
            }

            value = node.Value;
            return true;
        }
    }

    public bool Dequeue(out TValue value)
    {
        if(head == null)
        {
            value = default;
            return false;
        }

        using(lockSlim.WithWriteLock())
        {
            var h = head;
            head = h.Next;
            head.Prev = null;
            value = h.Value;
            return map.Remove(h.Key);
        }
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        using(lockSlim.WithUpgradeableReadLock())
        {
            if(map.TryGetValue(key, out var node))
            {
                using(lockSlim.WithWriteLock())
                {
                    if(map.Remove(key))
                    {
                        if(node.Next != null) node.Next.Prev = node.Prev;

                        if(node.Prev != null) node.Prev.Next = node.Next;

                        if(head == node) head = node.Next;

                        if(tail == node) tail = node.Prev;

                        value = node.Value;

                        return true;
                    }
                }
            }

            value = default;

            return false;
        }
    }

    private Node AddNodeInternal(TKey key, TValue value)
    {
        var node = new Node(key, value, tail, null);

        head ??= node;

        if(tail != null) tail.Next = node;

        tail = node;

        map.Add(key, node);

        return node;
    }

    internal sealed class Node
    {
        public Node(TKey key, TValue value, Node prev, Node next)
        {
            Key = key;
            Value = value;
            Prev = prev;
            Next = next;
        }

        public TKey Key { get; }
        public TValue Value { get; set; }
        public Node Prev { get; set; }
        public Node Next { get; set; }
    }
}