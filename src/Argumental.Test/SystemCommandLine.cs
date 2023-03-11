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
          .RunAsync(CommandPipeline<Task<int>>.Default()
            .AddArgs(args)
            .AddDescription("Sample app for Argumental")
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
      //Assert.AreEqual("Normal output.", result.Out.ToString()?.TrimEnd());
    }
  }
}
