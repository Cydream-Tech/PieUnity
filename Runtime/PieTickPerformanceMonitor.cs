using System;
using System.Diagnostics;
using System.Text;

namespace Pie
{
    /// <summary>
    /// Default-off bridge aggregates and bounded native-frame evidence. Runner stages
    /// are disjoint; six bridge segments are nested inside the bridge stage.
    /// Dispatcher can invoke JS too: these segments do not cover all JS work.
    /// Reports allocate on demand; enabled sampling uses fixed storage, no logs.
    /// </summary>
    public sealed class PieTickPerformanceMonitor : IDisposable
    {
        // Segment indices are a public contract with PieBridge.Tick; keep the
        // order and count in sync with SegmentCount.
        public const int SegmentPump = 0;
        public const int SegmentSse = 1;
        public const int SegmentFile = 2;
        public const int SegmentHost = 3;
        public const int SegmentReady = 4;
        public const int SegmentJs = 5;
        public const int SegmentCount = 6;

        private static readonly string[] SegmentNames = { "pump", "sse", "file", "host", "ready", "js" };

        // ticks < 2^i land in bucket i; the final bucket absorbs everything
        // above the table (a >1.6s segment is already a pathological frame).
        private const int BucketCount = 24;
        private static readonly long[] BucketFloors = BuildBucketFloors();

        private readonly long[][] _buckets = CreateBuckets();
        private readonly long[] _segmentTotals = new long[SegmentCount];
        private readonly long[] _segmentMax = new long[SegmentCount];
        private readonly long[] _segmentSamples = new long[SegmentCount];

        private readonly ISource _source;
        private long _segmentStart, _tickStart;
        private bool _tickActive, _tickFrameAttached;
        public PieTickPerformanceMonitor() : this(new UnitySource()) { }
        public PieTickPerformanceMonitor(ISource source) { _source = source; }

        private int _activeSegment = -1;
        private long _totalTicks;
        private long _totalTickTicks;
        private long _worstTickTicks;

        public bool IsEnabled { get; private set; }
        public long TotalTicks => _totalTicks;
        public double TotalMilliseconds => TicksToMilliseconds(_totalTickTicks, 1);
        public double WorstTickMilliseconds => TicksToMilliseconds(_worstTickTicks, 1);

        public void SetEnabled(bool enabled)
        {
            if (IsEnabled == enabled) return;
            Fence();
            IsEnabled = enabled;
            if (enabled) _source.Start(); else _source.Dispose();
            PieDiagnostics.Info(enabled
                ? "[PiePerf] Tick performance probes enabled."
                : "[PiePerf] Tick performance probes disabled.");
        }

        public void Reset()
        {
            Fence();
            _retained = 0;
            foreach (var bucket in _buckets) Array.Clear(bucket, 0, bucket.Length);
            Array.Clear(_segmentTotals, 0, _segmentTotals.Length);
            Array.Clear(_segmentMax, 0, _segmentMax.Length);
            Array.Clear(_segmentSamples, 0, _segmentSamples.Length);
            _totalTicks = 0;
            _totalTickTicks = 0;
            _worstTickTicks = 0;
            PieDiagnostics.Info("[PiePerf] Tick performance stats reset.");
        }

        /// <summary>Starts whole-Tick timing. No-op when disabled.</summary>
        public void BeginTick()
        {
            if (!IsEnabled) return;
            _tickActive = true;
            _tickFrameAttached = _frameActive && _source.Frame() == _pending.Frame;
            _tickStart = _source.Timestamp();
        }

        /// <summary>Records whole-Tick total/worst. No-op when disabled.</summary>
        public void EndTick()
        {
            if (!IsEnabled || !_tickActive) return;
            EndSegment();
            var ticks = _source.Timestamp() - _tickStart;
            _tickActive = false;
            if (_tickFrameAttached) _pending.BridgeTicks++;
            _totalTicks++;
            _totalTickTicks += ticks;
            if (ticks > _worstTickTicks) _worstTickTicks = ticks;
        }

        /// <summary>
        /// Begins timing one Tick segment; pair with EndSegment. No-ops when
        /// disabled (single branch), so the probe adds nothing to normal play.
        /// </summary>
        public void BeginSegment(int segmentIndex)
        {
            if (!IsEnabled) return;
            _activeSegment = segmentIndex;
            _segmentStart = _source.Timestamp();
        }

        /// <summary>Records the segment started by BeginSegment.</summary>
        public void EndSegment()
        {
            if (!IsEnabled || _activeSegment < 0) return;
            var elapsed = _source.Timestamp() - _segmentStart;
            RecordSample(_activeSegment, elapsed);
            if (_tickActive && _tickFrameAttached && _activeSegment < SegmentCount)
            {
                _pending.Segments[_activeSegment] += elapsed;
                _pending.SegmentSamples[_activeSegment]++;
            }
            _activeSegment = -1;
        }

