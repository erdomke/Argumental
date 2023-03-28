using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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

    public override IEnumerable<XElement> DocbookNames(IProperty property)
    {
      var isPositional = IsPositional(property);
      var prompt = Prompt(property);
      if (isPositional)
      {
        yield return new XElement(DocbookSchema.replaceable, prompt);
      }
      else
      {
        var isRequired = Use(property) == PropertyUse.Required;
        foreach (var alias in Aliases(property)
          .OrderBy(n => n.Length)
          .ThenBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
          if (property.Type is BooleanType && PosixConventions)
          {
            yield return new XElement(DocbookSchema.arg, (alias.Length == 1 ? "-" : "--") + alias);
          }
          else
          {
            var result = new XElement(DocbookSchema.arg);
            if (isRequired)
              result.Add(new XAttribute("choice", "req"));
            if (property.Type is ArrayType)
              result.Add(new XAttribute("rep", "repeat"));
            result.Add((alias.Length == 1 ? "-" : "--") + alias + " ");
            result.Add(new XElement(DocbookSchema.replaceable, prompt));
            yield return result;
          }
        }
      }
    }

    public override IEnumerable<IProperty> Flatten(IEnumerable<IProperty> properties)
    {
      var propList = new List<IProperty>();
      FlattenList(Array.Empty<IConfigSection>()
        , properties.Where(p => Use(p) < PropertyUse.Hidden)
        , PosixConventions
        , propList);
      return propList
        .OrderBy(p => IsPositional(p) ? 1 : 0)
        .ThenBy(p => {
          var use = Use(p);
          return use == PropertyUse.Required ? -1 : (int)use;
        })
        .ThenBy(p => Order(p))
        .ThenBy(p => Aliases(p).First());
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
