using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations;

public sealed class PreloadService : IPreloadService
{
    private readonly object _lock = new();
    private readonly double _hysteresis;
    private PreloadState _state = PreloadState.Waiting;
    private double _capturedForceValue;
    private double _capturedLengthValue;

    public PreloadMode Mode { get; set; }
    public double Threshold { get; set; }

    public PreloadState State { get { lock (_lock) return _state; } }
    public double CapturedForceValue { get { lock (_lock) return _capturedForceValue; } }
    public double CapturedLengthValue { get { lock (_lock) return _capturedLengthValue; } }

    public PreloadService(PreloadSettings settings)
    {
        Mode = string.Equals(settings.Mode, "OriginShift", StringComparison.OrdinalIgnoreCase)
            ? PreloadMode.OriginShift
            : PreloadMode.OffsetSubtraction;
        Threshold = settings.ThresholdKn;
        _hysteresis = Math.Max(0.0, settings.HysteresisKn);
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = PreloadState.Waiting;
            _capturedForceValue = 0.0;
            _capturedLengthValue = 0.0;
        }
    }

    public void ProcessSample(double correctedForce, double correctedLength)
    {
        lock (_lock)
        {
            double releaseThreshold = Threshold - _hysteresis;

            if (_state == PreloadState.Waiting && correctedForce >= Threshold)
            {
                _capturedForceValue = correctedForce;
                _capturedLengthValue = correctedLength;
                _state = PreloadState.ThresholdReached;
                return;
            }

            if (_state == PreloadState.ThresholdReached && correctedForce <= releaseThreshold)
            {
                _state = PreloadState.Waiting;
                _capturedForceValue = 0.0;
                _capturedLengthValue = 0.0;
            }
        }
    }

    public double ApplyForce(double correctedForce)
    {
        lock (_lock)
        {
            if (_state != PreloadState.ThresholdReached)
                return correctedForce;

            // Mode A: subtract force value at preload threshold
            return Mode == PreloadMode.OffsetSubtraction
                ? correctedForce - _capturedForceValue
                : correctedForce;
        }
    }

    public double ApplyLength(double correctedLength)
    {
        lock (_lock)
        {
            if (_state != PreloadState.ThresholdReached)
                return correctedLength;

            // Mode B: shift length origin to the point of preload threshold crossing
            return Mode == PreloadMode.OriginShift
                ? correctedLength - _capturedLengthValue
                : correctedLength;
        }
    }
}
