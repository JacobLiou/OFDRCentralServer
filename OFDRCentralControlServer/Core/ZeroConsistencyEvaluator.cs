using OFDRCentralControlServer.Models;

namespace OFDRCentralControlServer.Core
{
    public record ZeroConsistencyOptions(
        int MinPeakCount = 3,
        int RetestCount = 1,
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
    /// 归零结果轻量判定（缓解超时）：
    /// 1) 帧级坏帧：未找到峰、峰位/峰强数量不齐、峰数量低于 MinPeakCount、选中峰越出扫描窗口；
    /// 2) 首次不可疑直接采纳；坏帧后下一次不可疑也直接采纳（不做位置/强度二次收敛）；
    /// 3) history jump / 门限裕度 / dB 收敛仅保留配置兼容，不再用于拒绝。
    /// </summary>
    public static class ZeroConsistencyEvaluator
    {
        public static ZeroMeasurementEvaluation Evaluate(
            ZeroMeasurement current,
            ZeroMeasurementEvaluation? previous,
            ClientMeasurementHistory? lastZeroHistory,
            ZeroConsistencyOptions options)
        {
            _ = lastZeroHistory; // 保留参数签名兼容；历史跳变不再拒绝

            var suspiciousReason = AssessSuspiciousReason(current, options);
            if (suspiciousReason != null)
            {
                return new ZeroMeasurementEvaluation(current, IsSuspicious: true, suspiciousReason, IsConverged: false, ShouldAccept: false);
            }

            // 首次不可疑，或坏帧后下一次够峰：直接采纳（不再要求二次收敛）
            return new ZeroMeasurementEvaluation(
                current,
                IsSuspicious: false,
                SuspiciousReason: null,
                IsConverged: previous is { IsSuspicious: false },
                ShouldAccept: true);
        }

        private static string? AssessSuspiciousReason(
            ZeroMeasurement measurement,
            ZeroConsistencyOptions options)
            => PeakFrameGate.AssessRejectionReason(
                measurement.PeakCount,
                measurement.PeakDbCount,
                measurement.SelectedPosition,
                options.MinPeakCount,
                options.WindowStartM,
                options.WindowEndM,
                requirePositiveSelectedValue: true);
    }
}
