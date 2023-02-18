using Microsoft.Extensions.Configuration;

namespace Argumental.TestConsole
{
  internal class Program
  {
    static int Main(string[] args)
    {
      var rootCommand = new Command<int>("");
      //rootCommand.SetHandler(file =>
      //{
      //  return 0;
      //}, new Option<FileInfo>("file", "The file to read and display on the console."));

      rootCommand.SetHandler(options =>
      {
        return 0;
      }, new OptionSet<ConfigTest>());

      return CommandPipeline<int>.Default()
        .AddArgs(new[] { "--file", "thing.txt" })
        .AddCommand(rootCommand)
        .Run();
    }

    private class ConfigTest
    {
      [ConfigurationKeyName("file")]
      public string File { get; set; }
    }
  }
}