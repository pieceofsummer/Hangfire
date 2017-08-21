﻿using Hangfire.Server;

namespace Hangfire
{
    internal static class JobCancellationTokenExtensions
    {
        public static bool IsAborted(this IJobCancellationToken jobCancellationToken)
        {
            if (jobCancellationToken == null) return false;

            var serverJobCancellationToken = jobCancellationToken as ServerJobCancellationToken;
            if (serverJobCancellationToken != null)
            {
                // for ServerJobCancellationToken we may simply check IsAborted property
                // to prevent unnecessary creation of the linked CancellationTokenSource
                return serverJobCancellationToken.IsAborted;
            }
            
            return jobCancellationToken.CancellationToken.IsCancellationRequested;
        }
    }
}
