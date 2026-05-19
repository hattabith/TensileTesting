namespace TensileTestingApp.Services.Abstractions;

public enum ZeroCorrectionState
{
    None,
    Capturing,
    Ready
}

public enum ZeroQuality
{
    Unknown,
    Good,
    Warning,
    Bad
}

public interface IZeroCorrectionService
{
    ZeroCorrectionState State { get; }
    double ForceOffset { get; }
    double LengthOffset { get; }
    double ForceNoiseRms { get; }
    ZeroQuality Quality { get; }
    DateTime? EstablishedAt { get; }
    int SamplesCollected { get; }
    int SamplesRequired { get; }

    /// <summary>Begin accumulating samples for baseline estimation.</summary>
    void StartCapture();

    /// <summary>Feed one filtered sample pair. Automatically finalizes after <see cref="SamplesRequired"/> samples.</summary>
    void AddSample(double filteredForce, double filteredLength);

    /// <summary>Reset offsets and return to <see cref="ZeroCorrectionState.None"/>.</summary>
    void Clear();

    double ApplyForce(double filteredForce);
    double ApplyLength(double filteredLength);
}
