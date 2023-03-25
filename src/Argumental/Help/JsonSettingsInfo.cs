using System.Collections.Generic;
using System.Xml.Linq;

namespace Argumental.Help
{
  internal class JsonSettingsInfo : SerializationInfo
  {
    public List<string> Paths { get; } = new List<string>();

    public override IEnumerable<XElement> DocbookNames(IProperty property)
    {
      var name = new XElement(DocbookSchema.code, "$");
      foreach (var part in ConfigurationName(property))
      {
        if (part is AnyInteger)
          name.Add("[*]");
        else if (part is AnyString)
          name.Add(".*");
        else
          name.Add(".", new XElement(DocbookSchema.property, part.ToString()));
      }
      yield return name;
    }
  }
}
