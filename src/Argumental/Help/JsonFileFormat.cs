using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Argumental.Help
{
  internal class JsonFileFormat : IConfigFormat
  {
    public List<string> Paths { get; } = new List<string>();

    public IEnumerable<ConfigProperty> GetProperties(IEnumerable<IProperty> properties)
    {
      var propList = new List<IProperty>();
      Property.FlattenList(Array.Empty<IConfigSection>(), properties, false, propList);
      foreach (var property in Property.DefaultSort(propList))
      {
        var name = new XElement(DocbookSchema.code, "$");
        foreach (var part in property.Name)
        {
          if (part is AnyInteger)
            name.Add("[*]");
          else if (part is AnyString)
            name.Add(".*");
          else
            name.Add(".", new XElement(DocbookSchema.property, part.ToString()));
        }
        yield return new ConfigProperty(new[] { name }, property);
      }
    }
  }
}
