# AutoProxy
Biblioteka do automatycznego generowania klas pośrednich, które wymagają dodatkowej logiki przed wywołaniem każdej z metod - np. WCF (ChannelFactory).

## Cel
Aby bezpiecznie korzystać z WCF (zamykanie połączeń, obsługa FaultedState, etc.) należało stworzyć klasę pośrednią, którą wywoływało się w następujący sposób:

```csharp
MResponse response = wcfProxy.Invoke<IContract,MResponse>(proxy => proxy.Method());
```

Biblioteka `AutoProxy` rozwiązuje potrzebę pisania zbyt wiele kodu, wystarczy:

```csharp
MResponse response = autoProxy.Method();
```

## Jak zacząć

```csharp
// Przygotowanie fabryki klas pośrednich.
//  WcfInvoker<> to klasa umożliwiająca komunikację przez WCF
//  dla dowolnego kontraktu <TService>
//
AutoProxyFactory factory = new AutoProxyFactory(typeof(WcfInvoker<>));

// Stworzenie klasy pośredniej
IContract proxy = factory.CreateProxy<IContract>();

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

* Dostosowanie do bibliotek oferujących wstrzykiwanie zależności(DI)
* Obsługa metod &lt;T&gt;, parametrów domyślnych, ref i out.
