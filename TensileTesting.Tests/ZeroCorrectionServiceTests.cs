using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Abstractions;
using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests;

public class ZeroCorrectionServiceTests
{
    private static ZeroCorrectionService CreateService(int captureSamples = 10, double trimFraction = 0.1,
        double goodThreshold = 0.05, double maxThreshold = 0.20)
    {
        var settings = new ZeroCorrectionSettings
        {
            CaptureSamples = captureSamples,
            TrimFraction = trimFraction,
            GoodNoiseThresholdKn = goodThreshold,
            MaxNoiseThresholdKn = maxThreshold
        };
        return new ZeroCorrectionService(settings);
    }

    [Fact]
    public void InitialState_IsNone()
    {
        var svc = CreateService();
        Assert.Equal(ZeroCorrectionState.None, svc.State);
        Assert.Equal(0.0, svc.ForceOffset);
        Assert.Equal(0.0, svc.LengthOffset);
        Assert.Null(svc.EstablishedAt);
        Assert.Equal(ZeroQuality.Unknown, svc.Quality);
    }

    [Fact]
    public void ApplyForce_WhenStateIsNone_ReturnsUnchangedValue()
    {
        var svc = CreateService();
        Assert.Equal(1.23, svc.ApplyForce(1.23));
    }

    [Fact]
    public void ApplyLength_WhenStateIsNone_ReturnsUnchangedValue()
    {
        var svc = CreateService();
        Assert.Equal(5.0, svc.ApplyLength(5.0));
    }

    [Fact]
    public void StartCapture_TransitionsToCapturing()
    {
        var svc = CreateService();
        svc.StartCapture();
        Assert.Equal(ZeroCorrectionState.Capturing, svc.State);
        Assert.Equal(0, svc.SamplesCollected);
    }

    [Fact]
    public void AddSample_AccumulatesSamples()
    {
        var svc = CreateService(captureSamples: 5);
        svc.StartCapture();
        svc.AddSample(1.0, 2.0);
        svc.AddSample(1.1, 2.1);
        Assert.Equal(2, svc.SamplesCollected);
        Assert.Equal(ZeroCorrectionState.Capturing, svc.State);
    }

    [Fact]
    public void AddSample_AutoFinishesWhenCaptureSamplesReached()
    {
        var svc = CreateService(captureSamples: 4);
        svc.StartCapture();
        for (int i = 0; i < 4; i++)
            svc.AddSample(2.0, 3.0);

        Assert.Equal(ZeroCorrectionState.Ready, svc.State);
        Assert.Equal(2.0, svc.ForceOffset, precision: 10);
        Assert.Equal(3.0, svc.LengthOffset, precision: 10);
    }

    [Fact]
    public void AddSample_WhenNotCapturing_IsIgnored()
    {
        var svc = CreateService();
        svc.AddSample(1.0, 2.0); // state is None
        Assert.Equal(0, svc.SamplesCollected);
    }

    [Fact]
    public void ApplyForce_AfterCapture_SubtractsOffset()
    {
        var svc = CreateService(captureSamples: 4);
        svc.StartCapture();
        for (int i = 0; i < 4; i++)
            svc.AddSample(1.0, 0.0);

        Assert.Equal(0.5, svc.ApplyForce(1.5), precision: 10);
        Assert.Equal(-0.5, svc.ApplyForce(0.5), precision: 10);
    }

    [Fact]
    public void ApplyLength_AfterCapture_SubtractsOffset()
    {
        var svc = CreateService(captureSamples: 4);
        svc.StartCapture();
        for (int i = 0; i < 4; i++)
            svc.AddSample(0.0, 5.0);

        Assert.Equal(2.0, svc.ApplyLength(7.0), precision: 10);
    }

    [Fact]
    public void Clear_ResetsToNone()
    {
        var svc = CreateService(captureSamples: 4);
        svc.StartCapture();
        for (int i = 0; i < 4; i++)
            svc.AddSample(1.0, 1.0);

        svc.Clear();

        Assert.Equal(ZeroCorrectionState.None, svc.State);
        Assert.Equal(0.0, svc.ForceOffset);
        Assert.Null(svc.EstablishedAt);
        Assert.Equal(1.5, svc.ApplyForce(1.5)); // passthrough after clear
    }

