namespace OFDRCentralControlServer.Core
{
    /// <summary>
    /// 峰帧轻量门禁（zero/scan 共用）：正常帧至少 MinPeakCount 个峰；
    /// 无峰、峰位/峰强数量不齐、峰数不足、可选窗口越界视为坏帧。
    /// </summary>
    public static class PeakFrameGate
    {
        public static string? AssessRejectionReason(
            int peakCount,
            int peakDbCount,
            double selectedValue,
            int minPeakCount,
            double? windowStartM = null,
            double? windowEndM = null,
            bool requirePositiveSelectedValue = true)
        {
            if (requirePositiveSelectedValue && selectedValue <= 0)
            {
                return "no peak found";
            }

            if (peakCount != peakDbCount)
            {
                return $"peak count mismatch: positions={peakCount} dbs={peakDbCount}";
            }

            if (peakCount < minPeakCount)
            {
                return $"peak count {peakCount} < min {minPeakCount}";
            }

            if (windowStartM.HasValue
                && windowEndM.HasValue
                && (selectedValue < windowStartM.Value || selectedValue > windowEndM.Value))
            {
                return $"selected position {selectedValue} outside scan window [{windowStartM.Value}, {windowEndM.Value}]";
            }

            return null;
        }

        public static bool IsAcceptable(
            int peakCount,
            int peakDbCount,
            double selectedValue,
            int minPeakCount,
            double? windowStartM = null,
            double? windowEndM = null,
            bool requirePositiveSelectedValue = true)
            => AssessRejectionReason(
                peakCount,
                peakDbCount,
                selectedValue,
                minPeakCount,
                windowStartM,
                windowEndM,
                requirePositiveSelectedValue) == null;
    }
}
