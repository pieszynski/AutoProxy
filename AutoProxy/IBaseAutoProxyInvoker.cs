using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoProxy
{
    public interface IBaseAutoProxyInvoker<TService>
    {
        T Invoke<T>(Func<TService, T> callback);
        void Invoke(Action<TService> callback);
    }
}
