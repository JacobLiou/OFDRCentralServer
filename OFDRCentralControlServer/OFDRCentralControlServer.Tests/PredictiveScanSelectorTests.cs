using OFDRCentralControlServer.Core;
using OFDRCentralControlServer.Models;
using OFDRCentralControlServer.Protocol;

namespace OFDRCentralControlServer.Tests;

public class PredictiveScanSelectorTests
{
    private static readonly PredictiveScanOptions DefaultOptions = new(
        HistoryJumpThresholdM: 25.0,
        ConvergenceToleranceM: 2.0,
        TailPreferenceWeight: 12.0,
        LowConfidenceMargin: 8.0,
        WideSpreadThresholdM: 25.0,
        WideSpreadMarginThreshold: 12.0);

    [Fact]
    public void EvaluateCandidates_WithRecentShortHistory_PrefersNearCandidateOverLongTailFromLog()
    {
        const double zeroLength = 17.069;
        var candidateSet = new ScanCandidateSet(
            zeroLength,
            new[]
            {
                new PeakCandidate(0, 20.635, -46.229, 20.635 - zeroLength),
                new PeakCandidate(23, 90.625, -73.148, 90.625 - zeroLength),
                new PeakCandidate(80, 237.774, -64.044, 237.774 - zeroLength),
            },
            RawPositionsCount: 81,
            RawDbCount: 81);
        var lastScanHistory = new ClientMeasurementHistory(3, "scan", 3.572, -51.111, DateTime.UtcNow);

        var evaluation = PredictiveScanSelector.EvaluateCandidates(candidateSet, lastScanHistory, DefaultOptions);

        Assert.Equal(3.566, evaluation.Best.Candidate.DeltaFromZero, precision: 3);
        Assert.False(evaluation.IsLowConfidence);
        Assert.False(evaluation.TriggeredByHistoryJump);
    }

    [Fact]
    public void EvaluateCandidates_WithEqualSignals_GivesLaterCandidateHigherScoreBecauseOfTailBias()
    {
        var candidateSet = new ScanCandidateSet(
            0,
            new[]
            {
                new PeakCandidate(0, 10.0, -60.0, 10.0),
                new PeakCandidate(1, 10.0, -60.0, 10.0),
            },
            RawPositionsCount: 2,
            RawDbCount: 2);

        var evaluation = PredictiveScanSelector.EvaluateCandidates(candidateSet, lastScanHistory: null, DefaultOptions);

        Assert.Equal(1, evaluation.Best.Candidate.Index);
        Assert.True(evaluation.Best.TailBiasNormalized > 0.9);
    }

    [Fact]
    public void FrameParser_WithRealServerLogFrame_ParsesShortAndLongPeaks()
    {
        const string frame = "OP_20.645_287.940_288.932_289.838_290.800_291.854_292.714_293.539_294.536_295.590_297.386_298.292_299.229_299.977_-58.947_-77.767_-77.668_-78.494_-78.306_-77.872_-77.980_-77.791_-78.638_-78.321_-77.762_-78.016_-78.247_-78.669_12_SN9II1_4937.748_PO";

        var ok = FrameParser.TryParseFrame(frame, out var result);

        Assert.True(ok);
        Assert.NotNull(result.PeakPositions);
        Assert.NotNull(result.PeakDbs);
        Assert.Equal(14, result.PeakPositions!.Count);
        Assert.Equal(14, result.PeakDbs!.Count);
        Assert.Equal(20.645, result.PeakPositions.First(), precision: 3);
        Assert.Equal(299.977, result.PeakPositions.Last(), precision: 3);
        Assert.Equal(-58.947, result.PeakDbs.First(), precision: 3);
        Assert.Equal(-78.669, result.PeakDbs.Last(), precision: 3);
    }
}
