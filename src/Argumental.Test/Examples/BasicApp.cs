using Argumental;

namespace BasicApp
{
  class Program
  {
    public static Task<int> Main_(string[] args)
    {
      return CommandApp.Default()
        .RunAsync(app => CommandPipeline<Task<int>>.Default()
          .AddArgs(args)
          .AddCommand("", c =>
          {
            c.SetHandler((file) =>
            {
              Console.WriteLine("Read file: " + file);
              return Task.FromResult(0);
            }, new Option<string>("file", "The file to read and display on the console."));
          })
          .Invoke());
    }
  }

}
