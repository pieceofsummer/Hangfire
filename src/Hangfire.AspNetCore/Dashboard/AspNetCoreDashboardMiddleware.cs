// This file is part of Hangfire.
// Copyright � 2016 Sergey Odinokov.
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
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Extensions;

namespace Hangfire.Dashboard
{
    public class AspNetCoreDashboardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JobStorage _storage;
        private readonly DashboardOptions _options;
        private readonly RouteCollection _routes;

        public AspNetCoreDashboardMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] JobStorage storage,
            [NotNull] DashboardOptions options,
            [NotNull] RouteCollection routes)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            _next = next;
            _storage = storage;
            _options = options;
            _routes = routes;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var context = new AspNetCoreDashboardContext(_storage, _options, httpContext);
            var findResult = _routes.FindDispatcher(httpContext.Request.Path.Value);
            
            if (findResult == null)
            {
                return _next.Invoke(httpContext);
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var filter in _options.Authorization)
            {
                if (!filter.Authorize(context))
                {
                    if (httpContext.Authentication.GetAuthenticationSchemes().Any())
                    {
                        // There's at least one authentication scheme enabled for this server
                        // Issue an authentication challenge and allow it to do all the magic
                        
                        return httpContext.Authentication.ChallengeAsync(new AuthenticationProperties()
                        {
                            // provide it with url to redirect back after successful authentication
                            RedirectUri = UriHelper.BuildRelative(httpContext.Request.PathBase,
                                                                  httpContext.Request.Path,
                                                                  httpContext.Request.QueryString)
                        });
                    }
                    else
                    {
                        // No authentication schemes available for this server,
                        // so we fallback to a plain '403 Forbidden' response.
                        // It still may be handled by the upstream middleware, like 
                        // Microsoft.AspNetCore.Diagnostics.StatusCodePagesMiddleware

                        httpContext.Response.StatusCode = 403;
                        return Task.FromResult(0);
                    }
                }
            }

            context.UriMatch = findResult.Item2;

            return findResult.Item1.Dispatch(context);
        }
    }
}