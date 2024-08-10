using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ink.Linq.Async.Concurrent
{
    public readonly struct BufferedUnordered<T> : IAsyncEnumerable<T>
    {
        internal readonly IAsyncEnumerable<Task<T>> Source;
        internal readonly int BufferSize;

        public BufferedUnordered(IAsyncEnumerable<Task<T>> source, int bufferSize)
        {
            Source = source;
            BufferSize = bufferSize;
        
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Count must be greater than zero");
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            var buffer = new List<Task<T>>(BufferSize);
            await using var enumerator = Source.GetAsyncEnumerator(cancellationToken);
            var canMove = true;

            while (buffer.Count < BufferSize)
            {
                canMove = await enumerator.MoveNextAsync();
                if (!canMove) break;
                if (enumerator.Current.IsCompleted)
                {
                    yield return enumerator.Current.Result;
                    continue;
                }
                buffer.Add(enumerator.Current);
            }

            while (buffer.Count > 0)
            {
                var task = await Task.WhenAny(buffer);
                buffer.Remove(task);
                yield return task.Result;

                if (canMove && (canMove = await enumerator.MoveNextAsync()))
                {
                    if (enumerator.Current.IsCompleted)
                    {
                        yield return enumerator.Current.Result;
                        continue;
                    }
                    buffer.Add(enumerator.Current);
                }
            }
        }
    }
}