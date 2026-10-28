using System;
using System.Collections.Generic;

namespace Pie
{
    /// <summary>Live assistant projection only. Canonical history still comes
    /// from session_sync; this class never owns session persistence.</summary>
    public sealed class PiePresentationJournal
    {
        private sealed class Entry
        {
            public long LastSequence;
            public bool Terminal;
            public bool TerminalCommitAllowed;
            public bool TerminalCommitAccepted;
            public string TerminalStatus;
            public PieTimelineItem Item;
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        public PiePresentationApplyResult Apply(PiePresentationFrame frame, string sessionId)
        {
            if (frame == null || string.IsNullOrWhiteSpace(frame.itemId) || frame.seq <= 0)
                return new PiePresentationApplyResult();
            if (!string.IsNullOrEmpty(frame.sessionId) && !string.Equals(frame.sessionId, sessionId, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(sessionId))
                return new PiePresentationApplyResult();
            if (entries.TryGetValue(frame.itemId, out var existing) && existing.Terminal)
            {
                // A failed final full may be retried after the canonical terminal.
                // Accept exactly one authoritative full commit, never a running
                // revival or an arbitrary post-terminal delta stream.
                if (!existing.TerminalCommitAllowed || existing.TerminalCommitAccepted
                    || frame.type != "text_full" || frame.seq != existing.LastSequence + 1)
                    return new PiePresentationApplyResult { item = existing.Item, terminal = true };
                existing.TerminalCommitAccepted = true;
                existing.LastSequence = frame.seq;
                return new PiePresentationApplyResult
                {
                    accepted = true,
                    terminal = true,
                    item = new PieTimelineItem
                    {
                        schemaVersion = 1,
                        sessionId = string.IsNullOrEmpty(frame.sessionId) ? sessionId ?? "" : frame.sessionId,
                        clientTurnId = frame.clientTurnId ?? "",
                        itemId = frame.itemId,
                        kind = "assistant",
                        status = existing.TerminalStatus ?? "succeeded",
                        startedAt = frame.startedAt,
                        updatedAt = frame.updatedAt,
                        text = frame.text ?? "",
                        summary = frame.text ?? "",
                    },
                };
            }
            if (!entries.TryGetValue(frame.itemId, out var entry))
            {
                entry = new Entry
                {
                    Item = new PieTimelineItem
                    {
                        schemaVersion = 1,
                        sessionId = string.IsNullOrEmpty(frame.sessionId) ? sessionId ?? "" : frame.sessionId,
                        clientTurnId = frame.clientTurnId ?? "",
                        itemId = frame.itemId,
                        kind = "assistant",
                        status = "running",
                        startedAt = frame.startedAt,
                        text = "",
                        summary = "",
                    },
                };
                entries.Add(frame.itemId, entry);
            }

            if (frame.seq <= entry.LastSequence)
                return new PiePresentationApplyResult { item = entry.Item };
            var previousText = entry.Item.text;
            var isFull = frame.type == "text_full";
            if (!isFull && frame.seq != entry.LastSequence + 1)
                return new PiePresentationApplyResult { item = entry.Item, resyncRequired = true };

            var delta = isFull ? frame.text ?? "" : frame.delta ?? "";
            entry.Item.text = isFull ? delta : entry.Item.text + delta;
            entry.Item.summary = entry.Item.text;
            entry.Item.updatedAt = frame.updatedAt;
            entry.LastSequence = frame.seq;
            return new PiePresentationApplyResult
            {
                accepted = true,
                item = entry.Item,
                previousText = previousText,
                delta = isFull ? null : delta,
            };
        }

        public static string TerminalDelta(string previousText, string finalText)
        {
            previousText ??= "";
            finalText ??= "";
            if (finalText.StartsWith(previousText, StringComparison.Ordinal))
                return finalText.Length > previousText.Length ? finalText.Substring(previousText.Length) : null;
            return string.IsNullOrEmpty(finalText) ? null : finalText;
        }

        public bool NeedsTerminalText(string itemId, string finalText, bool isTruncated = false)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (!entries.TryGetValue(itemId, out var entry))
                return !string.IsNullOrEmpty(finalText);
            if (entry.Terminal) return false;
            var liveText = entry.Item?.text ?? "";
            var canonicalText = finalText ?? "";
            if (isTruncated && liveText.Length > canonicalText.Length
                && liveText.StartsWith(canonicalText, StringComparison.Ordinal)) return false;
            return !string.Equals(liveText, canonicalText, StringComparison.Ordinal);
        }

        public bool IsTerminal(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && entries.TryGetValue(itemId, out var entry) && entry.Terminal;
        }

        public void Retire(string itemId, string status = "succeeded", bool allowTerminalCommit = false)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (!entries.TryGetValue(itemId, out var entry))
            {
                entries.Add(itemId, new Entry
                {
                    Terminal = true,
                    TerminalStatus = status,
                    TerminalCommitAllowed = allowTerminalCommit,
                });
                return;
            }
            if (entry.Terminal) return;
            entry.Terminal = true;
            entry.TerminalStatus = status;
            entry.TerminalCommitAllowed = allowTerminalCommit;
            entry.Item = null;
        }

        public void Clear() => entries.Clear();
    }

    public struct PiePresentationApplyResult
    {
        public bool accepted;
        public bool resyncRequired;
        public bool terminal;
        public string delta;
        public string previousText;
        public PieTimelineItem item;
    }
}
