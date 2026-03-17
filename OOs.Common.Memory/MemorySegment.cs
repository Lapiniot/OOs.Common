using System.Buffers;

namespace OOs.Memory;

public class MemorySegment<T> : ReadOnlySequenceSegment<T>
{
    public MemorySegment(ReadOnlyMemory<T> memory) => Memory = memory;

    public MemorySegment<T> Add(ReadOnlyMemory<T> memory)
    {
        var segment = new MemorySegment<T>(memory) { RunningIndex = RunningIndex + Memory.Length };
        Next = segment;
        return segment;
    }

    public static MemorySegment<T> operator +(MemorySegment<T> left, ReadOnlyMemory<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);

        return left.Add(right);
    }
}