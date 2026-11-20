using OFDRCentralControlServer.Devices;
using OFDRCentralControlServer.Models;
using Serilog;
using System.Text.Json;
using System.Threading.Channels;

namespace OFDRCentralControlServer.Core
{
    public class MeasurementWorker
    {
        private readonly Channel<MeasureTask> _queue;

        private readonly TcpServer _server;

        private readonly FlaInstrumentAdapter _fla;

        private readonly OpticalSwitchController _switch;

        private readonly SemaphoreSlim _deviceLock = new(1, 1);

        private static readonly Random _jitter = new Random();

        // 切开关最多重试 3 次
        private const int RETRY_SWITCH_MAX = 3;

        // FLA 连接最多重试 3 次
        private const int RETRY_CONNECT_MAX = 3;

        // SR/G/WR/X 设置最多重试 3 次
        private const int RETRY_SET_MAX = 3;

        // SCAN/AutoPeak 测量最多重试 3 次
        private const int RETRY_MEASURE_MAX = 3;

        private static readonly TimeSpan BASE_DELAY_SWITCH = TimeSpan.FromMilliseconds(200);

        private static readonly TimeSpan BASE_DELAY_CONNECT = TimeSpan.FromMilliseconds(300);

        private static readonly TimeSpan BASE_DELAY_SET = TimeSpan.FromMilliseconds(150);

        private static readonly TimeSpan BASE_DELAY_MEASURE = TimeSpan.FromMilliseconds(300);

        private readonly string _scanPeakSelectionStrategy;

        private readonly int _scanPredictiveRetestCount;

        private readonly double _scanPredictiveHistoryJumpThresholdM;

        private readonly double _scanPredictiveConvergenceToleranceM;

        private readonly double _scanPredictiveTailPreferenceWeight;

        private readonly ZeroConsistencyOptions _zeroConsistency;

        private readonly ILogger Log = Serilog.Log.ForContext<MeasurementWorker>();

        public MeasurementWorker(
            Channel<MeasureTask> queue,
            TcpServer server,
            FlaInstrumentAdapter adapter,
            OpticalSwitchController sw,
            string? scanPeakSelectionStrategy = null,
            int scanPredictiveRetestCount = 1,
            double scanPredictiveHistoryJumpThresholdM = 25.0,
            double scanPredictiveConvergenceToleranceM = 2.0,
            double scanPredictiveTailPreferenceWeight = 12.0,
            ZeroConsistencyOptions? zeroConsistency = null)
        {
            _queue = queue;
            _server = server;
            _fla = adapter;
            _switch = sw;
            _scanPeakSelectionStrategy = string.IsNullOrWhiteSpace(scanPeakSelectionStrategy)
                ? "last-peak"
                : scanPeakSelectionStrategy.Trim().ToLowerInvariant();
            _scanPredictiveRetestCount = Math.Max(0, scanPredictiveRetestCount);
            _scanPredictiveHistoryJumpThresholdM = Math.Max(0.5, scanPredictiveHistoryJumpThresholdM);
            _scanPredictiveConvergenceToleranceM = Math.Max(0.1, scanPredictiveConvergenceToleranceM);
            _scanPredictiveTailPreferenceWeight = Math.Max(0, scanPredictiveTailPreferenceWeight);
            _zeroConsistency = zeroConsistency ?? new ZeroConsistencyOptions();
        }

