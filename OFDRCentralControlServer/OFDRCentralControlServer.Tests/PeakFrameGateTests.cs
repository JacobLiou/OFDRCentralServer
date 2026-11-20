using OFDRCentralControlServer.Core;

namespace OFDRCentralControlServer.Tests;

public class PeakFrameGateTests
{
    [Fact]
    public void IsAcceptable_ThreePeaks_IsTrue()
    {
        Assert.True(PeakFrameGate.IsAcceptable(
            peakCount: 3,
            peakDbCount: 3,
            selectedValue: 58.236,
            minPeakCount: 3,
            windowStartM: 0.0,
            windowEndM: 100.0));
    }

    [Fact]
    public void AssessRejectionReason_SinglePeak_IsRejected()
    {
        var reason = PeakFrameGate.AssessRejectionReason(
            peakCount: 1,
            peakDbCount: 1,
            selectedValue: 60.669,
            minPeakCount: 3);

        Assert.Contains("peak count", reason);
    }

    [Fact]
    public void AssessRejectionReason_TwoPeaksBelowMinThree_IsRejected()
    {
        var reason = PeakFrameGate.AssessRejectionReason(
            peakCount: 2,
            peakDbCount: 2,
            selectedValue: 58.236,
            minPeakCount: 3);

        Assert.Contains("peak count 2 < min 3", reason);
    }

    [Fact]
    public void AssessRejectionReason_Mismatch_IsRejected()
    {
        var reason = PeakFrameGate.AssessRejectionReason(
            peakCount: 4,
            peakDbCount: 3,
            selectedValue: 58.236,
            minPeakCount: 3);

        Assert.Contains("peak count mismatch", reason);
    }

    [Fact]
    public void AssessRejectionReason_NoPeak_IsRejected()
    {
        var reason = PeakFrameGate.AssessRejectionReason(
            peakCount: 0,
            peakDbCount: 0,
            selectedValue: -1,
            minPeakCount: 3);

        Assert.Equal("no peak found", reason);
    }

    [Fact]
    public void AssessRejectionReason_OutsideWindow_IsRejected()
    {
        var reason = PeakFrameGate.AssessRejectionReason(
            peakCount: 4,
            peakDbCount: 4,
            selectedValue: 101.5,
            minPeakCount: 3,
            windowStartM: 0.0,
            windowEndM: 100.0);

        Assert.Contains("outside scan window", reason);
    }

    [Fact]
    public void AssessRejectionReason_ScanLengthWithoutWindow_AcceptsPositiveLength()
    {
        var reason = PeakFrameGate.AssessRejectionReason(
            peakCount: 3,
            peakDbCount: 3,
            selectedValue: 12.5,
            minPeakCount: 3,
            windowStartM: null,
            windowEndM: null,
            requirePositiveSelectedValue: true);

        Assert.Null(reason);
    }
}
