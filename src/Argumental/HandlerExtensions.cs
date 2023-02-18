using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public static class HandlerExtensions
  {
    public static void SetHandler<TResult>(this IConfigHandler<TResult> config
      , Func<TResult> handler)
    {
      config.Handler = (_, __) => handler();
    }

    public static void SetHandler<T1, TResult>(this IConfigHandler<TResult> config
      , Func<T1, TResult> handler
      , IOptionProvider<T1> option1)
    {
      config.Providers.Add(option1);
      config.Handler = (_, c) => DynamicInvoke<TResult>(c, config.Providers, handler);
    }

    public static void SetHandler<T1, T2, TResult>(this IConfigHandler<TResult> config
      , Func<T1, T2, TResult> handler
      , IOptionProvider<T1> option1
      , IOptionProvider<T2> option2)
    {
      config.Providers.Add(option1);
      config.Providers.Add(option2);
      config.Handler = (_, c) => DynamicInvoke<TResult>(c, config.Providers, handler);
    }

    private static TResult DynamicInvoke<TResult>(IConfiguration configuration, IEnumerable<IOptionProvider> providers, Delegate handler)
    {
      return (TResult)handler.DynamicInvoke(providers.Select(p => p.Get(configuration)).ToArray());
    }
  }
}