        public async Task StartAsync(CancellationToken ct)
        {
            Log.Information("Worker started");

            while (!ct.IsCancellationRequested)
            {
                MeasureTask task;
                try { task = await _queue.Reader.ReadAsync(ct); }
                catch (OperationCanceledException) { break; }

                await _deviceLock.WaitAsync(ct);
                try
                {
                    Log.Information("Task start {TaskId} ClientId={ClientId} mode={Mode}", task.TaskId, task.ClientId, task.Mode);

                    // 标记为运行中并通知客户端
                    RunningTaskTracker.Instance.MarkRunning(task.TaskId);
                    //await _server.SendResultAsync(task, new ResultMessage("result", task.TaskId, status: "running"), ct);

                    await RetryAsync(
                        async () => await _switch.ConnectAsync(),
                        "switch",
                        RETRY_SWITCH_MAX,
                        BASE_DELAY_SWITCH,
                        ct,
                        Log
                    );

                    // 切光开关 —— 带重试
                    await RetryAsync(
                        async () => await _switch.SwitchToOutputMSWAsync(task.ClientId, ct),
                        "switch",
                        RETRY_SWITCH_MAX,
                        BASE_DELAY_SWITCH,
                        ct,
                        Log,
                        failDetail: $"channel={task.ClientId}"
                    );
                    Log.Information("Switch set to {Channel} for task {TaskId}", task.ClientId, task.TaskId);

                    var result = new ResultMessage("result", task.TaskId, status: ResultStatus.running.ToString(), data: null, error: null);
                    object data;
                    if (task.Mode.Equals("scan", StringComparison.OrdinalIgnoreCase))
                    {
                        double zero_length = ParseDouble(task.Params.GetValueOrDefault("zero_length", "5"), 5);

                        var scanOutcome = _scanPeakSelectionStrategy.Equals("scored-candidate", StringComparison.OrdinalIgnoreCase)
                            ? await ExecutePredictiveScanAsync(task.ClientId, zero_length, ct)
                            : await ExecuteFixedStrategyScanAsync(zero_length, ct);

                        if (scanOutcome.Success)
                        {
                            data = new
                            {
                                clientId = task.ClientId,
                                mode = task.Mode,
                                length_m = scanOutcome.LengthM,
                                peak_db = scanOutcome.PeakDb
                            };
                            result = new ResultMessage("result", task.TaskId, status: ResultStatus.success.ToString(), data: data, error: null);
                            HourlyResultStore.Instance.RecordSuccessfulMeasurement(task.ClientId, task.Mode, scanOutcome.LengthM, scanOutcome.PeakDb);
                        }
                        else
                        {
                            result = new ResultMessage("result", task.TaskId, status: ResultStatus.failed.ToString(), data: null, error: scanOutcome.Error ?? "FLA scan error reuslt < 0");
                        }

                        Log.Debug(JsonSerializer.Serialize(result));
                        Log.Information(
                            "FLA scan done for task {TaskId}: length_m={Length} peak_db={PeakDb} attempts={Attempts} success={Success} error={Error}",
                            task.TaskId,
                            scanOutcome.LengthM,
                            scanOutcome.PeakDb,
                            scanOutcome.Attempts,
                            scanOutcome.Success,
                            scanOutcome.Error);
                    }
                    else if (task.Mode.Equals("zero", StringComparison.OrdinalIgnoreCase))
                    {
                        // 执行归零：峰数不足等坏帧最多补测 1 次，够峰即直接采纳
                        var zeroOutcome = await ExecuteZeroWithVerificationAsync(task.ClientId, ct);

                        if (zeroOutcome.Success)
                        {
                            data = new
                            {
                                clientId = task.ClientId,
                                mode = task.Mode,
                                length_m = zeroOutcome.LengthM,
                                peak_db = zeroOutcome.PeakDb
                            };
                            result = new ResultMessage("result", task.TaskId, status: ResultStatus.success.ToString(), data: data, error: null);
                            HourlyResultStore.Instance.RecordSuccessfulMeasurement(task.ClientId, task.Mode, zeroOutcome.LengthM, zeroOutcome.PeakDb);
                        }
                        else
                        {
                            result = new ResultMessage("result", task.TaskId, status: ResultStatus.failed.ToString(), data: null, error: zeroOutcome.Error ?? "FLA zero error reuslt < 0");
                        }

                        Log.Debug(JsonSerializer.Serialize(result));
                        Log.Information(
                            "FLA zero done for task {TaskId}: zero_data=({LengthM}, {PeakDb}) attempts={Attempts} success={Success} measurements=[{Measurements}]",
                            task.TaskId,
                            zeroOutcome.LengthM,
                            zeroOutcome.PeakDb,
                            zeroOutcome.Attempts,
                            zeroOutcome.Success,
                            string.Join("; ", zeroOutcome.Measurements.Select(static m => $"{m.SelectedPosition:F3}m@{m.PeakCount}pk/{m.SelectedDb:F1}dB")));
                    }
                    else
                    {
                        throw new Exception($"unknown mode {task.Mode}");
                    }

                    //await _server.SendResultAsync(task, result, ct);
                    Log.Information("Task success {TaskId}", task.TaskId);
                    HourlyResultStore.Instance.AddOrUpdate(task.TaskId, result);
                    // 标记完成（脱离 running）
                    RunningTaskTracker.Instance.MarkFinished(task.TaskId);
                }
                catch (Exception ex)
                {
                    // 最终失败返回（status = complete, success=false）
                    var failResult = new ResultMessage("result", task.TaskId, status: ResultStatus.failed.ToString(), data: null, error: ex.Message.Substring(0, 128));
                    Log.Error(JsonSerializer.Serialize(failResult));
                    //await _server.SendResultAsync(task, failResult, ct);
                    HourlyResultStore.Instance.AddOrUpdate(task.TaskId, failResult);
                    // 标记完成（即使失败也不在 running）
                    RunningTaskTracker.Instance.MarkFinished(task.TaskId);
                    Log.Error(ex, "Task failed {TaskId}", task.TaskId);
                }
                finally
                {
                    _deviceLock.Release();
                }
            }

            Log.Information("Worker stopped");
        }

