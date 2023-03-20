namespace Argumental.Test
{
  [TestClass]
  public class CommandLineParser
  {
    [TestMethod]
    public void GettingStarted()
    {
      var result = CommandResult.Run(new[] { "--verbose" }, SimpleOptionsClass.Program.Main_);
      Assert.AreEqual("Verbose output enabled.", result.Out.ToString()?.TrimEnd());
      result = CommandResult.Run(Array.Empty<string>(), SimpleOptionsClass.Program.Main_);
      Assert.AreEqual("Normal output.", result.Out.ToString()?.TrimEnd());
    }

    [TestMethod]
    public void Options()
    {
      var result = CommandResult.Run(new[] { "-r", "file1.txt", "file2.txt" }, MultipleOptionsClass.Program.Main_);
      Assert.AreEqual("Normal, File, null, file1.txt, file2.txt", result.Out.ToString()?.TrimEnd());
      result = CommandResult.Run(new[] { "--verbose" }, MultipleOptionsClass.Program.Main_);
      Assert.AreEqual((int)ExitCode.UsageError, result.ExitCode);
    }
  }
}