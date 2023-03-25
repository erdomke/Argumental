using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argumental.Test
{
  [TestClass]
  public class DependencyInjection
  {
    [TestMethod]
    public void IOptions()
    {
      var result = CommandResult.Run(new[] { "-r", "file1.txt", "file2.txt" }, InjectingIOptions.Program.Main_);
      //Assert.AreEqual("", result.Error.ToString()?.TrimEnd());
      Assert.AreEqual(@"Normal, File, null, file1.txt, file2.txt", result.Out.ToString()?.TrimEnd());
      result = CommandResult.Run(new[] { "--verbose" }, InjectingIOptions.Program.Main_);
      Assert.AreEqual((int)ExitCode.UsageError, result.ExitCode);
      result = CommandResult.Run(new[] { "--help" }, InjectingIOptions.Program.Main_);
      Assert.AreEqual(@"Sample app for Argumental

Usage:
  scl -r <Read>... [--Offset <Offset>] [--StdIn] [--Verbose]

Options:
  -r <Read>, --Read <Read>  Input files to be processed.
  --Offset <Offset> File offset.
  --StdIn           Read from stdin.
  --Verbose         Prints all messages to standard output.
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
                    perform the operation.

Environment:
  SCL_READ__#       Input files to be processed.
  SCL_OFFSET        File offset.
  SCL_STDIN         Read from stdin.
  SCL_VERBOSE       Prints all messages to standard output.", result.Out.ToString()?.TrimEnd());
    }

    [TestMethod]
    public void LazyConstant()
    {
      var result = CommandResult.Run(new[] { "--file", "scl.runtimeconfig.json" }, InjectingLazyConstant.Program.Main_);
      Assert.AreEqual(@"File = scl.runtimeconfig.json, Logger = Logger", result.Out.ToString()?.TrimEnd());
    }

    [TestMethod]
    public void Provider()
    {
      var result = CommandResult.Run(new[] { "--file", "scl.runtimeconfig.json" }, InjectingProvider.Program.Main_);
      Assert.AreEqual(@"File = scl.runtimeconfig.json, Logger = Logger", result.Out.ToString()?.TrimEnd());
    }

    [TestMethod]
    public void Hosting()
    {
      var result = CommandResult.Run(new[] { "--read:0", "file1.txt", "--read:1", "file2.txt" }, HostingExample.Program.Main_);
      Assert.AreEqual(@"Normal, File, null, file1.txt, file2.txt", result.Out.ToString()?.TrimEnd());
      result = CommandResult.Run(new[] { "help" }, HostingExample.Program.Main_);
      Assert.AreEqual(@"Sample app for Argumental

Usage:
  scl --Read:# <Read> [--Offset <Offset>] [--StdIn <StdIn>]
    [--Verbose <Verbose>]

Options:
  --Read:# <Read>   Input files to be processed.
  --Offset <Offset> File offset.
  --StdIn <StdIn>   Read from stdin.
  --Verbose <Verbose>  Prints all messages to standard output.

Exit Status:
  0                 The application successfully completed.
  1                 The application failed with a general error.
  64                The number or syntax of the arguments passed to the command
                    is incorrect.
  66                An input file does not exist or is not readable.
  74                An error occurred while doing I/O on a file.
  75                A temporary failure occurred. Please try again.
  77                The current user does not have sufficient permission to
                    perform the operation.

Environment:
  READ__#           Input files to be processed.
  OFFSET            File offset.
  STDIN             Read from stdin.
  VERBOSE           Prints all messages to standard output.

Files:
  JSON: appsettings.json, appsettings.Production.json

  $.Read[*]         Input files to be processed.
  $.Offset          File offset.
  $.StdIn           Read from stdin.
  $.Verbose         Prints all messages to standard output.", result.Out.ToString()?.TrimEnd());
    }
  }
}
