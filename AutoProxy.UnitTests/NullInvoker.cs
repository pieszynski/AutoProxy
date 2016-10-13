using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoProxy.UnitTests
{
    public class NullInvoker<TService> : IBaseAutoProxyInvoker<TService>
    {
        public T Invoke<T>(Func<TService, T> callback)
        {
            //return callback(default(TService));
            return default(T);
        }

        public void Invoke(Action<TService> callback)
        {
            //callback(default(TService));
        }
    }
}
