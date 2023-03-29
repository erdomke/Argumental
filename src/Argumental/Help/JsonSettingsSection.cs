using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Argumental
{
  public class JsonSettingsSection : IDocbookSectionWriter
  {
    public SerializationInfo _info { get; }
    public IEnumerable<string> _paths { get; }

    public int Order => (int)DocbookSection.Files;

    public JsonSettingsSection(SerializationInfo info, IEnumerable<string> paths)
    {
      _info = info;
      _paths = paths;
    }

    public IEnumerable<XElement> Write(DocumentationContext context)
    {
      if (!_paths.Any() || _info == null)
        return Enumerable.Empty<XElement>();

      var jsonVars = Property
        .Flatten(context.Schemas.SelectMany(s => s.Properties), _info, false)
        .Where(p => p.Type.IsConvertibleFromString)
        .ToList();
      if (jsonVars.Count < 1)
        return Enumerable.Empty<XElement>();

      var para = new XElement(DocbookSchema.para, "JSON: ");
      var first = true;
      foreach (var file in _paths
        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
      {
        if (first)
          first = false;
        else
          para.Add(", ");
        para.Add(new XElement(DocbookSchema.filename, file));
      }
      var variableList = new XElement(DocbookSchema.variablelist);
      foreach (var property in jsonVars)
      {
        var entry = new XElement(DocbookSchema.varlistentry);
        foreach (var alias in DocbookNames(property, _info))
          entry.Add(new XElement(DocbookSchema.term, alias));
        entry.Add(new XElement(DocbookSchema.listitem, context.DescribeProperty(property, _info)));
        variableList.Add(entry);
      }
      return new[] { new XElement(DocbookSchema.section
        , new XElement(DocbookSchema.title, "Files")
        , para
        , variableList
      ) };
    }

    private IEnumerable<XElement> DocbookNames(IProperty property, SerializationInfo info)
    {
      var name = new XElement(DocbookSchema.code, "$");
      foreach (var part in info.ConfigurationName(property))
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
