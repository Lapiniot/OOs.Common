using System.Buffers;
using System.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Common.Tests
{
    [TestClass]
    public class SequenceExtensions_TryGetLine_Should
    {
        [TestMethod]
        public void ReturnTrueAndLine_GivenValidSingleSegmentSequence()
        {
            var sequence = new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x11, 0x22, 0x33,
                (byte)'\r', (byte)'\n',
                0x33, 0x22, 0x11, 0x00
            });

            var actual = sequence.TryGetLine(out var line);

            Assert.IsTrue(actual);
            Assert.AreEqual(4, line.Length);
            var span = line.Span;
            Assert.AreEqual(0x00, span[0]);
            Assert.AreEqual(0x11, span[1]);
            Assert.AreEqual(0x22, span[2]);
            Assert.AreEqual(0x33, span[3]);
        }

        [TestMethod]
        public void ReturnTrueAndLine_GivenValidContiguousSequence()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11, 0x22, 0x33, (byte)'\r', (byte)'\n'});
            var sequence = new ReadOnlySequence<byte>(segment, 0,
                segment.Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsTrue(actual);
            Assert.AreEqual(4, line.Length);
            var span = line.Span;
            Assert.AreEqual(0x00, span[0]);
            Assert.AreEqual(0x11, span[1]);
            Assert.AreEqual(0x22, span[2]);
            Assert.AreEqual(0x33, span[3]);
        }

        [TestMethod]
        public void ReturnTrueAndLine_GivenValidFragmentedSequenceCase1()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new byte[] {0x22, 0x33, (byte)'\r', (byte)'\n'})
                .Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsTrue(actual);
            Assert.AreEqual(4, line.Length);
            var span = line.Span;
            Assert.AreEqual(0x00, span[0]);
            Assert.AreEqual(0x11, span[1]);
            Assert.AreEqual(0x22, span[2]);
            Assert.AreEqual(0x33, span[3]);
        }

        [TestMethod]
        public void ReturnTrueAndLine_GivenValidFragmentedSequenceCase2()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11, 0x22, 0x33});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new[] {(byte)'\r', (byte)'\n'})
                .Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsTrue(actual);
            Assert.AreEqual(4, line.Length);
            var span = line.Span;
            Assert.AreEqual(0x00, span[0]);
            Assert.AreEqual(0x11, span[1]);
            Assert.AreEqual(0x22, span[2]);
            Assert.AreEqual(0x33, span[3]);
        }

        [TestMethod]
        public void ReturnTrueAndLine_GivenValidFragmentedSequenceCase3()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11, 0x22, 0x33});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new[] {(byte)'\r'})
                .Append(new byte[] {(byte)'\n', 0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsTrue(actual);
            Assert.AreEqual(4, line.Length);
            var span = line.Span;
            Assert.AreEqual(0x00, span[0]);
            Assert.AreEqual(0x11, span[1]);
            Assert.AreEqual(0x22, span[2]);
            Assert.AreEqual(0x33, span[3]);
        }

        [TestMethod]
        public void ReturnTrueAndLine_GivenValidFragmentedSequenceCase4()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11, 0x22, 0x33, (byte)'\r'});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new byte[] {(byte)'\n', 0x33, 0x22})
                .Append(new byte[] {0x11, 0x00}), 2);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsTrue(actual);
            Assert.AreEqual(4, line.Length);
            var span = line.Span;
            Assert.AreEqual(0x00, span[0]);
            Assert.AreEqual(0x11, span[1]);
            Assert.AreEqual(0x22, span[2]);
            Assert.AreEqual(0x33, span[3]);
        }

        [TestMethod]
        public void ReturnTrueAndLine_GivenValidFragmentedSequenceCase5()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11, 0x22, 0x33, (byte)'\r', (byte)'\n', 0x33, 0x22});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new byte[] {0x11, 0x00}), 2);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsTrue(actual);
            Assert.AreEqual(4, line.Length);
            var span = line.Span;
            Assert.AreEqual(0x00, span[0]);
            Assert.AreEqual(0x11, span[1]);
            Assert.AreEqual(0x22, span[2]);
            Assert.AreEqual(0x33, span[3]);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidSingleSegmentSequence_Case1()
        {
            var sequence = new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x11, 0x22, 0x33,
                0x33, 0x22, 0x11, 0x00
            });

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidSingleSegmentSequence_Case2()
        {
            var sequence = new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x11, 0x22, 0x33,
                (byte)'\r', 0x00,
                0x33, 0x22, 0x11, 0x00
            });

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidSingleSegmentSequence_Case3()
        {
            var sequence = new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x11, 0x22, 0x33,
                (byte)'\n', 0x00,
                0x33, 0x22, 0x11, 0x00
            });

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidSingleSegmentSequence_Case4()
        {
            var sequence = new ReadOnlySequence<byte>(new byte[]
            {
                0x00, 0x11, 0x22, 0x33,
                (byte)'\r', 0x00, (byte)'\n',
                0x33, 0x22, 0x11, 0x00
            });

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidContiguousSequence()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11, 0x22, 0x33});
            var sequence = new ReadOnlySequence<byte>(segment, 0,
                segment.Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidFragmentedSequenceCase1()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new byte[] {0x22, 0x33, (byte)'\r', 0x00, (byte)'\n'})
                .Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidFragmentedSequenceCase2()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new byte[] {0x22, 0x33, 0x00, (byte)'\n'})
                .Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidFragmentedSequenceCase3()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new byte[] {0x22, 0x33, (byte)'\r', 0x00})
                .Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }

        [TestMethod]
        public void ReturnFalseAndEmptyLine_GivenInvalidFragmentedSequenceCase4()
        {
            var segment = new Segment<byte>(new byte[] {0x00, 0x11});
            var sequence = new ReadOnlySequence<byte>(segment, 0, segment
                .Append(new byte[] {0x22, 0x33, (byte)'\n', (byte)'\r'})
                .Append(new byte[] {0x33, 0x22, 0x11, 0x00}), 4);

            var actual = sequence.TryGetLine(out var line);

            Assert.IsFalse(actual);
            Assert.AreEqual(0, line.Length);
        }
    }
}