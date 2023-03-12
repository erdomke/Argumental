using Argumental.Help;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Argumental
{
  public class CommandLineFormat : IConfigFormat
  {
    public ILookup<string, string> AliasLookup { get; }

    public Func<IProperty, bool> Filter { get; set; }

    public IEnumerable<string> HelpAliases { get; }
    public IEnumerable<string> VersionAliases { get; }

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

      if (string.IsNullOrEmpty(helpName))
        HelpAliases = Enumerable.Empty<string>();
      else
        HelpAliases = new[] { "--" + helpName }.Concat(AliasLookup[helpName]).OrderBy(a => a.Length);

      if (string.IsNullOrEmpty(versionName))
        VersionAliases = Enumerable.Empty<string>();
      else
        VersionAliases = new[] { "--" + versionName }.Concat(AliasLookup[versionName]).OrderBy(a => a.Length);

      PosixConventions = posixConventions;
    }

    public IEnumerable<ConfigAlias> GetAliases(IProperty property)
    {
      var fullName = property.Path.ToString();
      return new[] { "--" + fullName }.Concat(AliasLookup[fullName])
        .OrderBy(n => n.Length)
        .Select(n =>
        {
          var alias = new ConfigAlias(ConfigAliasType.Argument)
          {
            new ConfigAliasPart(n, ConfigAliasType.Argument)
          };
          if (!(property.Type is BooleanType && PosixConventions))
          {
            alias.Add(new ConfigAliasPart(" ", ConfigAliasType.Other));
            alias.Add(new ConfigAliasPart("<" + property.Path.Last().ToString() + ">", ConfigAliasType.Replaceable));
          }
          return alias;
        });
    }
  }
}
