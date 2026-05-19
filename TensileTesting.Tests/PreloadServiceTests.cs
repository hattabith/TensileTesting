using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Abstractions;
using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests;

public class PreloadServiceTests
{
    private static PreloadService CreateService(string mode = "OffsetSubtraction", double threshold = 0.5, double hysteresis = 0.05)
    {
        var settings = new PreloadSettings
        {
            Mode = mode,
            ThresholdKn = threshold,
            HysteresisKn = hysteresis
        };

        return new PreloadService(settings);
    }

    [Fact]
    public void InitialState_IsWaiting()
    {
        PreloadService service = CreateService();

        Assert.Equal(PreloadState.Waiting, service.State);
        Assert.Equal(0.0, service.CapturedForceValue);
        Assert.Equal(0.0, service.CapturedLengthValue);
    }

    [Fact]
    public void ProcessSample_LocksWhenThresholdReached()
    {
        PreloadService service = CreateService(threshold: 0.5);

        service.ProcessSample(0.6, 2.5);

        Assert.Equal(PreloadState.ThresholdReached, service.State);
        Assert.Equal(0.6, service.CapturedForceValue, 10);
        Assert.Equal(2.5, service.CapturedLengthValue, 10);
    }

    [Fact]
    public void OffsetSubtraction_SubtractsForceAfterLock()
    {
        PreloadService service = CreateService(mode: "OffsetSubtraction", threshold: 0.5);
        service.ProcessSample(0.7, 1.0);

        double adjustedForce = service.ApplyForce(1.4);

        Assert.Equal(0.7, adjustedForce, 10);
    }

    [Fact]
    public void OriginShift_DoesNotSubtractForce()
    {
        PreloadService service = CreateService(mode: "OriginShift", threshold: 0.5);
        service.ProcessSample(0.7, 1.0);

        double adjustedForce = service.ApplyForce(1.4);

        Assert.Equal(1.4, adjustedForce, 10);
    }

    [Fact]
    public void OriginShift_ShiftsLengthAfterLock()
    {
        PreloadService service = CreateService(mode: "OriginShift", threshold: 0.5);
        service.ProcessSample(0.7, 2.0);

        double adjustedLength = service.ApplyLength(5.5);

        Assert.Equal(3.5, adjustedLength, 10);
    }

    [Fact]
    public void Hysteresis_ReleasesLockWhenForceDropsBelowBand()
    {
        PreloadService service = CreateService(threshold: 0.5, hysteresis: 0.1);
        service.ProcessSample(0.6, 1.0);

        service.ProcessSample(0.45, 1.1);
        Assert.Equal(PreloadState.ThresholdReached, service.State);

        service.ProcessSample(0.39, 1.2);
        Assert.Equal(PreloadState.Waiting, service.State);
        Assert.Equal(0.0, service.CapturedForceValue, 10);
    }

    [Fact]
    public void Reset_ReturnsToWaitingAndClearsCapturedValues()
    {
        PreloadService service = CreateService();
        service.ProcessSample(0.6, 1.5);

        service.Reset();

        Assert.Equal(PreloadState.Waiting, service.State);
        Assert.Equal(0.0, service.CapturedForceValue, 10);
        Assert.Equal(0.0, service.CapturedLengthValue, 10);
    }
}
