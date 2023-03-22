using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Argumental
{
  public interface ICancellationTokenSource : IDisposable
  {
    CancellationToken Token { get; }
  }
}
