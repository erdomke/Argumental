using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Argumental
{
  public class CommandApp : IServiceProvider
  {
    private readonly Dictionary<Type, ExceptionHandler> _handlers 
      = new Dictionary<Type, ExceptionHandler>();
    private readonly Dictionary<Type, Func<IServiceProvider, object>> _services
      = new Dictionary<Type, Func<IServiceProvider, object>>();
    private readonly Dictionary<Type, object> _singletons
      = new Dictionary<Type, object>();
    private List<IDisposable> _instancesToDispose = new List<IDisposable>();

    public CommandApp(AssemblyMetadata metadata)
    {
      AddSingleton(metadata);
      AddTransient(s => ConfigFormatRepository.Default(s.GetService<IConfigurationBuilder>()));
      AddTransient(s => s.GetService<ICommandPipeline>()?.ConfigurationBuilder);
    }

    public CommandApp AddHandler<T>(int? exitCode, Action<T, CommandApp> handler) where T : Exception
    {
      _handlers[typeof(T)] = new ExceptionHandler(exitCode, handler);
      return this;
    }

    public CommandApp AddHandler<T>(ExitCode exitCode, Action<T, CommandApp> handler) where T : Exception
    {
      _handlers[typeof(T)] = new ExceptionHandler((int)exitCode, handler);
      return this;
    }
        
    public CommandApp AddTransient<T>(Func<IServiceProvider, T> factory) where T : class
    {
      foreach (var type in TypesToRegister(typeof(T)))
      {
        _services[type] = factory;
        _singletons.Remove(type);
      }
      return this;
    }

    public CommandApp AddSingleton<T>(T instance) where T : class
    {
      foreach (var type in TypesToRegister(typeof(T)))
      {
        _singletons[type] = instance;
        _services.Remove(type);
      }
      return this;
    }

    public T RegisterDisposable<T>(T value) where T : IDisposable
    {
      _instancesToDispose.Add(value);
      return value;
    }

    public int Run(Action<CommandApp> callback)
    {
      return RunAsync(app =>
      {
        callback(app);
        return Task.FromResult(Environment.ExitCode);
      }).Result;
    }

    public int Run(Func<CommandApp, int> callback)
    {
      return RunAsync(app => Task.FromResult(callback(app))).Result;
    }

    public Task<int> RunAsync(Func<CommandApp, Task> callback)
    {
      return RunAsync(async app =>
      {
        await callback(app).ConfigureAwait(false);
        return Environment.ExitCode;
      });
    }

    public async Task<int> RunAsync(Func<CommandApp, Task<int>> callback)
    {
      try
      {
        Environment.ExitCode = await callback(this).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        var exitCode = default(int?);
        var handled = false;
        foreach (var type in ex.GetType().ParentsAndSelf())
        {
          if (_handlers.TryGetValue(type, out var handler))
          {
            exitCode = exitCode ?? handler.ExitCode;
            if (handler.Handler != null)
            {
              handled = true;
              handler.Handler.DynamicInvoke(new object[] { ex, this });
            }
            if (exitCode.HasValue && handled)
              break;
          }
        }

        Environment.ExitCode = exitCode ?? 
          (Environment.ExitCode == 0 ? (int)ExitCode.Failure : Environment.ExitCode);
      }
      finally
      {
        foreach (var instance in _instancesToDispose)
        {
          try
          {
            instance.Dispose();
          }
          catch (Exception) { }
        }
      }
      return Environment.ExitCode;
    }

    public static CommandApp Default()
    {
      return Default(Console.Out);
    }

    public static CommandApp Default(TextWriter writer)
    {
      return new CommandApp(AssemblyMetadata.Default())
        .AddHandler<FileNotFoundException>(ExitCode.NoInput, null)
        .AddHandler<DirectoryNotFoundException>(ExitCode.NoInput, null)
        .AddHandler<IOException>(ExitCode.IoError, null)
        .AddHandler<UnauthorizedAccessException>(ExitCode.NoPermissions, null)
        .AddHandler<VersionException>(ExitCode.UsageError, (e, a) =>
        {
          writer.WriteLine(a.GetService<AssemblyMetadata>().Version);
        })
        .AddHandler<ConfigurationException>(ExitCode.UsageError, (e, a) =>
        {
          if (e.Pipeline != null)
            a.AddSingleton(e.Pipeline);
          if (e.ConfigurationBuilder != null)
            a.AddSingleton(e.ConfigurationBuilder);
          a.GetService<ConfigFormatRepository>().WriteError(writer, a.GetService<AssemblyMetadata>(), e);
        })
        .AddHandler<OptionsValidationException>(ExitCode.UsageError, (e, a) =>
        {
          var pipeline = a.GetService<ICommandPipeline>();
          var configEx = new ConfigurationException(pipeline?.GetParser().Command, e.Failures)
          {
            Pipeline = pipeline
          };
          a.GetService<ConfigFormatRepository>().WriteError(writer, a.GetService<AssemblyMetadata>(), configEx);
        })
        .AddHandler<Exception>(ExitCode.Failure, (e, _) =>
        {
          writer.WriteLine(e.ToString());
        });
    }

    public object GetService(Type serviceType)
    {
      if (_services.TryGetValue(serviceType, out var factory))
      {
        var result = factory(this);
        if (result is IDisposable disposable)
          RegisterDisposable(disposable);
        return result;
      }
      else if (_singletons.TryGetValue(serviceType, out var singleton))
      {
        return singleton;
      }
      else if (serviceType == typeof(CancelKeyPressSource)
        || serviceType == typeof(ICancellationTokenSource))
      {
        var result = RegisterDisposable(new CancelKeyPressSource());
        _singletons[typeof(CancelKeyPressSource)] = result;
        _singletons[typeof(ICancellationTokenSource)] = result;
        return result;
      }
      else if (serviceType == typeof(IServiceProvider)
        || serviceType == typeof(CommandApp))
      {
        return this;
      }
      else
      {
        return null;
      }
    }

    private IEnumerable<Type> TypesToRegister(Type type)
    {
      yield return type;
      if (type != typeof(ICommandPipeline)
        && typeof(ICommandPipeline).IsAssignableFrom(type))
        yield return typeof(ICommandPipeline);
    }

    private struct ExceptionHandler
    {
      public int? ExitCode { get; }
      public Delegate Handler { get; }

      public ExceptionHandler(int? exitCode, Delegate handler)
      {
        ExitCode = exitCode;
        Handler = handler;
      }
    }
  }
}
