using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Argumental
{
  internal class EnvironmentVariableFormat : IConfigFormat
  {
    public List<string> Prefixes { get; } = new List<string>();

    public IEnumerable<ConfigProperty> GetProperties(IEnumerable<IProperty> properties)
    {
      var propList = new List<IProperty>();
      Property.FlattenList(Array.Empty<IConfigSection>(), properties, false, propList);
      foreach (var property in propList)
      {
        var fullName = string.Join("__", property.Name.Select(p => p.ToString())).ToUpperInvariant();
        yield return new ConfigProperty(Prefixes
          .OrderBy(p => p ?? "", StringComparer.OrdinalIgnoreCase)
          .Select(p => 
            new XElement(DocbookSchema.envar, (p ?? "") + fullName)
          ), property);
      }
    }
  }
}
