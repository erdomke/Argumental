using Argumental;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InjectingLazyConstant
{
  class Program
  {
    public static int Main_(string[] args)
    {
      return CommandApp.Default()
        .Run(app => app.Register(CommandPipeline<int>.Default())
          .AddArgs(args)
          .AddCommand("", c =>
          {
            c.SetHandler((file, logger) =>
            {
              logger.LogCritical("Test message");
              Console.WriteLine($"File = {file}, Logger = {logger?.GetType().Name}");
              return 0;
            }, new Option<string>("file", "The file to read and display on the console.")
            , new Binder<ILogger>
            {
              Handler = ctx => LoggerFactory.Create(b =>
                b.AddConfiguration(ctx.Configuration))
                .CreateLogger("LoggerCategory")
            });
          })
          .Invoke());
    }
  }
}
