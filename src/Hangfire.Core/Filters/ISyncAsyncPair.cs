using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Filters
{
    /// <summary>
    /// A no-op interface to tie together a synchronous and corresponding asynchronous interfaces, 
    /// e.g. <seealso cref="Server.IServerFilter"/> and <seealso cref="Server.IAsyncServerFilter"/>.
    /// </summary>
    /// <typeparam name="TSync">Synchrnonous interface type</typeparam>
    /// <typeparam name="TAsync">Asynchronous interface type</typeparam>
    public interface ISyncAsyncPair<TSync, TAsync>
        where TSync : class, ISyncAsyncPair<TSync, TAsync>
        where TAsync : class, ISyncAsyncPair<TSync, TAsync>
    {

    }
}
