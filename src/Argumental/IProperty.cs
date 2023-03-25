using System;
using System.Collections.Generic;

namespace Argumental
{
  public interface IProperty
  {
    ConfigPath Name { get; }
    IDataType Type { get; }
    IEnumerable<Attribute> Attributes { get; }
  }
}
