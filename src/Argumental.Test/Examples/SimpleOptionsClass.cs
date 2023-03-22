using Argumental;
using System.ComponentModel.DataAnnotations;

namespace SimpleOptionsClass
{
  class Program
  {
    public class Options
    {
      [Display(Description = "Set output to verbose messages.")]
      public bool Verbose { get; set; }
    }

    public static int Main_(string[] args)
    {
      return CommandApp.Default()
        .Run(app => CommandPipeline<int>.Default()
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
            }, new OptionGroup<Options>());
          })
          .Invoke());
    }
  }
}
