using Argumental;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InjectingProvider
{
  class Program
  {
    public static int Main_(string[] args)
    {
      return CommandApp.Default()
        .Run(app => {
          var pipeline = CommandPipeline<int>.Default()
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
                Handler = ctx => ctx.ServiceProvider.GetService<ILoggerFactory>()
                  .CreateLogger("LoggerCategory")
              });
            });
          app.AddSingleton(pipeline);

          var configuration = pipeline.Build(b =>
            b.AddEnvironmentVariables("SCL_"));

          var provider = new ServiceCollection()
            .AddLogging(b => b.AddConfiguration(configuration))
            .BuildServiceProvider();
          app.RegisterDisposable(provider);

          return pipeline.Invoke(configuration, provider);
        });
    }
  }
}
