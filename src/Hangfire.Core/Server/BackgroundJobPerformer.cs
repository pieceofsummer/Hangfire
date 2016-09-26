// This file is part of Hangfire.
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
using Hangfire.Annotations;
using Hangfire.Common;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    public partial class BackgroundJobPerformer : IBackgroundJobPerformer
    {
        private readonly IJobFilterProvider _filterProvider;
        private readonly IBackgroundJobPerformer _innerPerformer;
        
        public BackgroundJobPerformer()
            : this(JobFilterProviders.Providers)
        {
        }

        public BackgroundJobPerformer([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, JobActivator.Current)
        {
        }

        public BackgroundJobPerformer(
            [NotNull] IJobFilterProvider filterProvider,
            [NotNull] JobActivator activator)
            : this(filterProvider, new CoreBackgroundJobPerformer(activator))
        {
        }

        internal BackgroundJobPerformer(
            [NotNull] IJobFilterProvider filterProvider, 
            [NotNull] IBackgroundJobPerformer innerPerformer)
        {
            if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
            if (innerPerformer == null) throw new ArgumentNullException(nameof(innerPerformer));

            _filterProvider = filterProvider;
            _innerPerformer = innerPerformer;
        }
        
        public Task<object> PerformAsync(PerformContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return new AsyncImpl(_filterProvider, _innerPerformer).InvokeAsync(context);
        }
    }
}
