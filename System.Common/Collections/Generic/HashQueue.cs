using System.Runtime.CompilerServices;
using System.Threading;
using static System.Threading.LockRecursionPolicy;

[assembly: InternalsVisibleTo("System.Common.Tests")]

namespace System.Collections.Generic
{
    public sealed class HashQueue<TK, TV> : IDisposable
    {
        private readonly ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim(NoRecursion);
        private readonly Dictionary<TK, Node> map = new Dictionary<TK, Node>();
        private Node head;
        private Node tail;

        internal Node Head
        {
            get { return head; }
            set { head = value; }
        }

        internal Node Tail
        {
            get { return tail; }
            set { tail = value; }
        }

        internal Dictionary<TK, Node> Map
        {
            get { return map; }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            lockSlim.Dispose();
        }

        #endregion

        public TV AddOrUpdate(TK key, TV addValue, Func<TK, TV, TV> updateValueFactory)
        {
            return map.TryGetValue(key, out var node)
                ? (node.Value = updateValueFactory(key, node.Value))
                : AddNodeInternal(key, addValue).Value;
        }

        public TV GetOrAdd(TK key, TV value)
        {
            return map.TryGetValue(key, out var node)
                ? node.Value
                : AddNodeInternal(key, value).Value;
        }

        public bool TryAdd(TK key, TV value)
        {
            if(map.ContainsKey(key)) return false;

            AddNodeInternal(key, value);

            return true;
        }

        public bool TryGetValue(TK key, out TV value)
        {
            if(!map.TryGetValue(key, out var node))
            {
                value = default;
                return false;
            }

            value = node.Value;
            return true;
        }

        public bool Dequeue(out TV value)
        {
            if(head == null)
            {
                value = default;
                return false;
            }

            var h = head;
            head = h.Next;
            head.Prev = null;
            value = h.Value;
            return map.Remove(h.Key);
        }

        public bool TryRemove(TK key, out TV value)
        {
            if(map.TryGetValue(key, out var node) && map.Remove(key))
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