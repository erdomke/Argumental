using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Argumental
{
  public interface IConfigHandler<TResult>
  {
    IList<IOptionProvider> Providers { get; }
    Func<IConfigHandler<TResult>, IConfiguration, TResult> Handler { get; set; }
  }
}
