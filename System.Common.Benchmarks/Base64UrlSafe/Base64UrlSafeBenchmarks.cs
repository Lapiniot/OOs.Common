using V1 = System.Common.Benchmarks.Base64UrlSafe.Base64UrlSafeV1;
using Next = System.Net.Http.Base64UrlSafe;
using System.Buffers.Text;

namespace System.Common.Benchmarks.Base64UrlSafe;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "RatioSD")]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class Base64UrlSafeBenchmarks
{
    private byte[]? bytes;
    private byte[]? utf8;

    [Params(4, 8, 12, 18, 256, 512)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Init()
    {
        bytes = new byte[Length];
        utf8 = new byte[Base64.GetMaxEncodedToUtf8Length(Length)];
#pragma warning disable CA5394 // Do not use insecure randomness
        Random.Shared.NextBytes(bytes);
#pragma warning restore CA5394 // Do not use insecure randomness
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ToBase64String")]
    public void ToBase64StringV1()
    {
        _ = V1.ToBase64String(bytes!);
    }

    [Benchmark]
    [BenchmarkCategory("ToBase64String")]
    public void ToBase64StringNext()
    {
        _ = Next.ToBase64String(bytes);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EncodeToUtf8")]
    public void EncodeToUtf8V1()
    {
        V1.EncodeToUtf8(bytes, utf8, out _, out _);
    }

    [Benchmark]
    [BenchmarkCategory("EncodeToUtf8")]
    public void EncodeToUtf8Next()
    {
        Next.EncodeToUtf8(bytes, utf8, out _, out _);
    }
}