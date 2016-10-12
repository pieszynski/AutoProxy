using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace AutoProxy
{
    public class BaseWcfInvoker<TService> : IBaseWcfInvoker<TService>
    {
        public T Invoke<T>(Func<TService, T> callback)
        {
            using (ChannelFactory<TService> factory = new ChannelFactory<TService>())
            {
                TService proxy = default(TService);
                try
                {
                    T response = callback(proxy);
                    return response;
                }
                finally
                {
                    ((ICommunicationObject)proxy)?.Abort();
                }
            }
        }

        public void Invoke(Action<TService> callback)
        {
            using (ChannelFactory<TService> factory = new ChannelFactory<TService>())
            {
                TService proxy = default(TService);
                try
                {
                    callback(proxy);
                }
                finally
                {
                    ((ICommunicationObject)proxy)?.Abort();
                }
            }
        }
    }
}
