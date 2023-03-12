using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Argumental
{
  public class CommandApp
  {
    private readonly Dictionary<Type, ExceptionHandler> _handlers = new Dictionary<Type, ExceptionHandler>();
    private Func<IConfigurationBuilder, ConfigFormatRepository> _repositoryFactory;
    private IConfigurationBuilderSource _builderSource;

    public AssemblyMetadata Metadata { get; }

    public CommandApp(AssemblyMetadata metadata)
    {
      Metadata = metadata;
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

    public CommandApp SetConfigFormat(Func<IConfigurationBuilder, ConfigFormatRepository> repositoryFactory)
    {
      _repositoryFactory = repositoryFactory;
      return this;
    }

    public ConfigFormatRepository GetConfigFormat()
    {
      return (_repositoryFactory ?? ConfigFormatRepository.Default).Invoke(_builderSource.ConfigurationBuilder);
    }

    public int Run<T>(CommandPipeline<T> pipeline)
    {
      _builderSource = pipeline;
      return RunAsync(() => {
        pipeline.Invoke();
        return Task.FromResult(Environment.ExitCode);
      }).Result;
    }

    public int Run(CommandPipeline<int> pipeline)
    {
      _builderSource = pipeline;
      return Run(pipeline.Invoke);
    }

    public int Run(Func<int> callback)
    {
      return RunAsync(() => Task.FromResult(callback())).Result;
    }

    public int Run(Action callback)
    {
      return RunAsync(() => {
        callback();
        return Task.FromResult(Environment.ExitCode);
      }).Result;
    }

    public Task<int> RunAsync<T>(CommandPipeline<Task<T>> pipeline)
    {
      _builderSource = pipeline;
      return RunAsync(pipeline.Invoke);
    }

    public Task<int> RunAsync(CommandPipeline<Task<int>> pipeline)
    {
      _builderSource = pipeline;
      return RunAsync(pipeline.Invoke);
    }

    public Task<int> RunAsync(Func<Task> callback)
    {
      return RunAsync(async () =>
      {
        await callback().ConfigureAwait(false);
        return Environment.ExitCode;
      });
    }

    public async Task<int> RunAsync(Func<Task<int>> callback)
    {
      try
      {
        Environment.ExitCode = await callback().ConfigureAwait(false);
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
      return Environment.ExitCode;
    }

    public static CommandApp Default()
    {
      var width = 80;
      try
      {
        width = Console.WindowWidth;
      }
      catch (IOException) { }
      return Default(new TextWrapper(Console.Out)
      {
        MaxWidth = width
      });
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
          writer.WriteLine(a.Metadata.Version);
        })
        .AddHandler<ConfigurationException>(ExitCode.UsageError, (e, a) =>
        {
          a.GetConfigFormat().WriteError(writer, a.Metadata, e);
        })
        .AddHandler<Exception>(ExitCode.Failure, (e, _) =>
        {
          writer.WriteLine(e.ToString());
        });
    }

    public CommandApp SetMetadata(Action<AssemblyMetadata> callback)
    {
      callback(Metadata);
      return this;
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
