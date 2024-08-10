using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ink.Linq.Async.Concurrent.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        /// <summary>
        /// Buffering started tasks in internal buffer and returning result in FIFO style
        /// <para>It is not recommended to use high values for buffer size</para>
        /// </summary>
        public static Buffered<T> Buffered<T>(this IAsyncEnumerable<Task<T>> source, int bufferSize) =>
            new Buffered<T>(source, bufferSize);

        /// <summary>
        /// Make buffered enumerable return first ready results from buffer
        /// <para>It is not recommended to use high values for buffer size</para>
        /// </summary>
        public static BufferedUnordered<T> Unordered<T>(this Buffered<T> source) =>
            new BufferedUnordered<T>(source.Source, source.BufferSize);
        
        /// <summary>
        /// Make buffered enumerable return results in original order from buffer
        /// <para>It is not recommended to use high values for buffer size</para>
        /// </summary>
        public static Buffered<T> Ordered<T>(this BufferedUnordered<T> source) =>
            new Buffered<T>(source.Source, source.BufferSize);

        /// <summary>
        /// Merge enumerables concurrently
        /// <para>It is not recommended to merge large counts of enumerables</para>
        /// </summary>
        public static Merge<T> Merge<T>(this IAsyncEnumerable<T> source, params IAsyncEnumerable<T>[] sources) =>
            source.Merge(sources.AsEnumerable());

        /// <summary>
        /// Merge enumerables concurrently
        /// <para>It is not recommended to merge large counts of enumerables</para>
        /// </summary>
        public static Merge<T> Merge<T>(this IAsyncEnumerable<T> source, IEnumerable<IAsyncEnumerable<T>> sources) =>
            new Merge<T>(sources.Prepend(source));
        
        /// <summary>
        /// Merge enumerables concurrently
        /// <para>It is not recommended to merge large counts of enumerables</para>
        /// </summary>
        public static Merge<T> MergeAsyncs<T>(this IEnumerable<IAsyncEnumerable<T>> sources) =>
            new Merge<T>(sources);
    }
}