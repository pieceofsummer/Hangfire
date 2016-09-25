namespace Hangfire.Filters
{
    /// <summary>
    /// Implements a lightweight bi-directional iterator over collection of job filters, 
    /// which can be either synchronous or asynchronous (or both).
    /// </summary>
    internal struct JobFilterCursor
    {
        private int _index;
        private readonly object[] _filters;

        public JobFilterCursor(int index, object[] filters)
        {
            _index = index;
            _filters = filters;
        }

        public JobFilterCursor(object[] filters)
        {
            _index = 0;
            _filters = filters;
        }

        public void Reset()
        {
            _index = 0;
        }

        /// <summary>
        /// Returns a next job filter matching at least one of the 
        /// <typeparamref name="TSync"/>, <typeparamref name="TAsync"/> types.
        /// </summary>
        /// <typeparam name="TSync">Sync filter type</typeparam>
        /// <typeparam name="TAsync">Async filter type</typeparam>
        public JobFilterPair<TSync, TAsync> GetNextFilter<TSync, TAsync>()
            where TSync : class, ISyncAsyncPair<TSync, TAsync>
            where TAsync : class, ISyncAsyncPair<TSync, TAsync>
        {
            while (_index < _filters.Length)
            {
                var sync = _filters[_index] as TSync;
                var async = _filters[_index] as TAsync;

                _index += 1;

                if (sync != null || async != null)
                {
                    return new JobFilterPair<TSync, TAsync>(sync, async);
                }
            }

            return default(JobFilterPair<TSync, TAsync>);
        }

        /// <summary>
        /// Returns a previous job filter matching at least one of the 
        /// <typeparamref name="TSync"/>, <typeparamref name="TAsync"/> types.
        /// </summary>
        /// <typeparam name="TSync">Sync filter type</typeparam>
        /// <typeparam name="TAsync">Async filter type</typeparam>
        public JobFilterPair<TSync, TAsync> GetPrevFilter<TSync, TAsync>()
            where TSync : class, ISyncAsyncPair<TSync, TAsync>
            where TAsync : class, ISyncAsyncPair<TSync, TAsync>
        {
            while (--_index > 0)
            {
                var sync = _filters[_index - 1] as TSync;
                var async = _filters[_index - 1] as TAsync;
                
                if (sync != null || async != null)
                {
                    return new JobFilterPair<TSync, TAsync>(sync, async);
                }
            }

            return default(JobFilterPair<TSync, TAsync>);
        }
    }
}
