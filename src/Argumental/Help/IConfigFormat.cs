using Argumental.Help;
using System;
using System.Collections.Generic;

namespace Argumental
{
  public interface IConfigFormat
  {
    IEnumerable<ConfigProperty> GetProperties(IEnumerable<IProperty> properties);
  }
}
