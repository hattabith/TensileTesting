namespace TensileTestingApp.Services.Abstractions;

public enum PreloadMode
{
    /// <summary>Subtract the force value captured at threshold crossing from all subsequent force readings.</summary>
    OffsetSubtraction,
    /// <summary>Shift the length origin to the point of threshold crossing without modifying force values.</summary>
    OriginShift
}

public enum PreloadState
{
    Waiting,
    ThresholdReached
}

public interface IPreloadService
{
    PreloadMode Mode { get; set; }
    PreloadState State { get; }
    double Threshold { get; set; }

    /// <summary>Force value captured at the moment the threshold was first exceeded.</summary>
    double CapturedForceValue { get; }

    /// <summary>Length value captured at the moment the threshold was first exceeded.</summary>
    double CapturedLengthValue { get; }

    /// <summary>Reset threshold detection to <see cref="PreloadState.Waiting"/>.</summary>
    void Reset();

    /// <summary>Update threshold detection state from the current corrected sample.</summary>
    void ProcessSample(double correctedForce, double correctedLength);

    double ApplyForce(double correctedForce);
    double ApplyLength(double correctedLength);
}
