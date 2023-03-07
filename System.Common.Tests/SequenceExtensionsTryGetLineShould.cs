using System.Buffers;
using System.Memory;

namespace System.Common.Tests;

[TestClass]
public class SequenceExtensionsTryGetLineShould
{
    [TestMethod]
    public void ReturnTrueAndLineGivenValidSingleSegmentSequence()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[]
        {
            0x00, 0x11, 0x22, 0x33,
            (byte)'\r', (byte)'\n',
            0x33, 0x22, 0x11, 0x00
        });

        var actual = sequence.TryReadLine(out var line);

        Assert.IsTrue(actual);
        Assert.AreEqual(4, line.Length);
        var span = line.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x11, span[1]);
        Assert.AreEqual(0x22, span[2]);
        Assert.AreEqual(0x33, span[3]);
    }

    [TestMethod]
    public void ReturnTrueAndLineGivenValidContiguousSequence()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11, 0x22, 0x33, (byte)'\r', (byte)'\n' });
        var sequence = new ReadOnlySequence<byte>(segment, 0,
            segment.Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsTrue(actual);
        Assert.AreEqual(4, line.Length);
        var span = line.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x11, span[1]);
        Assert.AreEqual(0x22, span[2]);
        Assert.AreEqual(0x33, span[3]);
    }

    [TestMethod]
    public void ReturnTrueAndLineGivenValidFragmentedSequenceCase1()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new byte[] { 0x22, 0x33, (byte)'\r', (byte)'\n' })
            .Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsTrue(actual);
        Assert.AreEqual(4, line.Length);
        var span = line.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x11, span[1]);
        Assert.AreEqual(0x22, span[2]);
        Assert.AreEqual(0x33, span[3]);
    }

    [TestMethod]
    public void ReturnTrueAndLineGivenValidFragmentedSequenceCase2()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11, 0x22, 0x33 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new[] { (byte)'\r', (byte)'\n' })
            .Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsTrue(actual);
        Assert.AreEqual(4, line.Length);
        var span = line.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x11, span[1]);
        Assert.AreEqual(0x22, span[2]);
        Assert.AreEqual(0x33, span[3]);
    }

    [TestMethod]
    public void ReturnTrueAndLineGivenValidFragmentedSequenceCase3()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11, 0x22, 0x33 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new[] { (byte)'\r' })
            .Append(new byte[] { (byte)'\n', 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsTrue(actual);
        Assert.AreEqual(4, line.Length);
        var span = line.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x11, span[1]);
        Assert.AreEqual(0x22, span[2]);
        Assert.AreEqual(0x33, span[3]);
    }

    [TestMethod]
    public void ReturnTrueAndLineGivenValidFragmentedSequenceCase4()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11, 0x22, 0x33, (byte)'\r' });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new byte[] { (byte)'\n', 0x33, 0x22 })
            .Append(new byte[] { 0x11, 0x00 }), 2);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsTrue(actual);
        Assert.AreEqual(4, line.Length);
        var span = line.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x11, span[1]);
        Assert.AreEqual(0x22, span[2]);
        Assert.AreEqual(0x33, span[3]);
    }

    [TestMethod]
    public void ReturnTrueAndLineGivenValidFragmentedSequenceCase5()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11, 0x22, 0x33, (byte)'\r', (byte)'\n', 0x33, 0x22 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new byte[] { 0x11, 0x00 }), 2);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsTrue(actual);
        Assert.AreEqual(4, line.Length);
        var span = line.Span;
        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x11, span[1]);
        Assert.AreEqual(0x22, span[2]);
        Assert.AreEqual(0x33, span[3]);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidSingleSegmentSequenceCase1()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[]
        {
            0x00, 0x11, 0x22, 0x33,
            0x33, 0x22, 0x11, 0x00
        });

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidSingleSegmentSequenceCase2()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[]
        {
            0x00, 0x11, 0x22, 0x33,
            (byte)'\r', 0x00,
            0x33, 0x22, 0x11, 0x00
        });

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidSingleSegmentSequenceCase3()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[]
        {
            0x00, 0x11, 0x22, 0x33,
            (byte)'\n', 0x00,
            0x33, 0x22, 0x11, 0x00
        });

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidSingleSegmentSequenceCase4()
    {
        var sequence = new ReadOnlySequence<byte>(new byte[]
        {
            0x00, 0x11, 0x22, 0x33,
            (byte)'\r', 0x00, (byte)'\n',
            0x33, 0x22, 0x11, 0x00
        });

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidContiguousSequence()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11, 0x22, 0x33 });
        var sequence = new ReadOnlySequence<byte>(segment, 0,
            segment.Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidFragmentedSequenceCase1()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new byte[] { 0x22, 0x33, (byte)'\r', 0x00, (byte)'\n' })
            .Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidFragmentedSequenceCase2()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new byte[] { 0x22, 0x33, 0x00, (byte)'\n' })
            .Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidFragmentedSequenceCase3()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new byte[] { 0x22, 0x33, (byte)'\r', 0x00 })
            .Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }

    [TestMethod]
    public void ReturnFalseAndEmptyLineGivenInvalidFragmentedSequenceCase4()
    {
        var segment = new MemorySegment<byte>(new byte[] { 0x00, 0x11 });
        var sequence = new ReadOnlySequence<byte>(segment, 0, segment
            .Append(new byte[] { 0x22, 0x33, (byte)'\n', (byte)'\r' })
            .Append(new byte[] { 0x33, 0x22, 0x11, 0x00 }), 4);

        var actual = sequence.TryReadLine(out var line);

        Assert.IsFalse(actual);
        Assert.AreEqual(0, line.Length);
    }
}