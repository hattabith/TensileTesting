using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations;

public sealed class ZeroCorrectionService : IZeroCorrectionService
{
    private readonly ZeroCorrectionSettings _settings;
    private readonly object _lock = new();

    private ZeroCorrectionState _state = ZeroCorrectionState.None;
    private readonly List<double> _forceSamples = new();
    private readonly List<double> _lengthSamples = new();
    private double _forceOffset;
    private double _lengthOffset;
    private double _forceNoiseRms;
    private ZeroQuality _quality = ZeroQuality.Unknown;
    private DateTime? _establishedAt;

    public ZeroCorrectionService(ZeroCorrectionSettings settings) => _settings = settings;

    public ZeroCorrectionState State { get { lock (_lock) return _state; } }
    public double ForceOffset { get { lock (_lock) return _forceOffset; } }
    public double LengthOffset { get { lock (_lock) return _lengthOffset; } }
    public double ForceNoiseRms { get { lock (_lock) return _forceNoiseRms; } }
    public ZeroQuality Quality { get { lock (_lock) return _quality; } }
    public DateTime? EstablishedAt { get { lock (_lock) return _establishedAt; } }
    public int SamplesCollected { get { lock (_lock) return _forceSamples.Count; } }
    public int SamplesRequired => _settings.CaptureSamples;

    public void StartCapture()
    {
        lock (_lock)
        {
            _forceSamples.Clear();
            _lengthSamples.Clear();
            _quality = ZeroQuality.Unknown;
            _state = ZeroCorrectionState.Capturing;
        }
    }

    public void AddSample(double filteredForce, double filteredLength)
    {
        lock (_lock)
        {
            if (_state != ZeroCorrectionState.Capturing)
                return;

            _forceSamples.Add(filteredForce);
            _lengthSamples.Add(filteredLength);

            if (_forceSamples.Count >= _settings.CaptureSamples)
                FinishCapture();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _forceSamples.Clear();
            _lengthSamples.Clear();
            _forceOffset = 0.0;
            _lengthOffset = 0.0;
            _forceNoiseRms = 0.0;
            _quality = ZeroQuality.Unknown;
            _establishedAt = null;
            _state = ZeroCorrectionState.None;
        }
    }

    public double ApplyForce(double filteredForce)
    {
        lock (_lock)
        {
            return _state == ZeroCorrectionState.Ready
                ? filteredForce - _forceOffset
                : filteredForce;
        }
    }

    public double ApplyLength(double filteredLength)
    {
        lock (_lock)
        {
            return _state == ZeroCorrectionState.Ready
                ? filteredLength - _lengthOffset
                : filteredLength;
        }
    }

    // Must be called while holding _lock.
    private void FinishCapture()
    {
        _forceOffset = TrimmedMean(_forceSamples, _settings.TrimFraction);
        _lengthOffset = TrimmedMean(_lengthSamples, _settings.TrimFraction);
        _forceNoiseRms = RmsDeviation(_forceSamples, _forceOffset);

        _quality = _forceNoiseRms <= _settings.GoodNoiseThresholdKn
            ? ZeroQuality.Good
            : _forceNoiseRms <= _settings.MaxNoiseThresholdKn
                ? ZeroQuality.Warning
                : ZeroQuality.Bad;

        _establishedAt = DateTime.Now;
        _state = ZeroCorrectionState.Ready;
    }

    private static double TrimmedMean(List<double> samples, double trimFraction)
    {
        int n = samples.Count;
        if (n == 0) return 0.0;
        if (n < 4 || trimFraction <= 0.0) return samples.Average();

        int trimCount = (int)(n * trimFraction);
        List<double> sorted = [.. samples.OrderBy(x => x)];
        List<double> trimmed = sorted.Skip(trimCount).Take(n - 2 * trimCount).ToList();
        return trimmed.Count > 0 ? trimmed.Average() : samples.Average();
    }

    private static double RmsDeviation(List<double> samples, double mean)
    {
        if (samples.Count == 0) return 0.0;
        return Math.Sqrt(samples.Average(x => (x - mean) * (x - mean)));
    }
}
