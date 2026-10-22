using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pie
{
    // All public methods are polled from the host thread. Background work never calls JS/Unity.
    public static class PieWebSocketBridge
    {
        private sealed class Connection
        {
            internal readonly ClientWebSocket Socket = new ClientWebSocket();
            internal readonly CancellationTokenSource Cancel = new CancellationTokenSource();
            internal readonly SemaphoreSlim SendGate = new SemaphoreSlim(1, 1);
            internal readonly ConcurrentQueue<Tuple<string, int>> Messages = new ConcurrentQueue<Tuple<string, int>>();
            internal readonly ConcurrentDictionary<int, int> Sends = new ConcurrentDictionary<int, int>();
            internal readonly object Sync = new object();
            internal Uri Url;
            internal int State, MessageBytes, MessageCount, SendBytes, SendCount, NextSend;
            internal int MaxMessage, MaxBytes, MaxCount, Timeout, CloseCode = 1000;
            internal string Error = "";
            internal bool Started, Stopped;
            internal long LastReceiveTicks = DateTime.UtcNow.Ticks;
            internal readonly CancellationToken Token;
            internal Connection() { Token = Cancel.Token; }
        }
        private static readonly ConcurrentDictionary<int, Connection> Connections = new ConcurrentDictionary<int, Connection>();
        private static int nextId;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static int Create(string url, int maxMessageBytes, int maxBufferedBytes, int maxBufferedMessages, int timeoutMs)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException("WebSocket bridge is unavailable on WebGL.");
#else
            if (maxMessageBytes <= 0 || maxBufferedBytes <= 0 || maxBufferedMessages <= 0 || timeoutMs <= 0)
                throw new ArgumentOutOfRangeException("WebSocket limits must be positive.");
            var uri = new Uri(url);
            if (uri.Scheme != "ws" && uri.Scheme != "wss") throw new ArgumentException("WebSocket URL required.");
            var c = new Connection { Url = uri, MaxMessage = maxMessageBytes, MaxBytes = maxBufferedBytes, MaxCount = maxBufferedMessages, Timeout = timeoutMs };
            var id = Interlocked.Increment(ref nextId);
            Connections[id] = c;
            return id;
#endif
        }
        public static void SetHeader(int id, string name, string value)
        {
            var c = Connections[id];
            lock (c.Sync) { if (c.Started) throw new InvalidOperationException("Handshake already started."); c.Socket.Options.SetRequestHeader(name, value); }
        }
        public static void Start(int id)
        {
            var c = Connections[id];
            lock (c.Sync) { if (c.Started) throw new InvalidOperationException("Already started."); c.Started = true; }
            _ = Run(c);
        }
        private static async Task Run(Connection c)
        {
            try
            {
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(c.Token))
                {
                    deadline.CancelAfter(c.Timeout);
                    try
                    {
                        // Unity Mono does not reliably interrupt a stalled HTTP upgrade with the token alone.
                        using (deadline.Token.Register(() => Fail(c, c.Token.IsCancellationRequested ? "aborted" : "timeout")))
                            await c.Socket.ConnectAsync(c.Url, deadline.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { Fail(c, c.Token.IsCancellationRequested ? "aborted" : "timeout"); return; }
                    catch { Fail(c, "handshake"); return; }
                }
                lock (c.Sync) { if (c.Token.IsCancellationRequested) return; c.State = 1; c.LastReceiveTicks = DateTime.UtcNow.Ticks; }
                var buffer = new byte[8192];
                while (!c.Token.IsCancellationRequested)
                {
                    using (var message = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await c.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), c.Token).ConfigureAwait(false);
                            Interlocked.Exchange(ref c.LastReceiveTicks, DateTime.UtcNow.Ticks);
                            if (result.MessageType == WebSocketMessageType.Close) { c.CloseCode = (int)(result.CloseStatus ?? WebSocketCloseStatus.Empty); return; }
                            if (result.MessageType != WebSocketMessageType.Text) { Fail(c, "protocol"); return; }
                            if (message.Length + result.Count > c.MaxMessage) { Fail(c, "overflow"); return; }
                            message.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage);
                        var bytes = (int)message.Length;
                        var text = Utf8.GetString(message.GetBuffer(), 0, bytes);
                        lock (c.Sync)
                        {
                            if (c.Stopped) return;
                            if (c.MessageCount >= c.MaxCount || c.MessageBytes + bytes > c.MaxBytes) { Fail(c, "overflow"); return; }
                            c.MessageCount++; c.MessageBytes += bytes; c.Messages.Enqueue(Tuple.Create(text, bytes));
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch { Fail(c, "protocol"); }
            finally
            {
                Stop(c);
            }
        }
        private static void Fail(Connection c, string kind)
        {
            lock (c.Sync) { if (c.Stopped) return; if (c.Error.Length == 0) c.Error = kind; c.State = 3; }
            Stop(c);
        }
        private static void Stop(Connection c)
        {
            lock (c.Sync) { if (c.Stopped) return; c.Stopped = true; c.State = 3; }
            try { c.Cancel.Cancel(); } finally { c.Cancel.Dispose(); c.Socket.Abort(); c.Socket.Dispose(); }
        }
        public static int GetState(int id) { return Connections.TryGetValue(id, out var c) ? Volatile.Read(ref c.State) : 3; }
        public static string GetErrorKind(int id) { return Connections.TryGetValue(id, out var c) ? c.Error : "closed"; }
        public static double GetIdleMilliseconds(int id) { return Connections.TryGetValue(id, out var c) ? (DateTime.UtcNow.Ticks - Interlocked.Read(ref c.LastReceiveTicks)) / (double)TimeSpan.TicksPerMillisecond : 0; }
        public static int GetCloseCode(int id) { return Connections.TryGetValue(id, out var c) ? c.CloseCode : 1000; }
        public static string Poll(int id)
        {
            if (!Connections.TryGetValue(id, out var c)) return null;
            lock (c.Sync)
            {
                if (!c.Messages.TryDequeue(out var item)) return null;
                c.MessageCount--; c.MessageBytes -= item.Item2; return item.Item1;
            }
        }
        public static int Send(int id, string text)
        {
            var c = Connections[id]; var bytes = Utf8.GetBytes(text); int sendId;
            lock (c.Sync)
            {
                if (c.State != 1) throw new InvalidOperationException("WebSocket is not open.");
                if (bytes.Length > c.MaxMessage || c.SendBytes + bytes.Length > c.MaxBytes || c.SendCount >= c.MaxCount)
                    throw new InvalidOperationException("WebSocket outgoing buffer exceeded.");
                c.SendBytes += bytes.Length; c.SendCount++; sendId = ++c.NextSend; c.Sends[sendId] = 0;
            }
            _ = SendAsync(c, sendId, bytes); return sendId;
        }
        private static async Task SendAsync(Connection c, int id, byte[] bytes)
        {
            bool entered = false;
            try
            {
                await c.SendGate.WaitAsync(c.Token).ConfigureAwait(false); entered = true;
                await c.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, c.Token).ConfigureAwait(false);
                c.Sends[id] = 1;
            }
            catch { c.Sends[id] = -1; Fail(c, c.Token.IsCancellationRequested ? "aborted" : "protocol"); }
            finally { if (entered) c.SendGate.Release(); lock (c.Sync) { c.SendBytes -= bytes.Length; c.SendCount--; } }
        }
        // Completed send entries are consumed once, preventing per-connection growth.
        public static int PollSend(int id, int sendId)
        {
            if (!Connections.TryGetValue(id, out var c) || !c.Sends.TryGetValue(sendId, out var status)) return -1;
            if (status != 0) c.Sends.TryRemove(sendId, out _);
            return status;
        }
        public static void Close(int id)
        {
            if (!Connections.TryGetValue(id, out var c)) return;
            lock (c.Sync) { if (c.State >= 2) return; if (c.State == 0) { Fail(c, "aborted"); return; } c.State = 2; }
            _ = CloseAsync(c);
        }
        private static async Task CloseAsync(Connection c)
        {
            using (var deadline = new CancellationTokenSource(1000))
            {
                bool entered = false;
                try
                {
                    await c.SendGate.WaitAsync(deadline.Token).ConfigureAwait(false); entered = true;
                    await c.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", deadline.Token).ConfigureAwait(false);
                    while (Volatile.Read(ref c.State) != 3) await Task.Delay(10, deadline.Token).ConfigureAwait(false);
                }
                catch { }
                finally { if (entered) c.SendGate.Release(); Stop(c); }
            }
        }
        public static void Release(int id)
        {
            if (!Connections.TryRemove(id, out var c)) return;
            Stop(c);
        }
        public static void CancelAll()
        {
            foreach (var id in Connections.Keys) Release(id);
        }
        public static int ActiveCount => Connections.Count;
    }
}
