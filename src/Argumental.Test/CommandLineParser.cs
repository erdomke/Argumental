using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Argumental.Test
{
  [TestClass]
  public class CommandLineParser
  {
    private class GettingStartedApp
    {
      public class Options
      {
        [Display(Description = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
      }

      public static int Main1(string[] args)
      {
        return CommandApp.Default()
          .Run(CommandPipeline<int>.Default()
            .AddArgs(args)
            .AddCommand("", c =>
            {
              c.SetHandler(o =>
              {
                if (o.Verbose)
                  Console.WriteLine("Verbose output enabled.");
                else
                  Console.WriteLine("Normal output.");
                return 0;
              }, new OptionSet<Options>());
            }));
      }
    }

    [TestMethod]
    public void GettingStarted()
    {
      var result = CommandResult.Run(new[] { "--verbose" }, GettingStartedApp.Main1);
      Assert.AreEqual("Verbose output enabled.", result.Out.ToString()?.TrimEnd());
      result = CommandResult.Run(Array.Empty<string>(), GettingStartedApp.Main1);
      Assert.AreEqual("Normal output.", result.Out.ToString()?.TrimEnd());
    }

    private class MultipleOptions
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

      public static int Main1(string[] args)
      {
        return CommandApp.Default()
          .Run(CommandPipeline<int>.Default()
            .AddArgs(args)
            .AddAlias("-r", "--read")
            .AddCommand("", c =>
            {
              c.SetHandler(o =>
              {
                Console.Write(o.Verbose ? "Verbose, " : "Normal, ");
                Console.Write(o.StdIn ? "StdIn, " : "File, "); 
                Console.Write(o.Offset.HasValue ? o.Offset.Value.ToString() + ", " : "null, ");
                Console.Write(string.Join(", ", o.InputFiles));
                return 0;
              }, new OptionSet<Options>());
            }));
      }
    }

    [TestMethod]
    public void Options()
    {
      var result = CommandResult.Run(new[] { "-r", "file1.txt", "file2.txt" }, MultipleOptions.Main1);
      Assert.AreEqual("Normal, File, null, file1.txt, file2.txt", result.Out.ToString()?.TrimEnd());
      result = CommandResult.Run(new[] { "--verbose" }, MultipleOptions.Main1);
      Assert.AreEqual((int)ExitCode.UsageError, result.ExitCode);
    }
  }
}