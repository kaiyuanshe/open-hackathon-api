using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Extensions
{
    public static class AsyncEnumeratorExtension
    {
        public static async Task ForEach<T>(this IAsyncEnumerable<T> source, Action<T> action, int? limit = null, CancellationToken cancellationToken = default)
        {
            IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator();
            int count = 0;
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    action(enumerator.Current);
                    if (limit.HasValue)
                    {
                        if (Interlocked.Increment(ref count) >= limit.Value)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        public static async Task ForEach<T>(this IAsyncEnumerable<T> source, Func<T, Task> func, int? limit = null, CancellationToken cancellationToken = default)
        {
            IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator();
            int count = 0;
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await func(enumerator.Current);
                    if (limit.HasValue)
                    {
                        if (Interlocked.Increment(ref count) >= limit.Value)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        public async static Task ParallelForEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task> func, int maxParallelism, int? limit = null, CancellationToken cancellationToken = default)
        {
            var taskSlots = new Task[maxParallelism];
            for (int i = 0; i < maxParallelism; i++)
            {
                taskSlots[i] = Task.CompletedTask;
            }

            IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator();
            int count = 0;
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await QueueAsync(func, enumerator.Current, taskSlots);
                    if (limit.HasValue)
                    {
                        if (Interlocked.Increment(ref count) >= limit.Value)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            await Task.WhenAll(taskSlots);
        }

        private async static ValueTask QueueAsync<T>(
            Func<T, Task> func,
            T item,
            Task[] taskSlots)
        {
            for (int i = 0; i < taskSlots.Length; i++)
            {
                if (taskSlots[i].IsCompleted)
                {
                    await taskSlots[i];
                    taskSlots[i] = func(item);
                    return;
                }
            }
            var task = await Task.WhenAny(taskSlots);
            await task;
            var index = Array.IndexOf(taskSlots, task);
            taskSlots[index] = func(item);
        }
    }
}
