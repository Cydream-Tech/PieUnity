using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Pie
{
    public static class PieFileBridge
    {
        private sealed class RequestState
        {
            public readonly int Id;
            public readonly CancellationTokenSource Cts = new CancellationTokenSource();
            public volatile bool IsComplete;
            public volatile bool ResultPushed;
            public string ResultJson;
            public string Error;

            public RequestState(int id)
            {
                Id = id;
            }
        }

        [Serializable]
        public sealed class FindRequestResult
        {
            public string[] Results;
            public int ScannedDirectories;
            public int ScannedFiles;
            public bool LimitReached;
            public string Pattern;
            public string RootPath;
        }

        [Serializable]
        public sealed class GrepRequestResult
        {
            public string[] Lines;
            public int MatchCount;
            public bool MatchLimitReached;
            public bool LinesTruncated;
            public int FilesScanned;
            public int TotalMatches;
            public int TotalFilesWithMatches;
            public bool ResultLimitReached;
            public string OutputMode;
            public int Offset;
            public int Limit;
            public string Pattern;
            public string SearchPath;
            public string Glob;
            public bool Literal;
            public bool IgnoreCase;
        }

        [Serializable]
        public sealed class FileStatResult
        {
            public string Name;
            public bool IsDirectory;
            public bool IsFile;
            public long Size;
            public long LastWriteTicksUtc;
        }

        [Serializable]
        public sealed class TextRangeReadResult
        {
            public string Content;
            public string ContentHash;
            public string SelectedContentHash;
            public int TotalLines;
            public long TotalBytes;
            public int SelectedLines;
            public long SelectedBytes;
            public long FirstLineBytes;
            public int OutputLines;
            public long OutputBytes;
            public bool Truncated;
            public string TruncatedBy;
            public bool FirstLineExceedsLimit;
            public long LastWriteTicksUtc;
            public long Size;
        }

        private static readonly ConcurrentDictionary<int, RequestState> _requests =
            new ConcurrentDictionary<int, RequestState>();

        private static int _nextRequestId = 1;

        private static readonly string[] _skipDirs = { ".git", "node_modules", "__pycache__", ".svn", ".hg" };
        private static readonly HashSet<string> _skipDirSet = new HashSet<string>(_skipDirs, StringComparer.OrdinalIgnoreCase);

        private static readonly string[] _binaryExtensions = {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp",
            ".mp3", ".mp4", ".wav", ".avi", ".mov",
            ".zip", ".tar", ".gz", ".bz2", ".7z", ".rar",
            ".exe", ".dll", ".so", ".dylib", ".bin",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx",
            ".woff", ".woff2", ".ttf", ".eot",
        };
        private static readonly HashSet<string> _binaryExtensionSet = new HashSet<string>(_binaryExtensions, StringComparer.OrdinalIgnoreCase);

        private const int GrepMaxLineLength = 400;
        private const int RegexTimeoutMs = 250;
        private const int MaxRegexPatternLength = 512;
        private const int MaxGlobPatternLength = 256;

        public static Task<string> ReadAllTextAsync(string path)
        {
            return Task.Run(() => File.ReadAllText(path));
        }

        public static Task<string> ReadTextRangeAsync(string path, int offsetLine, int limitLines, int maxOutputLines, int maxOutputBytes)
        {
            return Task.Run(() => JsonUtility.ToJson(ExecuteReadTextRange(path, offsetLine, limitLines, maxOutputLines, maxOutputBytes)));
        }

        public static Task<string> ReadAllBytesBase64Async(string path)
        {
            return Task.Run(() => Convert.ToBase64String(File.ReadAllBytes(path)));
        }

        public static Task WriteAllTextAsync(string path, string content)
        {
            return Task.Run(() =>
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, content ?? string.Empty);
            });
        }

        public static Task CreateDirectoryAsync(string path)
        {
            return Task.Run(() => Directory.CreateDirectory(path));
        }

        public static Task<string[]> ReadDirectoryAsync(string path)
        {
            return Task.Run(() =>
            {
                var files = Directory.GetFiles(path).Select(Path.GetFileName);
                var dirs = Directory.GetDirectories(path).Select(Path.GetFileName);
                return files.Concat(dirs).ToArray();
            });
        }

        public static Task<bool> ExistsAsync(string path)
        {
            return Task.Run(() => File.Exists(path) || Directory.Exists(path));
        }

        public static Task<FileStatResult> StatAsync(string path)
        {
            return Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    var info = new FileInfo(path);
                    return new FileStatResult
                    {
                        Name = info.Name,
                        IsDirectory = false,
                        IsFile = true,
                        Size = info.Length,
                        LastWriteTicksUtc = info.LastWriteTimeUtc.Ticks,
                    };
                }

                if (Directory.Exists(path))
                {
                    var info = new DirectoryInfo(path);
                    return new FileStatResult
                    {
                        Name = info.Name,
                        IsDirectory = true,
                        IsFile = false,
                        Size = 0,
                        LastWriteTicksUtc = info.LastWriteTimeUtc.Ticks,
                    };
                }

                throw new FileNotFoundException($"ENOENT: no such file or directory, stat '{path}'");
            });
        }

        public static Task DeleteFileAsync(string path)
        {
            return Task.Run(() => File.Delete(path));
        }

        private static TextRangeReadResult ExecuteReadTextRange(string path, int offsetLine, int limitLines, int maxOutputLines, int maxOutputBytes)
        {
            var info = new FileInfo(path);
            var startLine = Math.Max(0, offsetLine);
            var endLine = limitLines < 0 ? int.MaxValue : startLine + Math.Max(0, limitLines);
            var outputLineLimit = maxOutputLines < 0 ? int.MaxValue : Math.Max(0, maxOutputLines);
            var outputByteLimit = maxOutputBytes < 0 ? int.MaxValue : Math.Max(0, maxOutputBytes);
            var fullHash = new FnvaTextHash();
            var selectedHash = new FnvaTextHash();
            var outputLines = new List<string>();
            var currentOutputLine = new StringBuilder();
            var buffer = new char[8192];
            bool lineStarted = false;
            bool currentLineSelected = false;
            bool currentLineCanOutput = false;
            long currentLineBytes = 0;
            long currentOutputLineBytes = 0;
            long currentOutputPrefixBytes = 0;
            int lineIndex = 0;
            int selectedLines = 0;
            long selectedBytes = 0;
            long firstLineBytes = 0;
            long outputBytes = 0;
            bool truncated = false;
            string truncatedBy = null;
            bool firstLineExceedsLimit = false;

            void StartLine()
            {
                if (lineStarted)
                    return;

                currentLineSelected = lineIndex >= startLine && lineIndex < endLine;
                currentLineCanOutput = false;
                currentLineBytes = 0;
                currentOutputLineBytes = 0;
                currentOutputPrefixBytes = outputLines.Count > 0 ? 1 : 0;
                currentOutputLine.Length = 0;
                lineStarted = true;

                if (!currentLineSelected)
                    return;

                if (selectedLines > 0)
                {
                    selectedHash.Update("\n");
                    selectedBytes += 1;
                }

                if (!truncated && !firstLineExceedsLimit)
                {
                    if (outputLines.Count >= outputLineLimit)
                    {
                        truncated = true;
                        truncatedBy = "lines";
                    }
                    else
                    {
                        currentLineCanOutput = true;
                    }
                }
            }

            void ScanLineText(string textElement)
            {
                StartLine();
                if (!currentLineSelected)
                    return;

                selectedHash.Update(textElement);
                var elementBytes = Encoding.UTF8.GetByteCount(textElement);
                currentLineBytes += elementBytes;
                selectedBytes += elementBytes;

                if (selectedLines == 0 && currentLineBytes > outputByteLimit)
                {
                    firstLineExceedsLimit = true;
                    truncated = true;
                    truncatedBy = "bytes";
                    currentLineCanOutput = false;
                    currentOutputLine.Length = 0;
                    currentOutputLineBytes = 0;
                    return;
                }

                if (!currentLineCanOutput)
                    return;

                var candidateBytes = outputBytes + currentOutputPrefixBytes + currentOutputLineBytes + elementBytes;
                if (candidateBytes > outputByteLimit)
                {
                    truncated = true;
                    truncatedBy = "bytes";
                    currentLineCanOutput = false;
                    currentOutputLine.Length = 0;
                    currentOutputLineBytes = 0;
                    return;
                }

                currentOutputLine.Append(textElement);
                currentOutputLineBytes += elementBytes;
            }

            void FinishLine()
            {
                StartLine();
                if (currentLineSelected)
                {
                    if (selectedLines == 0)
                        firstLineBytes = currentLineBytes;
                    if (currentLineCanOutput)
                    {
                        var candidateBytes = outputBytes + currentOutputPrefixBytes + currentOutputLineBytes;
                        if (candidateBytes > outputByteLimit)
                        {
                            truncated = true;
                            truncatedBy = "bytes";
                            currentLineCanOutput = false;
                        }
                        else
                        {
                            outputLines.Add(currentOutputLine.ToString());
                            outputBytes = candidateBytes;
                        }
                    }
                    selectedLines++;
                }
                lineStarted = false;
                lineIndex++;
            }

            char? pendingHighSurrogate = null;

            void FlushPendingHighSurrogate()
            {
                if (!pendingHighSurrogate.HasValue)
                    return;

                ScanLineText(pendingHighSurrogate.Value.ToString());
                pendingHighSurrogate = null;
            }

            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        var ch = buffer[i];
                        fullHash.Update(ch);
                        if (pendingHighSurrogate.HasValue)
                        {
                            if (char.IsLowSurrogate(ch))
                            {
                                ScanLineText(new string(new[] { pendingHighSurrogate.Value, ch }));
                                pendingHighSurrogate = null;
                                continue;
                            }
                            FlushPendingHighSurrogate();
                        }

                        if (ch == '\n')
                        {
                            FinishLine();
                        }
                        else if (char.IsHighSurrogate(ch))
                        {
                            pendingHighSurrogate = ch;
                        }
                        else
                        {
                            ScanLineText(ch.ToString());
                        }
                    }
                }
            }

            FlushPendingHighSurrogate();
            FinishLine();

            return new TextRangeReadResult
            {
                Content = string.Join("\n", outputLines),
                ContentHash = fullHash.Digest(),
                SelectedContentHash = selectedHash.Digest(),
                TotalLines = lineIndex,
                TotalBytes = info.Length,
                SelectedLines = selectedLines,
                SelectedBytes = selectedBytes,
                FirstLineBytes = firstLineBytes,
                OutputLines = outputLines.Count,
                OutputBytes = outputBytes,
                Truncated = truncated,
                TruncatedBy = truncatedBy,
                FirstLineExceedsLimit = firstLineExceedsLimit,
                LastWriteTicksUtc = info.LastWriteTimeUtc.Ticks,
                Size = info.Length,
            };
        }

        private struct FnvaTextHash
        {
            private uint _hash;
            private bool _initialized;

            private void EnsureInitialized()
            {
                if (!_initialized)
                {
                    _hash = 0x811c9dc5;
                    _initialized = true;
                }
            }

            public void Update(char ch)
            {
                EnsureInitialized();
                unchecked
                {
                    _hash ^= ch;
                    _hash *= 0x01000193;
                }
            }

            public void Update(string text)
            {
                EnsureInitialized();
                if (text == null)
                    return;
                for (int i = 0; i < text.Length; i++)
                {
                    unchecked
                    {
                        _hash ^= text[i];
                        _hash *= 0x01000193;
                    }
                }
            }

            public string Digest()
            {
                EnsureInitialized();
                return _hash.ToString("x8");
            }
        }

        public static Task<string> FindAsync(string rootPath, string pattern, int limit)
        {
            return Task.Run(() =>
            {
                try
                {
                    var payload = ExecuteFind(rootPath, pattern, limit, CancellationToken.None);
                    return JsonUtility.ToJson(payload);
                }
                catch (RegexMatchTimeoutException ex)
                {
                    throw new TimeoutException("REGEX_INVALID_OR_TIMEOUT: regex matching timed out", ex);
                }
            });
        }

        public static Task<string> GrepAsync(string searchPath, string pattern, string globPattern, bool ignoreCase, bool literal, int contextLines, int limit, string outputMode, int offset)
        {
            return Task.Run(() =>
            {
                try
                {
                    var payload = ExecuteGrep(searchPath, pattern, globPattern, ignoreCase, literal, contextLines, limit, outputMode, offset, CancellationToken.None);
                    return JsonUtility.ToJson(payload);
                }
                catch (RegexMatchTimeoutException ex)
                {
                    throw new TimeoutException("REGEX_INVALID_OR_TIMEOUT: regex matching timed out", ex);
                }
            });
        }

        public static int StartFind(string rootPath, string pattern, int limit)
        {
            int id = Interlocked.Increment(ref _nextRequestId);
            var state = new RequestState(id);
            _requests[id] = state;

            PieDiagnostics.Verbose($"[PieFileBridge] find_files start path={rootPath} pattern={pattern} limit={limit}");
            Task.Run(() => RunFind(state, rootPath, pattern, limit));
            return id;
        }

        public static int StartGrep(string searchPath, string pattern, string globPattern, bool ignoreCase, bool literal, int contextLines, int limit, string outputMode, int offset)
        {
            int id = Interlocked.Increment(ref _nextRequestId);
            var state = new RequestState(id);
            _requests[id] = state;

            PieDiagnostics.Verbose($"[PieFileBridge] grep_text start path={searchPath} pattern={pattern} glob={globPattern} limit={limit}");
            Task.Run(() => RunGrep(state, searchPath, pattern, globPattern, ignoreCase, literal, contextLines, limit, outputMode, offset));
            return id;
        }

        public static bool IsRequestComplete(int requestId)
        {
            return _requests.TryGetValue(requestId, out var state) && state.IsComplete;
        }

        public static IEnumerable<int> GetActiveRequestIds()
        {
            return _requests.Keys;
        }

        public static bool IsResultPushed(int requestId)
        {
            return _requests.TryGetValue(requestId, out var state) && state.ResultPushed;
        }

        public static void MarkResultPushed(int requestId)
        {
            if (_requests.TryGetValue(requestId, out var state))
                state.ResultPushed = true;
        }

        public static string GetRequestResultJson(int requestId)
        {
            return _requests.TryGetValue(requestId, out var state) ? state.ResultJson : null;
        }

        public static string GetRequestError(int requestId)
        {
            return _requests.TryGetValue(requestId, out var state) ? state.Error : null;
        }

        public static void CancelRequest(int requestId)
        {
            if (_requests.TryGetValue(requestId, out var state))
                state.Cts.Cancel();
        }

        public static void CancelAllRequests()
        {
            foreach (var requestId in _requests.Keys)
            {
                if (_requests.TryRemove(requestId, out var state))
                {
                    try
                    {
                        state.Cts.Cancel();
                    }
                    catch
                    {
                        // Ignore cancellation races during domain reload / disposal.
                    }
                    finally
                    {
                        state.Cts.Dispose();
                    }
                }
            }
        }

        public static void ReleaseRequest(int requestId)
        {
            if (_requests.TryRemove(requestId, out var state))
                state.Cts.Dispose();
        }

        private static void RunFind(RequestState state, string rootPath, string pattern, int limit)
        {
            try
            {
                var payload = ExecuteFind(rootPath, pattern, limit, state.Cts.Token);
                state.ResultJson = JsonUtility.ToJson(payload);
                PieDiagnostics.Verbose($"[PieFileBridge] find_files done pattern={pattern} matches={payload.Results.Length} dirs={payload.ScannedDirectories} files={payload.ScannedFiles}");
            }
            catch (OperationCanceledException)
            {
                state.Error = "Operation aborted";
                PieDiagnostics.Warning("[PieFileBridge] find_files cancelled");
            }
            catch (RegexMatchTimeoutException ex)
            {
                state.Error = "REGEX_INVALID_OR_TIMEOUT: " + ex.Message;
                PieDiagnostics.Warning("[PieFileBridge] find_files regex timeout");
            }
            catch (Exception ex)
            {
                state.Error = ex.Message;
                PieDiagnostics.Error($"[PieFileBridge] find_files error: {ex.Message}");
            }
            finally
            {
                state.IsComplete = true;
            }
        }

        private static void RunGrep(RequestState state, string searchPath, string pattern, string globPattern, bool ignoreCase, bool literal, int contextLines, int limit, string outputMode, int offset)
        {
            try
            {
                var payload = ExecuteGrep(searchPath, pattern, globPattern, ignoreCase, literal, contextLines, limit, outputMode, offset, state.Cts.Token);
                state.ResultJson = JsonUtility.ToJson(payload);
                PieDiagnostics.Verbose($"[PieFileBridge] grep_text done pattern={pattern} matches={payload.MatchCount} files={payload.FilesScanned}");
            }
            catch (OperationCanceledException)
            {
                state.Error = "Operation aborted";
                PieDiagnostics.Warning("[PieFileBridge] grep_text cancelled");
            }
            catch (RegexMatchTimeoutException ex)
            {
                state.Error = "REGEX_INVALID_OR_TIMEOUT: " + ex.Message;
                PieDiagnostics.Warning("[PieFileBridge] grep_text regex timeout");
            }
            catch (Exception ex)
            {
                state.Error = ex.Message;
                PieDiagnostics.Error($"[PieFileBridge] grep_text error: {ex.Message}");
            }
            finally
            {
                state.IsComplete = true;
            }
        }

        private static FindRequestResult ExecuteFind(string rootPath, string pattern, int limit, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                throw new DirectoryNotFoundException($"Path not found: {rootPath}");

            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern must not be empty");

            var results = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);
            int scannedDirectories = 0;
            int scannedFiles = 0;
            bool limitReached = false;

            while (queue.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var dir = queue.Dequeue();
                scannedDirectories++;

                string[] entries;
                try
                {
                    entries = Directory.GetFileSystemEntries(dir);
                }
                catch
                {
                    continue;
                }

                Array.Sort(entries, StringComparer.OrdinalIgnoreCase);

                foreach (var entryPath in entries)
                {
                    token.ThrowIfCancellationRequested();

                    bool isDirectory;
                    try
                    {
                        isDirectory = Directory.Exists(entryPath);
                    }
                    catch
                    {
                        continue;
                    }

                    var entryName = Path.GetFileName(entryPath);
                    if (isDirectory)
                    {
                        if (_skipDirSet.Contains(entryName))
                            continue;

                        queue.Enqueue(entryPath);
                        continue;
                    }

                    if (!File.Exists(entryPath))
                        continue;

                    scannedFiles++;
                    var relativePath = MakeRelativePath(rootPath, entryPath);
                    if (MatchesFindPattern(relativePath, entryName, pattern))
                    {
                        if (results.Count < limit)
                        {
                            results.Add(relativePath);
                        }
                        else
                        {
                            limitReached = true;
                            break;
                        }
                    }
                }

                if (limitReached)
                    break;
            }

            return new FindRequestResult
            {
                Results = results.ToArray(),
                ScannedDirectories = scannedDirectories,
                ScannedFiles = scannedFiles,
                LimitReached = limitReached,
                Pattern = pattern,
                RootPath = rootPath,
            };
        }

        private sealed class GrepMatchRecord
        {
            public string RelativePath;
            public int LineIndex;
            public string[] Lines;
        }

        private sealed class GrepFileSummary
        {
            public string RelativePath;
            public int MatchCount;
            public long LastWriteTicksUtc;
        }

        private static GrepRequestResult ExecuteGrep(string searchPath, string pattern, string globPattern, bool ignoreCase, bool literal, int contextLines, int limit, string outputMode, int offset, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(searchPath))
                throw new ArgumentException("Search path must not be empty");

            bool isDirectory = Directory.Exists(searchPath);
            bool isFile = File.Exists(searchPath);
            if (!isDirectory && !isFile)
                throw new FileNotFoundException($"Path not found: {searchPath}");

            var regex = BuildRegex(pattern, ignoreCase, literal);
            var files = isDirectory ? CollectSearchFiles(searchPath, globPattern, token) : new List<string> { searchPath };
            files.Sort(StringComparer.OrdinalIgnoreCase);
            var mode = NormalizeGrepOutputMode(outputMode);
            var effectiveOffset = Math.Max(0, offset);
            var effectiveLimit = Math.Max(1, limit);

            var outputLines = new List<string>();
            var contentMatches = new List<GrepMatchRecord>();
            var fileSummaries = new List<GrepFileSummary>();
            int totalMatches = 0;
            bool matchLimitReached = false;
            bool linesTruncated = false;
            int filesScanned = 0;

            foreach (var filePath in files)
            {
                token.ThrowIfCancellationRequested();

                string content;
                try
                {
                    content = File.ReadAllText(filePath);
                }
                catch
                {
                    continue;
                }

                filesScanned++;
                var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                var relativePath = isDirectory ? MakeRelativePath(searchPath, filePath) : Path.GetFileName(filePath);
                int fileMatchCount = 0;

                for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                {
                    if (!regex.IsMatch(lines[lineIdx]))
                        continue;

                    totalMatches++;
                    fileMatchCount++;
                    if (mode == "content" && totalMatches > effectiveOffset && totalMatches <= effectiveOffset + effectiveLimit)
                    {
                        contentMatches.Add(new GrepMatchRecord
                        {
                            RelativePath = relativePath,
                            LineIndex = lineIdx,
                            Lines = lines,
                        });
                    }
                }

                if (fileMatchCount > 0)
                {
                    fileSummaries.Add(new GrepFileSummary
                    {
                        RelativePath = relativePath,
                        MatchCount = fileMatchCount,
                        LastWriteTicksUtc = File.GetLastWriteTimeUtc(filePath).Ticks,
                    });
                }
            }

            if (mode == "content")
            {
                matchLimitReached = totalMatches > effectiveOffset + effectiveLimit;
                foreach (var match in contentMatches.Take(effectiveLimit))
                {
                    int start = contextLines > 0 ? Math.Max(0, match.LineIndex - contextLines) : match.LineIndex;
                    int end = contextLines > 0 ? Math.Min(match.Lines.Length - 1, match.LineIndex + contextLines) : match.LineIndex;

                    for (int c = start; c <= end; c++)
                    {
                        string lineText = match.Lines[c];
                        if (lineText.Length > GrepMaxLineLength)
                        {
                            lineText = lineText.Substring(0, GrepMaxLineLength) + "...";
                            linesTruncated = true;
                        }

                        int currentLineNum = c + 1;
                        if (c == match.LineIndex)
                            outputLines.Add(match.RelativePath + ":" + currentLineNum + ": " + lineText);
                        else
                            outputLines.Add(match.RelativePath + "-" + currentLineNum + "- " + lineText);
                    }
                }
            }
            else
            {
                var sortedSummaries = fileSummaries
                    .OrderByDescending(summary => summary.LastWriteTicksUtc)
                    .ThenBy(summary => summary.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var page = sortedSummaries.Skip(effectiveOffset).Take(effectiveLimit).ToList();
                matchLimitReached = effectiveOffset + effectiveLimit < sortedSummaries.Count;

                if (mode == "files_with_matches")
                {
                    outputLines.AddRange(page.Select(summary => summary.RelativePath));
                }
                else
                {
                    outputLines.Add("Total matches: " + totalMatches);
                    outputLines.Add("Files with matches: " + sortedSummaries.Count);
                    outputLines.AddRange(page.Select(summary => summary.RelativePath + ": " + summary.MatchCount));
                }
            }

            return new GrepRequestResult
            {
                Lines = outputLines.ToArray(),
                MatchCount = totalMatches,
                MatchLimitReached = matchLimitReached,
                LinesTruncated = linesTruncated,
                FilesScanned = filesScanned,
                TotalMatches = totalMatches,
                TotalFilesWithMatches = fileSummaries.Count,
                ResultLimitReached = matchLimitReached,
                OutputMode = mode,
                Offset = effectiveOffset,
                Limit = effectiveLimit,
                Pattern = pattern,
                SearchPath = searchPath,
                Glob = globPattern,
                Literal = literal,
                IgnoreCase = ignoreCase,
            };
        }

        private static List<string> CollectSearchFiles(string rootPath, string globPattern, CancellationToken token)
        {
            var files = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);

            while (queue.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var dir = queue.Dequeue();
                string[] entries;
                try
                {
                    entries = Directory.GetFileSystemEntries(dir);
                }
                catch
                {
                    continue;
                }

                foreach (var entryPath in entries)
                {
                    token.ThrowIfCancellationRequested();

                    bool isDirectory;
                    try
                    {
                        isDirectory = Directory.Exists(entryPath);
                    }
                    catch
                    {
                        continue;
                    }

                    var entryName = Path.GetFileName(entryPath);
                    if (isDirectory)
                    {
                        if (!_skipDirSet.Contains(entryName))
                            queue.Enqueue(entryPath);
                        continue;
                    }

                    if (!File.Exists(entryPath))
                        continue;

                    string ext = Path.GetExtension(entryName);
                    if (_binaryExtensionSet.Contains(ext))
                        continue;

                    if (!MatchesSimpleGlob(entryName, globPattern))
                        continue;

                    files.Add(entryPath);
                }
            }

            return files;
        }

        private static Regex BuildRegex(string pattern, bool ignoreCase, bool literal)
        {
            if (literal)
                pattern = Regex.Escape(pattern);

            if ((pattern ?? string.Empty).Length > MaxRegexPatternLength)
                throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: regex pattern is too long");

            var options = RegexOptions.Multiline;
            if (ignoreCase)
                options |= RegexOptions.IgnoreCase;

            return new Regex(pattern, options, TimeSpan.FromMilliseconds(RegexTimeoutMs));
        }

        private static string NormalizeGrepOutputMode(string outputMode)
        {
            if (string.IsNullOrWhiteSpace(outputMode) || outputMode == "content")
                return "content";
            if (outputMode == "files_with_matches" || outputMode == "count")
                return outputMode;
            throw new ArgumentException("Invalid grep_text arguments: outputMode must be one of content, files_with_matches, or count.");
        }

        private static bool MatchesSimpleGlob(string entryName, string globPattern)
        {
            if (string.IsNullOrWhiteSpace(globPattern))
                return true;

            if (globPattern.StartsWith("*.", StringComparison.Ordinal))
                return string.Equals(Path.GetExtension(entryName), globPattern.Substring(1), StringComparison.OrdinalIgnoreCase);

            if (globPattern.StartsWith("**/*.", StringComparison.Ordinal))
                return string.Equals(Path.GetExtension(entryName), globPattern.Substring(4), StringComparison.OrdinalIgnoreCase);

            return MatchesGlob(entryName, globPattern);
        }

        private static bool MatchesFindPattern(string relativePath, string entryName, string pattern)
        {
            var normalizedPattern = (pattern ?? string.Empty).Trim().Replace("\\", "/");
            var normalizedRelativePath = (relativePath ?? string.Empty).Replace("\\", "/");
            if (normalizedPattern.StartsWith("./", StringComparison.Ordinal))
                normalizedPattern = normalizedPattern.Substring(2);

            if (HasGlobChars(normalizedPattern))
            {
                if (normalizedPattern.IndexOf('/') < 0)
                    return MatchesGlob(entryName, normalizedPattern);

                if (normalizedPattern.StartsWith("**/", StringComparison.Ordinal) &&
                    MatchesGlob(normalizedRelativePath, normalizedPattern.Substring(3)))
                    return true;

                return MatchesGlob(normalizedRelativePath, normalizedPattern);
            }

            return normalizedRelativePath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase)
                || normalizedRelativePath.EndsWith("/" + normalizedPattern, StringComparison.OrdinalIgnoreCase)
                || entryName.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase)
                || entryName.IndexOf(normalizedPattern, StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedRelativePath.IndexOf(normalizedPattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasGlobChars(string pattern)
        {
            return pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0;
        }

        private static bool MatchesGlob(string input, string pattern)
        {
            var regex = GlobToRegex(pattern);
            return regex.IsMatch(input);
        }

        private static Regex GlobToRegex(string pattern)
        {
            var normalizedPattern = (pattern ?? string.Empty).Replace("\\", "/");
            if (normalizedPattern.Length > MaxGlobPatternLength)
                throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: glob pattern is too long");

            var sb = new System.Text.StringBuilder("^");

            for (int i = 0; i < normalizedPattern.Length; i++)
            {
                char c = normalizedPattern[i];

                if (c == '*')
                {
                    bool isDoubleStar = i + 1 < normalizedPattern.Length && normalizedPattern[i + 1] == '*';
                    if (isDoubleStar)
                    {
                        bool followedBySlash = i + 2 < normalizedPattern.Length && normalizedPattern[i + 2] == '/';
                        if (followedBySlash)
                        {
                            sb.Append("(?:.*/)?");
                            i += 2;
                        }
                        else
                        {
                            sb.Append(".*");
                            i += 1;
                        }
                    }
                    else
                    {
                        sb.Append("[^/]*");
                    }
                    continue;
                }

                if (c == '?')
                {
                    sb.Append("[^/]");
                    continue;
                }

                if (c == '/')
                {
                    sb.Append("[/\\\\]");
                    continue;
                }

                sb.Append(Regex.Escape(c.ToString()));
            }

            sb.Append("$");
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(RegexTimeoutMs));
        }

        private static string MakeRelativePath(string rootPath, string fullPath)
        {
            string relativePath = fullPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relativePath.Replace("\\", "/");
        }
    }
}
