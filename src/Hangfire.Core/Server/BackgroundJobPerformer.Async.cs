using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Filters;
using Hangfire.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    public partial class BackgroundJobPerformer
    {
        /// <summary>
        /// Async implementation of <see cref="BackgroundJobPerformer"/>.
        /// Implemented as a separate class because it needs to maintain an internal state, 
        /// and <see cref="BackgroundJobPerformer"/>  cannot be used for that, because it's a singleton.
        /// </summary>
        internal class AsyncImpl
        {
            private static readonly ILog _log = LogProvider.For<BackgroundJobPerformer>();

            private readonly IJobFilterProvider _filterProvider;
            private readonly IBackgroundJobPerformer _innerPerformer;

            private JobFilterCursor _filters;
            private PerformContext _context = null;
            private PerformingContext _performingContext = null;
            private PerformedContext _performedContext = null;
            private ServerExceptionContext _exceptionContext = null;
            private object _result = null;

            public AsyncImpl(
                [NotNull] IJobFilterProvider filterProvider,
                [NotNull] IBackgroundJobPerformer innerPerformer)
            {
                if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
                if (innerPerformer == null) throw new ArgumentNullException(nameof(innerPerformer));

                _filterProvider = filterProvider;
                _innerPerformer = innerPerformer;
            }

            public async Task<object> InvokeAsync(PerformContext context)
            {
                if (context == null) throw new ArgumentNullException(nameof(context));

                object state = null;
                _context = context;
                _filters = new JobFilterCursor(_filterProvider
                                               .GetFilters(context.BackgroundJob.Job)
                                               .Select(x => x.Instance)
                                               .ToArray());
                try
                {
                    // Primary loop to handle job filters and background jobs
                    _performingContext = new PerformingContext(_context);

                    try
                    {
                        for (var next = State.Begin; next != State.End;
                            _context.CancellationToken.ThrowIfCancellationRequested())
                        {
                            // All processing is performed in a tight loop inside the following method.
                            // We'll only get back here sometimes to wait for the next Task to complete.
                            // For performance's sake, we'll only await for really unfinished jobs 
                            // (i.e. not constanst/cached/finished ones like Task.FromResult() etc.)
                            await JobFilters(ref next, ref state);
                        }
                    }
                    catch (Exception ex)
                    {
                        // This is an exception from pre/post-processing phases. 
                        // Though it shouldn't be processed by other job filters,
                        // it still may be handled by exception filters.
                        CoreBackgroundJobPerformer.HandleJobPerformanceException(ex,
                            _context.CancellationToken.ShutdownToken);
                    }

                    if (_performedContext?.Exception != null && !_performedContext.ExceptionHandled)
                    {
                        // This is an exception from job processing phase. 
                        // It has been delivered to all job filters, but still unhandled.
                        // Rethrow it here, so it will be picked up by exception filters.
                        ExceptionDispatchInfo.Capture(_performedContext.Exception).Throw();
                    }
                }
                catch (JobAbortedException)
                {
                    // Never intercept JobAbortException, it is supposed for internal use.
                    throw;
                }
                catch (OperationCanceledException) when
                    (_context.CancellationToken.ShutdownToken.IsCancellationRequested)
                {
                    // Don't intercept OperationCancelledException after cancellation was requested.
                    throw;
                }
                catch (Exception ex)
                {
                    // Secondary loop to handle exception filters
                    _exceptionContext = new ServerExceptionContext(_context, ex);

                    for (var next = EFState.Begin; next != EFState.End;
                        _context.CancellationToken.ThrowIfCancellationRequested())
                    {
                        await ExceptionFilters(ref next, ref state);
                    }

                    if (!_exceptionContext.ExceptionHandled)
                    {
                        // None of the exception filters has handled the exception.
                        // We're out of options, so just re-throw it here.
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }

                return _result;
            }

            private Task JobFilters(ref State next, ref object state)
            {
                Debug.Assert(next > State.PerformAsyncBegin || _performingContext != null, "performingContext not initialized");
                Debug.Assert(next < State.PerformAsyncEnd || _performedContext != null, "performedContext not initialized");

                JobFilterPair<IServerFilter, IAsyncServerFilter> filter;
                IServerFilter syncFilter; IAsyncServerFilter asyncFilter;
                Task task;

                switch (next)
                {
                    case State.Begin:
                        {
                            _filters.Reset();
                            goto case State.OnPerformingNext;
                        }

                    case State.OnPerformingNext:
                        {
                            filter = _filters.GetNextFilter<IServerFilter, IAsyncServerFilter>();
                            if (filter.NotFound)
                            {
                                goto case State.PerformAsyncBegin;
                            }
                            else if (filter.Async != null)
                            {
                                state = filter.Async;
                                goto case State.OnPerformingAsyncBegin;
                            }
                            else
                            {
                                state = filter.Sync;
                                goto case State.OnPerformingSync;
                            }
                        }

                    case State.OnPerformingAsyncBegin:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerFilter)state;
                            _log.DebugFormat("enter '{0}.OnPerformingAsync'", asyncFilter.GetType().Name);

                            task = asyncFilter.OnPerformingAsync(_performingContext);
                            if (task.Status != TaskStatus.RanToCompletion)
                            {
                                next = State.OnPerformingAsyncEnd;
                                return task;
                            }

                            goto case State.OnPerformingAsyncEnd;
                        }

                    case State.OnPerformingAsyncEnd:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerFilter)state;
                            _log.DebugFormat("leave '{0}.OnPerformingAsync'", asyncFilter.GetType().Name);

                            goto case State.OnPerformingCheckCancel;
                        }

                    case State.OnPerformingSync:
                        {
                            Debug.Assert(state != null);

                            syncFilter = (IServerFilter)state;
                            _log.DebugFormat("enter '{0}.OnPerforming'", syncFilter.GetType().Name);

                            syncFilter.OnPerforming(_performingContext);

                            _log.DebugFormat("leave '{0}.OnPerforming'", syncFilter.GetType().Name);

                            goto case State.OnPerformingCheckCancel;
                        }

                    case State.OnPerformingCheckCancel:
                        {
                            if (_performingContext.Canceled)
                            {
                                _performedContext = new PerformedContext(_context, null, true, null);
                                goto case State.OnCancelPrev;
                            }

                            goto case State.OnPerformingNext;
                        }

                    case State.OnCancelPrev:
                        {
                            filter = _filters.GetPrevFilter<IServerFilter, IAsyncServerFilter>();
                            if (filter.NotFound)
                            {
                                goto case State.End;
                            }
                            else if (filter.Async != null)
                            {
                                state = filter.Async;
                                goto case State.OnCancelAsyncBegin;
                            }
                            else
                            {
                                state = filter.Sync;
                                goto case State.OnCancelSync;
                            }
                        }

                    case State.OnCancelAsyncBegin:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerFilter)state;
                            _log.DebugFormat("enter '{0}.OnPerformedAsync'", asyncFilter.GetType().Name);

                            task = asyncFilter.OnPerformedAsync(_performedContext);
                            if (task.Status != TaskStatus.RanToCompletion)
                            {
                                next = State.OnCancelAsyncEnd;
                                return task;
                            }

                            goto case State.OnCancelAsyncEnd;
                        }

                    case State.OnCancelAsyncEnd:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerFilter)state;
                            _log.DebugFormat("leave '{0}.OnPerformedAsync'", asyncFilter.GetType().Name);

                            goto case State.OnCancelPrev;
                        }

                    case State.OnCancelSync:
                        {
                            Debug.Assert(state != null);

                            syncFilter = (IServerFilter)state;
                            _log.DebugFormat("enter '{0}.OnPerformed'", syncFilter.GetType().Name);

                            syncFilter.OnPerformed(_performedContext);

                            _log.DebugFormat("leave '{0}.OnPerformed'", syncFilter.GetType().Name);

                            goto case State.OnCancelPrev;
                        }

                    case State.PerformAsyncBegin:
                        {
                            _log.DebugFormat("enter '{0}'", _context.BackgroundJob.Job);

                            task = ExecuteJobMethodAsync();
                            if (task.Status != TaskStatus.RanToCompletion)
                            {
                                next = State.PerformAsyncEnd;
                                return task;
                            }

                            goto case State.PerformAsyncEnd;
                        }

                    case State.PerformAsyncEnd:
                        {
                            _log.DebugFormat("leave '{0}'", _context.BackgroundJob.Job);

                            Debug.Assert(_performedContext != null);

                            _filters.Reset();
                            goto case State.OnPerformedNext;
                        }

                    case State.OnPerformedNext:
                        {
                            filter = _filters.GetNextFilter<IServerFilter, IAsyncServerFilter>();
                            if (filter.NotFound)
                            {
                                goto case State.End;
                            }
                            else if (filter.Async != null)
                            {
                                state = filter.Async;
                                goto case State.OnPerformedAsyncBegin;
                            }
                            else
                            {
                                state = filter.Sync;
                                goto case State.OnPerformedSync;
                            }
                        }

                    case State.OnPerformedAsyncBegin:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerFilter)state;
                            _log.DebugFormat("enter '{0}.OnPerformedAsync'", asyncFilter.GetType().Name);

                            task = asyncFilter.OnPerformedAsync(_performedContext);
                            if (task.Status != TaskStatus.RanToCompletion)
                            {
                                next = State.OnPerformedAsyncEnd;
                                return task;
                            }

                            goto case State.OnPerformedAsyncEnd;
                        }

                    case State.OnPerformedAsyncEnd:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerFilter)state;
                            _log.DebugFormat("leave '{0}.OnPerformedAsync'", asyncFilter.GetType().Name);

                            goto case State.OnPerformedNext;
                        }

                    case State.OnPerformedSync:
                        {
                            Debug.Assert(state != null);

                            syncFilter = (IServerFilter)state;
                            _log.DebugFormat("enter '{0}.OnPerformed'", syncFilter.GetType().Name);

                            syncFilter.OnPerformed(_performedContext);

                            _log.DebugFormat("leave '{0}.OnPerformed'", syncFilter.GetType().Name);

                            goto case State.OnPerformedNext;
                        }

                    case State.End:
                        {
                            next = State.End;
                            return Task.FromResult(0);
                        }

                    default:
                        throw new InvalidOperationException("Invalid state");
                }
            }

            private Task ExceptionFilters(ref EFState next, ref object state)
            {
                Debug.Assert(_exceptionContext != null, "exceptionContext not initialized");

                JobFilterPair<IServerExceptionFilter, IAsyncServerExceptionFilter> filter;
                IServerExceptionFilter syncFilter; IAsyncServerExceptionFilter asyncFilter;
                Task task;

                switch (next)
                {
                    case EFState.Begin:
                        {
                            _filters.Reset();
                            goto case EFState.ErrorHandlerNext;
                        }

                    case EFState.ErrorHandlerNext:
                        {
                            filter = _filters.GetNextFilter<IServerExceptionFilter, IAsyncServerExceptionFilter>();
                            if (filter.NotFound)
                            {
                                goto case EFState.End;
                            }
                            else if (filter.Async != null)
                            {
                                state = filter.Async;
                                goto case EFState.ErrorHandlerAsyncBegin;
                            }
                            else
                            {
                                state = filter.Sync;
                                goto case EFState.ErrorHandlerSync;
                            }
                        }

                    case EFState.ErrorHandlerAsyncBegin:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerExceptionFilter)state;
                            _log.DebugFormat("enter '{0}.OnServerExceptionAsync'", asyncFilter.GetType().Name);

                            task = asyncFilter.OnServerExceptionAsync(_exceptionContext);
                            if (task.Status != TaskStatus.RanToCompletion)
                            {
                                next = EFState.ErrorHandlerAsyncEnd;
                                return task;
                            }

                            goto case EFState.ErrorHandlerAsyncEnd;
                        }

                    case EFState.ErrorHandlerAsyncEnd:
                        {
                            Debug.Assert(state != null);

                            asyncFilter = (IAsyncServerExceptionFilter)state;
                            _log.DebugFormat("leave '{0}.OnServerExceptionAsync'", asyncFilter.GetType().Name);

                            goto case EFState.ErrorHandlerNext;
                        }

                    case EFState.ErrorHandlerSync:
                        {
                            Debug.Assert(state != null);

                            syncFilter = (IServerExceptionFilter)state;
                            _log.DebugFormat("enter '{0}.OnServerException'", syncFilter.GetType().Name);

                            syncFilter.OnServerException(_exceptionContext);

                            _log.DebugFormat("leave '{0}.OnServerException'", syncFilter.GetType().Name);

                            goto case EFState.ErrorHandlerNext;
                        }

                    case EFState.End:
                        {
                            next = EFState.End;
                            return Task.FromResult(0);
                        }

                    default:
                        throw new InvalidOperationException("Invalid state");
                }
            }

            private async Task ExecuteJobMethodAsync()
            {
                try
                {
                    _result = await _innerPerformer.PerformAsync(_context);
                    _performedContext = new PerformedContext(_context, _result, false, null);
                }
                catch (Exception ex)
                {
                    _performedContext = new PerformedContext(_context, null, false, ex);
                }
            }

            private enum EFState
            {
                Begin,
                ErrorHandlerNext,
                ErrorHandlerAsyncBegin,
                ErrorHandlerAsyncEnd,
                ErrorHandlerSync,
                End
            }

            private enum State
            {
                Begin,

                // OnPerforming states:
                OnPerformingNext,
                OnPerformingAsyncBegin,
                OnPerformingAsyncEnd,
                OnPerformingSync,
                OnPerformingCheckCancel,

                // OnCancel states:
                OnCancelPrev,
                OnCancelAsyncBegin,
                OnCancelAsyncEnd,
                OnCancelSync,

                // Perform states:
                PerformAsyncBegin,
                PerformAsyncEnd,

                // OnPerformed states:
                OnPerformedNext,
                OnPerformedAsyncBegin,
                OnPerformedAsyncEnd,
                OnPerformedSync,

                End
            }
        }
    }
}
