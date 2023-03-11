using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  /// <summary>
  /// Adapted from <see href="https://manpages.ubuntu.com/manpages/lunar/man3/sysexits.h.3head.html"/>
  /// </summary>
  public enum ExitCode
  {
    /// <summary>
    /// The application successfully completed
    /// </summary>
    Success = 0,
    /// <summary>
    /// The application failed with a general error
    /// </summary>
    Failure = 1,
    /// <summary>
    /// The command was used incorrectly, e.g., with the wrong number of arguments, a bad
    /// flag, bad syntax in a parameter, or whatever.
    /// </summary>
    UsageError =	64,
    /// <summary>
    /// The input data was incorrect in some way. This should only be used for user's data
    /// and not system files.
    /// </summary>
    DataError = 65,
    /// <summary>
    /// An input file (not a system file) did not exist or was not readable. This could
    /// also include errors like "No message" to a mailer (if it cared to catch it).
    /// </summary>
    NoInput = 66,
    /// <summary>
    /// The user specified did not exist. This might be used for mail addresses or remote
    /// logins.
    /// </summary>
    NoUser = 67,
    /// <summary>
    /// The host specified did not exist. This is used in mail addresses or network
    /// requests.
    /// </summary>
    NoHost = 68,
    /// <summary>
    /// A service is unavailable. This can occur if a support program or file does not
    /// exist. This can also be used as a catch-all message when something you wanted to
    /// do doesn't work, but you don't know why.
    /// </summary>
    Unavailable =	69,
    /// <summary>
    /// An internal software error has been detected. This should be limited to non-
    /// operating system related errors if possible.
    /// </summary>
    Software = 70,
    /// <summary>
    /// An operating system error has been detected. This is intended to be used for such
    /// things as "cannot fork", "cannot create pipe", or the like.
    /// </summary>
    OsError	= 71,
    /// <summary>
    /// Some system file (e.g., /etc/passwd, /etc/utmp, etc.) does not exist, cannot be
    /// opened, or has some sort of error (e.g., syntax error).
    /// </summary>
    OsFile = 72,
    /// <summary>
    /// A (user-pecified) output file cannot be created.
    /// </summary>
    CantCreate = 73,
    /// <summary>
    /// An error occurred while doing I/O on some file.
    /// </summary>
    IoError =	74,
    /// <summary>
    /// Temporary failure, indicating something that is not really an error. For example
    /// that a mailer could not create a connection, and the request should be reattempted
    /// later.
    /// </summary>
    TempFailure =	75,
    /// <summary>
    /// The remote system returned something that was "not possible" during a protocol
    /// exchange.
    /// </summary>
    ProtocolError =	76,
    /// <summary>
    /// You did not have sufficient permission to perform the operation. This is not
    /// intended for file system problems, which should use EX_NOINPUT or EX_CANTCREAT, but
    /// rather for higher level permissions.
    /// </summary>
    NoPermissions =	77,
    /// <summary>
    /// Something was found in an unconfigured or misconfigured state.
    /// </summary>
    ConfigError =	78
  }
}
