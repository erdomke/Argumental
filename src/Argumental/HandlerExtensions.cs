using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public static class HandlerExtensions
  {
    public static IEnumerable<object> GetOptions<TResult>(this IConfigHandler<TResult> handler, IConfiguration configuration)
    {
      var validations = new List<ValidationResult>();
      var results = new List<object>();
      var success = true;
      foreach (var provider in handler.Providers)
      {
        if (provider.TryGet(configuration, validations, out var value))
          results.Add(value);
        else
          success = false;
      }
      if (!success)
        throw new ConfigurationException(null, handler as ICommand, validations.Select(v => v.ErrorMessage));
      return results;
    }

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
      config.Handler = (_, c) => DynamicInvoke(c, config, handler);
    }

    public static void SetHandler<T1, T2, TResult>(this IConfigHandler<TResult> config
      , Func<T1, T2, TResult> handler
      , IOptionProvider<T1> option1
      , IOptionProvider<T2> option2)
    {
      config.Providers.Add(option1);
      config.Providers.Add(option2);
      config.Handler = (_, c) => DynamicInvoke(c, config, handler);
    }

    public static void SetHandler<T1, T2, T3, TResult>(this IConfigHandler<TResult> config
      , Func<T1, T2, T3, TResult> handler
      , IOptionProvider<T1> option1
      , IOptionProvider<T2> option2
      , IOptionProvider<T3> option3)
    {
      config.Providers.Add(option1);
      config.Providers.Add(option2);
      config.Providers.Add(option3);
      config.Handler = (_, c) => DynamicInvoke(c, config, handler);
    }

    public static void SetHandler<T1, T2, T3, T4, TResult>(this IConfigHandler<TResult> config
      , Func<T1, T2, T3, T4, TResult> handler
      , IOptionProvider<T1> option1
      , IOptionProvider<T2> option2
      , IOptionProvider<T3> option3
      , IOptionProvider<T4> option4)
    {
      config.Providers.Add(option1);
      config.Providers.Add(option2);
      config.Providers.Add(option3);
      config.Providers.Add(option4);
      config.Handler = (_, c) => DynamicInvoke(c, config, handler);
    }

    private static TResult DynamicInvoke<TResult>(IConfiguration configuration, IConfigHandler<TResult> configHandler, Delegate handler)
    {
      return (TResult)handler.DynamicInvoke(configHandler.GetOptions(configuration).ToArray());
    }
  }
}
