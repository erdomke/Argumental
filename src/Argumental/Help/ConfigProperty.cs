using System.Collections.Generic;
using System.Xml.Linq;

namespace Argumental
{
  public class ConfigProperty
  {
    public IEnumerable<XElement> DocbookAliases { get; }
    public bool IsGlobal { get; }
    public IProperty Property { get; }

    public ConfigProperty(IEnumerable<XElement> aliases, IProperty property, bool global = false)
    {
      DocbookAliases = aliases;
      Property = property;
      IsGlobal = global;
    }
  }
}
