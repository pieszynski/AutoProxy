# AutoProxy
Biblioteka do automatycznego generowania klas pośrednich, które wymagają dodatkowej logiki przed wywołaniem każdej z metod - np. WCF (ChannelFactory).

## Cel
Aby bezpiecznie<sup>1</sup> korzystać z WCF (zamykanie połączeń, obsługa FaultedState, etc.) należało stworzyć klasę pośrednią, którą wywoływało się w następujący sposób:

```csharp
MResponse response = wcfProxy.Invoke<IContract,MResponse>(proxy => proxy.Method());
```

Biblioteka `AutoProxy` rozwiązuje potrzebę pisania zbyt wiele kodu, wystarczy:

```csharp
MResponse response = autoProxy.Method();
```

<sup>1</sup> Bezpieczeństwo a może bardziej niezawodność polegało na tym, że dla każdego wywołania metody otwierane było połączenie WCF przez ChannelFactory&lt;IContract&gt;. W przeciwnym przypadku jeśli wykonywanych było kilka operacji na jednym obiekcie połączeniowym (proxy) to w momencie gdy na jednym z wywołań pojawił się FaultedState to i na każdym kolejnym będzie się on pojawiał. Trzeba by było zapewnić ponowne otwieranie połączenia w trakcie życia obiektu połączeniowego co wydaje się bardziej uciążliwe i łatwiej o tym fakcie zapomnieć. Stąd używanie klasy typu WcfProxy i metod Invoke wydaje się optymalnym rozwiązaniem w kwestii zapewnienia niezawodnego wywoływania kolejnych metod przez kanał WCF.

## Jak zacząć

Podstawowe użycie
```csharp
// Przygotowanie fabryki klas pośrednich.
//  WcfInvoker<> to klasa umożliwiająca komunikację przez WCF
//  dla dowolnego kontraktu <TService>
//
IAutoProxyFactory factory = new AutoProxyFactory(typeof(WcfInvoker<>));

// Stworzenie klasy pośredniej
IContract proxy = factory.CreateProxy<IContract>();

// Wygodne użycie dowolnej metody kontraktu
MResponse response = proxy.Method();
```

Użycie w połączeniu z biblioteką do wstrzykiwania zależności(DI)
```csharp
// Przygotowanie fabryki klas pośrednich.
//  DIWcfInvoker<> to klasa umożliwiająca komunikację przez WCF
//  dla dowolnego kontraktu <TService> ale potrzebuje podania
//  w konstruktorze zależności ILogger
//
IAutoProxyFactory factory = new AutoProxyFactory(typeof(DIWcfInvoker<>));

// przygotowanie zależności na przykładzie biblioteki SimpleInjector
SimpleInjector.Container container = new SimpleInjector.Container();
container.Register<IUnitLogger, ServiceLogger>();
container.Register(typeof(IContract), factory.GetProxyClassForType<IContract>());

// Stworzenie klasy pośredniej
IContract proxy = container.GetInstance<IContract>();

// Wygodne użycie dowolnej metody kontraktu
MResponse response = proxy.Method();
```

To w zasadzie tyle. Poniżej podaję przykładową implementacja najprostszej klasy pośredniej do komunikacji przez WCFa.

<details>
  <summary>WcfInvoker&lt;TService&gt; (kliknij aby rozwinąć)</summary>
  <p>
```csharp
public class WcfInvoker<TService> : IBaseAutoProxyInvoker<TService>
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
```
</p>
</details>

## ToDo

* Obsługa metod &lt;T&gt;, parametrów domyślnych, ref i out.

## Napisy końcowe
Projekt realizujacy podobne zadanie w zasadzie już istnieje - `Castle.DynamicProxy` (nuget: Castle.Core). Podobne oczekiwania jak wyżej można osiągnąć w poniższy sposób a sam projekt `AutoProxy` służył tylko temu aby sprawdzić ile pracy potrzeba samemu na stworzenie mniej więcej czegoś podobnego. Tworzenie kodu w IL jest arcyniewygodne a obiektów Expression nie można użyć do tworzenia ciała metod niestatycznych (Expression.Lambda.CompileToMethod()) bo Expression nie ma możliwości uzyskania "this" - wszystko jest wyjaśnione w [DLR: CompileToMethod does not support instance methods, constructors, dynamicmethods](http://dlr.codeplex.com/workitem/1378?ProjectName=dlr).

```csharp

static void Main(string[] args)
{
    // zgodnie z zaleceniem twórców jeśli proces będzie długo istniał
    //  a tworzonych będzie wiele obiektów proxy należy używać tej
    //  samej instancji "ProxyGenerator" inaczej nie będzie używany
    //  cache obiektów oraz ciągłe zwiększanie się używanej pamięci
    var pg = new Castle.DynamicProxy.ProxyGenerator();

    // użycie metody .CreateInterfaceProxyWithTargetInterface() 
    //  pozwala na podmianę proxy w interceptorze
    //  korzystając z interfejsu IChangeProxyTarget
    ILogger logger = pg.CreateInterfaceProxyWithTargetInterface<ILogger>(
        null,
        new MyLoggerInterceptor()
        );

    logger.Log("trololo");
}

public class MyLoggerInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        if (null == invocation.InvocationTarget)
        {
            // jeśli nie użyto metody .CreateInterfaceProxyWithTargetInterface()
            // to zmienna "change" będzie NULL!
            IChangeProxyTarget change = invocation as IChangeProxyTarget;
            // zmiana proxy tylko dla tego żądania
            change.ChangeInvocationTarget(new Logger());
            // zmiana proxy na zawsze (ale nie dla tego żądania! tylko dla kolejnych)
            change.ChangeProxyTarget(new Logger());
        }
        
        invocation.Proceed();            
    }
}
```