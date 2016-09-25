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

using Hangfire.Filters;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    /// <summary>
    /// Defines methods that are required for the server exception filter.
    /// </summary>
    public interface IAsyncServerExceptionFilter : ISyncAsyncPair<IServerExceptionFilter, IAsyncServerExceptionFilter>
    {
        /// <summary>
        /// Called when an exception occurred during the performance of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        Task OnServerExceptionAsync(ServerExceptionContext filterContext);
    }
}
