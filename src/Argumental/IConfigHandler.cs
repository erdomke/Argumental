using System;
using System.Collections.Generic;

namespace Argumental
{
  public interface IConfigHandler
  {
    IList<IOptionProvider> Providers { get; }
  }

  public interface IConfigHandler<TResult> : IConfigHandler
  {
    Func<InvocationContext, TResult> Handler { get; set; }
  }
}
