using System.Diagnostics.Contracts;
using System.Threading.Channels;
using Ink.Linq.Async.Concurrent.Extensions;

namespace Ink.Linq.Async.Concurrent.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task TestAsyncBarrierFailure()
    {
        await using var barrier = new AsyncBarrier(2);
        try
        {
            await barrier.WaitAsync().WaitAsync(TimeSpan.FromMilliseconds(1000));
            throw new Exception("Must be deadlocked");
        }
        catch(TimeoutException) {}
    }

    [Test, Timeout(1000)]
    public async Task TestAsyncBarrier()
    {
        await using var barrier = new AsyncBarrier(5);

        var tasks = new List<Task>();
        tasks.Add(barrier.WaitAsync());
        await Task.Delay(50);
        ThrowIfAnyCompleted(); // We have 1 subscriber , barrier requirements are not satisfied
        tasks.Add(barrier.WaitAsync());
        await Task.Delay(50);
        ThrowIfAnyCompleted(); // We have 2 subscribers, barrier requirements are not satisfied
        tasks.Add(barrier.WaitAsync());
        await Task.Delay(50);
        ThrowIfAnyCompleted(); // We have 3 subscribers, barrier requirements are not satisfied
        tasks.Add(barrier.WaitAsync());
        await Task.Delay(50);
        ThrowIfAnyCompleted(); // We have 4 subscribers, barrier requirements are not satisfied
        
        await barrier.WaitAsync(); // We got 5 subscribers and must complete here
        await Task.WhenAll(tasks);
        return;

        void ThrowIfAnyCompleted()
        {
            var count = tasks
                .Count(x => x.IsCompleted);
            if (count != 0)
            {
                throw new Exception("Some tasks are completed");
            }
        }
    }

    [Test, Timeout(2000)]
    public async Task BufferedConcurrency()
    {
        await using var barrier = new AsyncBarrier(5);
        var func = async (int x) =>
        {
            // Wait 100ms just for fun
            await Task.Delay(100);
            
            // Wait for group of 5 tasks
            await barrier.WaitAsync();

            return x;
        };

        await Enumerable.Range(0, 20)
            .ToAsyncEnumerable()
            .Select(func)
            .Buffered(5) // Load 5 tasks at once to satisfy barrier requirements
            .Unordered()
            .ForEachAsync(_ => {});
    }

    [Test, Timeout(1000)]
    public async Task Merge([Values(2, 4, 9, 16)] int mergeCount)
    {
        await using var barrier = new AsyncBarrier(mergeCount);

        var sequence = Enumerable.Range(0, 10)
            .ToAsyncEnumerable()
            .SelectAwait(async x => await Func(x));

        var sequences = await Enumerable
            .Repeat(sequence, mergeCount)
            .MergeAsyncs()
            .SumAsync();

        return;

        async Task<int> Func(int x)
        {
            await Task.Delay(50); // Wait 50ms just for fun
            // ReSharper disable once AccessToDisposedClosure
            await barrier.WaitAsync();
            return x;
        }
    }
}