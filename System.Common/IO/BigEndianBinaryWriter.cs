using System.Converters;
using System.Text;

namespace System.IO
{
    public sealed class BigEndianBinaryWriter : BinaryWriter
    {
        public BigEndianBinaryWriter(Stream output) : base(output)
        {
        }

        public BigEndianBinaryWriter(Stream output, Encoding encoding) : base(output, encoding)
        {
        }

        public BigEndianBinaryWriter(Stream output, Encoding encoding, bool leaveOpen) :
            base(output, encoding, leaveOpen)
        {
        }

        public override void Write(char ch)
        {
            base.Write(ch.ChangeByteOrder());
        }

        public override void Write(char[] chars)
        {
            throw new NotSupportedException();
        }

        public override void Write(char[] chars, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(decimal value)
        {
            base.Write(value.ChangeByteOrder());
        }

        public override void Write(double value)
        {
            base.Write(value.ChangeByteOrder());
        }

        public override void Write(short value)
        {
            base.Write(value.ChangeByteOrder());
        }

        public override void Write(int value)
        {
            base.Write(value.ChangeByteOrder());
        }

        public override void Write(long value)
        {
            base.Write(value.ChangeByteOrder());
        }

        public override void Write(float value)
        {
            throw new NotSupportedException();
        }

        public override void Write(ushort value)
        {
            base.Write(value.ChangeByteOrder());
        }

        public override void Write(uint value)
        {
            base.Write(value.ChangeByteOrder());
        }

        public override void Write(ulong value)
        {
            base.Write(value.ChangeByteOrder());
        }
    }
}