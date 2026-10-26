using System;
using System.Diagnostics;
using UnityEngine;

namespace Pie.Unity.Capabilities
{
    // Pull-only, screenshot-specific diagnostics. Not a model capability and
    // not a general profiler. No image data, paths, arguments or per-stage logs.
    internal static class PieScreenshotTimingProbe
    {
        internal enum Stage { Capture, Render, Blit, ReadbackApply, PixelCopy, Encode, Write }
        private const int Capacity = 32;
        private static readonly string[] Names = { "capture", "render", "blit", "readbackApply", "pixelCopy", "encode", "write" };
        private static bool _enabled;
        private static int _epoch, _next, _count, _sequence, _overwritten;
        private static Sample[] _samples;
        private static Sample _active;

        internal sealed class Sample
        {
            internal int frame, sequence, width, height;
            internal bool success, fallback, captureOnly;
            internal string phase;
            internal long total;
            internal readonly long[] ticks = new long[7];
            internal readonly int[] calls = new int[7];
        }
        [Serializable] private sealed class Options { public bool enabled; }
        [Serializable] private sealed class StageReport { public string name; public double ms; public int calls; }
        [Serializable] private sealed class SampleReport
        {
            public int frame, sequence, width, height;
            public bool success, fallback, captureOnly;
            public string phase;
            public double totalMs;
            public StageReport[] stages;
        }
        [Serializable] private sealed class Report
        {
            public bool enabled;
            public int epoch, capacity = Capacity, overwritten;
            public string clock = "Stopwatch synchronous wall time; total includes child stages; frame=-1 outside Play Mode";
            public SampleReport[] samples;
        }

        internal static void Register()
        {
            _enabled = false;
            Reset();
            PieDevRpc.Register("perf.screenshot.set_enabled", json => {
                var enabled = !string.IsNullOrWhiteSpace(json) && JsonUtility.FromJson<Options>(json).enabled;
                if (_enabled != enabled) { _epoch++; _active = null; }
                _enabled = enabled;
                if (enabled && _samples == null) _samples = new Sample[Capacity];
                return enabled ? "{\"enabled\":true}" : "{\"enabled\":false}";
            });
            PieDevRpc.Register("perf.screenshot.reset", _ => { Reset(); return "{\"reset\":true}"; });
            PieDevRpc.Register("perf.screenshot.get_report", _ => BuildReport());
        }
        private static void Reset()
        {
            _epoch++; _active = null;
            _next = _count = _sequence = _overwritten = 0;
            if (_samples != null) Array.Clear(_samples, 0, _samples.Length);
        }
        internal static CaptureScope BeginCapture(bool captureOnly = false, string phase = null)
        {
            if (!_enabled) return default;
            var previous = _active;
            var sample = new Sample { sequence = ++_sequence, frame = Application.isPlaying ? Time.frameCount : -1, captureOnly = captureOnly, phase = phase };
            _active = sample;
            return new CaptureScope(sample, previous, _epoch, Stopwatch.GetTimestamp());
        }
        internal static void Dimensions(int width, int height)
        {
            if (_active == null) return;
            _active.width = width; _active.height = height;
        }
        internal static void Fallback() { if (_active != null) _active.fallback = true; }
        internal static StageScope Measure(Stage stage)
        {
            var sample = _active;
            return sample == null ? default : new StageScope(sample, (int)stage, Stopwatch.GetTimestamp());
        }
        internal readonly struct StageScope : IDisposable
        {
            private readonly Sample _sample;
            private readonly int _stage;
            private readonly long _start;
            internal StageScope(Sample sample, int stage, long start) { _sample = sample; _stage = stage; _start = start; }
            public void Dispose()
            {
                if (_sample == null) return;
                _sample.ticks[_stage] += Stopwatch.GetTimestamp() - _start;
                _sample.calls[_stage]++;
            }
        }
        internal readonly struct CaptureScope : IDisposable
        {
            private readonly Sample _sample, _previous;
            private readonly int _captureEpoch;
            private readonly long _start;
            internal CaptureScope(Sample sample, Sample previous, int epoch, long start) { _sample = sample; _previous = previous; _captureEpoch = epoch; _start = start; }
            internal void Succeed() { if (_sample != null) _sample.success = true; }
            public void Dispose()
            {
                if (_sample == null || _captureEpoch != _epoch) return;
                _sample.total = Stopwatch.GetTimestamp() - _start;
                _active = _previous;
                _samples[_next] = _sample;
                _next = (_next + 1) % Capacity;
                if (_count < Capacity) _count++; else _overwritten++;
            }
        }
        private static string BuildReport()
        {
            var rows = new SampleReport[_count];
            for (var i = 0; i < _count; i++)
            {
                var sample = _samples[(_next - _count + Capacity + i) % Capacity];
                var stages = new StageReport[Names.Length];
                for (var j = 0; j < Names.Length; j++)
                    stages[j] = new StageReport { name = Names[j], calls = sample.calls[j], ms = Milliseconds(sample.ticks[j]) };
                rows[i] = new SampleReport { frame = sample.frame, sequence = sample.sequence, width = sample.width, height = sample.height, success = sample.success, fallback = sample.fallback, captureOnly = sample.captureOnly, phase = sample.phase, totalMs = Milliseconds(sample.total), stages = stages };
            }
            return JsonUtility.ToJson(new Report { enabled = _enabled, epoch = _epoch, overwritten = _overwritten, samples = rows });
        }
        private static double Milliseconds(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
    }
}
