using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System;

public static class Base32
{
    public static string ToBase32String(long value) => Avx2.IsSupported
        ? string.Create(13, value, WriteBufferAvx2)
        : string.Create(13, value, WriteBuffer);

    private static void WriteBuffer(Span<char> span, long value)
    {
        // this fixed size array will be referenced directly from 
        // data section of dll as a part of no-alloc optimization
        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8;

        // write in backward order to eliminate redundant bounds checks
        span[12] = (char)alphabet[(int)(value & 31)];
        span[11] = (char)alphabet[(int)((value >>> 5) & 31)];
        span[10] = (char)alphabet[(int)((value >>> 10) & 31)];
        span[9] = (char)alphabet[(int)((value >>> 15) & 31)];
        span[8] = (char)alphabet[(int)((value >>> 20) & 31)];
        span[7] = (char)alphabet[(int)((value >>> 25) & 31)];
        span[6] = (char)alphabet[(int)((value >>> 30) & 31)];
        span[5] = (char)alphabet[(int)((value >>> 35) & 31)];
        span[4] = (char)alphabet[(int)((value >>> 40) & 31)];
        span[3] = (char)alphabet[(int)((value >>> 45) & 31)];
        span[2] = (char)alphabet[(int)((value >>> 50) & 31)];
        span[1] = (char)alphabet[(int)((value >>> 55) & 31)];
        span[0] = (char)alphabet[(int)((value >>> 60) & 31)];
    }

    private static void WriteBufferAvx2(Span<char> span, long value)
    {
        // Shift indices are already re-shuffled in a way which gives correct order after a set of SIMD unpack/pack operations 
        ReadOnlySpan<ulong> shifts = [60, 55, 20, 15, 50, 45, 10, 05, 40, 35, 00, 00, 30, 25, 00, 00,];
        ref var shiftsRef = ref MemoryMarshal.GetReference(shifts);
        var values = Vector256.Create(value);
        var mask5bits = Vector256.Create(0b11111);

        var q1 = Avx2.ShiftRightLogicalVariable(values, Vector256.LoadUnsafe(ref shiftsRef, 0)).AsInt32();
        var q2 = Avx2.ShiftRightLogicalVariable(values, Vector256.LoadUnsafe(ref shiftsRef, 4)).AsInt32();
        var q3 = Avx2.ShiftRightLogicalVariable(values, Vector256.LoadUnsafe(ref shiftsRef, 8)).AsInt32();
        var q4 = Avx2.ShiftRightLogicalVariable(values, Vector256.LoadUnsafe(ref shiftsRef, 12)).AsInt32();

        var q12s = Avx2.And(Avx2.UnpackLow(Avx2.UnpackLow(q1, q2), Avx2.UnpackHigh(q1, q2)), mask5bits);
        var q34s = Avx2.And(Avx2.UnpackLow(Avx2.UnpackLow(q3, q4), Avx2.UnpackHigh(q3, q4)), mask5bits);
        var indices = Avx2.PackSignedSaturate(q12s, q34s);

        // Generate mask for indexes from the range [26..31]
        var mask = Avx2.CompareGreaterThan(indices, Vector256.Create((short)25));
        var offsets = Avx2.BlendVariable(Vector256.Create((short)65), Vector256.Create((short)24), mask);
        var chars = Avx2.Add(indices, offsets);

        ref var destination = ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span));
        chars.GetLower().StoreUnsafe(ref destination);
        // Simulate shift by 5 chars across entire 256bit lane
        Ssse3.AlignRight(chars.GetUpper(), chars.GetLower(), 10).StoreUnsafe(ref destination, 5);
    }
}