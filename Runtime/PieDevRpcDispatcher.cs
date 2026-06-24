#if PIE_UNITY_SPLIT_SOURCES
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Pie
{
    public static class PieDevRpcDispatcher
    {
        private sealed class WorkItem
        {
            public Func<string> action;
            public ManualResetEventSlim done;
            public string result;
            public Exception error;
            public int state;
        }

        private static readonly ConcurrentQueue<WorkItem> Queue = new ConcurrentQueue<WorkItem>();
        private static int _mainThreadId = -1;

        public static int MainThreadId => _mainThreadId;
        public static int CurrentThreadId => Thread.CurrentThread.ManagedThreadId;

        public static void InitializeMainThread()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static string InvokeSync(Func<string> action, int timeoutMs = 5000)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                return action();

            var item = new WorkItem
            {
                action = action,
                done = new ManualResetEventSlim(false),
            };

            Queue.Enqueue(item);
            if (!item.done.Wait(timeoutMs))
            {
                Interlocked.CompareExchange(ref item.state, 2, 0);
                throw new TimeoutException("Timed out waiting for Unity main thread.");
            }

            if (item.error != null)
                throw item.error;

            return item.result;
        }

        public static void Tick()
        {
            while (Queue.TryDequeue(out var item))
            {
                if (Interlocked.CompareExchange(ref item.state, 1, 0) != 0)
                {
                    item.done.Set();
                    continue;
                }
                try
                {
                    item.result = item.action();
                }
                catch (Exception ex)
                {
                    item.error = ex;
                }
                finally
                {
                    item.done.Set();
                }
            }
        }
    }
}

#endif
