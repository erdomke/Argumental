using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class CommandLineInfo : SerializationInfo
  {
    private List<KeyValuePair<string, string>> _switchMappings = new List<KeyValuePair<string, string>>();

    public IEqualityComparer<string> OptionComparer { get; protected set; }
    public bool PosixConventions { get; set; }
    internal IProperty HelpOption { get; set; }
    internal IProperty VersionOption { get; set; }

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

    public virtual IEnumerable<string> Aliases(IProperty property)
    {
      if (property == HelpOption)
      {
        var name = property.Name.ToString();
        if (name != "?")
          yield return "?";
        if (name.Length > 1)
          yield return name.Substring(0, 1);
        yield return name;
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

    public IEnumerable<IProperty> GlobalOptions()
    {
      if (VersionOption != null)
        yield return VersionOption;
      if (HelpOption != null)
        yield return HelpOption;
    }

    public virtual bool IsPositional(IProperty property)
    {
      return property.Attributes.OfType<PositionalAttribute>().Any();
    }
  }
}
