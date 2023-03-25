using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class CommandLineInfo : BaseCommandLineInfo
  {
    private List<KeyValuePair<string, string>> _switchMappings = new List<KeyValuePair<string, string>>();

    public CommandLineInfo()
    {
      OptionComparer = StringComparer.OrdinalIgnoreCase;
    }

    public CommandLineInfo AddAlias(string alias, string full)
    {
      _switchMappings.Add(new KeyValuePair<string, string>(alias.TrimStart('-', '/'), full.TrimStart('-', '/')));
      return this;
    }

    public CommandLineInfo SetOptionComparer(IEqualityComparer<string> optionComparer)
    {
      OptionComparer = optionComparer;
      return this;
    }

    public override IEnumerable<string> Aliases(IProperty property)
    {
      if (property == HelpOption)
      {
        foreach (var alias in base.Aliases(property))
          yield return alias;
      }
      else
      {
        var name = string.Join(":", ConfigurationName(property));
        yield return name;
        foreach (var mapping in _switchMappings
          .Where(k => OptionComparer.Equals(k.Value, name)))
        {
          yield return mapping.Key;
        }
      }
    }
  }
}
