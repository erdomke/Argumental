namespace Argumental.Test
{
  internal class CommandResult
  {
    public int ExitCode { get; private set; }
    public TextWriter Out { get; } = new StringWriter();
    public TextWriter Error { get; } = new StringWriter();

    private CommandResult() { }

    public static CommandResult Run(string[] args, Func<string[], int> command)
    {
      var origOut = Console.Out;
      var origErr = Console.Error;
      try
      {
        var result = new CommandResult();
        Console.SetOut(result.Out);
        Console.SetError(result.Error);
        result.ExitCode = command(args);
        return result;
      }
      finally
      {
        Console.SetOut(origOut);
        Console.SetError(origErr);
      }
    }
  }
}
