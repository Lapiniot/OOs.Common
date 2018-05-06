using System.IO;
using System.Text;
using static System.Text.Encoding;

namespace System.Json
{
    public static class JsonExtensions
    {
        public static TEnum ToEnumValue<TEnum>(this JsonValue json) where TEnum : struct
        {
            var type = typeof(TEnum);

            CheckEnumType(type);

            return (TEnum)Enum.Parse(type, json, true);
        }

        public static JsonValue ToJsonValue<TEnum>(this TEnum value) where TEnum : struct
        {
            CheckEnumType(typeof(TEnum));

            return value.ToString().ToLower();
        }

        private static void CheckEnumType(Type type)
        {
            if(!type.IsEnum) throw new ArgumentException($"{type.Name} must be valid enume type");
        }

        public static byte[] Serialize(this JsonValue json, Encoding encoding = null)
        {
            using(var stream = new MemoryStream())
            {
                json.SerializeTo(stream, encoding);

                return stream.ToArray();
            }
        }

        public static void SerializeTo(this JsonValue json, Stream stream, Encoding encoding = null)
        {
            using(var writer = new StreamWriter(stream, encoding ?? ASCII, 2 * 1024, true))
            {
                json.Save(writer);

                writer.Flush();
            }
        }

        public static JsonValue Deserialize(byte[] bytes, Encoding encoding = null)
        {
            return Deserialize(bytes, 0, bytes.Length, encoding);
        }

        public static JsonValue Deserialize(byte[] bytes, int index, int count, Encoding encoding = null)
        {
            using(var stream = new MemoryStream(bytes, index, count))
            using(var reader = new StreamReader(stream, encoding ?? UTF8))
            {
                return JsonValue.Load(reader);
            }
        }
    }
}