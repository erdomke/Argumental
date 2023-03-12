using Argumental.Help;
using System;
using System.Collections.Generic;

namespace Argumental
{
  public interface IConfigFormat
  {
    Func<IProperty, bool> Filter { get; }

    IEnumerable<ConfigAlias> GetAliases(IProperty property);
  }
}
