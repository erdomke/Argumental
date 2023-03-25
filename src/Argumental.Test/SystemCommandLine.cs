namespace Argumental.Test
{
  [TestClass]
  public class SystemCommandLine
  {
    private static Func<AssemblyMetadata> _origMetadata;

    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
      _origMetadata = AssemblyMetadata._defaultMetadata;
      AssemblyMetadata._defaultMetadata = () =>
      {
        return new AssemblyMetadata().SetName("scl")
          .SetDescription("Sample app for Argumental")
          .SetVersion("1.0.0");
      };
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
      AssemblyMetadata._defaultMetadata = _origMetadata;
    }
    
    [TestMethod]
    public async Task Basic()
    {
      var result = await CommandResult.RunAsync(new[] { "--file", "something.txt" }, BasicApp.Program.Main_);
      Assert.AreEqual("Read file: something.txt", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "--help" }, BasicApp.Program.Main_);
      Assert.AreEqual(@"Sample app for Argumental

Usage:
  scl [--file <file>]

Options:
  --file <file>     The file to read and display on the console.
  --version         Show version information
  -?, -h, --help    Show help and usage information

Exit Status:
  0                 The application successfully completed.
  1                 The application failed with a general error.
  64                The number or syntax of the arguments passed to the command
                    is incorrect.
  66                An input file does not exist or is not readable.
  74                An error occurred while doing I/O on a file.
  75                A temporary failure occurred. Please try again.
  77                The current user does not have sufficient permission to
                    perform the operation.", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "--version" }, BasicApp.Program.Main_);
      Assert.AreEqual(@"1.0.0", result.Out.ToString()?.TrimEnd());
    }

    
    [TestMethod]
    public async Task Subcommand()
    {
      var result = await CommandResult.RunAsync(new[] { "--file", "sampleQuotes.txt" }, SubcommandApp.Program.Main_);
      Assert.AreEqual(@"Required command was not provided.

Sample app for Argumental

Usage:
  scl [command] [options]

Options:
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  read              Read and display the file.", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "read", "-h" }, SubcommandApp.Program.Main_);
      Assert.AreEqual(@"Read and display the file.

Usage:
  scl read [--delay <delay>] [--fgcolor <fgcolor>] [--file <file>]
    [--light-mode]

Options:
  --delay <delay>   Delay between lines, specified as milliseconds per character
                    in a line. [default: 42]
  --fgcolor <fgcolor>  Foreground color of text displayed on the console.
                    [default: White]
  --file <file>     The file to read and display on the console.
  --light-mode      Background color of text displayed on the console: default
                    is black, light mode is white.
  --version         Show version information
  -?, -h, --help    Show help and usage information

Exit Status:
  0                 The application successfully completed.
  1                 The application failed with a general error.
  64                The number or syntax of the arguments passed to the command
                    is incorrect.
  66                An input file does not exist or is not readable.
  74                An error occurred while doing I/O on a file.
  75                A temporary failure occurred. Please try again.
  77                The current user does not have sufficient permission to
                    perform the operation.", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "read", "--file", "sampleQuotes.txt" }, SubcommandApp.Program.Main_);
      Assert.AreEqual(@"sampleQuotes.txt, 42, White, False", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "read", "--file", "sampleQuotes.txt", "--delay", "0" }, SubcommandApp.Program.Main_);
      Assert.AreEqual(@"sampleQuotes.txt, 0, White, False", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "read", "--file", "sampleQuotes.txt", "--fgcolor", "red", "--light-mode" }, SubcommandApp.Program.Main_);
      Assert.AreEqual(@"sampleQuotes.txt, 42, Red, True", result.Out.ToString()?.TrimEnd());
      result = await CommandResult.RunAsync(new[] { "read", "--file", "sampleQuotes.txt", "--delay", "forty-two" }, SubcommandApp.Program.Main_);
      Assert.AreEqual(@"Failed to convert configuration value at 'delay' to type 'System.Int32'. ->
forty-two is not a valid value for Int32. (Parameter 'value') -> Input string
was not in a correct format.

Read and display the file.

Usage:
  scl read [--delay <delay>] [--fgcolor <fgcolor>] [--file <file>]
    [--light-mode]

Options:
  --delay <delay>   Delay between lines, specified as milliseconds per character
                    in a line. [default: 42]
  --fgcolor <fgcolor>  Foreground color of text displayed on the console.
                    [default: White]
  --file <file>     The file to read and display on the console.
  --light-mode      Background color of text displayed on the console: default
                    is black, light mode is white.
  --version         Show version information
  -?, -h, --help    Show help and usage information

Exit Status:
  0                 The application successfully completed.
  1                 The application failed with a general error.
  64                The number or syntax of the arguments passed to the command
                    is incorrect.
  66                An input file does not exist or is not readable.
  74                An error occurred while doing I/O on a file.
  75                A temporary failure occurred. Please try again.
  77                The current user does not have sufficient permission to
                    perform the operation.", result.Out.ToString()?.TrimEnd());
    }
  }
}
