using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoProxy
{
    public interface IAutoProxyFactory
    {
        T CreateProxy<T>() where T : class;
        Type GetProxyClassForType<T>() where T : class;
    }
}
