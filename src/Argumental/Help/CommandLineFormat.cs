using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Argumental
{
  public class CommandLineFormat : IConfigFormat
  {
    private string _helpName;
    private string _versionName;

    public Dictionary<string, List<string>> AliasLookup { get; private set; }

    public bool PosixConventions { get; private set; }

    public CommandLineFormat AddConfiguration(IDictionary<string, string> switchMappings, bool posixConventions, string helpName = null, string versionName = null)
    {
      if (switchMappings != null)
      {
        if (AliasLookup == null)
        {
          AliasLookup = switchMappings is Dictionary<string, string> dict
            ? new Dictionary<string, List<string>>(dict.Comparer)
            : new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var group in switchMappings.ToLookup(k => k.Value.TrimStart('-', '/')
          , k => k.Key
          , AliasLookup.Comparer))
        {
          if (AliasLookup.TryGetValue(group.Key, out var list))
            list.AddRange(group);
          else
            AliasLookup[group.Key] = group.ToList();
        }
      }

      _helpName = _helpName ?? helpName;
      _versionName = _versionName ?? versionName;

      PosixConventions = PosixConventions || posixConventions;
      return this;
    }

    public IEnumerable<ConfigProperty> GetProperties(IEnumerable<IProperty> properties)
    {
      var propList = new List<IProperty>();
      Property.FlattenList(Array.Empty<IConfigSection>(), properties, PosixConventions, propList);
      propList = Property.DefaultSort(propList).ToList();

      var globalStart = propList.Count;
      if (!string.IsNullOrEmpty(_versionName))
        propList.Add(new Property(new ConfigPath(new ConfigSection(_versionName, "Show version information")), new BooleanType()));
      if (!string.IsNullOrEmpty(_helpName))
        propList.Add(new Property(new ConfigPath(new ConfigSection(_helpName, "Show help and usage information")), new BooleanType()));

      for (var i = 0; i < propList.Count; i++)
      {
        var fullName = propList[i].Name.ToString();
        if (propList[i].IsPositional)
          yield return new ConfigProperty(new[] { 
            new XElement(DocbookSchema.replaceable, propList[i].Name.OfType<ConfigSection>().Last().ToString()) 
          }
          , propList[i], i >= globalStart);
        else
          yield return new ConfigProperty(new[] { "--" + fullName }
            .Concat(AliasLookup?.TryGetValue(fullName, out var list) == true 
              ? list
              : (IEnumerable<string>)Array.Empty<string>())
            .OrderBy(n => n.Length)
            .Select(n => {
              if (propList[i].Type is BooleanType && PosixConventions)
                return new XElement(DocbookSchema.arg, n);
              var result = new XElement(DocbookSchema.arg);
              if (propList[i].IsRequired())
                result.Add(new XAttribute("choice", "req"));
              if (propList[i].Type is ArrayType)
                result.Add(new XAttribute("rep", "repeat"));
              result.Add(n + " ");
              result.Add(new XElement(DocbookSchema.replaceable, propList[i].Name.OfType<ConfigSection>().Last().ToString()));
              return result;
            })
            .ToList(), propList[i], i >= globalStart);
      }
    }
  }
}
