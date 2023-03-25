using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Argumental
{
  public abstract class BaseCommandLineInfo : SerializationInfo
  {
    public IEqualityComparer<string> OptionComparer { get; protected set; } = StringComparer.Ordinal;
    public bool PosixConventions { get; set; } = true;

    internal IProperty HelpOption { get; set; }
    internal IProperty VersionOption { get; set; }

    public virtual IEnumerable<string> Aliases(IProperty property)
    {
      var name = property.Name.ToString();
      if (property == HelpOption)
      {
        if (name != "?")
          yield return "?";
        if (name.Length > 1)
          yield return name.Substring(0, 1);
        yield return name;
      }
      else
      {
        yield return name;
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
