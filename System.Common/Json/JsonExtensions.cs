using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static System.Text.Encoding;

namespace System.Json
{
    public static class JsonExtensions
    {
        public static TEnum ToEnumValue<TEnum>(this JsonValue json) where TEnum : struct, Enum
        {
            return (TEnum)Enum.Parse(typeof(TEnum), json, true);
        }

        public static JsonValue ToJsonValue<TEnum>(this TEnum value) where TEnum : struct, Enum
        {
            return value.ToString().ToLower();
        }

        public static byte[] Serialize(this JsonValue json, Encoding encoding = null)
        {
            using var stream = new MemoryStream();
            json.SerializeTo(stream, encoding);
            return stream.ToArray();
        }

        public static void SerializeTo(this JsonValue json, Stream stream, Encoding encoding = null)
        {
            using var writer = new StreamWriter(stream, encoding ?? ASCII, 2 * 1024, true);
            json.Save(writer);
            writer.Flush();
        }

        public static JsonValue Deserialize(byte[] bytes, int index, int count, Encoding encoding = null)
        {
            using var stream = new MemoryStream(bytes, index, count, false);
            using var reader = new StreamReader(stream, encoding ?? UTF8);
            return JsonValue.Load(reader);
        }

        public static JsonValue Deserialize(byte[] bytes, Encoding encoding = null)
        {
            return Deserialize(bytes, 0, bytes.Length, encoding);
        }

        public static JsonValue Deserialize(ReadOnlyMemory<byte> memory, Encoding encoding = null)
        {
            return MemoryMarshal.TryGetArray(memory, out var segment)
                ? Deserialize(segment.Array, segment.Offset, segment.Count, encoding)
                : Deserialize(memory.ToArray(), encoding);
        }
    }
}