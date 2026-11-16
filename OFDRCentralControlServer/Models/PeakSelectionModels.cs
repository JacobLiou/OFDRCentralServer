namespace OFDRCentralControlServer.Models
{
    public record PeakCandidate(
        int Index,
        double Position,
        double Db,
        double DeltaFromZero);

    public record ScanCandidateSet(
        double ZeroLength,
        IReadOnlyList<PeakCandidate> Candidates,
        int RawPositionsCount,
        int RawDbCount);

    public record ClientMeasurementHistory(
        int ClientId,
        string Mode,
        double LengthM,
        double PeakDb,
        DateTime RecordedAt);

    public record ZeroMeasurement(
        double SelectedPosition,
        double SelectedDb,
        int PeakCount);
}