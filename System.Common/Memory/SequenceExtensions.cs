using System.Buffers;

namespace System.Memory;

public static class SequenceExtensions
{
    public const byte CR = (byte)'\r';
    public const byte LF = (byte)'\n';

    public static bool TryReadLine(this ReadOnlySequence<byte> sequence, out ReadOnlyMemory<byte> line)
    {
        line = default;

        if(sequence.IsSingleSegment)
        {
            var span = sequence.FirstSpan;
            var index = span.IndexOf(CR);
            if(index <= 0 || index >= span.Length - 1 || span[index + 1] != LF) return false;

            line = sequence.First[..index];
            return true;
        }

        var pos = sequence.PositionOf(CR);
        if(pos == null) return false;
        var position = sequence.GetPosition(1, pos.Value);
        if(!sequence.TryGet(ref position, out var mem) || mem.Length <= 0 || mem.Span[0] != LF) return false;

        var slice = sequence.Slice(0, pos.Value);

        line = slice.First.Length == slice.Length ? slice.First : slice.ToArray();
        return true;
    }

    public static bool TryReadLine(this ref SequenceReader<byte> sequenceReader, out ReadOnlySequence<byte> line, bool strict = true)
    {
        if(sequenceReader.TryReadTo(out line, LF, advancePastDelimiter: true))
        {
            var length = line.Length;

            if(length <= 0) return true;

            if(line.IsSingleSegment)
            {
                if(line.FirstSpan[^1] is CR)
                {
                    line = line.Slice(0, length - 1);
                }
            }
            else
            {
                if(line.Slice(length - 1).FirstSpan[0] is CR)
                {
                    line = line.Slice(0, length - 1);
                }
            }

            return true;
        }

        if(!strict)
        {
            line = sequenceReader.Sequence.Slice(sequenceReader.Position);
            sequenceReader.Advance(sequenceReader.Remaining);
            return true;
        }

        line = default;
        return false;
    }
}