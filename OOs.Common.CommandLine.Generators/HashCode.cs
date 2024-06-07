using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OOs.Common.CommandLine.Generators;

internal struct HashCode
{
    private static readonly uint s_seed = GenerateGlobalSeed();

    private static uint GenerateGlobalSeed()
    {
        using var rnd = RandomNumberGenerator.Create();
        var bytes = new byte[sizeof(uint)];
        rnd.GetBytes(bytes);
        return Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference<byte>(bytes));
    }

    private const uint Prime2 = 2246822519U;
    private const uint Prime3 = 3266489917U;
    private const uint Prime4 = 668265263U;
    private const uint Prime5 = 374761393U;

    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var hc1 = (uint)(value1?.GetHashCode() ?? 0);
        var hc2 = (uint)(value2?.GetHashCode() ?? 0);

        var hash = MixEmptyState();
        hash += 8;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);

        hash = MixFinal(hash);
        return (int)hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixEmptyState() => s_seed + Prime5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint QueueRound(uint hash, uint queuedValue) => RotateLeft(hash + queuedValue * Prime3, 17) * Prime4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int offset) => (value << offset) | (value >> (32 - offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }
}