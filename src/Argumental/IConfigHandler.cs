using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Argumental
{
  public interface IConfigHandler<TResult>
  {
    IList<IOptionProvider> Providers { get; }
    Func<IConfigHandler<TResult>, IConfigurationRoot, TResult> Handler { get; set; }
  }
}
