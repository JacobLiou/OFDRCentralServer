using OFDRCentralControlServer.Core;
using OFDRCentralControlServer.Models;

namespace OFDRCentralControlServer.Tests;

public class ZeroConsistencyEvaluatorTests
{
    private static readonly ZeroConsistencyOptions DefaultOptions = new(
        MinPeakCount: 2,
        RetestCount: 2,
        ConvergenceToleranceM: 0.1,
        HistoryJumpThresholdM: 2.0,
        RetestDelayMs: 500);

    [Fact]
    public void Evaluate_FirstMeasurementWithEnoughPeaks_IsAcceptedDirectly()
    {
        var measurement = new ZeroMeasurement(58.236, -69.297, PeakCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.False(evaluation.IsSuspicious);
        Assert.False(evaluation.IsConverged);
        Assert.True(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_SinglePeakFrame_IsSuspicious()
    {
        // 复现 2026-11-16 13:07:50 日志：OP_60.669_-59.924 仅 1 个峰
        var measurement = new ZeroMeasurement(60.669, -59.924, PeakCount: 1);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
        Assert.Contains("peak count", evaluation.SuspiciousReason);
    }

    [Fact]
    public void Evaluate_NoPeakFound_IsSuspicious()
    {
        var measurement = new ZeroMeasurement(-1, 0, PeakCount: 0);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
        Assert.Equal("no peak found", evaluation.SuspiciousReason);
    }

    [Fact]
    public void Evaluate_HistoryJumpBeyondThreshold_IsSuspicious()
    {
        var lastZeroHistory = new ClientMeasurementHistory(5, "zero", 58.236, -69.297, DateTime.UtcNow);
        var measurement = new ZeroMeasurement(60.669, -59.924, PeakCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
        Assert.Contains("history jump", evaluation.SuspiciousReason);
    }

    [Fact]
    public void Evaluate_HistoryWithinThreshold_IsNotSuspicious()
    {
        var lastZeroHistory = new ClientMeasurementHistory(5, "zero", 58.236, -69.297, DateTime.UtcNow);
        var measurement = new ZeroMeasurement(58.30, -69.3, PeakCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory, DefaultOptions);

        Assert.False(evaluation.IsSuspicious);
        Assert.True(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_ValidMeasurementAfterSuspiciousPrevious_IsStillNotAccepted()
    {
        var suspiciousFirst = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(60.669, -59.924, PeakCount: 1), previous: null, lastZeroHistory: null, DefaultOptions);
        Assert.True(suspiciousFirst.IsSuspicious);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.236, -69.297, PeakCount: 4), previous: suspiciousFirst, lastZeroHistory: null, DefaultOptions);

        Assert.False(second.IsSuspicious);
        Assert.False(second.IsConverged);
        Assert.False(second.ShouldAccept);
    }

    [Fact]
    public void Evaluate_TwoConsecutiveConsistentMeasurements_ConvergeAndAccept()
    {
        var first = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.236, -69.297, PeakCount: 4), previous: null, lastZeroHistory: null, DefaultOptions);
        Assert.True(first.ShouldAccept);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.240, -69.3, PeakCount: 4), previous: first, lastZeroHistory: null, DefaultOptions);

        Assert.False(second.IsSuspicious);
        Assert.True(second.IsConverged);
        Assert.True(second.ShouldAccept);
    }

    [Fact]
    public void Evaluate_TwoConsecutiveButDivergentMeasurements_DoNotConverge()
    {
        var first = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.236, -69.297, PeakCount: 4), previous: null, lastZeroHistory: null, DefaultOptions);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(60.669, -59.924, PeakCount: 4), previous: first, lastZeroHistory: null, DefaultOptions);

        Assert.False(second.IsSuspicious);
        Assert.False(second.IsConverged);
        Assert.False(second.ShouldAccept);
    }

    [Fact]
    public void Evaluate_ExactToleranceDifference_Converges()
    {
        var first = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.200, -69.297, PeakCount: 4), previous: null, lastZeroHistory: null, DefaultOptions);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.300, -69.3, PeakCount: 4), previous: first, lastZeroHistory: null, DefaultOptions);

        Assert.True(second.IsConverged);
        Assert.True(second.ShouldAccept);
    }

    [Fact]
    public void Evaluate_RealLogSequence_ConvergesOnThirdAttempt()
    {
        // 完整复现 2026-11-16 13:07 日志序列：
        // 13:07:50 OP_60.669_-59.924（单峰，可疑）
        // 13:07:54 OP_3.640_5.994_7.737_58.236_...（4 峰，last-peak=58.236）
        // 假设第三次补测得到 58.240（4 峰）
        var first = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(60.669, -59.924, PeakCount: 1), previous: null, lastZeroHistory: null, DefaultOptions);
        Assert.True(first.IsSuspicious);
        Assert.False(first.ShouldAccept);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.236, -69.297, PeakCount: 4), previous: first, lastZeroHistory: null, DefaultOptions);
        Assert.False(second.IsSuspicious);
        Assert.False(second.ShouldAccept);

        var third = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.240, -69.3, PeakCount: 4), previous: second, lastZeroHistory: null, DefaultOptions);
        Assert.True(third.IsConverged);
        Assert.True(third.ShouldAccept);
        Assert.Equal(58.240, third.Measurement.SelectedPosition, precision: 3);
    }
}
