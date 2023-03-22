using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Argumental
{
  public class DocbookWriter : IHelpWriter
  {
    public XElement Write(HelpContext context)
    {
      var root = new XElement(DocbookSchema.article);
      var info = new XElement(DocbookSchema.info
        , new XElement(DocbookSchema.title, context.Metadata?.Name));
      
      if (context.Metadata?.BuildDate != null)
        info.Add(new XElement(DocbookSchema.date, context.Metadata.BuildDate));
      if (!string.IsNullOrEmpty(context.Metadata?.Version))
        info.Add(new XElement(DocbookSchema.releaseinfo, context.Metadata.Version));

      if (!string.IsNullOrEmpty(context.Metadata?.Copyright))
      {
        var copyrightText = Regex.Replace(context.Metadata.Copyright, @"^(Copyright\s+)?(\(c\)|©)?\s*", "", RegexOptions.IgnoreCase);
        var years = new List<string>();
        var holder = new XElement(DocbookSchema.holder, Regex.Replace(copyrightText, @"\b\d{4}\b", m =>
        {
          years.Add(m.Value);
          return "";
        }).Trim());
        if (years.Count > 0)
        {
          var copyright = new XElement(DocbookSchema.copyright);
          foreach (var year in years)
            copyright.Add(new XElement(DocbookSchema.year, year));
          copyright.Add(holder);
          info.Add(copyright);
        }
      }
      root.Add(info);

      foreach (var error in context.Errors)
      {
        root.Add(new XElement(DocbookSchema.important, new XElement(DocbookSchema.para, error)));
      }

      if (context.Schemas.Count == 1
          && context.Schemas[0] is ICommand command
          && string.IsNullOrEmpty(command.Name.ToString()))
      {
        root.Add(WriteCommandEntry(command, context));
      }
      else if (context.Section == HelpSection.Root)
      {
        root.Add(WriteSummaryEntry(context));
      }
      else if (context.Section == HelpSection.Command
        && context.Schemas.Count > 0)
      {
        root.Add(WriteCommandEntry(context.Schemas[0], context));
      }
      else
      {
        root.Add(WriteSummaryEntry(context));
        foreach (var schema in context.Schemas)
          root.Add(WriteCommandEntry(schema, context));
      }

      return root;
    }

    private XElement WriteSummaryEntry(HelpContext context)
    {
      var entry = new XElement(DocbookSchema.refentry);
      var nameDiv = new XElement(DocbookSchema.refnamediv
        , new XElement(DocbookSchema.refname, context.Metadata?.Name)
      );
      entry.Add(nameDiv);

      var description = context.Metadata?.Description;
      if (!string.IsNullOrEmpty(description))
        nameDiv.Add(new XElement(DocbookSchema.refpurpose, description));

      var synopsis = new XElement(DocbookSchema.cmdsynopsis
        , new XElement(DocbookSchema.command, context.Metadata?.Name)
        , " "
        , WriteArgument("command", null, false)
        , " "
        , WriteArgument("options", null, false));
      entry.Add(new XElement(DocbookSchema.refsynopsisdiv, synopsis));

      var options = context.Formats.GetProperties<CommandLineFormat>(Array.Empty<IProperty>()).ToList();
      if (options.Count > 0)
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in options)
          variableList.Add(WriteOption(option, typeof(CommandLineFormat)));
        entry.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Options")
          , variableList
        ));
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
        entry.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Commands")
          , variableList
        ));
      }

      return entry;
    }

    private XElement WriteCommandEntry(ISchemaProvider schema, HelpContext context)
    {
      var command = schema as ICommand;
      var name = (command?.Name.Last() as ConfigSection)?.Name;
      if (string.IsNullOrEmpty(name))
        name = context.Metadata?.Name;

      var entry = new XElement(DocbookSchema.refentry);
      var nameDiv = new XElement(DocbookSchema.refnamediv
        , new XElement(DocbookSchema.refname, name)
      );
      entry.Add(nameDiv);

      var description = ((schema as ICommand)?.Name.Last() as ConfigSection)?.Description
        ?? context.Metadata?.Description;
      if (!string.IsNullOrEmpty(description))
        nameDiv.Add(new XElement(DocbookSchema.refpurpose, description));

      if (command != null)
        entry.Add(new XElement(DocbookSchema.refsynopsisdiv, WriteSynopsis(command, context)));

      var options = context.Formats.GetProperties<CommandLineFormat>(schema.Properties)
        .Where(p => !p.Property.IsPositional)
        .ToList();
      if (options.Count > 0)
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in options)
          variableList.Add(WriteOption(option, typeof(CommandLineFormat)));
        entry.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Options")
          , variableList
        ));
      }

      var envVars = context.Formats.GetProperties<EnvironmentVariableFormat>(schema.Properties)
        .Where(p => p.Property.Type.IsConvertibleFromString)
        .ToList();
      if (envVars.Count > 0)
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in envVars)
          variableList.Add(WriteOption(option, typeof(EnvironmentVariableFormat)));
        entry.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Environment")
          , variableList
        ));
      }

      return entry;
    }

    private XElement WriteSynopsis(ICommand command, HelpContext context)
    {
      var result = new XElement(DocbookSchema.cmdsynopsis
        , new XElement(DocbookSchema.command, context.Metadata.Name)
      );
      foreach (var part in command.Name
        .OfType<ConfigSection>()
        .Where(p => !string.IsNullOrEmpty(p.Name)))
      {
        result.Add(" ");
        result.Add(WriteArgument(part.Name, null));
      }
      foreach (var property in context.Formats
        .GetProperties<CommandLineFormat>(command.Properties)
        .Where(p => !p.IsGlobal))
      {
        SplitAlias(property.Aliases.First(), out var name, out var prompt);
        result.Add(" ");
        result.Add(WriteArgument(name
          , prompt
          , property.Property.IsRequired()
          , property.Property.Type is ArrayType));
      }

      return result;
    }

    private void SplitAlias(string alias, out string name, out string prompt)
    {
      name = alias;
      prompt = null;
      if (name.StartsWith("<"))
      {
        prompt = name.TrimStart('<').TrimEnd('>');
        name = null;
      }
      else if (name.TryFindIndex(" <", out var idx))
      {
        prompt = name.Substring(idx + 1).TrimStart('<').TrimEnd('>');
        name = name.Substring(0, idx);
      }
    }

    private XElement WriteArgument(string optionName, string prompt, bool? required = null, bool repeat = false)
    {
      var result = new XElement(DocbookSchema.arg);
      if (required == true)
        result.Add(new XAttribute("choice", "req"));
      else if (!required.HasValue)
        result.Add(new XAttribute("choice", "plain"));
      if (repeat)
        result.Add(new XAttribute("rep", "repeat"));

      if (!string.IsNullOrEmpty(optionName))
        result.Add(optionName + (string.IsNullOrEmpty(prompt) ? "" : " "));

      if (!string.IsNullOrEmpty(prompt))
        result.Add(new XElement(DocbookSchema.replaceable, prompt));

      return result;
    }

    private XElement WriteOption(ConfigProperty property, Type formatType)
    {
      var result = new XElement(DocbookSchema.varlistentry);
      foreach (var alias in property.Aliases)
      {
        var term = new XElement(DocbookSchema.term);
        if (formatType == typeof(CommandLineFormat))
        {
          SplitAlias(alias, out var name, out var prompt);
          var param = new XElement(DocbookSchema.parameter, new XAttribute("class", "command"));
          if (!string.IsNullOrEmpty(name))
            param.Add(name + (string.IsNullOrEmpty(prompt) ? "" : " "));
          if (!string.IsNullOrEmpty(prompt))
            param.Add(new XElement(DocbookSchema.replaceable, prompt));
          term.Add(param);
        }
        else if (formatType == typeof(EnvironmentVariableFormat))
        {
          term.Add(new XElement(DocbookSchema.envar, alias));
        }
        result.Add(term);
      }
      
      var para = new XElement(DocbookSchema.para);
      var description = property.Property.Name.OfType<ConfigSection>().LastOrDefault()?.Description;
      if (!string.IsNullOrEmpty(description))
        para.Add(description);
      if (property.Property.DefaultValue != null)
      {
        para.Add(" [default: ");
        para.Add(new XElement(DocbookSchema.literal, property.Property.DefaultValue.ToString()));
        para.Add("]");
      }
      result.Add(new XElement(DocbookSchema.listitem, para));

      return result;
    }

    public void Write(HelpContext context, TextWriter writer)
    {
      using (var xml = XmlWriter.Create(writer, new XmlWriterSettings()
      {
        OmitXmlDeclaration = false,
        Indent = true,
        IndentChars = "  "
      }))
        Write(context).WriteTo(xml);
    }
  }
}
