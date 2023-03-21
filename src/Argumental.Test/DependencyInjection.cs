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
  scl --Read <Read>... [--Offset <Offset>] [--StdIn] [--Verbose]

Options:
  --Read <Read>     Input files to be processed.
  --Offset <Offset> File offset.
  --StdIn           Read from stdin.
  --Verbose         Prints all messages to standard output.
  --version         Show version information
  -?, -h, --help    Show help and usage information

Environment:
  SCL_OFFSET        File offset.
  SCL_READ__#       Input files to be processed.
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
  }
}