        private void RecordSample(int segmentIndex, long ticks)
        {
            if (segmentIndex < 0 || segmentIndex >= SegmentCount) return;
            _buckets[segmentIndex][BucketIndex(ticks)]++;
            _segmentTotals[segmentIndex] += ticks;
            _segmentSamples[segmentIndex]++;
            if (ticks > _segmentMax[segmentIndex]) _segmentMax[segmentIndex] = ticks;
        }

        /// <summary>JSON report for humans and tooling. Empty until samples exist.</summary>
        public string BuildReport()
        {
            if (_totalTicks == 0) return "";

            var sb = new StringBuilder(320);
            sb.Append("{\"ticks\":").Append(_totalTicks)
              .Append(",\"totalMs\":").Append(Format(TotalMilliseconds))
              .Append(",\"avgTickMs\":").Append(Format(TotalMilliseconds / _totalTicks))
              .Append(",\"worstTickMs\":").Append(Format(WorstTickMilliseconds))
              .Append(",\"segments\":[");

            for (var i = 0; i < SegmentCount; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{\"name\":\"").Append(SegmentNames[i]).Append("\"")
                  .Append(",\"avgMs\":").Append(Format(TicksToMilliseconds(_segmentTotals[i], _segmentSamples[i])))
                  .Append(",\"maxMs\":").Append(Format(TicksToMilliseconds(_segmentMax[i], 1)))
                  .Append(",\"p95Ms\":").Append(Format(TicksToMilliseconds(PercentileTicks(i, 0.95), 1)))
                  .Append(",\"p99Ms\":").Append(Format(TicksToMilliseconds(PercentileTicks(i, 0.99), 1)))
                  .Append("}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        // Sources are injectable so tests never depend on Unity scheduling or wall time.
        public interface ISource : IDisposable
        {
            long Frequency { get; }
            long Timestamp();
            int Frame();
            int Collections(int generation);
            long TotalManagedBytes();
            void Start();
            string MarkerName(int index);
            void ReadMarker(int index, out bool valid, out int count, out long value);
        }

        private sealed class UnitySource : ISource
        {
            private static readonly string[] Names = { "Main Thread", "GC.Collect", "GC.Alloc", "Gfx.WaitForPresentOnGfxThread", "WaitForTargetFPS", "PlayerLoop" };
#if UNITY_2020_2_OR_NEWER
            private readonly global::Unity.Profiling.ProfilerRecorder[] _recorders = new global::Unity.Profiling.ProfilerRecorder[6];
#endif
            public long Frequency => Stopwatch.Frequency;
            public long Timestamp() => Stopwatch.GetTimestamp();
            public int Frame() => UnityEngine.Time.frameCount;
            public int Collections(int generation) => GC.CollectionCount(generation);
            // Non-compacting probe read for managed-heap pressure attribution;
            // this is NOT a per-subsystem allocation profiler.
            public long TotalManagedBytes() => GC.GetTotalMemory(false);
            public string MarkerName(int index) => Names[index];
            public void Start()
            {
#if UNITY_2020_2_OR_NEWER
                for (int i = 0; i < 6; i++)
                {
                    try
                    {
                        var category = i == 1 || i == 2 ? global::Unity.Profiling.ProfilerCategory.Memory :
                            i == 3 ? global::Unity.Profiling.ProfilerCategory.Render : global::Unity.Profiling.ProfilerCategory.Internal;
                        _recorders[i] = global::Unity.Profiling.ProfilerRecorder.StartNew(category, Names[i], 1);
                    }
                    catch (Exception) { /* Unavailable marker: report valid=false, not a guessed value. */ }
                }
#endif
            }
            public void ReadMarker(int index, out bool valid, out int count, out long value)
            {
                valid = false; count = 0; value = 0;
#if UNITY_2020_2_OR_NEWER
                var recorder = _recorders[index];
                valid = recorder.Valid;
                if (valid) { count = recorder.Count; if (count > 0) value = recorder.LastValue; }
#endif
            }
            public void Dispose()
            {
#if UNITY_2020_2_OR_NEWER
                for (int i = 0; i < 6; i++) { _recorders[i].Dispose(); _recorders[i] = default; }
#endif
            }
        }

        public const int StageDispatcher = 0, StageHeartbeat = 1, StageBridge = 2;
        public const int FrameCapacity = 64;
        private sealed class FrameEvidence
        {
            public int Frame, Updates, BridgeTicks, StageMask, NativeReadAtFrame;
            public long Start, Interval, Runner;
            public bool RunnerComplete;
            public long ManagedHeapStart, ManagedHeapEnd;
            public readonly long[] Stages = new long[3], Segments = new long[6], Values = new long[6];
            public readonly int[] SegmentSamples = new int[6], Gc = new int[3], Counts = new int[6];
            public readonly bool[] Valid = new bool[6];
            public long Score => Math.Max(Interval, Runner);
            public void Clear()
            {
                Frame = Updates = BridgeTicks = StageMask = NativeReadAtFrame = 0;
                Start = Interval = Runner = 0; RunnerComplete = false;
                ManagedHeapStart = ManagedHeapEnd = 0;
                Array.Clear(Stages, 0, 3); Array.Clear(Segments, 0, 6); Array.Clear(SegmentSamples, 0, 6);
                Array.Clear(Gc, 0, 3); Array.Clear(Counts, 0, 6); Array.Clear(Values, 0, 6); Array.Clear(Valid, 0, 6);
            }
        }
        private FrameEvidence _pending = new FrameEvidence();
        private readonly FrameEvidence[] _frames = CreateFrames();
        private readonly int[] _gcStart = new int[3];
        private int _retained, _stage = -1;
        private bool _frameActive, _runnerActive;
        private long _runnerStart, _stageStart;
        private static FrameEvidence[] CreateFrames()
        {
            var frames = new FrameEvidence[FrameCapacity];
            for (int i = 0; i < frames.Length; i++) frames[i] = new FrameEvidence();
            return frames;
        }
        private void Fence()
        {
            _frameActive = _runnerActive = _tickActive = false;
            _activeSegment = _stage = -1;
            _pending.Clear();
        }
        public void Dispose() { SetEnabled(false); }

        /// <summary>Called before dispatcher; a newly enabled probe starts next Update.</summary>
        public void BeginRunnerFrame()
        {
            if (!IsEnabled) return;
            int frame = _source.Frame();
            long now = _source.Timestamp();
            if (_frameActive && frame != _pending.Frame)
            {
                _pending.Interval = now - _pending.Start;
                _pending.ManagedHeapEnd = _source.TotalManagedBytes();
                for (int i = 0; i < 3; i++) _pending.Gc[i] = _source.Collections(i) - _gcStart[i];
                // End-of-frame values are read at the next Update. The target
                // Player's sleep calibration observes a one-frame lag; retain
                // the read frame explicitly, never assume this on every host.
                _pending.NativeReadAtFrame = frame;
                for (int i = 0; i < 6; i++)
                    _source.ReadMarker(i, out _pending.Valid[i], out _pending.Counts[i], out _pending.Values[i]);
                RetainPending();
                _frameActive = false;
            }
            if (!_frameActive)
            {
                _pending.Clear(); _pending.Frame = frame; _pending.Start = now;
                _pending.ManagedHeapStart = _source.TotalManagedBytes();
                for (int i = 0; i < 3; i++) _gcStart[i] = _source.Collections(i);
                _frameActive = true;
            }
            _pending.Updates++; _runnerStart = now; _runnerActive = true;
        }
        public void BeginRunnerStage(int stage)
        {
            if (!IsEnabled || !_runnerActive) return;
            _stage = stage; _stageStart = _source.Timestamp();
        }
        public void EndRunnerStage()
        {
            if (!IsEnabled || !_runnerActive || _stage < 0) return;
            _pending.Stages[_stage] += _source.Timestamp() - _stageStart;
            _pending.StageMask |= 1 << _stage; _stage = -1;
        }
        public void EndRunnerFrame(bool complete)
        {
            if (!IsEnabled || !_runnerActive) return;
            EndRunnerStage();
            _pending.Runner += _source.Timestamp() - _runnerStart;
            _pending.RunnerComplete = _pending.Updates == 1 ? complete : _pending.RunnerComplete && complete;
            _runnerActive = false;
        }
        private void RetainPending()
        {
            int slot = _retained;
            if (_retained < FrameCapacity) _retained++;
            else
            {
                slot = 0;
                for (int i = 1; i < FrameCapacity; i++) if (_frames[i].Score < _frames[slot].Score) slot = i;
                if (_pending.Score <= _frames[slot].Score) return;
            }
            var spare = _frames[slot]; _frames[slot] = _pending; _pending = spare;
        }

        // Separate report keeps the existing aggregate perf.get_report schema stable.
        // Pending frame is excluded: its Update-to-next-Update interval is not yet known.
        public string BuildFramesReport()
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\"capacity\":64,\"retention\":\"worst max(updateInterval,runnerWall)\",\"intervalAttribution\":\"previous native frame; Update-to-next-distinct-frame-Update\",\"gcAttribution\":\"collection deltas over that interval\",\"stageRelationship\":\"disjoint runner stages; bridgeSegments nested in bridge\",\"nativeAttribution\":\"last-collected values read at next Update; read frame explicit, calibrate lag per host\",\"nativeUnits\":\"raw recorder values; marker units require native calibration, not milliseconds\",\"frames\":[");
            for (int f = 0; f < _retained; f++)
            {
                if (f > 0) sb.Append(',');
                var x = _frames[f];
                sb.Append("{\"nativeFrame\":").Append(x.Frame).Append(",\"updates\":").Append(x.Updates)
                  .Append(",\"bridgeTicks\":").Append(x.BridgeTicks).Append(",\"runnerComplete\":").Append(x.RunnerComplete ? "true" : "false")
                  .Append(",\"stageMask\":").Append(x.StageMask).Append(",\"updateIntervalMs\":").Append(Format(TicksToMilliseconds(x.Interval, 1)))
                  .Append(",\"runnerWallMs\":").Append(Format(TicksToMilliseconds(x.Runner, 1))).Append(",\"runnerStages\":{");
                string[] names = { "dispatcherMs", "heartbeatMs", "bridgeMs" };
                for (int i = 0; i < 3; i++) { if (i > 0) sb.Append(','); sb.Append('"').Append(names[i]).Append("\":").Append(Format(TicksToMilliseconds(x.Stages[i], 1))); }
                sb.Append("},\"bridgeSegments\":[");
                for (int i = 0; i < 6; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"name\":\"").Append(SegmentNames[i]).Append("\",\"ms\":").Append(Format(TicksToMilliseconds(x.Segments[i], 1))).Append(",\"samples\":").Append(x.SegmentSamples[i]).Append('}');
                }
                sb.Append("],\"gcCollectionDeltas\":[");
                for (int i = 0; i < 3; i++) { if (i > 0) sb.Append(','); sb.Append(x.Gc[i]); }
                sb.Append("],\"managedHeap\":{").Append("\"startBytes\":").Append(x.ManagedHeapStart)
                  .Append(",\"endBytes\":").Append(x.ManagedHeapEnd)
                  .Append(",\"deltaBytes\":").Append(x.ManagedHeapEnd - x.ManagedHeapStart)
                  .Append("},\"nativeReadAtFrame\":").Append(x.NativeReadAtFrame).Append(",\"nativeLastCollectedAtNextUpdate\":[");
                for (int i = 0; i < 6; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"name\":"); AppendJsonString(sb, _source.MarkerName(i));
                    sb.Append(",\"valid\":").Append(x.Valid[i] ? "true" : "false").Append(",\"count\":").Append(x.Counts[i]).Append(",\"rawValue\":").Append(x.Values[i]).Append('}');
                }
                sb.Append("]}");
            }
            return sb.Append("]}").ToString();
        }
        private static void AppendJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (char c in value)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                else sb.Append(c);
            }
            sb.Append('"');
        }

        private long PercentileTicks(int segmentIndex, double percentile)
        {
            var buckets = _buckets[segmentIndex];
            long total = 0;
            for (var i = 0; i < buckets.Length; i++) total += buckets[i];
            if (total == 0) return 0;

            // Zero-cost samples (bucket 0) still count toward the rank, which
            // keeps p95 honest on segments that are usually idle.
            var target = (long)Math.Ceiling(total * percentile);
            long cumulative = 0;
            for (var i = 0; i < buckets.Length; i++)
            {
                cumulative += buckets[i];
                if (cumulative >= target)
                    return i == 0 ? 0 : BucketFloors[i];
            }
            return BucketFloors[BucketCount - 1];
        }

        private static int BucketIndex(long ticks)
        {
            // Floor semantics: bucket i holds samples in [floors[i],
            // floors[i+1]), so a returned bucket floor is a lower-bound
            // estimate of the percentile (never overstates a segment).
            // floors = [0, 1, 2, 4, ... 2^(n-1)]; the final bucket is the
            // open interval [2^(n-1), ∞) — slow-frame outliers report its
            // floor, which under-states them. BucketCount is sized so that
            // floor covers ≈1.4s at Stopwatch.Frequency; pathological frames
            // beyond that still show up in maxMs (exact) even when p95/p99
            // saturate at the last floor.
            var idx = BucketCount - 1;
            while (idx > 0 && ticks < BucketFloors[idx])
                idx--;
            return idx;
        }

        private static long[] BuildBucketFloors()
        {
            var floors = new long[BucketCount];
            long value = 1;
            for (var i = 1; i < BucketCount; i++)
            {
                floors[i] = value;
                value <<= 1;
            }
            return floors;
        }

        private static long[][] CreateBuckets()
        {
            var buckets = new long[SegmentCount][];
            for (var i = 0; i < SegmentCount; i++)
                buckets[i] = new long[BucketCount];
            return buckets;
        }

        private double TicksToMilliseconds(long ticks, long count)
        {
            if (count <= 0) return 0;
            return ticks * 1000.0 / _source.Frequency / count;
        }

        private static string Format(double value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
