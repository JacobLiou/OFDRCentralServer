using OFDRCentralControlServer.Models;

namespace OFDRCentralControlServer.Core
{
    public record PredictiveScanOptions(
        double HistoryJumpThresholdM,
        double ConvergenceToleranceM,
        double TailPreferenceWeight,
        double LowConfidenceMargin,
        double WideSpreadThresholdM,
        double WideSpreadMarginThreshold);

    public record PredictiveScanEvaluation(
        ScoredPeakCandidate Best,
        ScoredPeakCandidate? RunnerUp,
        int PositiveCandidateCount,
        bool IsLowConfidence,
        bool TriggeredByHistoryJump,
        double ScoreMargin,
        double CandidateSpread);

    public record ScoredPeakCandidate(
        PeakCandidate Candidate,
        double Score,
        double DbNormalized,
        double HistoryDistance,
        double TailBiasNormalized);

    public static class PredictiveScanSelector
    {
        public static PredictiveScanEvaluation EvaluateCandidates(
            ScanCandidateSet candidateSet,
            ClientMeasurementHistory? lastScanHistory,
            PredictiveScanOptions options)
        {
            var positiveCandidates = candidateSet.Candidates
                .Where(static candidate => candidate.DeltaFromZero > 0)
                .ToArray();

            if (positiveCandidates.Length == 0)
            {
                throw new InvalidOperationException("Predictive scan found no positive-delta candidates.");
            }

            var minDb = positiveCandidates.Min(static candidate => candidate.Db);
            var maxDb = positiveCandidates.Max(static candidate => candidate.Db);

            var scoredCandidates = positiveCandidates
                .Select((candidate, visibleIndex) => ScoreCandidate(candidate, visibleIndex, positiveCandidates.Length, lastScanHistory, minDb, maxDb, options))
                .OrderByDescending(static scored => scored.Score)
                .ToArray();

            var best = scoredCandidates[0];
            var runnerUp = scoredCandidates.Length > 1 ? scoredCandidates[1] : null;
            var scoreMargin = runnerUp == null ? 100d : best.Score - runnerUp.Score;
            var candidateSpread = positiveCandidates.Max(static candidate => candidate.DeltaFromZero)
                - positiveCandidates.Min(static candidate => candidate.DeltaFromZero);
            var historyJump = lastScanHistory != null
                && Math.Abs(best.Candidate.DeltaFromZero - lastScanHistory.LengthM) > options.HistoryJumpThresholdM;
            var noHistoryWideSpread = lastScanHistory == null
                && positiveCandidates.Length > 1
                && candidateSpread > options.WideSpreadThresholdM
                && scoreMargin < options.WideSpreadMarginThreshold;
            var isLowConfidence = scoreMargin < options.LowConfidenceMargin || historyJump || noHistoryWideSpread;

            return new PredictiveScanEvaluation(best, runnerUp, positiveCandidates.Length, isLowConfidence, historyJump, scoreMargin, candidateSpread);
        }

        private static ScoredPeakCandidate ScoreCandidate(
            PeakCandidate candidate,
            int visibleIndex,
            int totalCandidates,
            ClientMeasurementHistory? lastScanHistory,
            double minDb,
            double maxDb,
            PredictiveScanOptions options)
        {
            var score = 0d;
            var historyDistance = lastScanHistory == null
                ? double.NaN
                : Math.Abs(candidate.DeltaFromZero - lastScanHistory.LengthM);

            if (lastScanHistory != null)
            {
                score += Math.Max(0d, 120d - historyDistance * 8d);
            }
            else
            {
                score += Math.Max(0d, 30d - candidate.DeltaFromZero * 0.12d);
            }

            score += 10d / (1d + candidate.DeltaFromZero);

            var dbNormalized = Math.Abs(maxDb - minDb) < 0.001d
                ? 0.5d
                : (candidate.Db - minDb) / (maxDb - minDb);
            score += dbNormalized * 25d;

            var tailBiasNormalized = totalCandidates <= 1
                ? 1d
                : (double)visibleIndex / (totalCandidates - 1);
            score += tailBiasNormalized * options.TailPreferenceWeight;

            return new ScoredPeakCandidate(candidate, score, dbNormalized, historyDistance, tailBiasNormalized);
        }
    }
}