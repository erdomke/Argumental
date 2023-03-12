using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Argumental.Help
{
  public class ConfigAlias : IEnumerable<ConfigAliasPart>
  {
    private readonly List<ConfigAliasPart> _parts = new List<ConfigAliasPart>();

    public ConfigAliasType Type { get; }

    public ConfigAlias(ConfigAliasType type) 
    {
      Type = type;
    }

    public ConfigAlias(ConfigAliasType type, string fullName)
    {
      Type = type;
      Add(new ConfigAliasPart(fullName, type));
    }

    public void Add(ConfigAliasPart part)
    {
      _parts.Add(part);
    }

    public IEnumerator<ConfigAliasPart> GetEnumerator()
    {
      return _parts.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public override string ToString()
    {
      return string.Join("", _parts.Select(p => p.Value));
    }
  }
}
