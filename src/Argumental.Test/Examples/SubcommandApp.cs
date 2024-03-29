﻿using Argumental;

namespace SubcommandApp
{
  class Program
  {
    public static Task<int> Main_(string[] args)
    {
      return CommandApp.Default()
        .RunAsync(app => CommandPipeline<Task<int>>.Default()
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
              .SetDefaultValue(42)
            , new Option<ConsoleColor>("fgcolor", "Foreground color of text displayed on the console.")
              .SetDefaultValue(ConsoleColor.White)
            , new Option<bool>("light-mode", "Background color of text displayed on the console: default is black, light mode is white."));
          })
          .Invoke());
    }
  }

}
