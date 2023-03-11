using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Argumental
{
  public class CommandApp
  {
    private Dictionary<Type, ExceptionHandler> _handlers = new Dictionary<Type, ExceptionHandler>();

    public CommandApp AddHandler<T>(int? exitCode, Action<T> handler) where T : Exception
    {
      _handlers[typeof(T)] = new ExceptionHandler(exitCode, handler);
      return this;
    }

    public CommandApp AddHandler<T>(ExitCode exitCode, Action<T> handler) where T : Exception
    {
      _handlers[typeof(T)] = new ExceptionHandler((int)exitCode, handler);
      return this;
    }

    public int Run<T>(CommandPipeline<T> pipeline)
    {
      return RunAsync(() => {
        pipeline.Run();
        return Task.FromResult(Environment.ExitCode);
      }).Result;
    }

    public int Run(CommandPipeline<int> pipeline)
    {
      return Run(pipeline.Run);
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
      return RunAsync(pipeline.Run);
    }

    public Task<int> RunAsync(CommandPipeline<Task<int>> pipeline)
    {
      return RunAsync(pipeline.Run);
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
              handler.Handler.DynamicInvoke(new[] { ex });
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
      return Default(Console.Out);
    }

    public static CommandApp Default(TextWriter writer)
    {
      return new CommandApp()
        .AddHandler<FileNotFoundException>(ExitCode.NoInput, null)
        .AddHandler<DirectoryNotFoundException>(ExitCode.NoInput, null)
        .AddHandler<IOException>(ExitCode.IoError, null)
        .AddHandler<UnauthorizedAccessException>(ExitCode.NoPermissions, null)
        .AddHandler<CommandException>(ExitCode.UsageError, null)
        .AddHandler<Exception>(ExitCode.Failure, e =>
        {
          writer.WriteLine(e.ToString());
        });
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