        /// <summary>
        /// 重试 策略助手（无返回值）
        /// </summary>
        /// <param name="action"></param>
        /// <param name="opName"></param>
        /// <param name="maxAttempts"></param>
        /// <param name="baseDelay"></param>
        /// <param name="ct"></param>
        /// <param name="log"></param>
        /// <param name="failDetail"></param>
        /// <returns></returns>
        private static async Task RetryAsync(
            Func<Task> action,
            string opName,
            int maxAttempts,
            TimeSpan baseDelay,
            CancellationToken ct,
            ILogger log,
            string? failDetail = null)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await action();
                    return; // 成功
                }
                catch (OperationCanceledException)
                {
                    throw; // 尊重取消，不重试
                }
                catch (Exception ex)
                {
                    if (attempt >= maxAttempts)
                    {
                        // 最终失败
                        log.Error(ex, "[Retry:{Op}] failed after {Attempts} attempts{Detail}",
                                  opName, attempt,
                                  string.IsNullOrEmpty(failDetail) ? "" : $" ({failDetail})");
                        throw;
                    }

                    // 记录重试警告
                    log.Warning(ex, "[Retry:{Op}] attempt {Attempt}/{Max} failed{Detail}",
                                opName, attempt, maxAttempts,
                                string.IsNullOrEmpty(failDetail) ? "" : $" ({failDetail})");

                    // 指数退避 + 抖动
                    var delayMs = (int)(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    delayMs += _jitter.Next(0, Math.Max(20, delayMs / 4)); // jitter
                    var delay = TimeSpan.FromMilliseconds(delayMs);

                    await Task.Delay(delay, ct);
                }
            }
        }

        /// <summary>
        /// 重试策略助手（有返回值）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="opName"></param>
        /// <param name="maxAttempts"></param>
        /// <param name="baseDelay"></param>
        /// <param name="ct"></param>
        /// <param name="log"></param>
        /// <param name="failDetail"></param>
        /// <returns></returns>
        private static async Task<T> RetryAsync<T>(
            Func<Task<T>> action,
            string opName,
            int maxAttempts,
            TimeSpan baseDelay,
            CancellationToken ct,
            ILogger log,
            string? failDetail = null)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    var result = await action();
                    return result; // 成功
                }
                catch (OperationCanceledException)
                {
                    throw; // 尊重取消，不重试
                }
                catch (Exception ex)
                {
                    if (attempt >= maxAttempts)
                    {
                        log.Error(ex, "[Retry:{Op}] failed after {Attempts} attempts{Detail}",
                                  opName, attempt,
                                  string.IsNullOrEmpty(failDetail) ? "" : $" ({failDetail})");
                        throw;
                    }

                    log.Warning(ex, "[Retry:{Op}] attempt {Attempt}/{Max} failed{Detail}",
                                opName, attempt, maxAttempts,
                                string.IsNullOrEmpty(failDetail) ? "" : $" ({failDetail})");

                    var delayMs = (int)(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    delayMs += _jitter.Next(0, Math.Max(20, delayMs / 4)); // jitter
                    var delay = TimeSpan.FromMilliseconds(delayMs);

                    await Task.Delay(delay, ct);
                }
            }
        }

        /// <summary>
        /// 固定策略扫描 + 三峰门禁：峰数不足或长度无效时最多补测 1 次，够峰且 length>0 即采纳。
        /// </summary>
        private async Task<ScanExecutionOutcome> ExecuteFixedStrategyScanAsync(double zeroLength, CancellationToken ct)
        {
            var options = _zeroConsistency;
            var totalAttempts = 1 + Math.Max(0, options.RetestCount);
            var summaries = new List<string>(totalAttempts);

            for (var attempt = 1; attempt <= totalAttempts; attempt++)
            {
                if (attempt > 1 && options.RetestDelayMs > 0)
                {
                    await Task.Delay(options.RetestDelayMs, ct);
                }

                var res = await RetryAsync(
                    async () => await _fla.ScanLengthAsync(zeroLength, ct),
                    "scan",
                    RETRY_MEASURE_MAX,
                    BASE_DELAY_MEASURE,
                    ct,
                    Log
                );

                summaries.Add($"{res.Scan_Len:F3}m@{res.PeakCount}pk/{res.Scan_Db:F1}dB");

                var rejection = PeakFrameGate.AssessRejectionReason(
                    res.PeakCount,
                    res.PeakDbCount,
                    res.Scan_Len,
                    options.MinPeakCount,
                    windowStartM: null,
                    windowEndM: null,
                    requirePositiveSelectedValue: true);

                Log.Information(
                    "Scan peak-gate attempt {Attempt}/{TotalAttempts}: length={Length} db={Db} peaks={PeakCount}/{PeakDbCount} rejected={Rejected} reason={Reason}",
                    attempt,
                    totalAttempts,
                    res.Scan_Len,
                    res.Scan_Db,
                    res.PeakCount,
                    res.PeakDbCount,
                    rejection != null,
                    rejection ?? "");

                if (rejection == null)
                {
                    if (attempt > 1)
                    {
                        Log.Warning(
                            "Scan accepted after retest: attempts={Attempts} length={Length} peaks={PeakCount}",
                            attempt,
                            res.Scan_Len,
                            res.PeakCount);
                    }

                    return new ScanExecutionOutcome(true, res.Scan_Len, res.Scan_Db, null, attempt);
                }
            }

            return new ScanExecutionOutcome(
                false,
                -1d,
                0d,
                $"Scan insufficient peaks after retest: [{string.Join("; ", summaries)}]",
                totalAttempts);
        }

        /// <summary>
        /// 归零测量 + 三峰门禁：
        /// 首次坏帧（峰数量不足、无峰、数量不齐、越窗）时最多补测 1 次；下一次够峰即直接采纳。
        /// </summary>
        private async Task<ZeroExecutionOutcome> ExecuteZeroWithVerificationAsync(int clientId, CancellationToken ct)
        {
            HourlyResultStore.Instance.TryGetLastZeroHistory(clientId, out var lastZeroHistory);
            var options = _zeroConsistency;
            var totalAttempts = 1 + Math.Max(0, options.RetestCount);

            var measurements = new List<ZeroMeasurement>(totalAttempts);
            ZeroMeasurementEvaluation? previousEvaluation = null;

            for (var attempt = 1; attempt <= totalAttempts; attempt++)
            {
                if (attempt > 1 && options.RetestDelayMs > 0)
                {
                    await Task.Delay(options.RetestDelayMs, ct);
                }

                var zero = await RetryAsync(
                    async () => await _fla.ZeroLengthAsync(ct),
                    "zero",
                    RETRY_MEASURE_MAX,
                    BASE_DELAY_MEASURE,
                    ct,
                    Log
                );

                var measurement = new ZeroMeasurement(zero.Zero_Len, zero.Zero_Db, zero.PeakCount, zero.PeakDbCount);
                measurements.Add(measurement);

                var evaluation = ZeroConsistencyEvaluator.Evaluate(measurement, previousEvaluation, lastZeroHistory, options);

                Log.Information(
                    "Zero consistency attempt {Attempt}/{TotalAttempts}: clientId={ClientId} pos={Position} db={Db} peaks={PeakCount}/{PeakDbCount} suspicious={Suspicious} reason={Reason} converged={Converged} accepted={Accepted}",
                    attempt,
                    totalAttempts,
                    clientId,
                    measurement.SelectedPosition,
                    measurement.SelectedDb,
                    measurement.PeakCount,
                    measurement.PeakDbCount,
                    evaluation.IsSuspicious,
                    evaluation.SuspiciousReason ?? "",
                    evaluation.IsConverged,
                    evaluation.ShouldAccept);

                if (evaluation.ShouldAccept)
                {
                    if (attempt > 1)
                    {
                        Log.Warning(
                            "Zero accepted after retest: clientId={ClientId} attempts={Attempts} previousPos={PreviousPos} currentPos={CurrentPos} peaks={PeakCount}",
                            clientId,
                            attempt,
                            previousEvaluation?.Measurement.SelectedPosition,
                            measurement.SelectedPosition,
                            measurement.PeakCount);
                    }

                    return new ZeroExecutionOutcome(true, measurement.SelectedPosition, measurement.SelectedDb, attempt, measurements, null);
                }

                previousEvaluation = evaluation;
            }

            var summary = string.Join("; ", measurements.Select(static m => $"{m.SelectedPosition:F3}m@{m.PeakCount}pk/{m.SelectedDb:F1}dB"));
            return new ZeroExecutionOutcome(
                false,
                -1d,
                0d,
                totalAttempts,
                measurements,
                $"Zero insufficient peaks after retest: [{summary}]");
        }

        private async Task<ScanExecutionOutcome> ExecutePredictiveScanAsync(int clientId, double zeroLength, CancellationToken ct)
        {
            HourlyResultStore.Instance.TryGetLastScanHistory(clientId, out var lastScanHistory);

            var peakGateAttempts = 1 + Math.Max(0, _zeroConsistency.RetestCount);
            var scoringAttempts = 1 + _scanPredictiveRetestCount;
            var totalAttempts = Math.Max(peakGateAttempts, scoringAttempts);

            PredictiveScanEvaluation? previousAttempt = null;
            var peakSummaries = new List<string>(totalAttempts);
            string? lastPeakRejection = null;

            for (var attempt = 1; attempt <= totalAttempts; attempt++)
            {
                if (attempt > 1 && _zeroConsistency.RetestDelayMs > 0 && lastPeakRejection != null)
                {
                    await Task.Delay(_zeroConsistency.RetestDelayMs, ct);
                }

                var candidateSet = await RetryAsync(
                    async () => await _fla.GetScanCandidateSetAsync(zeroLength, ct)
                        ?? throw new InvalidOperationException("FLA scan returned no candidate set."),
                    "scan-candidates",
                    RETRY_MEASURE_MAX,
                    BASE_DELAY_MEASURE,
                    ct,
                    Log
                );

                peakSummaries.Add($"{candidateSet.RawPositionsCount}pk/{candidateSet.RawDbCount}db");

                var peakRejection = PeakFrameGate.AssessRejectionReason(
                    candidateSet.RawPositionsCount,
                    candidateSet.RawDbCount,
                    selectedValue: 1d, // 峰数门禁优先；选中值由评分逻辑再校验
                    _zeroConsistency.MinPeakCount,
                    windowStartM: null,
                    windowEndM: null,
                    requirePositiveSelectedValue: false);

                lastPeakRejection = peakRejection;

                Log.Information(
                    "Predictive scan peak-gate attempt {Attempt}/{TotalAttempts}: clientId={ClientId} peaks={PeakCount}/{PeakDbCount} rejected={Rejected} reason={Reason}",
                    attempt,
                    totalAttempts,
                    clientId,
                    candidateSet.RawPositionsCount,
                    candidateSet.RawDbCount,
                    peakRejection != null,
                    peakRejection ?? "");

                if (peakRejection != null)
                {
                    if (attempt >= peakGateAttempts)
                    {
                        return new ScanExecutionOutcome(
                            false,
                            -1d,
                            0d,
                            $"Scan insufficient peaks after retest: [{string.Join("; ", peakSummaries)}]",
                            attempt);
                    }

                    continue;
                }

                var evaluation = PredictiveScanSelector.EvaluateCandidates(candidateSet, lastScanHistory, BuildPredictiveScanOptions());

                Log.Information(
                    "Predictive scan attempt {Attempt}/{TotalAttempts}: clientId={ClientId} bestDelta={BestDelta} bestDb={BestDb} bestScore={BestScore} margin={ScoreMargin} lowConfidence={LowConfidence} historyJump={HistoryJump} positiveCandidates={PositiveCandidates}",
                    attempt,
                    totalAttempts,
                    clientId,
                    evaluation.Best.Candidate.DeltaFromZero,
                    evaluation.Best.Candidate.Db,
                    evaluation.Best.Score,
                    evaluation.ScoreMargin,
                    evaluation.IsLowConfidence,
                    evaluation.TriggeredByHistoryJump,
                    evaluation.PositiveCandidateCount
                );

                if (evaluation.Best.Candidate.DeltaFromZero <= 0)
                {
                    lastPeakRejection = "no peak found";
                    if (attempt >= peakGateAttempts)
                    {
                        return new ScanExecutionOutcome(
                            false,
                            evaluation.Best.Candidate.DeltaFromZero,
                            evaluation.Best.Candidate.Db,
                            $"Scan insufficient peaks after retest: [{string.Join("; ", peakSummaries)}]",
                            attempt);
                    }

                    continue;
                }

                if (!evaluation.IsLowConfidence)
                {
                    return new ScanExecutionOutcome(true, evaluation.Best.Candidate.DeltaFromZero, evaluation.Best.Candidate.Db, null, attempt);
                }

                if (previousAttempt != null
                    && Math.Abs(previousAttempt.Best.Candidate.DeltaFromZero - evaluation.Best.Candidate.DeltaFromZero) <= _scanPredictiveConvergenceToleranceM)
                {
                    Log.Warning(
                        "Predictive scan accepted after convergence: clientId={ClientId} previousDelta={PreviousDelta} currentDelta={CurrentDelta} tolerance={Tolerance}",
                        clientId,
                        previousAttempt.Best.Candidate.DeltaFromZero,
                        evaluation.Best.Candidate.DeltaFromZero,
                        _scanPredictiveConvergenceToleranceM
                    );

                    return new ScanExecutionOutcome(true, evaluation.Best.Candidate.DeltaFromZero, evaluation.Best.Candidate.Db, null, attempt);
                }

                previousAttempt = evaluation;
                lastPeakRejection = null;
            }

            var reason = previousAttempt == null
                ? "Predictive scan did not produce usable candidates"
                : $"Predictive scan low confidence: bestDelta={previousAttempt.Best.Candidate.DeltaFromZero:F3}, margin={previousAttempt.ScoreMargin:F2}, historyJump={previousAttempt.TriggeredByHistoryJump}";

            return new ScanExecutionOutcome(
                false,
                previousAttempt?.Best.Candidate.DeltaFromZero ?? -1d,
                previousAttempt?.Best.Candidate.Db ?? 0d,
                reason,
                totalAttempts);
        }

        private PredictiveScanOptions BuildPredictiveScanOptions()
            => new(
                _scanPredictiveHistoryJumpThresholdM,
                _scanPredictiveConvergenceToleranceM,
                _scanPredictiveTailPreferenceWeight,
                8d,
                _scanPredictiveHistoryJumpThresholdM,
                12d);

        private static double ParseDouble(string s, double def)
            => double.TryParse(s, out var v) ? v : def;

        private sealed record ScanExecutionOutcome(bool Success, double LengthM, double PeakDb, string? Error, int Attempts);

        private sealed record ZeroExecutionOutcome(
            bool Success,
            double LengthM,
            double PeakDb,
            int Attempts,
            IReadOnlyList<ZeroMeasurement> Measurements,
            string? Error);

    }
}