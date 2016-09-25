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

using Hangfire.Filters;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    /// <summary>
    /// Defines methods that are required for an asynchronous server filter.
    /// </summary>
    public interface IAsyncServerFilter : ISyncAsyncPair<IServerFilter, IAsyncServerFilter>
    {
        /// <summary>
        /// Called before the performance of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        Task OnPerformingAsync(PerformingContext filterContext);

        /// <summary>
        /// Called after the performance of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        Task OnPerformedAsync(PerformedContext filterContext);
    }
}