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
