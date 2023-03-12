using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argumental.Test
{
  [TestClass]
  public class SystemCommandLine
  {
    private class BasicApp
    {
      public static Task<int> Main1(string[] args)
      {
        return CommandApp.Default()
          .SetMetadata(m => m.SetName("scl")
            .SetDescription("Sample app for Argumental")
            .SetVersion("1.0.0"))
          .RunAsync(CommandPipeline<Task<int>>.Default()
            .AddArgs(args)
            .AddCommand("", c =>
            {
              c.SetHandler((file) =>
              {
                Console.WriteLine("Read file: " + file);
                return Task.FromResult(0);
              }, new Option<string>("file", "The file to read and display on the console."));
            }));
      }
    }

    [TestMethod]
    public async Task Basic()
    {
      var result = await CommandResult.RunAsync(new[] { "--file", "something.txt" }, BasicApp.Main1);
      Assert.AreEqual("Read file: something.txt", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "--help" }, BasicApp.Main1);
      Assert.AreEqual(@"Sample app for Argumental

Usage:
  scl [--file <file>]

Options:
  --file <file>     The file to read and display on the console.
  --version         Show version information
  -?, -h, --help    Show help and usage information", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "--version" }, BasicApp.Main1);
      Assert.AreEqual(@"1.0.0", result.Out.ToString()?.TrimEnd());
    }

    private class SubcommandApp
    {
      public static Task<int> Main1(string[] args)
      {
        return CommandApp.Default()
          .SetMetadata(m => m.SetName("scl")
            .SetDescription("Sample app for Argumental")
            .SetVersion("1.0.0"))
          .RunAsync(CommandPipeline<Task<int>>.Default()
            .AddArgs(args)
            .AddCommand("read", c =>
            {
              ((ConfigSection)c.Name.Last()).Description = "Read and display the file.";
              c.SetHandler((file, delay, fgcolor, lightMode) =>
              {
                Console.WriteLine($"{file}, {delay}, {fgcolor}, {lightMode}");
                return Task.FromResult(0);
              }, new Option<string>("file", "The file to read and display on the console.")
              , new Option<int>("delay", "Delay between lines, specified as milliseconds per character in a line.")
              {
                DefaultValue = 42
              }, new Option<ConsoleColor>("fgcolor", "Foreground color of text displayed on the console.")
              {
                DefaultValue = ConsoleColor.White
              }, new Option<bool>("light-mode", "Background color of text displayed on the console: default is black, light mode is white."));
            }));
      }
    }

    [TestMethod]
    public async Task Subcommand()
    {
      var result = await CommandResult.RunAsync(new[] { "--file", "sampleQuotes.txt" }, SubcommandApp.Main1);
      Assert.AreEqual(@"Required command was not provided.

Sample app for Argumental

Usage:
  scl [command] [options]

Options:
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  read              Read and display the file.", result.Out.ToString()?.TrimEnd());
//      result = await CommandResult.RunAsync(new[] { "read", "-h" }, SubcommandApp.Main1);
//      Assert.AreEqual(@"Required command was not provided.

//Sample app for Argumental

//Usage:
//  scl [command] [options]

//Options:
//  --version         Show version information
//  -?, -h, --help    Show help and usage information

//Commands:
//  read              Read and display the file.", result.Out.ToString()?.TrimEnd());
    }
  }
}
