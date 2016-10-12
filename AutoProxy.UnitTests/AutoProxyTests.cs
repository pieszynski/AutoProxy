﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace AutoProxy.UnitTests
{
    public interface IFewMethods
    {
        string Sum(string a, int b);
        void MakeIt(object o);
    }

    public class AutoProxyTests
    {
        [Fact]
        public void CreateProxy()
        {
            // Arrange
            AutoProxyFactory factory = new AutoProxyFactory();

            // Act
            IFewMethods proxy = factory.CreateProxy<IFewMethods>();
            proxy.Sum("4s", 5);

            // Assert
            Assert.NotNull(proxy);
        }
    }

    class BB : BaseWcfInvoker<IFewMethods>, IFewMethods
    {
        class DCMakeIt
        {
            public object o;
            public void MakeIt(IFewMethods proxy)
            {
                proxy.MakeIt(o);
            }
        }

        public void MakeIt(object o)
        {
            DCMakeIt dMakeIt = new DCMakeIt();
            dMakeIt.o = o;
            this.Invoke(dMakeIt.MakeIt);
        }

        class DCSum
        {
            public string a;
            public int b;
            public string Sum(IFewMethods proxy)
            {
                return proxy.Sum(a, b);
            }
        }
        
        public string Sum(string a, int b)
        {
            DCSum dSum = new DCSum();
            dSum.a = a;
            dSum.b = b;
            string response = this.Invoke<string>(dSum.Sum);
            //string response = this.Invoke<string>(proxy => proxy.Sum(a, b));
            return response;
        }
    }
}