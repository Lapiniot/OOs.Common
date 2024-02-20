using System.Diagnostics.CodeAnalysis;

namespace OOs.Extensions.Diagnostics;

/// <summary>
/// Provides simple implementation of <see cref="IMetricsListener"/> which listens to metrics 
/// emited from system and stores measurements in memory to the typed dictionaries 
/// so they can be accessed by corresponding <see cref="Instrument.Name"/> key.
/// This implementation also provides periodic polling for observable instruments.
/// </summary>
public sealed class InMemoryMetricsCollector : MetricsCollector
{
    private readonly Dictionary<string, decimal> decimalValues = [];
    private readonly Dictionary<string, double> doubleValues = [];
    private readonly Dictionary<string, int> intValues = [];
    private readonly Dictionary<string, long> longValues = [];

    public InMemoryMetricsCollector([NotNull] IOptionsMonitor<MetricsCollectorOptions> options)
    {
        RecordInterval = options.CurrentValue.RecordInterval;
        options.OnChange(OnOptionsChanged);
    }

    private void OnDecimalMeasurement(Instrument instrument, decimal measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        decimalValues[instrument.Name] = measurement;

    private void OnDoubleMeasurement(Instrument instrument, double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        doubleValues[instrument.Name] = measurement;

    private void OnIntMeasurement(Instrument instrument, int measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        intValues[instrument.Name] = measurement;

    private void OnLongMeasurement(Instrument instrument, long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        longValues[instrument.Name] = measurement;

    private void OnOptionsChanged(MetricsCollectorOptions options, string? arg2) => RecordInterval = options.RecordInterval;

    protected override MeasurementHandlers GetMeasurementHandlers() => new()
    {
        LongHandler = OnLongMeasurement,
        IntHandler = OnIntMeasurement,
        DoubleHandler = OnDoubleMeasurement,
        DecimalHandler = OnDecimalMeasurement
    };

    public IReadOnlyDictionary<string, decimal> DecimalValues => decimalValues;

    public IReadOnlyDictionary<string, double> DoubleValues => doubleValues;

    public IReadOnlyDictionary<string, int> IntValues => intValues;

    public IReadOnlyDictionary<string, long> LongValues => longValues;

    public override string Name { get; } = nameof(InMemoryMetricsCollector);
}