using System.Converters;
using System.Text;

namespace System.IO
{
    public sealed class BigEndianBinaryReader : BinaryReader
    {
        public BigEndianBinaryReader(Stream input) : base(input)
        {
        }

        public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) :
            base(input, encoding, leaveOpen)
        {
        }

        public override short ReadInt16()
        {
            return base.ReadInt16().ChangeByteOrder();
        }

        public override ushort ReadUInt16()
        {
            return base.ReadUInt16().ChangeByteOrder();
        }

        public override int ReadInt32()
        {
            return base.ReadInt32().ChangeByteOrder();
        }

        public override uint ReadUInt32()
        {
            return base.ReadUInt32().ChangeByteOrder();
        }

        public override long ReadInt64()
        {
            return base.ReadInt64().ChangeByteOrder();
        }

        public override ulong ReadUInt64()
        {
            return base.ReadUInt64().ChangeByteOrder();
        }

        public override int Read()
        {
            return base.Read().ChangeByteOrder();
        }

        public override int PeekChar()
        {
            return base.PeekChar().ChangeByteOrder();
        }

        public override decimal ReadDecimal()
        {
            return base.ReadDecimal().ChangeByteOrder();
        }

        public override double ReadDouble()
        {
            return base.ReadDouble().ChangeByteOrder();
        }

        public override float ReadSingle()
        {
            throw new NotSupportedException();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            var num = base.Read(buffer, index, count);

            for(var i = index; i < num; i++) buffer[i] = buffer[i].ChangeByteOrder();

            return count;
        }

        public override char ReadChar()
        {
            return base.ReadChar().ChangeByteOrder();
        }

        public override char[] ReadChars(int count)
        {
            var chars = base.ReadChars(count);

            for(var i = 0; i < chars.Length; i++) chars[i] = chars[i].ChangeByteOrder();

            return chars;
        }
    }
}