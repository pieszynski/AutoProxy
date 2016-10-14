using System;
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
            IAutoProxyFactory factory = new AutoProxyFactory(typeof(NullInvoker<>));

            // Act
            IFewMethods proxy = factory.CreateProxy<IFewMethods>();
            proxy = factory.CreateProxy<IFewMethods>();
            proxy.MakeIt("asdf");
            string nullSum = proxy.Sum("4s", 5);

            // Assert
            Assert.NotNull(proxy);
            Assert.Null(nullSum);
        }

        [Fact]
        public void CreateProxyWithDependencyInjection()
        {
            // Arrange
            IAutoProxyFactory factory = new AutoProxyFactory(typeof(DINullInvoker<>));

            SimpleInjector.Container container = new SimpleInjector.Container();
            container.Register<IUnitLogger, NullLogger>();
            container.Register(typeof(IFewMethods), factory.GetProxyClassForType<IFewMethods>());
            
            // Act
            IFewMethods proxy = container.GetInstance<IFewMethods>();
            proxy.MakeIt("asdf");
            string nullSum = proxy.Sum("4s", 5);

            // Assert
            Assert.NotNull(proxy);
            Assert.Null(nullSum);
        }
    }

    class BB : NullInvoker<IFewMethods>, IFewMethods
    {
        class DCMakeIt
        {
            public object o;
            public void MakeIt(IFewMethods proxy)
            {
                if (null == proxy)
                    throw new ArgumentNullException(nameof(proxy));

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
                if (null == proxy)
                    return default(string);

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
