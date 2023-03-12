using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Argumental.TestConsole
{
  internal class Program
  {
    static int Main(string[] args)
    {
      var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
      var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

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