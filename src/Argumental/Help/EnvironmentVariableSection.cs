using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Argumental.Help
{
  internal class EnvironmentVariableSection : IDocbookSectionWriter
  {
    private readonly SerializationInfo _info;
    private readonly IEnumerable<string> _prefixes;

    public int Order => (int)DocbookSection.Environment;

    public EnvironmentVariableSection(SerializationInfo info, IEnumerable<string> prefixes)
    {
      _info = info;
      _prefixes = prefixes;
      if (!(_prefixes?.Any() == true))
        _prefixes = new[] { "" };
    }

    public IEnumerable<XElement> Write(DocumentationContext context)
    {
      var envVars = Property
        .Flatten(context.Schemas.SelectMany(s => s.Properties), _info, false)
        .ToList();
      if (envVars.Count < 1)
        return Enumerable.Empty<XElement>();
      
      var variableList = new XElement(DocbookSchema.variablelist);
      foreach (var property in envVars)
      {
        var entry = new XElement(DocbookSchema.varlistentry);
        var fullName = string.Join("__", _info.ConfigurationName(property)).ToUpperInvariant();
        foreach (var alias in _prefixes
          .OrderBy(p => p ?? "", StringComparer.OrdinalIgnoreCase)
          .Select(p => new XElement(DocbookSchema.envar, (p ?? "") + fullName)))
        {
          entry.Add(new XElement(DocbookSchema.term, alias));
        }
        entry.Add(new XElement(DocbookSchema.listitem, context.DescribeProperty(property, _info)));
        variableList.Add(entry);
      }
      return new[] { new XElement(DocbookSchema.section
        , new XElement(DocbookSchema.title, "Environment")
        , variableList
      ) };
    }
  }
}
