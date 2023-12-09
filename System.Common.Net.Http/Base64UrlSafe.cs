using System.Buffers;
using System.Buffers.Text;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Net.Http;

public static class Base64UrlSafe
{
    private const int MaxAllocatedOnStack = 512;

    // Quickly compute actual non-padded data length:
    // this is essentially Math.Ceiling(length*4/3) formula optimized for integer math
    // Math.Ceiling(a/b) == (a+b-1)/b
    private static int GetMaxEncodedToUtf8NoPaddingLength(int length) => ((length << 2) + 2) / 3;

    public static string ToBase64String(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return string.Create(GetMaxEncodedToUtf8NoPaddingLength(bytes.Length), bytes, EncodeToUtf16);
    }

    public static byte[] FromBase64String(string base64String)
    {
        ArgumentNullException.ThrowIfNull(base64String);

        var len = base64String.Length;
        var totalWidth = len % 4 == 0 ? len : ((len >> 2) + 1) << 2;
        return Convert.FromBase64String(base64String.Replace('-', '+').Replace('_', '/').PadRight(totalWidth, '='));
    }

    [SkipLocalsInit]
    private static void EncodeToUtf16(Span<char> utf16, byte[] bytes)
    {
        var max = Base64.GetMaxEncodedToUtf8Length(bytes.Length);

        byte[] pooled = null;
        Span<byte> utf8 = max <= MaxAllocatedOnStack
            ? stackalloc byte[MaxAllocatedOnStack]
            : (pooled = ArrayPool<byte>.Shared.Rent(max));

        Base64.EncodeToUtf8(bytes, utf8, out _, out _);
        ConvertToUrlSafe<ushort, Utf8ToUtf16StringSpec>(utf8.Slice(0, utf16.Length), MemoryMarshal.Cast<char, ushort>(utf16));

        if (pooled is { })
        {
            ArrayPool<byte>.Shared.Return(pooled);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConvertToUrlSafe<T, TSpec>(Span<byte> utf8Bytes, Span<T> destination)
        where T : struct
        where TSpec : struct, IVectorStoreSpecification<T>, IScalarConversionSpecification<T>
    {
        ref var srcRef = ref MemoryMarshal.GetReference(utf8Bytes);
        var length = (nuint)utf8Bytes.Length;

        if (!Vector128.IsHardwareAccelerated || length < (uint)Vector128<byte>.Count)
        {
            // Just delegate conversion to the specialized routine implemented by TSpec type
            TSpec.Convert(ref srcRef, ref MemoryMarshal.GetReference(destination), length);
        }
        else
        {
            ref var dstRef = ref MemoryMarshal.GetReference(destination);

            if (Vector256.IsHardwareAccelerated && length >= (uint)Vector256<byte>.Count)
            {
                nuint offset = 0;
                var oneFromEndOffset = length - (uint)Vector256<byte>.Count;
                Vector256<byte> vector, replaced;

                do
                {
                    vector = Vector256.LoadUnsafe(ref srcRef, offset);
                    replaced = Vector256.ConditionalSelect(Vector256.Equals(vector, Vector256.Create((byte)'+')), Vector256.Create((byte)'-'), vector);
                    replaced = Vector256.ConditionalSelect(Vector256.Equals(vector, Vector256.Create((byte)'/')), Vector256.Create((byte)'_'), replaced);
                    TSpec.Store(replaced, ref Unsafe.Add(ref dstRef, offset));
                    offset += (nuint)Vector256<byte>.Count;
                } while (offset <= oneFromEndOffset);

                if (offset != (uint)length)
                {
                    offset = oneFromEndOffset;
                    vector = Vector256.LoadUnsafe(ref srcRef, offset);
                    replaced = Vector256.ConditionalSelect(Vector256.Equals(vector, Vector256.Create((byte)'+')), Vector256.Create((byte)'-'), vector);
                    replaced = Vector256.ConditionalSelect(Vector256.Equals(vector, Vector256.Create((byte)'/')), Vector256.Create((byte)'_'), replaced);
                    TSpec.Store(replaced, ref Unsafe.Add(ref dstRef, offset));
                }
            }
            else
            {
                nuint offset = 0;
                var oneFromEndOffset = length - (uint)Vector128<byte>.Count;
                Vector128<byte> vector, replaced;

                do
                {
                    vector = Vector128.LoadUnsafe(ref srcRef, offset);
                    replaced = Vector128.ConditionalSelect(Vector128.Equals(vector, Vector128.Create((byte)'+')), Vector128.Create((byte)'-'), vector);
                    replaced = Vector128.ConditionalSelect(Vector128.Equals(vector, Vector128.Create((byte)'/')), Vector128.Create((byte)'_'), replaced);
                    TSpec.Store(replaced, ref Unsafe.Add(ref dstRef, offset));
                    offset += (nuint)Vector128<byte>.Count;
                } while (offset <= oneFromEndOffset);

                if (offset != (uint)length)
                {
                    offset = oneFromEndOffset;
                    vector = Vector128.LoadUnsafe(ref srcRef, offset);
                    replaced = Vector128.ConditionalSelect(Vector128.Equals(vector, Vector128.Create((byte)'+')), Vector128.Create((byte)'-'), vector);
                    replaced = Vector128.ConditionalSelect(Vector128.Equals(vector, Vector128.Create((byte)'/')), Vector128.Create((byte)'_'), replaced);
                    TSpec.Store(replaced, ref Unsafe.Add(ref dstRef, offset));
                }
            }
        }
    }

    public static void EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
    {
        if (Base64.EncodeToUtf8InPlace(buffer, dataLength, out bytesWritten) is OperationStatus.Done)
        {
            bytesWritten = GetMaxEncodedToUtf8NoPaddingLength(dataLength);
            var utf8 = buffer.Slice(0, bytesWritten);
            ConvertToUrlSafe<byte, Utf8ToUtf8InPlaceSpec>(utf8, utf8);
        }
    }

    public static void EncodeToUtf8(Span<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
    {
        Base64.EncodeToUtf8(bytes, utf8, out bytesConsumed, out _, isFinalBlock);
        bytesWritten = GetMaxEncodedToUtf8NoPaddingLength(bytesConsumed);
        utf8 = utf8.Slice(0, bytesWritten);
        ConvertToUrlSafe<byte, Utf8ToUtf8InPlaceSpec>(utf8, utf8);
    }

    /// <summary>
    /// Enforces implementors to provide specialized non-SIMD scalar algorythm 
    /// of UTF-8 base64-encoded bytes conversion to the URL-safe representation.
    /// </summary>
    /// <typeparam name="T">Destination memory element type.</typeparam>
    /// <remarks>
    /// This algorythm will be used as last resort when SIMD vectorization is not supported or input data size is too small.
    /// </remarks>
    private interface IScalarConversionSpecification<T> where T : struct
    {
        static abstract void Convert(ref byte source, ref T destination, nuint length);
    }

    /// <summary>
    /// Enforces implementors to provide specialized algorythm which stores processed SIMD vectors of <see cref="byte"/> 
    /// to the destination memory.
    /// </summary>
    /// <typeparam name="T">Destination memory element type.</typeparam>
    /// <remarks>
    /// This agreement implies that vectors of <see cref="byte"/> already contain processed data.
    /// The only responsibility of implementor is to store to the memory in a specialized and 
    /// performant way (widen and store with size extension to Unicode e.g.).
    /// </remarks>
    private interface IVectorStoreSpecification<T> where T : struct
    {
        static abstract void Store(Vector128<byte> vector, ref T destination);
        static abstract void Store(Vector256<byte> vector, ref T destination);
    }

    private struct Utf8ToUtf16StringSpec : IVectorStoreSpecification<ushort>, IScalarConversionSpecification<ushort>
    {
        public static void Convert(ref byte source, ref ushort destination, nuint length)
        {
            for (nuint i = 0; i < length; i++)
            {
                var value = Unsafe.Add(ref source, i);
                Unsafe.Add(ref destination, i) = value == '+' ? '-' : value == '/' ? '_' : value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(Vector128<byte> vector, ref ushort destination)
        {
            var (lower, upper) = Vector128.Widen(vector);
            lower.StoreUnsafe(ref destination);
            upper.StoreUnsafe(ref destination, (uint)Vector128<ushort>.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(Vector256<byte> vector, ref ushort destination)
        {
            var (lower, upper) = Vector256.Widen(vector);
            lower.StoreUnsafe(ref destination);
            upper.StoreUnsafe(ref destination, (uint)Vector256<ushort>.Count);
        }
    }

    private struct Utf8ToUtf8InPlaceSpec : IVectorStoreSpecification<byte>, IScalarConversionSpecification<byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Convert(ref byte source, ref byte _, nuint length)
        {
            for (ref var endRef = ref Unsafe.Add(ref source, length);
                Unsafe.IsAddressLessThan(ref source, ref endRef);
                source = ref Unsafe.Add(ref source, 1))
            {
                if (source == '+')
                    source = (byte)'-';
                else if (source == '/')
                    source = (byte)'_';
            }
        }

        public static void Store(Vector128<byte> vector, ref byte destination) => vector.StoreUnsafe(ref destination);
        public static void Store(Vector256<byte> vector, ref byte destination) => vector.StoreUnsafe(ref destination);
    }
}