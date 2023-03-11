using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  public class VersionException : Exception
  {
    public VersionException(string message) : base(message)
    {
    }
  }
}
