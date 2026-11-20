using OFDRCentralControlServer.Core;
using OFDRCentralControlServer.Models;

namespace OFDRCentralControlServer.Tests;

public class ZeroConsistencyEvaluatorTests
{
    private static readonly ZeroConsistencyOptions DefaultOptions = new(
        MinPeakCount: 3,
        RetestCount: 1,
        ConvergenceToleranceM: 0.1,
        HistoryJumpThresholdM: 2.0,
        RetestDelayMs: 500,
        DetectionThresholdDb: -80.0,
        ThresholdMarginDb: 5.0,
        DbConvergenceToleranceDb: 3.0,
        WindowStartM: 0.0,
        WindowEndM: 100.0);

    [Fact]
    public void Evaluate_FirstMeasurementWithEnoughPeaks_IsAcceptedDirectly()
    {
        var measurement = new ZeroMeasurement(58.236, -69.297, PeakCount: 4, PeakDbCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.False(evaluation.IsSuspicious);
        Assert.True(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_ThreePeaks_IsAcceptedDirectly()
    {
        var measurement = new ZeroMeasurement(58.236, -69.297, PeakCount: 3, PeakDbCount: 3);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.False(evaluation.IsSuspicious);
        Assert.True(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_SinglePeakFrame_IsSuspicious()
    {
        // 复现 2026-11-16 13:07:50 日志：OP_60.669_-59.924 仅 1 个峰
        var measurement = new ZeroMeasurement(60.669, -59.924, PeakCount: 1, PeakDbCount: 1);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
        Assert.Contains("peak count", evaluation.SuspiciousReason);
    }

    [Fact]
    public void Evaluate_TwoPeaks_IsSuspiciousWhenMinIsThree()
    {
        var measurement = new ZeroMeasurement(58.236, -69.297, PeakCount: 2, PeakDbCount: 2);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_NoPeakFound_IsSuspicious()
    {
        var measurement = new ZeroMeasurement(-1, 0, PeakCount: 0, PeakDbCount: 0);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
        Assert.Equal("no peak found", evaluation.SuspiciousReason);
    }

    [Fact]
    public void Evaluate_PeakCountMismatch_IsSuspicious()
    {
        var measurement = new ZeroMeasurement(60.669, -59.924, PeakCount: 4, PeakDbCount: 3);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
        Assert.Contains("peak count mismatch", evaluation.SuspiciousReason);
    }

    [Fact]
    public void Evaluate_SelectedPositionOutsideScanWindow_IsSuspicious()
    {
        var measurement = new ZeroMeasurement(101.5, -69.297, PeakCount: 4, PeakDbCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.True(evaluation.IsSuspicious);
        Assert.False(evaluation.ShouldAccept);
        Assert.Contains("outside scan window", evaluation.SuspiciousReason);
    }

    [Fact]
    public void Evaluate_SelectedPositionAtWindowEnd_IsNotSuspicious()
    {
        var measurement = new ZeroMeasurement(100.0, -69.297, PeakCount: 4, PeakDbCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.False(evaluation.IsSuspicious);
        Assert.True(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_SelectedPeakCloseToThreshold_IsStillAccepted()
    {
        // 门限裕度不再拒绝：三峰弱信号也直接采纳，避免拖长补测
        var measurement = new ZeroMeasurement(58.236, -78.856, PeakCount: 4, PeakDbCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory: null, DefaultOptions);

        Assert.False(evaluation.IsSuspicious);
        Assert.True(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_HistoryJump_IsStillAccepted()
    {
        // 历史跳变不再拒绝
        var lastZeroHistory = new ClientMeasurementHistory(5, "zero", 58.236, -69.297, DateTime.UtcNow);
        var measurement = new ZeroMeasurement(60.669, -59.924, PeakCount: 4, PeakDbCount: 4);

        var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previous: null, lastZeroHistory, DefaultOptions);

        Assert.False(evaluation.IsSuspicious);
        Assert.True(evaluation.ShouldAccept);
    }

    [Fact]
    public void Evaluate_ValidMeasurementAfterSuspiciousPrevious_IsAcceptedImmediately()
    {
        var suspiciousFirst = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(60.669, -59.924, PeakCount: 1, PeakDbCount: 1), previous: null, lastZeroHistory: null, DefaultOptions);
        Assert.True(suspiciousFirst.IsSuspicious);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.236, -69.297, PeakCount: 4, PeakDbCount: 4), previous: suspiciousFirst, lastZeroHistory: null, DefaultOptions);

        Assert.False(second.IsSuspicious);
        Assert.True(second.ShouldAccept);
    }

    [Fact]
    public void Evaluate_RealLogSequence_AcceptsOnSecondAttempt()
    {
        // 1 峰坏帧后，第二次 4 峰直接采纳（不再要求第三次收敛）
        var first = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(60.669, -59.924, PeakCount: 1, PeakDbCount: 1), previous: null, lastZeroHistory: null, DefaultOptions);
        Assert.True(first.IsSuspicious);
        Assert.False(first.ShouldAccept);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.236, -69.297, PeakCount: 4, PeakDbCount: 4), previous: first, lastZeroHistory: null, DefaultOptions);
        Assert.False(second.IsSuspicious);
        Assert.True(second.ShouldAccept);
        Assert.Equal(58.236, second.Measurement.SelectedPosition, precision: 3);
    }

    [Fact]
    public void Evaluate_DbJumpAfterGoodFrame_StillAcceptedWhenReEvaluating()
    {
        // 二次 dB 收敛已取消：只要够峰就采纳
        var first = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.236, -69.297, PeakCount: 4, PeakDbCount: 4), previous: null, lastZeroHistory: null, DefaultOptions);
        Assert.True(first.ShouldAccept);

        var second = ZeroConsistencyEvaluator.Evaluate(
            new ZeroMeasurement(58.240, -59.924, PeakCount: 4, PeakDbCount: 4), previous: first, lastZeroHistory: null, DefaultOptions);

        Assert.False(second.IsSuspicious);
        Assert.True(second.ShouldAccept);
    }
}
