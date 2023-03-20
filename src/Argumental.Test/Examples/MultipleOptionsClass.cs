using Argumental;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace MultipleOptionsClass
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

    public static int Main_(string[] args)
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
            }, new Option<Options>());
          }));
    }
  }
}
