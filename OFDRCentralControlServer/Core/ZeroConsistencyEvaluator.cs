using OFDRCentralControlServer.Models;

namespace OFDRCentralControlServer.Core
{
    public record ZeroConsistencyOptions(
        int MinPeakCount = 2,
        int RetestCount = 2,
        double ConvergenceToleranceM = 0.1,
        double HistoryJumpThresholdM = 2.0,
        int RetestDelayMs = 500);

    public record ZeroMeasurementEvaluation(
        ZeroMeasurement Measurement,
        bool IsSuspicious,
        string? SuspiciousReason,
        bool IsConverged,
        bool ShouldAccept);

    /// <summary>
    /// 归零结果一致性判定（设备偶发数据异常时的多次对比依据）：
    /// 1) 帧级可疑：峰数量低于 MinPeakCount（单峰即异常）或未找到峰；
    /// 2) 历史跳变：与该通道最近一次成功归零差异超过 HistoryJumpThresholdM；
    /// 3) 收敛：连续两次测量均不可疑，且选中峰位置差 <= ConvergenceToleranceM 才采纳。
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

            var converged = Math.Abs(current.SelectedPosition - previous.Measurement.SelectedPosition) <= options.ConvergenceToleranceM;
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

            if (measurement.PeakCount < options.MinPeakCount)
            {
                return $"peak count {measurement.PeakCount} < min {options.MinPeakCount}";
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
