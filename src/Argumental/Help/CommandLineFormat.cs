using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class CommandLineFormat : IConfigFormat
  {
    private readonly string _helpName;
    private readonly string _versionName;

    public ILookup<string, string> AliasLookup { get; }

    public Func<IProperty, bool> Filter { get; set; }

    public bool PosixConventions { get; }

    public CommandLineFormat(IDictionary<string, string> switchMappings, bool posixConventions, string helpName = null, string versionName = null)
    {
      if (switchMappings is Dictionary<string, string> dict) 
      { 
        AliasLookup = switchMappings.ToLookup(k => k.Value.TrimStart('-', '/')
          , k => k.Key
          , dict.Comparer);
      }
      else
      {
        AliasLookup = (switchMappings ?? new Dictionary<string, string>())
          .ToLookup(k => k.Value.TrimStart('-', '/')
            , k => k.Key);
      }

      _helpName = helpName;
      _versionName = versionName;
      
      PosixConventions = posixConventions;
    }

    public IEnumerable<ConfigProperty> GetProperties(IEnumerable<IProperty> properties)
    {
      var propList = new List<IProperty>();
      if (PosixConventions)
        propList.AddRange(properties.Where(p => !(p.Name.Last() is AnyListIndex)));
      else
        propList.AddRange(properties.Where(p => !(p.Type is ArrayType)));

      var globalStart = propList.Count;
      if (!string.IsNullOrEmpty(_versionName))
        propList.Add(new Property(new ConfigPath(new ConfigSection(_versionName, "Show version information")), new BooleanType()));
      if (!string.IsNullOrEmpty(_helpName))
        propList.Add(new Property(new ConfigPath(new ConfigSection(_helpName, "Show help and usage information")), new BooleanType()));

      for (var i = 0; i < propList.Count; i++)
      {
        var fullName = propList[i].Name.ToString();
        if (propList[i].IsPositional)
          yield return new ConfigProperty(new[] { $"<{propList[i].Name.Last().ToString().Replace(' ', '-')}>" }
          , propList[i], i >= globalStart);
        else
          yield return new ConfigProperty(new[] { "--" + fullName }.Concat(AliasLookup[fullName])
            .OrderBy(n => n.Length)
            .Select(n => propList[i].Type is BooleanType && PosixConventions
              ? n
              : $"{n} <{propList[i].Name.Last().ToString().Replace(' ', '-')}>")
            .ToList(), propList[i], i >= globalStart);
      }
    }
  }
}
