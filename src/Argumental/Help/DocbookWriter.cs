using Argumental.Help;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Argumental
{
  /// <summary>
  /// Writes documentation in the <see href="https://tdg.docbook.org/tdg/5.2/"/>docbook</see> format.
  /// </summary>
  /// <remarks>
  /// The documentation is structured to follow the order and contents of a 
  /// <see href="https://man7.org/linux/man-pages/man7/man-pages.7.html"/>standard manpage</see>
  /// </remarks>
  public class DocbookWriter : IHelpWriter
  {
    /// <inheritdoc />
    public string Format => "docbook";

    public Dictionary<int, string> ExitCodeMessages { get; } = typeof(ExitCode)
      .GetFields(BindingFlags.Public | BindingFlags.Static)
      .ToDictionary(f => (int)f.GetRawConstantValue()
      , f => f.GetCustomAttribute<DescriptionAttribute>().Description);

    /// <inheritdoc />
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
      else if (context.Section == DocumentationScope.Root)
      {
        root.Add(WriteSummaryEntry(context));
      }
      else if (context.Section == DocumentationScope.Command
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
        , new XElement(DocbookSchema.arg, "command")
        , " "
        , new XElement(DocbookSchema.arg, "options"));
      entry.Add(new XElement(DocbookSchema.refsynopsisdiv, synopsis));

      var commandMetadata = context.ConfigFormats.GetSerializationInfo<CommandLineInfo>();
      if (commandMetadata?.GlobalOptions().Any() == true)
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in commandMetadata.GlobalOptions())
          variableList.Add(WriteOption(commandMetadata, option));
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

      var result = new XElement(DocbookSchema.refentry);
      var nameDiv = new XElement(DocbookSchema.refnamediv
        , new XElement(DocbookSchema.refname, name)
      );
      result.Add(nameDiv);

      var description = ((schema as ICommand)?.Name.Last() as ConfigSection)?.Description
        ?? context.Metadata?.Description;
      if (!string.IsNullOrEmpty(description))
        nameDiv.Add(new XElement(DocbookSchema.refpurpose, description));

      var commandMetadata = context.ConfigFormats.GetSerializationInfo<CommandLineInfo>();
      if (commandMetadata != null && command != null)
        result.Add(new XElement(DocbookSchema.refsynopsisdiv, WriteSynopsis(command, context)));

      var options = commandMetadata.Flatten(schema.Properties)
        .Concat(commandMetadata.GlobalOptions())
        .ToList();
      if (options.Count > 0)
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in options)
          variableList.Add(WriteOption(commandMetadata, option));
        result.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Options")
          , variableList
        ));
      }

      if (context.App?.ExitCodes.Any() == true)
      {
        var variableList = new XElement(DocbookSchema.variablelist
          , new XElement(DocbookSchema.varlistentry
            , new XElement(DocbookSchema.term
              , new XElement(DocbookSchema.returnvalue, "0")
            ),
            new XElement(DocbookSchema.listitem, 
              new XElement(DocbookSchema.para, ExitCodeMessages[0]))
          )
        );
        foreach (var exitCode in context.App?.ExitCodes)
        {
          var para = new XElement(DocbookSchema.para);
          if (ExitCodeMessages.TryGetValue(exitCode.Key, out var message))
          {
            para.Add(message);
          }
          else
          {
            var first = true;
            foreach (var type in exitCode)
            {
              if (first)
                first = false;
              else
                para.Add(", ");
              para.Add(new XElement(DocbookSchema.errorname, type.Name));
            }
          }
          variableList.Add(new XElement(DocbookSchema.varlistentry
            , new XElement(DocbookSchema.term
              , new XElement(DocbookSchema.returnvalue, exitCode.Key.ToString())
            ),
            new XElement(DocbookSchema.listitem, para)
          ));
        }
        result.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Exit Status")
          , variableList
        ));
      }

      var envVarMetadata = context.ConfigFormats.GetSerializationInfo<EnvironmentVariableInfo>();
      var envVars = envVarMetadata?.Flatten(schema.Properties).ToList();
      if (envVars?.Count > 0)
      {
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in envVars)
          variableList.Add(WriteOption(envVarMetadata, option));
        result.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Environment")
          , variableList
        ));
      }

      var jsonFiles = context.ConfigFormats.GetSerializationInfo<JsonSettingsInfo>();
      if (jsonFiles?.Paths.Count > 0)
      {
        var jsonVars = jsonFiles.Flatten(schema.Properties)
          .Where(p => p.Type.IsConvertibleFromString)
          .ToList();
        var para = new XElement(DocbookSchema.para, "JSON: ");
        var first = true;
        foreach (var file in jsonFiles.Paths
          .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
          if (first)
            first = false;
          else
            para.Add(", ");
          para.Add(new XElement(DocbookSchema.filename, file));
        }
        var variableList = new XElement(DocbookSchema.variablelist);
        foreach (var option in jsonVars)
          variableList.Add(WriteOption(jsonFiles, option));
        result.Add(new XElement(DocbookSchema.refsection
          , new XElement(DocbookSchema.title, "Files")
          , para
          , variableList
        ));
      }

      return result;
    }

    private XElement WriteSynopsis(ICommand command, HelpContext context)
    {
      var commandMetadata = context.ConfigFormats.GetSerializationInfo<CommandLineInfo>();
      var result = new XElement(DocbookSchema.cmdsynopsis
        , new XElement(DocbookSchema.command, context.Metadata.Name)
      );
      foreach (var part in command.Name
        .OfType<ConfigSection>()
        .Where(p => !string.IsNullOrEmpty(p.Name)))
      {
        result.Add(" ", new XElement(DocbookSchema.arg, part.Name, new XAttribute("choice", "plain")));
      }
      foreach (var property in commandMetadata.Flatten(command.Properties))
      {
        result.Add(" ", commandMetadata.DocbookNames(property).First());
      }

      return result;
    }

    private XElement WriteOption(SerializationInfo metadata, IProperty property)
    {
      var result = new XElement(DocbookSchema.varlistentry);
      foreach (var alias in metadata.DocbookNames(property))
      {
        var term = new XElement(DocbookSchema.term);
        if (alias.Name == DocbookSchema.arg)
          term.Add(new XElement(DocbookSchema.parameter
            , new XAttribute("class", "command")
            , alias.Nodes()
          ));
        else
          term.Add(alias);
        result.Add(term);
      }
      
      var para = new XElement(DocbookSchema.para);
      var description = metadata.Description(property);
      if (!string.IsNullOrEmpty(description))
        para.Add(description);
      var defaultValue = metadata.DefaultValue(property);
      if (defaultValue != null)
      {
        para.Add(" [default: ");
        para.Add(new XElement(DocbookSchema.literal, defaultValue.ToString()));
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
