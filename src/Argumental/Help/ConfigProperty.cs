using System.Collections.Generic;

namespace Argumental
{
  public class ConfigProperty
  {
    public IEnumerable<string> Aliases { get; }
    public bool IsGlobal { get; }
    public IProperty Property { get; }

    public ConfigProperty(IEnumerable<string> aliases, IProperty property, bool global = false)
    {
      Aliases = aliases;
      Property = property;
      IsGlobal = global;
    }
  }
}
