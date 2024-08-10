using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ink.Linq.Async.Concurrent
{
    public readonly struct Buffered<T> : IAsyncEnumerable<T>
    {
        internal readonly IAsyncEnumerable<Task<T>> Source;
        internal readonly int BufferSize;

        public Buffered(IAsyncEnumerable<Task<T>> source, int bufferSize)
        {
            Source = source;
            BufferSize = bufferSize;
        
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Count must be greater than zero");
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            var buffer = new Queue<Task<T>>(BufferSize);
            await using var enumerator = Source.GetAsyncEnumerator(cancellationToken);
            var canMove = true;

            while (buffer.Count < BufferSize)
            {
                canMove = await enumerator.MoveNextAsync();
                if (!canMove) break;
                if (enumerator.Current.IsCompleted && buffer.Count == 0)
                {
                    yield return enumerator.Current.Result;
                    continue;
                }
                buffer.Enqueue(enumerator.Current);
            }

            while (buffer.Count > 0)
            {
                var task = buffer.Dequeue();
                yield return await task;

                if (canMove && (canMove = await enumerator.MoveNextAsync()))
                    buffer.Enqueue(enumerator.Current);
            }
        }
    }
}