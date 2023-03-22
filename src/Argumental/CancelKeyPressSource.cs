using System;
using System.Threading;

namespace Argumental
{
  public class CancelKeyPressSource : ICancellationTokenSource
  {
    private CancellationTokenSource _cancellationTokenSource;
    private object _lock = new object();

    public CancellationToken Token
    {
      get
      {
        lock (_lock)
        {
          if (_cancellationTokenSource == null)
          {
            _cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += Console_CancelKeyPress;
          }
          return _cancellationTokenSource.Token;
        }
      }
    }

    private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
      e.Cancel = true;
      _cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
      if (_cancellationTokenSource != null)
      {
        _cancellationTokenSource.Dispose();
        Console.CancelKeyPress -= Console_CancelKeyPress;
      }
      _cancellationTokenSource = null;
    }
  }
}
