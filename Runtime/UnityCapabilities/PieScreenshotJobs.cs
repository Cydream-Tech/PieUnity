using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Pie.Unity.Capabilities
{
    // Admission/cache methods are main-thread only. Workers and deadline callbacks
    // touch only atomic state/slot references, never the cache or Unity objects.
    internal sealed class PieScreenshotJobs
    {
        internal sealed class Output
        {
            internal string Path;
            internal int Bytes;
        }
        internal sealed class State
        {
            internal readonly string Status, Error;
            internal readonly Output Result;
            internal readonly DateTime At = DateTime.UtcNow;
            internal State(string status, Output result = null, string error = null)
            { Status = status; Result = result; Error = error != null && error.Length > 1024 ? error.Substring(0, 1024) : error; }
        }
        internal sealed class Job
        {
            internal readonly string Id;
            internal readonly State Pending = new State("pending");
            internal State Current;
            internal Timer Timer;
            // Only main-thread capture writes metadata; workers never read it.
            internal int Width, Height;
            internal string Camera, Mode, Directory;
            internal Job(string id) { Id = id; Current = Pending; }
            internal State Read() => Volatile.Read(ref Current);
            internal bool Finish(State state) => ReferenceEquals(Interlocked.CompareExchange(ref Current, state, Pending), Pending);
        }
        private readonly Dictionary<string, Job> jobs = new Dictionary<string, Job>(StringComparer.Ordinal);
        private readonly int deadlineMs;
        private readonly Func<DateTime> sweepClock;
        private readonly Func<Action, bool> queue;
        private Job slot;
        internal PieScreenshotJobs(int deadlineMs = 30000, Func<DateTime> sweepClock = null, Func<Action, bool> queue = null)
        {
            this.deadlineMs = deadlineMs;
            this.queue = queue ?? (work => ThreadPool.QueueUserWorkItem(_ => work()));
            this.sweepClock = sweepClock ?? (() => DateTime.UtcNow);
        }
        internal bool Busy => Volatile.Read(ref slot) != null;
        internal int RecordCount { get { Sweep(); return jobs.Count; } }

        // Deferred capture retains the physical slot until its owner either hands
        // off managed work or fails. Logical cancellation never releases GPU work.
        internal sealed class CaptureOwner
        {
            private readonly PieScreenshotJobs manager;
            internal readonly Job Job;
            private int handedOff;
            internal CaptureOwner(PieScreenshotJobs manager, Job job) { this.manager = manager; Job = job; }
            internal bool Pending => ReferenceEquals(Job.Read(), Job.Pending);
            internal void Submit(Func<Output> work)
            {
                if (Interlocked.Exchange(ref handedOff, 1) != 0) return;
                try
                {
                    if (work == null) throw new ArgumentNullException(nameof(work));
                    if (!manager.Queue(Job, work))
                        throw new InvalidOperationException("Could not queue screenshot encoding.");
                }
                catch (Exception ex) { Job.Finish(new State("failed", error: ex.Message)); manager.Release(Job); }
            }
            internal void Fail(Exception error)
            {
                if (Interlocked.Exchange(ref handedOff, 1) != 0) return;
                Job.Finish(new State("failed", error: error.Message));
                manager.Release(Job);
            }
            internal void Release()
            {
                if (Interlocked.Exchange(ref handedOff, 1) != 0) return;
                // Release is normally cancellation cleanup. A pending release is
                // an owner failure, never a permanently pending cache entry.
                Job.Finish(new State("failed", error: "Screenshot capture owner released before completion."));
                manager.Release(Job);
            }
        }
        internal Job Begin(string id, Func<Job, Func<Output>> capture)
            => BeginDeferred(id, owner => owner.Submit(capture(owner.Job)));

        internal Job BeginDeferred(string id, Action<CaptureOwner> capture)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 256)
                throw new ArgumentException("requestId must contain 1-256 characters.");
            Sweep();
            if (jobs.TryGetValue(id, out var duplicate)) return duplicate;
            // Reset invalidates lookup but must not allow an active physical job's
            // id to recapture, nor release its slot until its worker exits.
            var job = new Job(id);
            if (Interlocked.CompareExchange(ref slot, job, null) != null)
                throw new InvalidOperationException("Screenshot capture/encode is busy.");
            Sweep(31); // Reserve space so worker completion cannot create a 33rd terminal record.
            jobs.Add(id, job); // Ownership precedes capture, including throwing captures.
            var owner = new CaptureOwner(this, job);
            try
            {
                job.Timer = new Timer(_ => job.Finish(new State("timed_out")), null, deadlineMs, Timeout.Infinite);
                capture(owner);
            }
            catch (Exception ex)
            {
                owner.Fail(ex);
            }
            Sweep();
            return job;
        }
        // The scheduling wrapper needs manager/job for terminal CAS; importantly
        // it does not retain the deferred capture owner (or any Unity state).
        private bool Queue(Job job, Func<Output> work) => queue(() => Run(job, work));
        private void Run(Job job, Func<Output> work)
        {
            Output output = null;
            try
            {
                if (!ReferenceEquals(job.Read(), job.Pending)) return;
                output = work(); // owns managed pixels; no cache lock during encode/IO
                if (job.Finish(new State("succeeded", output))) output = null;
            }
            catch (Exception ex) { job.Finish(new State("failed", error: ex.Message)); }
            finally
            {
                // A losing completion may remove ONLY its own unique publication.
                try
                {
                    if (output != null) { try { File.Delete(output.Path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
                }
                finally { Release(job); }
            }
        }
        private void Release(Job job)
        {
            try { job.Timer?.Dispose(); }
            finally
            {
                job.Timer = null;
                Interlocked.CompareExchange(ref slot, null, job);
            }
        }
        internal Job Poll(string id) { Sweep(); return id != null && jobs.TryGetValue(id, out var job) ? job : null; }
        internal Job Cancel(string id)
        {
            var job = Poll(id);
            job?.Finish(new State("cancelled"));
            return job;
        }
        internal void Reset()
        {
            Volatile.Read(ref slot)?.Finish(new State("cancelled"));
            jobs.Clear(); // does not release physical ownership or wait
        }
        private void Sweep(int capacity = 32)
        {
            var terminal = new List<Job>();
            foreach (var job in jobs.Values)
                if (job.Read().Status != "pending") terminal.Add(job);
            terminal.Sort((a, b) => a.Read().At.CompareTo(b.Read().At));
            var cutoff = sweepClock().AddSeconds(-60);
            for (var i = 0; i < terminal.Count; i++)
                if (i < terminal.Count - capacity || terminal[i].Read().At < cutoff)
                    jobs.Remove(terminal[i].Id);
        }
    }

    internal static class PieScreenshotFiles
    {
        // Reject portable-dangerous components (drive roots, ADS, traversal and
        // Windows trailing-dot/space aliases), even when running on macOS.
        internal static string Destination(string directory, string relative)
        {
            relative = string.IsNullOrWhiteSpace(relative) ? $"capture_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.png" : relative.Trim().Replace('\\', '/');
            foreach (var part in relative.Split('/'))
                if (part.Length == 0 || part == "." || part == ".." || part.EndsWith(".") || part.EndsWith(" ") || part.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|', '\0' }) >= 0)
                    throw new ArgumentException("Screenshot path must stay under PieScreenshots.");
            if (!relative.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) relative += ".png";
            var root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(directory, relative));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Invalid screenshot path.");
            return path;
        }
        internal static string WriteUnique(string destination, byte[] bytes, string trustedPersistentRoot)
        {
            var directory = Path.GetDirectoryName(destination);
            // The OS-selected persistent root (and its ancestors) may itself be
            // reached through a legitimate alias/junction. Trust that boundary,
            // but reject caller-selected links beneath it, including PieScreenshots.
            var boundary = new DirectoryInfo(trustedPersistentRoot).FullName;
            var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var parent = new DirectoryInfo(directory);
            while (parent != null && !string.Equals(parent.FullName, boundary, comparison))
            {
                if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Screenshot destination contains a symbolic link.");
                parent = parent.Parent;
            }
            if (parent == null) throw new IOException("Screenshot destination escaped the persistent root.");
            Directory.CreateDirectory(directory);
            var temp = Path.Combine(directory, ".pie-" + Guid.NewGuid().ToString("N") + ".tmp");
            var ownsTemp = false;
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ownsTemp = true;
                    stream.Write(bytes, 0, bytes.Length);
                }
                for (var attempt = 0; attempt < 1024; attempt++)
                {
                    var path = attempt == 0 ? destination : Path.Combine(directory, Path.GetFileNameWithoutExtension(destination) + "_" + attempt + Path.GetExtension(destination));
                    // File.Move's no-overwrite check and rename can race on
                    // macOS runtimes. A nonblocking CreateNew sidecar serializes
                    // just this candidate across sync/async writers (and hosts),
                    // without any main-thread lock or waiting for worker IO.
                    var reservation = path + ".pie-reservation";
                    FileStream claim;
                    try { claim = new FileStream(reservation, FileMode.CreateNew, FileAccess.Write, FileShare.None); }
                    catch (IOException) when (File.Exists(reservation) || File.Exists(path)) { continue; }
                    // Other IO failures (disk full, unavailable storage) fail
                    // immediately instead of spinning through 1024 candidates.
                    try
                    {
                        if (File.Exists(path)) continue;
                        try { File.Move(temp, path); return path; }
                        catch (IOException) { if (!File.Exists(path)) throw; }
                    }
                    finally { claim.Dispose(); TryDelete(reservation); }
                }
                throw new IOException("Screenshot filename collision limit exceeded.");
            }
            finally { if (ownsTemp) TryDelete(temp); }
        }
        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
