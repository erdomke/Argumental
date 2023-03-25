using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Argumental
{
  internal class EnvironmentVariableInfo : SerializationInfo
  {
    public List<string> Prefixes { get; } = new List<string>();

    public override IEnumerable<XElement> DocbookNames(IProperty property)
    {
      var fullName = Name(property);
      return Prefixes
        .OrderBy(p => p ?? "", StringComparer.OrdinalIgnoreCase)
        .Select(p => new XElement(DocbookSchema.envar, (p ?? "") + fullName));
    }

    public override string Name(IProperty property)
    {
      return string.Join("__", ConfigurationName(property)).ToUpperInvariant();
    }
  }
}
