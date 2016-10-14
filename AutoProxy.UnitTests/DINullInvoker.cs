using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoProxy.UnitTests
{
    public class DIBase
    {
        protected IUnitLogger Logger;

        public DIBase(IUnitLogger logger)
        {
            this.Logger = logger;
        }
    }
    public class DINullInvoker<TService> : DIBase, IBaseAutoProxyInvoker<TService>
    {
        public DINullInvoker(IUnitLogger logger)
            :base(logger)
        {
        }

        public void Invoke(Action<TService> callback)
        {
            this.Logger.Info(".Invoke()");
        }

        public T Invoke<T>(Func<TService, T> callback)
        {
            this.Logger.Info(".Invoke<T>()");
            return default(T);
        }
    }
}
