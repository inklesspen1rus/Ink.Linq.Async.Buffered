using System.Threading.Channels;

namespace Ink.Linq.Async.Concurrent.Tests;

public class AsyncBarrier : IAsyncDisposable
{
    private readonly Task _worker;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Channel<object?> _channelIn;
    private readonly Channel<object?> _channelOut;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Releases subscribers in fixed-size groups.
    /// </summary>
    /// <param name="count"></param>
    public AsyncBarrier(int count)
    {
        // Barrier will be unblocked only when 5 tasks will try to acquire it
        _channelIn = Channel.CreateBounded<object?>(count);
        _channelOut = Channel.CreateBounded<object?>(count);
        _semaphore = new SemaphoreSlim(count);
        _cancellationTokenSource = new CancellationTokenSource();
        _worker = Task.Run(async () =>
        {
            try
            {
                var cancellationToken = _cancellationTokenSource.Token;
                for (;;)
                {
                    for (var x = 0; x < count; x++)
                        _ = await _channelIn.Reader.ReadAsync(cancellationToken);
                    for (var x = 0; x < count; x++)
                        await _semaphore.WaitAsync(cancellationToken);
                    for (var x = 0; x < count; x++)
                        await _channelOut.Writer.WriteAsync(null, cancellationToken);
                    _semaphore.Release(count);
                }
            }
            catch (TaskCanceledException) {}
            catch (OperationCanceledException) {}
            // ReSharper disable once FunctionNeverReturns
        });
    }

    /// <summary>
    /// Completed only if barrier get full group of subscribers
    /// </summary>
    public async Task WaitAsync()
    {
        await _semaphore.WaitAsync(_cancellationTokenSource.Token);
        await _channelIn.Writer.WriteAsync(null, _cancellationTokenSource.Token);
        _semaphore.Release();
        await _channelOut.Reader.ReadAsync(_cancellationTokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        await _worker;
    }
}