using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kibo.TestingFramework
{
    public static class Poller
    {
        public static async Task<T> WaitUntilAsync<T>(
            Func<Task<T>> action,
            Func<T, bool> condition,
            TimeSpan? interval = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            interval ??= TimeSpan.FromMilliseconds(500);
            timeout ??= TimeSpan.FromSeconds(15);
            var start = DateTime.UtcNow;
            T? lastResult = default;
            Exception? lastException = null;
            while (DateTime.UtcNow - start < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    lastResult = await action();
                    if (condition(lastResult))
                        return lastResult;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
                await Task.Delay(interval.Value, cancellationToken);
            }
            var msg = $"Polling timed out after {timeout}. Last result: {lastResult?.ToString() ?? "<null>"}";
            if (lastException != null)
                msg += $". Last exception: {lastException}";
            throw new TimeoutException(msg);
        }
    }
}
