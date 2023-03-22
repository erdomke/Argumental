using Argumental;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace InjectingIOptions
{
  class Program
  {
    public class Options
    {
      [Required]
      [ConfigurationKeyName("Read")]
      [Display(Name = "Read", Description = "Input files to be processed.")]
      public IEnumerable<string> InputFiles { get; set; }

      [Display(Description = "Prints all messages to standard output.")]
      public bool Verbose { get; set; }

      [Display(Description = "Read from stdin.")]
      public bool StdIn { get; set; }

      [Display(Description = "File offset.")]
      public long? Offset { get; set; }
    }

    public interface ICommand
    {
      int Execute();
    }

    public class OptionsCommand : ICommand
    {
      private readonly ILogger<OptionsCommand> _logger;
      private readonly IOptions<Options> _options;

      public OptionsCommand(ILogger<OptionsCommand> logger, IOptions<Options> options)
      {
        _logger = logger;
        _options = options;
      }

      public int Execute()
      {
        _logger.LogInformation("Execution started.");
        Console.Write(_options.Value.Verbose ? "Verbose, " : "Normal, ");
        Console.Write(_options.Value.StdIn ? "StdIn, " : "File, ");
        Console.Write(_options.Value.Offset.HasValue ? _options.Value.Offset.Value.ToString() + ", " : "null, ");
        Console.Write(string.Join(", ", _options.Value.InputFiles));
        return 0;
      }
    }

    public static int Main_(string[] args)
    {
      return CommandApp.Default()
        .Run(app =>
        {
          var pipeline = CommandPipeline<IServiceRegistrar>.Default()
            .AddArgs(args)
            .AddAlias("-r", "--read")
            .AddCommand("", c =>
            {
              c.RegisterImplementation<ICommand, OptionsCommand>(new OptionGroup<Options>());
            });
          app.AddSingleton(pipeline);

          var configuration = pipeline.Build(b => 
            b.AddEnvironmentVariables("SCL_"));

          var services = new ServiceCollection()
             .AddLogging(b => b.AddConfiguration(configuration));
          var provider = pipeline.Invoke(configuration, null)
            .AddServices(services)
            .BuildServiceProvider();
          return app.RegisterDisposable(provider)
            .GetService<ICommand>()
            .Execute();
        });
    }
  }
}
