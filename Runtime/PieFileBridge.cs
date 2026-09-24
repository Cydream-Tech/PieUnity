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
			public bool SearchIncomplete;
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
			public bool SearchIncomplete;
			public string SafetyLimit;
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
        public sealed class DirectoryReadResult
        {
            public string[] Entries;
            public bool LimitReached;
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
            public bool SelectedBytesLookBinary;
            public bool FileBytesLookBinary;
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

        private const int GrepMaxLineLength = 500;
        private const int RegexTimeoutMs = 250;
        private const int MaxRegexPatternLength = 512;
        private const int MaxGlobPatternLength = 256;
        private const int MaxGlobBraceExpansions = 64;
        private const int MaxScannedDirectories = 10000;
		private const int MaxDirectoryEntries = 10000;
		private const int MaxSearchFiles = 10000;
		private const long MaxGrepFileBytes = 8L * 1024L * 1024L;
		private const long MaxGrepTotalBytes = 64L * 1024L * 1024L;
		private const int MaxGrepFileLines = 250000;
		private const int MaxGrepContextLines = 20;
		private const int MaxGrepResultLimit = 1000;

		[Serializable]
		private sealed class RegexLinesRequest
		{
			public string[] Lines;
		}

		[Serializable]
		private sealed class RegexLinesResult
		{
			public int[] MatchLines;
		}

		private sealed class CompiledGlobMatcher
		{
			private readonly Regex[] _patterns;
			private readonly Regex[] _leadingDoubleStarPatterns;
			private readonly bool _matchEntryName;

			public CompiledGlobMatcher(string normalizedPattern, Regex[] patterns, Regex[] leadingDoubleStarPatterns)
			{
				_patterns = patterns;
				_leadingDoubleStarPatterns = leadingDoubleStarPatterns;
				_matchEntryName = normalizedPattern.IndexOf('/') < 0;
			}

			public bool IsMatch(string relativePath, string entryName)
			{
				if (_matchEntryName)
					return MatchesAny(entryName, _patterns);

				var normalizedRelativePath = (relativePath ?? string.Empty).Replace("\\", "/");
				return (_leadingDoubleStarPatterns != null && MatchesAny(normalizedRelativePath, _leadingDoubleStarPatterns))
					|| MatchesAny(normalizedRelativePath, _patterns);
			}

			private static bool MatchesAny(string input, Regex[] patterns)
			{
				foreach (var pattern in patterns)
				{
					if (pattern.IsMatch(input ?? string.Empty))
						return true;
				}
				return false;
			}
		}

		private sealed class CompiledFindPattern
		{
			public readonly string NormalizedPattern;
			public readonly CompiledGlobMatcher GlobMatcher;

			public CompiledFindPattern(string normalizedPattern, CompiledGlobMatcher globMatcher)
			{
				NormalizedPattern = normalizedPattern;
				GlobMatcher = globMatcher;
			}
		}

        private static string ValidateTraversalRoot(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath) ?? string.Empty;
            var current = root;
            var relative = fullPath.Substring(root.Length);
            var parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                current = string.IsNullOrEmpty(current) ? part : Path.Combine(current, part);
                var attributes = File.GetAttributes(current);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Symbolic links and junctions are not supported by the Unity filesystem sandbox: {current}");
            }
            return fullPath;
        }

        private static bool IsWithinTraversalRoot(string candidate, string root)
        {
            var fullCandidate = Path.GetFullPath(candidate);
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var prefix = fullRoot + Path.DirectorySeparatorChar;
            return string.Equals(fullCandidate, fullRoot, StringComparison.OrdinalIgnoreCase)
                || fullCandidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReparsePoint(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

		private static string[] ReadDirectoryEntriesBounded(string path, int limit, out bool overflow, CancellationToken token = default(CancellationToken))
		{
			if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
			// Keep one bounded lookahead entry for callers that need to distinguish
			// an exact 10k directory from overflow without materializing the rest.
			var effectiveLimit = Math.Min(limit, MaxDirectoryEntries + 1);
			var entries = new List<string>(effectiveLimit);
			overflow = false;
			using (var enumerator = Directory.EnumerateFileSystemEntries(path).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					token.ThrowIfCancellationRequested();
					if (entries.Count >= effectiveLimit)
					{
						overflow = true;
						break;
					}
					entries.Add(enumerator.Current);
				}
			}
			entries.Sort(StringComparer.OrdinalIgnoreCase);
			return entries.ToArray();
		}

        public static Task<string> ReadAllTextAsync(string path)
        {
            return Task.Run(() => File.ReadAllText(path));
        }

        public static string ReadPrefixHex(string path, int maxBytes)
        {
            return ReadRangeHex(path, 0, maxBytes);
        }

        public static string ReadRangeHex(string path, int offset, int maxBytes)
        {
            var limit = Math.Max(0, Math.Min(maxBytes, 65536));
            if (limit == 0) return "";
            using (var stream = File.OpenRead(path))
            {
                var start = Math.Max(0, offset);
                if (start >= stream.Length) return "";
                stream.Seek(start, SeekOrigin.Begin);
                var buffer = new byte[(int)Math.Min(limit, stream.Length - start)];
                var read = 0;
                while (read < buffer.Length)
                {
                    var next = stream.Read(buffer, read, buffer.Length - read);
                    if (next == 0) break;
                    read += next;
                }
                return BitConverter.ToString(buffer, 0, read).Replace("-", "");
            }
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

        public static Task<DirectoryReadResult> ReadDirectoryBoundedAsync(string path, int limit)
        {
			return Task.Run(() =>
			{
				var result = ReadDirectoryBounded(path, limit);
				result.Entries = result.Entries.Select(Path.GetFileName).ToArray();
				return result;
			});
        }

		public static DirectoryReadResult ReadDirectoryBounded(string path, int limit)
		{
			if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
			bool overflow;
			var entries = ReadDirectoryEntriesBounded(path, limit, out overflow);
			return new DirectoryReadResult { Entries = entries, LimitReached = overflow };
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

        private static TextRangeReadResult ExecuteReadTextRange(string path, int offsetLine, int limitLines, int maxOutputLines, int maxOutputBytes, CancellationToken token)
        {
			token.ThrowIfCancellationRequested();
            var info = new FileInfo(path);
            long startLine = Math.Max(0, offsetLine);
            long endLine = limitLines < 0 ? long.MaxValue : startLine + (long)Math.Max(0, limitLines);
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
            var fileBinaryDetector = new StreamingBinaryDetector();
            var selectedBinaryDetector = new StreamingBinaryDetector();

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

            long observedSize = 0;
            // Decode and classify the same open file snapshot. A second open would
            // let a replacement race pair an old binary verdict with new content.
            using (var rawStream = File.OpenRead(path))
            {
                observedSize = rawStream.Length;
                var rawBuffer = new byte[8192];
                var decoder = new UTF8Encoding(false, false).GetDecoder();
                var rawLineIndex = 0;
                int rawRead;
                while ((rawRead = rawStream.Read(rawBuffer, 0, rawBuffer.Length)) > 0)
                {
					token.ThrowIfCancellationRequested();
                    for (var rawIndex = 0; rawIndex < rawRead; rawIndex++)
                    {
                        var value = rawBuffer[rawIndex];
                        fileBinaryDetector.Update(value);
                        if (rawLineIndex >= startLine && rawLineIndex < endLine)
                            selectedBinaryDetector.Update(value);
                        if (value == 0x0a)
                            rawLineIndex++;
                    }

                    int bytesUsed;
                    int charsUsed;
                    bool completed;
                    decoder.Convert(rawBuffer, 0, rawRead, buffer, 0, buffer.Length, false, out bytesUsed, out charsUsed, out completed);
                    for (int i = 0; i < charsUsed; i++)
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
				token.ThrowIfCancellationRequested();

                int finalBytesUsed;
                int finalCharsUsed;
                bool finalCompleted;
                decoder.Convert(new byte[0], 0, 0, buffer, 0, buffer.Length, true, out finalBytesUsed, out finalCharsUsed, out finalCompleted);
                for (int i = 0; i < finalCharsUsed; i++)
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
                    if (ch == '\n') FinishLine();
                    else if (char.IsHighSurrogate(ch)) pendingHighSurrogate = ch;
                    else ScanLineText(ch.ToString());
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
				TotalBytes = observedSize,
                SelectedLines = selectedLines,
                SelectedBytes = selectedBytes,
                FirstLineBytes = firstLineBytes,
                OutputLines = outputLines.Count,
                OutputBytes = outputBytes,
                Truncated = truncated,
                TruncatedBy = truncatedBy,
                FirstLineExceedsLimit = firstLineExceedsLimit,
                SelectedBytesLookBinary = selectedBinaryDetector.LooksBinary,
                FileBytesLookBinary = fileBinaryDetector.LooksBinary,
                LastWriteTicksUtc = info.LastWriteTimeUtc.Ticks,
				Size = observedSize,
            };
        }

        private sealed class StreamingBinaryDetector
        {
            private long _bytes;
            private long _suspiciousControls;
            private int _remainingUtf8Bytes;
            private uint _codePoint;
            private uint _minimumCodePoint;
            private bool _invalidUtf8;
            private bool _hasNul;

            public void Update(byte value)
            {
                _bytes++;
                if (value == 0) _hasNul = true;
                if ((value < 0x20 && value != 0x09 && value != 0x0a && value != 0x0d) || value == 0x7f)
                    _suspiciousControls++;

                if (_invalidUtf8) return;
                if (_remainingUtf8Bytes == 0)
                {
                    if (value <= 0x7f) return;
                    if (value >= 0xc2 && value <= 0xdf)
                    {
                        _remainingUtf8Bytes = 1;
                        _codePoint = (uint)(value & 0x1f);
                        _minimumCodePoint = 0x80;
                        return;
                    }
                    if (value >= 0xe0 && value <= 0xef)
                    {
                        _remainingUtf8Bytes = 2;
                        _codePoint = (uint)(value & 0x0f);
                        _minimumCodePoint = 0x800;
                        return;
                    }
                    if (value >= 0xf0 && value <= 0xf4)
                    {
                        _remainingUtf8Bytes = 3;
                        _codePoint = (uint)(value & 0x07);
                        _minimumCodePoint = 0x10000;
                        return;
                    }
                    _invalidUtf8 = true;
                    return;
                }

                if ((value & 0xc0) != 0x80)
                {
                    _invalidUtf8 = true;
                    return;
                }
                _codePoint = (_codePoint << 6) | (uint)(value & 0x3f);
                _remainingUtf8Bytes--;
                if (_remainingUtf8Bytes == 0 && (_codePoint < _minimumCodePoint || _codePoint > 0x10ffff
                    || (_codePoint >= 0xd800 && _codePoint <= 0xdfff)))
                    _invalidUtf8 = true;
            }

            public bool LooksBinary => _invalidUtf8 || _remainingUtf8Bytes != 0 || _hasNul
                || (_bytes > 0 && (double)_suspiciousControls / _bytes > 0.02);
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
            var compiledPattern = ValidateFindPattern(pattern);
            return Task.Run(() =>
            {
                try
                {
                    var payload = ExecuteFind(rootPath, pattern, compiledPattern, limit, CancellationToken.None);
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
            var compiledGlob = ValidateOptionalGlobPattern(globPattern);
            return Task.Run(() =>
            {
                try
                {
                    var payload = ExecuteGrep(searchPath, pattern, globPattern, compiledGlob, ignoreCase, literal, contextLines, limit, outputMode, offset, CancellationToken.None);
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
            var compiledPattern = ValidateFindPattern(pattern);
            int id = Interlocked.Increment(ref _nextRequestId);
            var state = new RequestState(id);
            _requests[id] = state;

            PieDiagnostics.Verbose($"[PieFileBridge] find_files start path={rootPath} pattern={pattern} limit={limit}");
            Task.Run(() => RunFind(state, rootPath, pattern, compiledPattern, limit));
            return id;
        }

		public static int StartReadTextRange(string path, int offsetLine, int limitLines, int maxOutputLines, int maxOutputBytes)
		{
			int id = Interlocked.Increment(ref _nextRequestId);
			var state = new RequestState(id);
			_requests[id] = state;
			Task.Run(() => RunReadTextRange(state, path, offsetLine, limitLines, maxOutputLines, maxOutputBytes));
			return id;
		}

		public static int StartReadDirectoryBounded(string path, int limit)
		{
			int id = Interlocked.Increment(ref _nextRequestId);
			var state = new RequestState(id);
			_requests[id] = state;
			Task.Run(() => RunReadDirectoryBounded(state, path, limit));
			return id;
		}

        public static int StartGrep(string searchPath, string pattern, string globPattern, bool ignoreCase, bool literal, int contextLines, int limit, string outputMode, int offset)
        {
            var compiledGlob = ValidateOptionalGlobPattern(globPattern);
            int id = Interlocked.Increment(ref _nextRequestId);
            var state = new RequestState(id);
            _requests[id] = state;

            PieDiagnostics.Verbose($"[PieFileBridge] grep_text start path={searchPath} pattern={pattern} glob={globPattern} limit={limit}");
            Task.Run(() => RunGrep(state, searchPath, pattern, globPattern, compiledGlob, ignoreCase, literal, contextLines, limit, outputMode, offset));
            return id;
        }

		public static int StartRegexLines(string pattern, bool ignoreCase, string linesJson)
		{
			int id = Interlocked.Increment(ref _nextRequestId);
			var state = new RequestState(id);
			_requests[id] = state;
			Task.Run(() => RunRegexLines(state, pattern, ignoreCase, linesJson));
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

        private static void RunFind(RequestState state, string rootPath, string pattern, CompiledFindPattern compiledPattern, int limit)
        {
            try
            {
                var payload = ExecuteFind(rootPath, pattern, compiledPattern, limit, state.Cts.Token);
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

		private static void RunReadTextRange(RequestState state, string path, int offsetLine, int limitLines, int maxOutputLines, int maxOutputBytes)
		{
			try
			{
				var payload = ExecuteReadTextRange(path, offsetLine, limitLines, maxOutputLines, maxOutputBytes, state.Cts.Token);
				state.ResultJson = JsonUtility.ToJson(payload);
			}
			catch (OperationCanceledException)
			{
				state.Error = "Operation aborted";
			}
			catch (Exception ex)
			{
				state.Error = ex.Message;
				PieDiagnostics.Error($"[PieFileBridge] read_file range error: {ex.Message}");
			}
			finally
			{
				state.IsComplete = true;
			}
		}

		private static void RunReadDirectoryBounded(RequestState state, string path, int limit)
		{
			try
			{
				if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
				bool overflow;
				var entries = ReadDirectoryEntriesBounded(path, limit, out overflow, state.Cts.Token);
				state.ResultJson = JsonUtility.ToJson(new DirectoryReadResult
				{
					Entries = entries.Select(Path.GetFileName).ToArray(),
					LimitReached = overflow,
				});
			}
			catch (OperationCanceledException)
			{
				state.Error = "Operation aborted";
			}
			catch (Exception ex)
			{
				state.Error = ex.Message;
			}
			finally
			{
				state.IsComplete = true;
			}
		}

        private static void RunGrep(RequestState state, string searchPath, string pattern, string globPattern, CompiledGlobMatcher compiledGlob, bool ignoreCase, bool literal, int contextLines, int limit, string outputMode, int offset)
        {
            try
            {
                var payload = ExecuteGrep(searchPath, pattern, globPattern, compiledGlob, ignoreCase, literal, contextLines, limit, outputMode, offset, state.Cts.Token);
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

		private static void RunRegexLines(RequestState state, string pattern, bool ignoreCase, string linesJson)
		{
			try
			{
				var request = JsonUtility.FromJson<RegexLinesRequest>(linesJson);
				var lines = request != null && request.Lines != null ? request.Lines : new string[0];
				if (lines.Length > MaxGrepFileLines)
					throw new IOException($"Regex input exceeds the {MaxGrepFileLines} line safety limit");
				var regex = BuildRegex(pattern, ignoreCase, false);
				var matches = new List<int>();
				for (int index = 0; index < lines.Length; index++)
				{
					state.Cts.Token.ThrowIfCancellationRequested();
					if (regex.IsMatch(lines[index] ?? string.Empty)) matches.Add(index);
				}
				state.ResultJson = JsonUtility.ToJson(new RegexLinesResult { MatchLines = matches.ToArray() });
			}
			catch (OperationCanceledException)
			{
				state.Error = "Operation aborted";
			}
			catch (RegexMatchTimeoutException ex)
			{
				state.Error = "REGEX_INVALID_OR_TIMEOUT: " + ex.Message;
			}
			catch (Exception ex)
			{
				state.Error = ex.Message;
			}
			finally
			{
				state.IsComplete = true;
			}
		}

        private static FindRequestResult ExecuteFind(string rootPath, string pattern, CompiledFindPattern compiledPattern, int limit, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                throw new DirectoryNotFoundException($"Path not found: {rootPath}");

            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern must not be empty");

            rootPath = ValidateTraversalRoot(rootPath);
            var results = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int scannedDirectories = 0;
            int scannedFiles = 0;
            bool limitReached = false;
			bool searchIncomplete = false;

            while (queue.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var dir = Path.GetFullPath(queue.Dequeue());
                if (!IsWithinTraversalRoot(dir, rootPath) || IsReparsePoint(dir))
                    throw new IOException($"Search directory escapes or crosses a reparse point: {dir}");
                if (!visited.Add(dir))
                    continue;
                scannedDirectories++;
                if (scannedDirectories > MaxScannedDirectories)
                {
                    searchIncomplete = true;
                    break;
                }

				string[] entries;
				try
				{
					bool directoryOverflow;
					entries = ReadDirectoryEntriesBounded(dir, MaxDirectoryEntries, out directoryOverflow, token);
					if (directoryOverflow) searchIncomplete = true;
				}
                catch
                {
					searchIncomplete = true;
                    continue;
                }

				foreach (var entryPath in entries)
                {
                    token.ThrowIfCancellationRequested();
					if (!IsWithinTraversalRoot(entryPath, rootPath) || IsReparsePoint(entryPath))
					{
						searchIncomplete = true;
						continue;
					}

					bool isDirectory;
					try
					{
						var attributes = File.GetAttributes(entryPath);
						isDirectory = (attributes & FileAttributes.Directory) != 0;
					}
					catch
					{
						searchIncomplete = true;
						continue;
					}

                    var entryName = Path.GetFileName(entryPath);
                    if (isDirectory)
                    {
                        if (_skipDirSet.Contains(entryName))
                            continue;

						if (scannedDirectories + queue.Count >= MaxScannedDirectories)
						{
							searchIncomplete = true;
							continue;
						}
						queue.Enqueue(entryPath);
                        continue;
                    }

                    if (!File.Exists(entryPath))
                        continue;

                    scannedFiles++;
					if (scannedFiles > MaxSearchFiles)
					{
						searchIncomplete = true;
						limitReached = true;
						break;
					}
                    var relativePath = MakeRelativePath(rootPath, entryPath);
                    if (MatchesFindPattern(relativePath, entryName, compiledPattern))
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
				SearchIncomplete = searchIncomplete,
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

        private static GrepRequestResult ExecuteGrep(string searchPath, string pattern, string globPattern, CompiledGlobMatcher compiledGlob, bool ignoreCase, bool literal, int contextLines, int limit, string outputMode, int offset, CancellationToken token)
        {
			if (contextLines < 0 || contextLines > MaxGrepContextLines)
				throw new ArgumentOutOfRangeException(nameof(contextLines), $"context must be between 0 and {MaxGrepContextLines}");
			if (limit < 1 || limit > MaxGrepResultLimit)
				throw new ArgumentOutOfRangeException(nameof(limit), $"limit must be between 1 and {MaxGrepResultLimit}");
            if (string.IsNullOrWhiteSpace(searchPath))
                throw new ArgumentException("Search path must not be empty");

            bool isDirectory = Directory.Exists(searchPath);
            bool isFile = File.Exists(searchPath);
            if (!isDirectory && !isFile)
                throw new FileNotFoundException($"Path not found: {searchPath}");
            searchPath = ValidateTraversalRoot(searchPath);

            var regex = BuildRegex(pattern, ignoreCase, literal);
			bool searchIncomplete = false;
			string safetyLimit = null;
			string collectionSafetyLimit = null;
			var files = isDirectory
				? CollectSearchFiles(searchPath, compiledGlob, token, out searchIncomplete, out collectionSafetyLimit)
				: new List<string> { searchPath };
			if (isDirectory)
				safetyLimit = collectionSafetyLimit;
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
			long totalBytesScanned = 0;

            foreach (var filePath in files)
            {
                token.ThrowIfCancellationRequested();

				long fileBytes;
                try
                {
					fileBytes = new FileInfo(filePath).Length;
					if (totalBytesScanned + fileBytes > MaxGrepTotalBytes)
					{
						searchIncomplete = true;
						safetyLimit = "total_bytes";
						break;
					}
					totalBytesScanned += fileBytes;
					if (fileBytes > MaxGrepFileBytes)
					{
						if (!isDirectory)
							throw new IOException($"grep_text is limited to {MaxGrepFileBytes} bytes per file");
						searchIncomplete = true;
						safetyLimit = "file_size";
						continue;
					}
                }
                catch
                {
					if (!isDirectory)
						throw;
					searchIncomplete = true;
					safetyLimit = safetyLimit ?? "io_error";
                    continue;
                }

				var lines = new List<string>();
				bool lineOverflow = false;
				try
				{
					using (var reader = new StreamReader(filePath, Encoding.UTF8, true, 4096))
					{
						while (true)
						{
							token.ThrowIfCancellationRequested();
							var line = reader.ReadLine();
							if (line == null)
								break;
							if (lines.Count >= MaxGrepFileLines)
							{
								searchIncomplete = true;
								safetyLimit = "file_size";
								lineOverflow = true;
								break;
							}
							lines.Add(line);
						}
					}
				}
				catch (OperationCanceledException) { throw; }
				catch
				{
					if (!isDirectory) throw;
					searchIncomplete = true;
					safetyLimit = "io_error";
					continue;
				}
				if (lineOverflow) continue;
                filesScanned++;
				var lineArray = lines.ToArray();
                var relativePath = isDirectory ? MakeRelativePath(searchPath, filePath) : Path.GetFileName(filePath);
                int fileMatchCount = 0;

				for (int lineIdx = 0; lineIdx < lineArray.Length; lineIdx++)
                {
					token.ThrowIfCancellationRequested();
                    if (!regex.IsMatch(lineArray[lineIdx]))
                        continue;

                    totalMatches++;
                    fileMatchCount++;
                    if (mode == "content" && totalMatches > effectiveOffset && totalMatches <= effectiveOffset + effectiveLimit)
                    {
                        contentMatches.Add(new GrepMatchRecord
                        {
                            RelativePath = relativePath,
                            LineIndex = lineIdx,
							Lines = lineArray,
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
				SearchIncomplete = searchIncomplete,
				SafetyLimit = safetyLimit,
            };
        }

        private static List<string> CollectSearchFiles(string rootPath, CompiledGlobMatcher compiledGlob, CancellationToken token, out bool searchIncomplete, out string safetyLimit)
        {
            rootPath = ValidateTraversalRoot(rootPath);
            var files = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int scannedDirectories = 0;
			searchIncomplete = false;
			safetyLimit = null;

            while (queue.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var dir = Path.GetFullPath(queue.Dequeue());
                if (!IsWithinTraversalRoot(dir, rootPath) || IsReparsePoint(dir))
                    throw new IOException($"Search directory escapes or crosses a reparse point: {dir}");
                if (!visited.Add(dir))
                    continue;
                if (++scannedDirectories > MaxScannedDirectories)
                {
					searchIncomplete = true;
					safetyLimit = "directory_count";
                    break;
				}
				string[] entries;
				try
				{
					bool directoryOverflow;
					entries = ReadDirectoryEntriesBounded(dir, MaxDirectoryEntries, out directoryOverflow, token);
					if (directoryOverflow)
					{
						searchIncomplete = true;
						safetyLimit = "directory_entries";
					}
                }
                catch
                {
					searchIncomplete = true;
					safetyLimit = "io_error";
                    continue;
                }

                foreach (var entryPath in entries)
                {
                    token.ThrowIfCancellationRequested();
					if (!IsWithinTraversalRoot(entryPath, rootPath) || IsReparsePoint(entryPath))
					{
						searchIncomplete = true;
						safetyLimit = "io_error";
						continue;
					}

					bool isDirectory;
					try
					{
						var attributes = File.GetAttributes(entryPath);
						isDirectory = (attributes & FileAttributes.Directory) != 0;
					}
					catch
					{
						searchIncomplete = true;
						safetyLimit = "io_error";
						continue;
					}

                    var entryName = Path.GetFileName(entryPath);
                    if (isDirectory)
                    {
                        if (!_skipDirSet.Contains(entryName))
						{
							if (scannedDirectories + queue.Count >= MaxScannedDirectories)
							{
								searchIncomplete = true;
								safetyLimit = "directory_count";
							}
							else queue.Enqueue(entryPath);
						}
                        continue;
                    }

                    if (!File.Exists(entryPath))
                        continue;

                    string ext = Path.GetExtension(entryName);
                    if (_binaryExtensionSet.Contains(ext))
                        continue;

					var relativePath = MakeRelativePath(rootPath, entryPath);
					if (compiledGlob != null && !compiledGlob.IsMatch(relativePath, entryName))
                        continue;

					files.Add(entryPath);
					if (files.Count > MaxSearchFiles)
					{
						files.RemoveAt(files.Count - 1);
						searchIncomplete = true;
						safetyLimit = "file_count";
						return files;
					}
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

        private static bool MatchesFindPattern(string relativePath, string entryName, CompiledFindPattern pattern)
        {
            var normalizedRelativePath = (relativePath ?? string.Empty).Replace("\\", "/");
            if (pattern.GlobMatcher != null)
                return pattern.GlobMatcher.IsMatch(normalizedRelativePath, entryName);

            return normalizedRelativePath.Equals(pattern.NormalizedPattern, StringComparison.OrdinalIgnoreCase)
                || normalizedRelativePath.EndsWith("/" + pattern.NormalizedPattern, StringComparison.OrdinalIgnoreCase)
                || entryName.Equals(pattern.NormalizedPattern, StringComparison.OrdinalIgnoreCase)
                || entryName.IndexOf(pattern.NormalizedPattern, StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedRelativePath.IndexOf(pattern.NormalizedPattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasGlobChars(string pattern)
        {
            return pattern.IndexOf('*') >= 0
                || pattern.IndexOf('?') >= 0
                || pattern.IndexOf('[') >= 0
                || pattern.IndexOf('{') >= 0;
        }

        private static CompiledFindPattern ValidateFindPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern must not be empty");
            var normalizedPattern = pattern.Trim().Replace("\\", "/");
            if (normalizedPattern.Length > MaxGlobPatternLength)
                throw new ArgumentException($"REGEX_INVALID_OR_TIMEOUT: glob pattern exceeds the {MaxGlobPatternLength} character safety limit");
            if (normalizedPattern.StartsWith("./", StringComparison.Ordinal))
                normalizedPattern = normalizedPattern.Substring(2);
            var globMatcher = HasGlobChars(normalizedPattern)
                ? CompileGlobMatcher(normalizedPattern)
                : null;
            return new CompiledFindPattern(normalizedPattern, globMatcher);
        }

        private static CompiledGlobMatcher ValidateOptionalGlobPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return null;
            var normalizedPattern = pattern.Replace("\\", "/");
            if (normalizedPattern.Length > MaxGlobPatternLength)
                throw new ArgumentException($"REGEX_INVALID_OR_TIMEOUT: glob pattern exceeds the {MaxGlobPatternLength} character safety limit");
            return CompileGlobMatcher(normalizedPattern);
        }

        private static CompiledGlobMatcher CompileGlobMatcher(string normalizedPattern)
        {
            var patterns = CompileGlobRegexes(normalizedPattern);
            var leadingDoubleStarPatterns = normalizedPattern.StartsWith("**/", StringComparison.Ordinal)
                ? CompileGlobRegexes(normalizedPattern.Substring(3))
                : null;
            return new CompiledGlobMatcher(normalizedPattern, patterns, leadingDoubleStarPatterns);
        }

        private static Regex[] CompileGlobRegexes(string pattern)
        {
            return ExpandBracePatterns(pattern)
                .Select(GlobToRegex)
                .ToArray();
        }

        private static IEnumerable<string> ExpandBracePatterns(string pattern)
        {
            var pending = new Queue<string>();
            var expanded = new List<string>();
            pending.Enqueue(pattern ?? string.Empty);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                int open = current.IndexOf('{');
                if (open < 0)
                {
                    expanded.Add(current);
                    continue;
                }
                int close = current.IndexOf('}', open + 1);
                if (close < 0)
                    throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: unclosed glob brace");
                var choices = current.Substring(open + 1, close - open - 1).Split(',');
                if (choices.Length < 2)
                    throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: glob braces require alternatives");
                foreach (var choice in choices)
                {
                    if (expanded.Count + pending.Count >= MaxGlobBraceExpansions)
                        throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: too many glob brace expansions");
                    pending.Enqueue(current.Substring(0, open) + choice + current.Substring(close + 1));
                }
            }
            return expanded;
        }

        private static Regex GlobToRegex(string pattern)
        {
            var normalizedPattern = (pattern ?? string.Empty).Replace("\\", "/");
            if (normalizedPattern.Length > MaxGlobPatternLength)
                throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: glob pattern is too long");
			if (Regex.IsMatch(normalizedPattern, @"(?:^|[^\\])[@+?!*]\("))
				throw new ArgumentException("GLOB_UNSUPPORTED: grep_text does not support extglob; use brace alternatives such as *.{ts,js}");

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

                if (c == '[')
                {
                    int close = normalizedPattern.IndexOf(']', i + 1);
                    if (close < 0)
                        throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: unclosed glob character class");
                    var content = normalizedPattern.Substring(i + 1, close - i - 1);
                    if (content.Length == 0)
                        throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: empty glob character class");
                    bool negate = content[0] == '!' || content[0] == '^';
                    if (negate) content = content.Substring(1);
                    if (content.Length == 0)
                        throw new ArgumentException("REGEX_INVALID_OR_TIMEOUT: empty glob character class");
                    sb.Append("[");
                    if (negate) sb.Append("^");
                    foreach (char classChar in content)
                    {
                        if (classChar == '\\' || classChar == ']' || classChar == '^')
                            sb.Append("\\");
                        sb.Append(classChar);
                    }
                    sb.Append("]");
                    i = close;
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
