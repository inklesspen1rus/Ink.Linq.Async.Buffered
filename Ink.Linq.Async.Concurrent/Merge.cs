using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ink.Linq.Async.Concurrent
{
    public readonly struct Merge<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<IAsyncEnumerable<T>> _sources;

        public Merge(IEnumerable<IAsyncEnumerable<T>> sources)
        {
            _sources = sources;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            var enumerables = _sources.ToList();
            var sources = new List<IAsyncEnumerator<T>>(enumerables.Count);
            var tasks = new List<Task<bool>>(sources.Count);
            var disposes = new List<Task>(sources.Count);
            try
            {
                foreach (var asyncEnumerable in _sources)
                {
                    var source = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
                    sources.Add(source);
                    tasks.Add(source.MoveNextAsync().AsTask());
                }

                while (sources.Count > 0)
                {
                    await Task.WhenAny(tasks);
                    for (var x = sources.Count - 1; x >= 0; x--)
                    {
                        var task = tasks[x];
                        if (!task.IsCompleted) continue;
                        
                        if (task.Result)
                        {
                            yield return sources[x].Current;
                            tasks[x] = sources[x].MoveNextAsync().AsTask();
                        }
                        else
                        {
                            tasks.RemoveAt(x);
                            var source = sources[x];
                            sources.RemoveAt(x);
                            disposes.Add(source.DisposeAsync().AsTask());
                        }
                    }
                }
            }
            finally
            {
                await Task.WhenAll(sources.Select(x => x.DisposeAsync().AsTask()));
                await Task.WhenAll(disposes);
            }
        }
    }
}