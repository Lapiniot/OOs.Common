using System.Buffers;

namespace System.Memory;

public static class SequenceFactory
{
    public static ReadOnlySequence<T> Create<T>(ReadOnlyMemory<T> memory1) => new(memory1);

    public static ReadOnlySequence<T> Create<T>(ReadOnlyMemory<T> memory1, ReadOnlyMemory<T> memory2)
    {
        var segment = new MemorySegment<T>(memory1);
        return new(segment, 0, segment + memory2, memory2.Length);
    }

    public static ReadOnlySequence<T> Create<T>(ReadOnlyMemory<T> memory1, ReadOnlyMemory<T> memory2, ReadOnlyMemory<T> memory3)
    {
        var segment = new MemorySegment<T>(memory1);
        return new(segment, 0, segment + memory2 + memory3, memory3.Length);
    }

    public static ReadOnlySequence<T> Create<T>(ReadOnlyMemory<T> memory1, ReadOnlyMemory<T> memory2, ReadOnlyMemory<T> memory3, ReadOnlyMemory<T> memory4)
    {
        var segment = new MemorySegment<T>(memory1);
        return new(segment, 0, segment + memory2 + memory3 + memory4, memory4.Length);
    }

    public static ReadOnlySequence<T> Create<T>(ReadOnlyMemory<T> memory1, ReadOnlyMemory<T> memory2, ReadOnlyMemory<T> memory3, ReadOnlyMemory<T> memory4, ReadOnlyMemory<T> memory5)
    {
        var segment = new MemorySegment<T>(memory1);
        return new(segment, 0, segment + memory2 + memory3 + memory4 + memory5, memory5.Length);
    }
}