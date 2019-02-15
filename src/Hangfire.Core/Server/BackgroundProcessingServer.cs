﻿// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Processing;

namespace Hangfire.Server
{
    /// <summary>
    /// Responsible for running the given collection background processes.
    /// </summary>
    /// 
    /// <remarks>
    /// Immediately starts the processes in a background thread.
    /// Responsible for announcing/removing a server, bound to a storage.
    /// Wraps all the processes with a infinite loop and automatic retry.
    /// Executes all the processes in a single context.
    /// Uses timeout in dispose method, waits for all the components, cancel signals shutdown
    /// Contains some required processes and uses storage processes.
    /// Generates unique id.
    /// Properties are still bad.
    /// </remarks>
    public sealed class BackgroundProcessingServer : IBackgroundProcessingServer
    {
        private static int _lastThreadId = 0;

        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppedCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        private readonly IBackgroundServerProcess _process;
        private readonly BackgroundProcessingServerOptions _options;
        private readonly IBackgroundDispatcher _dispatcher;

        private int _disposed;

        public BackgroundProcessingServer([NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(JobStorage.Current, processes)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(JobStorage.Current, processes, properties)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(storage, processes, new Dictionary<string, object>())
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(storage, processes, properties, new BackgroundProcessingServerOptions())
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties,
            [NotNull] BackgroundProcessingServerOptions options)
            : this(storage, GetProcesses(processes), properties, options)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcessDispatcherBuilder> dispatcherBuilders,
            [NotNull] IDictionary<string, object> properties,
            [NotNull] BackgroundProcessingServerOptions options)
            : this(new BackgroundServerProcess(storage, dispatcherBuilders, options, properties), options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundProcessingServer"/>
        /// class and immediately starts all the given background processes.
        /// </summary>
        internal BackgroundProcessingServer(
            [NotNull] BackgroundServerProcess process,
            [NotNull] BackgroundProcessingServerOptions options)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _dispatcher = CreateDispatcher();

#if !NETSTANDARD1_3
            AppDomain.CurrentDomain.DomainUnload += OnCurrentDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnCurrentDomainUnload;
#endif
        }

        public void SendStop()
        {
            ThrowIfDisposed();

            _stoppingCts.Cancel();
            _stoppedCts.CancelAfter(_options.StopTimeout);
            _shutdownCts.CancelAfter(_options.ShutdownTimeout);
        }

        public bool WaitForShutdown()
        {
            ThrowIfDisposed();
            return _dispatcher.Wait(_options.ShutdownTimeout + _options.LastChanceTimeout);
        }

        public async Task WaitForShutdownAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            await _dispatcher.WaitAsync(_options.ShutdownTimeout + _options.LastChanceTimeout, cancellationToken).ConfigureAwait(false);
        }

        public bool Shutdown()
        {
            ThrowIfDisposed();

            SendStop();
            return WaitForShutdown();
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            SendStop();
            return WaitForShutdownAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _disposed) == 1) return;

            if (!_stoppingCts.IsCancellationRequested)
            {
                Shutdown();
            }

            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

            _dispatcher.Dispose();
            _stoppingCts.Dispose();
            _stoppedCts.Dispose();
            _shutdownCts.Dispose();
        }

        private void OnCurrentDomainUnload(object sender, EventArgs args)
        {
            if (Volatile.Read(ref _disposed) == 1) return;

            _stoppingCts.Cancel();
            _stoppedCts.Cancel();
            _shutdownCts.Cancel();

            _dispatcher.Wait(_options.LastChanceTimeout);
        }

        private static IBackgroundProcessDispatcherBuilder[] GetProcesses([NotNull] IEnumerable<IBackgroundProcess> processes)
        {
            if (processes == null) throw new ArgumentNullException(nameof(processes));
            return processes.Select(x => x.UseBackgroundPool(threadCount: 1)).ToArray();
        }

        private IBackgroundDispatcher CreateDispatcher()
        {
            var execution = new BackgroundExecution(
                _stoppingCts.Token,
                new BackgroundExecutionOptions
                {
                    Name = nameof(BackgroundServerProcess),
                    ErrorThreshold = TimeSpan.Zero,
                    StillErrorThreshold = TimeSpan.Zero,
                    RetryDelay = retry => _options.RestartDelay
                });

            return new BackgroundDispatcher(
                execution,
                RunServer,
                execution,
                ThreadFactory);
        }

        private void RunServer(Guid executionId, object state)
        {
            _process.Execute(executionId, (BackgroundExecution)state, _stoppingCts.Token, _stoppedCts.Token, _shutdownCts.Token);
        }

        private static IEnumerable<Thread> ThreadFactory(ThreadStart threadStart)
        {
            yield return new Thread(threadStart)
            {
                IsBackground = true,
                Name = $"{nameof(BackgroundServerProcess)} #{Interlocked.Increment(ref _lastThreadId)}",
            };
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) == 1)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
