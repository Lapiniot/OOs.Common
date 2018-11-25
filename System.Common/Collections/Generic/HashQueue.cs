﻿using System.Threading;
using static System.Threading.LockRecursionPolicy;

namespace System.Collections.Generic
{
    public sealed class HashQueue<TK, TV> : IDisposable, IEnumerable<TV>
    {
        private readonly ReaderWriterLockSlim lockSlim;
        private readonly Dictionary<TK, Node> map;
        private Node head;
        private Node tail;

        public HashQueue(params (TK key, TV value)[] items) : this()
        {
            foreach(var item in items)
            {
                AddNodeInternal(item.key, item.value);
            }
        }

        public HashQueue()
        {
            map = new Dictionary<TK, Node>();
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

        internal Dictionary<TK, Node> Map => map;

        #region Implementation of IDisposable

        public void Dispose()
        {
            lockSlim.Dispose();
        }

        #endregion

        public IEnumerator<TV> GetEnumerator()
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

        public TV AddOrUpdate(TK key, TV addValue, Func<TK, TV, TV> updateValueFactory)
        {
            using(lockSlim.WithWriteLock())
            {
                return map.TryGetValue(key, out var node)
                    ? node.Value = updateValueFactory(key, node.Value)
                    : AddNodeInternal(key, addValue).Value;
            }
        }

        public TV AddOrUpdate(TK key, TV addValue, TV updateValue)
        {
            using(lockSlim.WithWriteLock())
            {
                return map.TryGetValue(key, out var node)
                    ? node.Value = updateValue
                    : AddNodeInternal(key, addValue).Value;
            }
        }

        public TV GetOrAdd(TK key, TV value)
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

        public bool TryAdd(TK key, TV value)
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

        public bool TryGet(TK key, out TV value)
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

        public bool Dequeue(out TV value)
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

        public bool TryRemove(TK key, out TV value)
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

        private Node AddNodeInternal(TK key, TV value)
        {
            var node = new Node(key, value, tail, null);

            if(head == null) head = node;

            if(tail != null) tail.Next = node;

            tail = node;

            map.Add(key, node);

            return node;
        }

        internal sealed class Node
        {
            public Node(TK key, TV value, Node prev, Node next)
            {
                Key = key;
                Value = value;
                Prev = prev;
                Next = next;
            }

            public TK Key { get; }
            public TV Value { get; set; }
            public Node Prev { get; set; }
            public Node Next { get; set; }
        }
    }
}