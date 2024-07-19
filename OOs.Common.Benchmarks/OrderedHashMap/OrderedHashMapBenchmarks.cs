using OOs.Collections.Generic;

namespace OOs.Common.Benchmarks.OrderedHashMap;

#nullable disable

[HideColumns("Error", "StdDev", "RatioSD", "Median")]
[MemoryDiagnoser]
public class OrderedHashMapBenchmarks
{
    private Dictionary<string, string> sampledData;
    private OrderedHashMapV1<string, string> mapV1;
    private OrderedHashMap<string, string> map;
#if NET9_0_OR_GREATER
    private OrderedDictionary<string, string> orderedDictionary;
#endif

    private static Dictionary<string, string> GetSampleData(int count)
    {
        Dictionary<string, string> dictionary = [];
        for (var i = 0; i < count; i++)
            dictionary["sample-key-" + i] = "sample-value-" + i;
        return dictionary;
    }

    [Params(500, 5000, 50000)]
    public static int Count { get; set; }

    [ParamsAllValues]
    public static Mode Mode { get; set; }

    [GlobalSetup]
    public void Setup() => sampledData = GetSampleData(Count);

    [IterationSetup(Targets = [nameof(AddOrUpdateV1), nameof(AddOrUpdateV1Concurrent)])]
    public void SetupForAddOrUpdateV1() => mapV1 = Mode is Mode.Add ? new() : new(sampledData);

    [IterationSetup(Targets = [nameof(AddOrUpdateCurrent), nameof(AddOrUpdateCurrentConcurrent)])]
    public void SetupForAddOrUpdateCurrent() => map = Mode is Mode.Add ? new() : new(sampledData);

#if NET9_0_OR_GREATER

    [IterationSetup(Targets = [nameof(AddOrUpdateOrderedDictionary), nameof(AddOrUpdateOrderedDictionaryConcurrent)])]
    public void SetupForAddOrUpdateOrderedDictionary() => orderedDictionary = Mode is Mode.Add ? new() : new(sampledData);

#endif

    [Benchmark(Baseline = true)]
    public void AddOrUpdateV1()
    {
        foreach (var (k, v) in sampledData)
        {
            mapV1.AddOrUpdate(k, v);
        }
    }

    [Benchmark]
    public void AddOrUpdateCurrent()
    {
        foreach (var (k, v) in sampledData)
        {
            map.AddOrUpdate(k, v);
        }
    }

#if NET9_0_OR_GREATER

    [Benchmark]
    public void AddOrUpdateOrderedDictionary()
    {
        foreach (var (k, v) in sampledData)
        {
            orderedDictionary[k] = v;
        }
    }

#endif

    [Benchmark]
    public void AddOrUpdateV1Concurrent() =>
        Parallel.ForEach(sampledData, item => mapV1.AddOrUpdate(item.Key, item.Value));

    [Benchmark]
    public void AddOrUpdateCurrentConcurrent() =>
        Parallel.ForEach(sampledData, item => map.AddOrUpdate(item.Key, item.Value));

#if NET9_0_OR_GREATER

    [Benchmark]
    public void AddOrUpdateOrderedDictionaryConcurrent()
    {
        Parallel.ForEach(sampledData, item =>
        {
            lock (orderedDictionary)
            {
                orderedDictionary[item.Key] = item.Value;
            }
        });
    }

#endif
}

public enum Mode
{
    Add,
    Update
}