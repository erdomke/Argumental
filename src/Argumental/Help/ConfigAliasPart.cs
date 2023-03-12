using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  public struct ConfigAliasPart
  {
    public string Value { get; }
    public ConfigAliasType Type { get; }

    public ConfigAliasPart(string value, ConfigAliasType type)
    {
      Value = value;
      Type = type;
    }
  }
}
