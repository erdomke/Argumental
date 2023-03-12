using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Argumental.Help
{
  public abstract class ManPageWriter : ConfigSchemaWriter
  {
    public override void WriteHelp(AssemblyMetadata metadata, IEnumerable<ISchemaProvider> schemas, IEnumerable<string> errors)
    {
      var schemaList = schemas.ToList();
      var commandlineFormat = _repository.Formats.OfType<CommandLineFormat>().FirstOrDefault();
      if (schemaList.Count == 1 
        && schemaList[0] is ICommand command
        && string.IsNullOrEmpty(command.Name.ToString()))
      {
        WriteCommand(metadata
          , command
          , commandlineFormat?.HelpAliases ?? Enumerable.Empty<string>()
          , commandlineFormat?.VersionAliases ?? Enumerable.Empty<string>()
          , metadata.Description);
      }
      else
      {
        if (!string.IsNullOrEmpty(metadata.Description))
          WriteDescription(metadata.Description);

        WriteStartSection(SchemaSection.Usage);
        WriteStartSection(SchemaSection.Synopsis);
        WriteApplication(metadata.Name);
        WriteArgument("command", null, false);
        WriteArgument("options", null, false);
        WriteEndSection(SchemaSection.Synopsis);
        WriteEndSection(SchemaSection.Usage);

        if (commandlineFormat?.HelpAliases.Any() == true
          || commandlineFormat?.VersionAliases.Any() == true)
        {
          WriteStartSection(SchemaSection.Options);
          WriteCommonOptions(commandlineFormat?.HelpAliases, commandlineFormat?.VersionAliases);
          WriteEndSection(SchemaSection.Options);
        }

        if (schemaList.OfType<ICommand>().Any())
        {
          WriteStartSection(SchemaSection.Commands);
          foreach (var cmd in schemaList.OfType<ICommand>())
          {
            var aliases = new[] { cmd.Name }
              .Select(name =>
              {
                var alias = new ConfigAlias(ConfigAliasType.Argument);
                var first = true;
                foreach (var part in name)
                {
                  if (first)
                    first = false;
                  else
                    alias.Add(new ConfigAliasPart(" ", ConfigAliasType.Other));
                  alias.Add(new ConfigAliasPart(part.ToString(), ConfigAliasType.Argument));
                }
                return alias;
              });
            WriteOption(aliases, (cmd.Name.Last() as ConfigSection)?.Description, null);
          }
          WriteEndSection(SchemaSection.Commands);
        }
      }
    }

    private IEnumerable<IProperty> Options(IEnumerable<IProperty> properties)
    {
      return properties
        .Where(p => p.Path.All(s => s is ConfigSection) && !p.Hidden)
        .Select((p, i) => new { Property = p, Index = i })
        .OrderBy(p => p.Property.IsPositional
          ? p.Index
          : (p.Property.IsRequired() ? -2 : -1))
        .Select(p => p.Property);
    }

    public virtual void WriteCommand(AssemblyMetadata metadata, ICommand command
      , IEnumerable<string> helpAliases
      , IEnumerable<string> versionAliases
      , string description = null)
    {
      if (description == null)
      {
        var name = command.Name.Last() as ConfigSection;
        if (!string.IsNullOrEmpty(name?.Description))
          description = name.Description;
      }

      if (description != null)
        WriteDescription(description);

      WriteStartSection(SchemaSection.Usage);
      WriteCommandSynopsis(metadata.Name, command.Name, command.Properties);
      WriteEndSection(SchemaSection.Usage);

      WriteStartSection(SchemaSection.Options);
      foreach (var property in Options(command.Properties).Where(p => !p.IsPositional))
        WriteOption(property);
      WriteCommonOptions(helpAliases, versionAliases);
      WriteEndSection(SchemaSection.Options);
    }

    private void WriteCommonOptions(IEnumerable<string> helpAliases
      , IEnumerable<string> versionAliases)
    {
      if (versionAliases?.Any() == true)
        WriteOption(versionAliases.Select(a => new ConfigAlias(ConfigAliasType.Argument, a)), "Show version information", null);
      if (helpAliases?.Any() == true)
        WriteOption(helpAliases.Select(a => new ConfigAlias(ConfigAliasType.Argument, a)), "Show help and usage information", null);
    }

    public virtual void WriteCommandSynopsis(string application, ConfigPath command, IEnumerable<IProperty> properties)
    {
      WriteStartSection(SchemaSection.Synopsis);
      WriteApplication(application);
      foreach (var part in command
        .OfType<ConfigSection>()
        .Where(p => !string.IsNullOrEmpty(p.Name)))
        WriteArgument(part.Name, null);
      var posixConventions = _repository.Formats.OfType<CommandLineFormat>().FirstOrDefault()?.PosixConventions == true;
      foreach (var property in Options(properties))
      {
        var prompt = property.Path.Last().ToString().Replace(' ', '-');
        if (posixConventions && property.Type is BooleanType)
          prompt = null;
        WriteArgument(property.IsPositional ? null : "--" + property.Path.ToString()
          , prompt
          , property.IsRequired()
          , property.Type is ArrayType);
      }
      WriteEndSection(SchemaSection.Synopsis);
    }

    public abstract void WriteDescription(string description);

    public abstract void WriteApplication(string name);

    public abstract void WriteArgument(string optionName, string prompt, bool? required = null, bool repeat = false);

    public virtual void WriteOption(IProperty property)
    {
      WriteOption(_repository.GetAliases(property), (property.Path.Last() as ConfigSection)?.Description, property.DefaultValue);
    }

    public abstract void WriteOption(IEnumerable<ConfigAlias> aliases, string description, object defaultValue);

    public abstract void WriteStartSection(SchemaSection section);

    public abstract void WriteEndSection(SchemaSection section);
  }
}
