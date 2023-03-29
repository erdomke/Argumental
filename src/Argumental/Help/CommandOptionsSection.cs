using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Argumental.Help
{
  internal class CommandOptionsSection : IDocbookSectionWriter
  {
    private readonly CommandLineInfo _info;
    private readonly AssemblyMetadata _metadata;

    public int Order => (int)DocbookSection.Options;

    public CommandOptionsSection(CommandLineInfo info, AssemblyMetadata metadata)
    {
      _info = info;
      _metadata = metadata;
    }

    public IEnumerable<XElement> Write(DocumentationContext context)
    {
      if (context.Schemas.Count == 1
        && context.Schemas[0] is ICommand command
        && string.IsNullOrEmpty(command.Name.ToString()))
      {
        yield return new XElement(DocbookSchema.section
          , new XElement(DocbookSchema.title, "Synopsis")
          , WriteSynopsis(command, context)
        );
        var options = WriteCommandOptions(command, context);
        if (options != null)
          yield return options;
      }
      else if (context.Scope == DocumentationScope.Root)
      {
        foreach (var part in WriteSummaryEntry(context))
          yield return part;
      }
      else if (context.Scope == DocumentationScope.Command
        && context.Schemas.Count > 0)
      {
        yield return WriteCommandSection((ICommand)context.Schemas[0], context);
      }
      else
      {
        foreach (var part in WriteSummaryEntry(context))
          yield return part;
        foreach (var schema in context.Schemas.OfType<ICommand>())
          yield return WriteCommandSection(schema, context);
      }
    }

    private IEnumerable<XElement> WriteSummaryEntry(DocumentationContext context)
    {
      yield return new XElement(DocbookSchema.cmdsynopsis
        , new XElement(DocbookSchema.command, _metadata?.Name)
        , " "
        , new XElement(DocbookSchema.arg, "command")
        , " "
        , new XElement(DocbookSchema.arg, "options"));
      
      if (_info.GlobalOptions().Any())
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in _info.GlobalOptions())
        {
          var entry = new XElement(DocbookSchema.varlistentry);
          foreach (var name in DocbookNames(option, false))
            entry.Add(new XElement(DocbookSchema.term, name));
          entry.Add(new XElement(DocbookSchema.listitem, context.DescribeProperty(option, _info)));
          variableList.Add(entry);
        }
        yield return new XElement(DocbookSchema.section
          , new XElement(DocbookSchema.title, "Options")
          , variableList
        );
      }

      if (context.Schemas.OfType<ICommand>().Any())
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var cmd in context.Schemas.OfType<ICommand>())
        {
          var term = new XElement(DocbookSchema.term);
          var cmdDescrip = default(string);
          foreach (var name in cmd.Name)
          {
            if (term.FirstNode != null)
              term.Add(" ");
            term.Add(new XElement(DocbookSchema.parameter
              , new XAttribute("class", "command")
              , name.ToString()
            ));
            cmdDescrip = (name as ConfigSection)?.Description;
          }
          variableList.Add(new XElement(DocbookSchema.varlistentry
            , term
            , new XElement(DocbookSchema.listitem
              , new XElement(DocbookSchema.para, cmdDescrip)
            )
          ));
        }
        yield return new XElement(DocbookSchema.section
          , new XElement(DocbookSchema.title, "Commands")
          , variableList
        );
      }
    }

    private XElement WriteCommandSection(ICommand command, DocumentationContext context)
    {
      var name = string.Join(" ", command.Name.Select(n => n.ToString()));
      var result = new XElement(DocbookSchema.section
        , new XElement(DocbookSchema.title, name)
      );

      var description = (command.Name.Last() as ConfigSection)?.Description;
      if (!string.IsNullOrEmpty(description))
        result.Add(new XElement(DocbookSchema.para, description));

      result.Add(WriteSynopsis(command, context));
      var options = WriteCommandOptions(command, context);
      if (options != null)
        result.Add(options);
      return result;
    }

    private XElement WriteCommandOptions(ISchemaProvider schema, DocumentationContext context)
    {
      var options = Flatten(schema.Properties)
        .Concat(_info.GlobalOptions())
        .ToList();
      if (options.Count < 1)
        return null;

      var variableList = new XElement(DocbookSchema.variablelist);
      foreach (var option in options)
      {
        var entry = new XElement(DocbookSchema.varlistentry);
        foreach (var name in DocbookNames(option, false))
          entry.Add(new XElement(DocbookSchema.term, name));
        entry.Add(new XElement(DocbookSchema.listitem, context.DescribeProperty(option, _info)));
        variableList.Add(entry);
      }
      return new XElement(DocbookSchema.section
        , new XElement(DocbookSchema.title, "Options")
        , variableList
      );
    }

    private XElement WriteSynopsis(ICommand command, DocumentationContext context)
    {
      var result = new XElement(DocbookSchema.cmdsynopsis
        , new XElement(DocbookSchema.command, _metadata.Name)
      );
      foreach (var part in command.Name
        .OfType<ConfigSection>()
        .Where(p => !string.IsNullOrEmpty(p.Name)))
      {
        result.Add(" ", new XElement(DocbookSchema.arg, part.Name, new XAttribute("choice", "plain")));
      }
      foreach (var property in Property.Flatten(command.Properties, _info, _info.PosixConventions))
      {
        result.Add(" ", DocbookNames(property, true).First());
      }

      return result;
    }

    private IEnumerable<XElement> DocbookNames(IProperty property, bool synopsis)
    {
      var isPositional = _info.IsPositional(property);
      var prompt = _info.Prompt(property);
      if (isPositional)
      {
        yield return new XElement(DocbookSchema.replaceable, prompt);
      }
      else
      {
        var isRequired = _info.Use(property) == PropertyUse.Required;
        foreach (var alias in _info.Aliases(property)
          .OrderBy(n => n.Length)
          .ThenBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
          if (property.Type is BooleanType && _info.PosixConventions)
          {
            if (synopsis)
              yield return new XElement(DocbookSchema.arg, (alias.Length == 1 ? "-" : "--") + alias);
            else
              yield return new XElement(DocbookSchema.parameter, new XAttribute("class", "command"), (alias.Length == 1 ? "-" : "--") + alias);
          }
          else
          {
            var result = new XElement(synopsis 
              ? DocbookSchema.arg 
              : DocbookSchema.parameter);
            if (synopsis)
            {
              if (isRequired)
                result.Add(new XAttribute("choice", "req"));
              if (property.Type is ArrayType)
                result.Add(new XAttribute("rep", "repeat"));
            }
            else
            {
              result.Add(new XAttribute("class", "command"));
            }
            result.Add((alias.Length == 1 ? "-" : "--") + alias + " ");
            result.Add(new XElement(DocbookSchema.replaceable, prompt));
            yield return result;
          }
        }
      }
    }

    private IEnumerable<IProperty> Flatten(IEnumerable<IProperty> properties)
    {
      var propList = new List<IProperty>();
      Property.FlattenList(Array.Empty<IConfigSection>()
        , properties.Where(p => _info.Use(p) < PropertyUse.Hidden)
        , _info.PosixConventions
        , propList);
      return propList
        .OrderBy(p => _info.IsPositional(p) ? 1 : 0)
        .ThenBy(p => {
          var use = _info.Use(p);
          return use == PropertyUse.Required ? -1 : (int)use;
        })
        .ThenBy(p => _info.Order(p))
        .ThenBy(p => _info.Aliases(p).First());
    }
  }
}