    [Fact]
    public void Quality_IsGood_WhenNoiseBelowGoodThreshold()
    {
        // Identical samples → zero noise
        var svc = CreateService(captureSamples: 6, goodThreshold: 0.05, maxThreshold: 0.20);
        svc.StartCapture();
        for (int i = 0; i < 6; i++)
            svc.AddSample(1.0, 2.0);

        Assert.Equal(ZeroQuality.Good, svc.Quality);
    }

    [Fact]
    public void Quality_IsWarning_WhenNoiseBetweenThresholds()
    {
        // Samples spread by 0.1 kN RMS — between good (0.05) and max (0.20)
        var svc = CreateService(captureSamples: 4, trimFraction: 0.0, goodThreshold: 0.05, maxThreshold: 0.20);
        svc.StartCapture();
        svc.AddSample(1.0, 0.0);
        svc.AddSample(1.1, 0.0);
        svc.AddSample(0.9, 0.0);
        svc.AddSample(1.0, 0.0);

        Assert.Equal(ZeroQuality.Warning, svc.Quality);
    }

    [Fact]
    public void Quality_IsBad_WhenNoiseExceedsMaxThreshold()
    {
        var svc = CreateService(captureSamples: 4, trimFraction: 0.0, goodThreshold: 0.01, maxThreshold: 0.05);
        svc.StartCapture();
        svc.AddSample(0.0, 0.0);
        svc.AddSample(1.0, 0.0);
        svc.AddSample(0.0, 0.0);
        svc.AddSample(1.0, 0.0);

        Assert.Equal(ZeroQuality.Bad, svc.Quality);
    }

    [Fact]
    public void EstablishedAt_IsSetAfterCapture()
    {
        var svc = CreateService(captureSamples: 2);
        var before = DateTime.Now.AddSeconds(-1);
        svc.StartCapture();
        svc.AddSample(0.0, 0.0);
        svc.AddSample(0.0, 0.0);
        var after = DateTime.Now.AddSeconds(1);

        Assert.NotNull(svc.EstablishedAt);
        Assert.InRange(svc.EstablishedAt!.Value, before, after);
    }

    [Fact]
    public void StartCapture_Twice_ClearsOldSamples()
    {
        var svc = CreateService(captureSamples: 4);
        svc.StartCapture();
        svc.AddSample(99.0, 99.0); // junk first capture
        svc.StartCapture();        // restart
        for (int i = 0; i < 4; i++)
            svc.AddSample(1.0, 2.0);

        Assert.Equal(1.0, svc.ForceOffset, precision: 10);
    }

    [Fact]
    public void ConcurrentReads_WhileCapturing_DoNotThrow()
    {
        var svc = CreateService(captureSamples: 50);
        svc.StartCapture();

        var reader = Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
            {
                _ = svc.State;
                _ = svc.ForceOffset;
                _ = svc.Quality;
                _ = svc.SamplesCollected;
            }
        });

        for (int i = 0; i < 50; i++)
            svc.AddSample(1.0, 2.0);

        reader.Wait(TestContext.Current.CancellationToken);
        Assert.Equal(ZeroCorrectionState.Ready, svc.State);
    }

    [Fact]
    public void TrimmedMean_RejectsOutliers()
    {
        // With 10 samples: 9 × 1.0 and 1 × 100.0
        // TrimFraction=0.1 trims 1 from each end, removing the 100.0 outlier
        var svc = CreateService(captureSamples: 10, trimFraction: 0.1);
        svc.StartCapture();
        for (int i = 0; i < 9; i++)
            svc.AddSample(1.0, 0.0);
        svc.AddSample(100.0, 0.0); // outlier — should be trimmed

        // Offset should be close to 1.0, not pulled toward 100.0
        Assert.True(svc.ForceOffset < 5.0, $"Expected outlier trimming but ForceOffset = {svc.ForceOffset}");
    }
}
