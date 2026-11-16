using OFDRCentralControlServer.Models;

namespace OFDRCentralControlServer.Core
{
    public record ZeroConsistencyOptions(
        int MinPeakCount = 2,
        int RetestCount = 2,
        double ConvergenceToleranceM = 0.1,
        double HistoryJumpThresholdM = 2.0,
        int RetestDelayMs = 500,
        double DetectionThresholdDb = -80.0,
        double ThresholdMarginDb = 5.0,
        double DbConvergenceToleranceDb = 3.0,
        double WindowStartM = 0.0,
        double WindowEndM = 100.0);

    public record ZeroMeasurementEvaluation(
        ZeroMeasurement Measurement,
        bool IsSuspicious,
        string? SuspiciousReason,
        bool IsConverged,
        bool ShouldAccept);

    /// <summary>
    /// 归零结果一致性判定（设备偶发数据异常时的多次对比依据）：
    /// 1) 帧级可疑：未找到峰、峰位/峰强数量不齐（帧损坏）、峰数量低于 MinPeakCount（单峰即异常）；
    /// 2) 位置越界：选中峰位置落在扫描窗口 [WindowStartM, WindowEndM] 之外；
    /// 3) 门限裕度：选中峰强度距检测门限不足 ThresholdMarginDb（贴近噪声底，信号不可信）；
    /// 4) 历史跳变：与该通道最近一次成功归零差异超过 HistoryJumpThresholdM；
    /// 5) 收敛：连续两次测量均不可疑，且选中峰位置差 <= ConvergenceToleranceM、强度差 <= DbConvergenceToleranceDb 才采纳。
    /// 首次测量不可疑时直接采纳（仅可疑时才触发补测对比）。
    /// </summary>
    public static class ZeroConsistencyEvaluator
    {
        public static ZeroMeasurementEvaluation Evaluate(
            ZeroMeasurement current,
            ZeroMeasurementEvaluation? previous,
            ClientMeasurementHistory? lastZeroHistory,
            ZeroConsistencyOptions options)
        {
            var suspiciousReason = AssessSuspiciousReason(current, lastZeroHistory, options);
            if (suspiciousReason != null)
            {
                return new ZeroMeasurementEvaluation(current, IsSuspicious: true, suspiciousReason, IsConverged: false, ShouldAccept: false);
            }

            // 首次测量且不可疑：直接采纳
            if (previous == null)
            {
                return new ZeroMeasurementEvaluation(current, IsSuspicious: false, null, IsConverged: false, ShouldAccept: true);
            }

            // 上一次可疑：本次虽不可疑，但没有可信的对比基准，继续补测
            if (previous.IsSuspicious)
            {
                return new ZeroMeasurementEvaluation(current, IsSuspicious: false, null, IsConverged: false, ShouldAccept: false);
            }

            var positionConverged = Math.Abs(current.SelectedPosition - previous.Measurement.SelectedPosition) <= options.ConvergenceToleranceM;
            var dbConverged = Math.Abs(current.SelectedDb - previous.Measurement.SelectedDb) <= options.DbConvergenceToleranceDb;
            var converged = positionConverged && dbConverged;
            return new ZeroMeasurementEvaluation(current, IsSuspicious: false, null, converged, ShouldAccept: converged);
        }

        private static string? AssessSuspiciousReason(
            ZeroMeasurement measurement,
            ClientMeasurementHistory? lastZeroHistory,
            ZeroConsistencyOptions options)
        {
            if (measurement.SelectedPosition <= 0)
            {
                return "no peak found";
            }

            if (measurement.PeakCount != measurement.PeakDbCount)
            {
                return $"peak count mismatch: positions={measurement.PeakCount} dbs={measurement.PeakDbCount}";
            }

            if (measurement.PeakCount < options.MinPeakCount)
            {
                return $"peak count {measurement.PeakCount} < min {options.MinPeakCount}";
            }

            if (measurement.SelectedPosition < options.WindowStartM || measurement.SelectedPosition > options.WindowEndM)
            {
                return $"selected position {measurement.SelectedPosition} outside scan window [{options.WindowStartM}, {options.WindowEndM}]";
            }

            if (measurement.SelectedDb < options.DetectionThresholdDb + options.ThresholdMarginDb)
            {
                return $"selected peak db {measurement.SelectedDb} within {options.ThresholdMarginDb}dB of detection threshold {options.DetectionThresholdDb}";
            }

            if (lastZeroHistory != null
                && Math.Abs(measurement.SelectedPosition - lastZeroHistory.LengthM) > options.HistoryJumpThresholdM)
            {
                return $"history jump {measurement.SelectedPosition - lastZeroHistory.LengthM:F3}m exceeds {options.HistoryJumpThresholdM}m";
            }

            return null;
        }
    }
}
