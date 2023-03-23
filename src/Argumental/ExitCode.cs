using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Argumental
{
  /// <summary>
  /// Adapted from <see href="https://manpages.ubuntu.com/manpages/lunar/man3/sysexits.h.3head.html"/>
  /// </summary>
  public enum ExitCode
  {
    [Description("The application successfully completed.")]
    Success = 0,
    [Description("The application failed with a general error.")]
    Failure = 1,
    [Description("The number or syntax of the arguments passed to the command is incorrect.")]
    UsageError =	64,
    [Description("The input data is incorrect.")]
    DataError = 65,
    [Description("An input file does not exist or is not readable.")]
    NoInput = 66,
    [Description("The user specified does not exist.")]
    NoUser = 67,
    [Description("The host specified does not exist.")]
    NoHost = 68,
    [Description("A service is unavailable.")]
    Unavailable =	69,
    [Description("An internal software error occurred.")]
    Software = 70,
    [Description("An operating system error occurred.")]
    OsError	= 71,
    [Description("A system file does not exist, cannot be opened, or has a syntax error).")]
    OsFile = 72,
    [Description("A (user-pecified) output file cannot be created.")]
    CantCreate = 73,
    [Description("An error occurred while doing I/O on a file.")]
    IoError =	74,
    [Description("A temporary failure occurred. Please try again.")]
    TempFailure =	75,
    [Description("A remote system returned an invalid value during a protocol exchange.")]
    ProtocolError =	76,
    [Description("The current user does not have sufficient permission to perform the operation.")]
    NoPermissions =	77,
    [Description("Something was found in an unconfigured or misconfigured state.")]
    ConfigError =	78
  }
}
