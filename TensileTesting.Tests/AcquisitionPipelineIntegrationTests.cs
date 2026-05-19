using TensileTestingApp.Configuration;
using TensileTestingApp.Models;
using TensileTestingApp.Services.Abstractions;
using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests;

public class AcquisitionPipelineIntegrationTests
{
    [Fact]
    public void Pipeline_AppliesZeroThenPreloadOffsetSubtraction()
    {
        var zeroSettings = new ZeroCorrectionSettings
        {
            CaptureSamples = 4,
            TrimFraction = 0.0,
            GoodNoiseThresholdKn = 0.05,
            MaxNoiseThresholdKn = 0.20
        };
        var preloadSettings = new PreloadSettings
        {
            Mode = "OffsetSubtraction",
            ThresholdKn = 0.50,
            HysteresisKn = 0.05
        };

        IZeroCorrectionService zeroService = new ZeroCorrectionService(zeroSettings);
        IPreloadService preloadService = new PreloadService(preloadSettings);

        // Establish baseline around 1.00 kN and 10.00 mm.
        zeroService.StartCapture();
        zeroService.AddSample(1.0, 10.0);
        zeroService.AddSample(1.0, 10.0);
        zeroService.AddSample(1.0, 10.0);
        zeroService.AddSample(1.0, 10.0);

        var sample = new TensileTestData
        {
            Timestamp = DateTime.UtcNow,
            Force = 1.8,
            FilteredForce = 1.8,
            Length = 12.3,
            FilteredLength = 12.3
        };

        sample.CorrectedForce = zeroService.ApplyForce(sample.FilteredForce);
        sample.CorrectedLength = zeroService.ApplyLength(sample.FilteredLength);
        preloadService.ProcessSample(sample.CorrectedForce, sample.CorrectedLength);
        sample.PreloadAdjustedForce = preloadService.ApplyForce(sample.CorrectedForce);
        sample.PreloadAdjustedLength = preloadService.ApplyLength(sample.CorrectedLength);
        sample.IsZeroApplied = zeroService.State == ZeroCorrectionState.Ready;
        sample.IsPreloadApplied = preloadService.State == PreloadState.ThresholdReached;

        Assert.Equal(0.8, sample.CorrectedForce, 10);
        Assert.Equal(2.3, sample.CorrectedLength, 10);
        Assert.Equal(0.0, sample.PreloadAdjustedForce, 10); // locked at first sample value (0.8)
        Assert.Equal(2.3, sample.PreloadAdjustedLength, 10); // unchanged in offset subtraction mode
        Assert.True(sample.IsZeroApplied);
        Assert.True(sample.IsPreloadApplied);
    }

    [Fact]
    public void Pipeline_AppliesOriginShiftOnLengthInModeB()
    {
        var zeroSettings = new ZeroCorrectionSettings
        {
            CaptureSamples = 2,
            TrimFraction = 0.0
        };
        var preloadSettings = new PreloadSettings
        {
            Mode = "OriginShift",
            ThresholdKn = 0.40,
            HysteresisKn = 0.05
        };

        IZeroCorrectionService zeroService = new ZeroCorrectionService(zeroSettings);
        IPreloadService preloadService = new PreloadService(preloadSettings);

        zeroService.StartCapture();
        zeroService.AddSample(0.5, 5.0);
        zeroService.AddSample(0.5, 5.0);

        // First point crosses threshold and defines origin for mode B.
        double correctedForce1 = zeroService.ApplyForce(1.0);  // 0.5
        double correctedLength1 = zeroService.ApplyLength(8.0); // 3.0
        preloadService.ProcessSample(correctedForce1, correctedLength1);

        // Next point should keep force as-is and shift length by captured value (3.0).
        double correctedForce2 = zeroService.ApplyForce(1.2);   // 0.7
        double correctedLength2 = zeroService.ApplyLength(9.5); // 4.5

        double adjustedForce2 = preloadService.ApplyForce(correctedForce2);
        double adjustedLength2 = preloadService.ApplyLength(correctedLength2);

        Assert.Equal(0.7, adjustedForce2, 10);
        Assert.Equal(1.5, adjustedLength2, 10);
    }
}
