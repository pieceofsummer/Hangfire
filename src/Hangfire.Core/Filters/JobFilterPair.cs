using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Filters
{
    /// <summary>
    /// Represents either sync or async job filter at given position in <see cref="JobFilterCursor"/>.
    /// </summary>
    /// <typeparam name="TSync">Sync filter type</typeparam>
    /// <typeparam name="TAsync">Async filter type</typeparam>
    internal struct JobFilterPair<TSync, TAsync>
        where TSync : class, ISyncAsyncPair<TSync, TAsync>
        where TAsync : class, ISyncAsyncPair<TSync, TAsync>
    {
        public readonly TSync Sync;
        public readonly TAsync Async;

        public bool NotFound => Async == null && Sync == null;

        /// <summary>
        /// Initializes a <see cref="JobFilterPair{TSync, TAsync}"/> structure.
        /// </summary>
        /// <param name="sync">Synchronous filter implementation, or <c>null</c> if filter implements only asynchronous variant.</param>
        /// <param name="async">Asynchronous filter implementation, or <c>null</c> if filter implements only synchronous variant.</param>
        public JobFilterPair(TSync sync, TAsync async)
        {
            Sync = sync;
            Async = async;
        }
    }
}
