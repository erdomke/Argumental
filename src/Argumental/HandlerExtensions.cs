using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Argumental
{
  public static class HandlerExtensions
  {
    public static void SetHandler<TResult>(this IConfigHandler<TResult> config
      , Func<TResult> handler)
    {
      config.Handler = (_) => handler();
    }

    public static void SetHandler<T1, TResult>(this IConfigHandler<TResult> config
      , Func<T1, TResult> handler
      , IOptionProvider<T1> option1)
    {
      config.Providers.Add(option1);
      config.Handler = (c) => DynamicInvoke(c, config, handler);
    }

    public static void SetHandler<T1, T2, TResult>(this IConfigHandler<TResult> config
      , Func<T1, T2, TResult> handler
      , IOptionProvider<T1> option1
      , IOptionProvider<T2> option2)
    {
      config.Providers.Add(option1);
      config.Providers.Add(option2);
      config.Handler = (c) => DynamicInvoke(c, config, handler);
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
      config.Handler = (c) => DynamicInvoke(c, config, handler);
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
      config.Handler = (c) => DynamicInvoke(c, config, handler);
    }

    public static void Register(this Command<IServiceRegistrar> command, params IOptionGroup[] optionGroups)
    {
      foreach (var optionGroup in optionGroups)
        command.Providers.Add(optionGroup);
      command.Handler = c => new Registrar(c);
    }

    public static Command<IServiceRegistrar> RegisterOptions(this Command<IServiceRegistrar> command, params IOptionGroup[] optionGroups)
    {
      foreach (var optionGroup in optionGroups)
        command.Providers.Add(optionGroup);
      command.Handler = c => new Registrar(c);
      return command;
    }

    public static Command<IServiceRegistrar> RegisterImplementation<TService, TImplementation>(this Command<IServiceRegistrar> command, params IOptionGroup[] optionGroups)
      where TService : class
      where TImplementation : class, TService
    {
      foreach (var optionGroup in optionGroups)
        command.Providers.Add(optionGroup);
      command.Handler = c => new Registrar(c, typeof(TService), typeof(TImplementation));
      return command;
    }

    private class Registrar : IServiceRegistrar
    {
      private readonly InvocationContext _context;
      private readonly Type _service;
      private readonly Type _implementation;

      public Registrar(InvocationContext context)
      {
        _context = context;
      }

      public Registrar(InvocationContext context, Type service, Type implementation)
      {
        _context = context;
        _service = service;
        _implementation = implementation;
      }

      public IServiceCollection Register(IServiceCollection services)
      {
        foreach (var option in _context.Handler.Providers.OfType<IOptionGroup>())
          option.Register(services, _context.Configuration);

        if (_service != null && _implementation != null)
          services.AddSingleton(_service, _implementation);

        return services;
      }
    }

    private static TResult DynamicInvoke<TResult>(InvocationContext context, IConfigHandler<TResult> configHandler, Delegate handler)
    {
      var args = configHandler.Providers.Select(p => p.Get(context)).ToArray();
      context.AssertSuccess();
      return (TResult)handler.DynamicInvoke(args);
    }
  }
}
