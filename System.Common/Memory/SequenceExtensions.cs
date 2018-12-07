using System.Buffers;

namespace System.Memory
{
    public static class SequenceExtensions
    {
        public const byte CR = (byte)'\r';
        public const byte LF = (byte)'\n';

        public static bool TryGetLine(this ReadOnlySequence<byte> buffer, out ReadOnlyMemory<byte> line)
        {
            line = default;

            if(buffer.IsSingleSegment)
            {
                var span = buffer.First.Span;
                var index = span.IndexOf(CR);
                if(index <= 0 || index >= span.Length - 1 || span[index + 1] != LF) return false;

                line = buffer.First.Slice(0, index);
                return true;
            }

            var pos = buffer.PositionOf(CR);
            if(pos == null) return false;
            var position = buffer.GetPosition(1, pos.Value);
            if(!buffer.TryGet(ref position, out var mem) || mem.Length <= 0 || mem.Span[0] != LF) return false;

            var slice = buffer.Slice(0, pos.Value);

            line = slice.First.Length == slice.Length ? slice.First : slice.ToArray();
            return true;
        }
    }
}