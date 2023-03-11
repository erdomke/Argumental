using Microsoft.Extensions.Configuration;

namespace Argumental.TestConsole
{
  internal class Program
  {
    static int Main(string[] args)
    {
      return CommandApp.Default()
        .Run(CommandPipeline<int>.Default()
          .AddArgs(args)
          .AddCommand("", c =>
          {
            c.SetHandler(options =>
            {
              return 0;
            }, new OptionSet<ConfigTest>());
          }));
    }

    private class ConfigTest
    {
      [ConfigurationKeyName("file")]
      public string File { get; set; }
    }
  }
}