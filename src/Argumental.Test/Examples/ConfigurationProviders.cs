using Argumental;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConfigurationProviders
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
      var pipeline = CommandPipeline<int>.Default()
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
        });
      var host = Host.CreateDefaultBuilder(args)
        .ConfigureHostConfiguration(c =>
        {
          c.Sources.Clear();
          c.AddEnvironmentVariables("SCL_");
        })
        .ConfigureServices((c, s) =>
        {
          //s.Configure
          //pipeline.Invoke(c.Configuration);
        })
        .Build();
      host.Run();
      return 0;
    }
  }
}
